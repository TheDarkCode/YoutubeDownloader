using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Utility.CommandLine;
using VideoLibrary;

namespace YoutubeDownloader
{
    class Program
    {
        [Argument('b', "basedir")]
        private static string baseDir { get; set; }

        [Argument('f', "ffmpeg")]
        private static string ffmpegPath { get; set; }

        [Argument('v', "videodir")]
        private static string videoDir { get; set; }

        [Argument('a', "audiodir")]
        private static string audioDir { get; set; }

        [Argument('l', "links")]
        private static string links { get; set; }

        [Argument('a', "array")]
        private static string[] array { get; set; }

        [Argument('p', "parallelization")]
        private static int parallelization { get; set; }

        static void Main(string[] args)
        {            
            Console.Title = "Youtube Downloader";

            Console.WriteLine(Directory.GetCurrentDirectory());

            Console.WriteLine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            string BASE_DIR = string.IsNullOrWhiteSpace(baseDir) ? @"C:\YoutubeDownloader\" : baseDir;
            string FFMPEG_PATH = string.IsNullOrWhiteSpace(ffmpegPath) ? @"C:\YoutubeDownloader\ffmpeg.exe" : ffmpegPath;

            checkPathForDirectory(BASE_DIR);

            string VIDEO_DIR = string.IsNullOrWhiteSpace(videoDir) ? (BASE_DIR + @"Videos\") : videoDir;
            string AUDIO_DIR = string.IsNullOrWhiteSpace(audioDir) ? (BASE_DIR + @"Audio\") : audioDir;

            checkPathForDirectory(VIDEO_DIR);
            checkPathForDirectory(AUDIO_DIR);

            // https://youtube.com/watch?v=....
            string[] ytUrls;
                
            ytUrls = (array == null || array.Length == 0) ? File.ReadAllLines(string.IsNullOrWhiteSpace(links) ? BASE_DIR + "urls.txt" : links) : array;

            Console.WriteLine("Count of urls to download: {0}", ytUrls.Length);

            YouTube youtube = YouTube.Default;

            Parallel.ForEach(ytUrls, new ParallelOptions { MaxDegreeOfParallelism = (parallelization == 0) ? 8 : parallelization }, url =>
            {
                ProcessYouTubeVideo(youtube, url, VIDEO_DIR, AUDIO_DIR, FFMPEG_PATH);
            });

            Console.WriteLine("Completed...");

            Console.ReadLine();
        }

        private static void checkPathForDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void ProcessYouTubeVideo(YouTube youtube, string url, string videoDir, string audioDir, string ffmpegPath)
        {
            Console.WriteLine("Processing URL: " + url);
            var video = youtube.GetVideo(url);
            string videoPath = videoDir + video.FullName;
            string audioPath = audioDir + video.Title + ".mp3";

            // Download Video
            if (!File.Exists(videoPath))
            {

                Console.WriteLine("Downloading video to: " + videoPath);
                File.WriteAllBytes(videoPath, video.GetBytes());
                Console.WriteLine("Downloaded video successful");
            }
            else
            {
                Console.WriteLine("Video already downloaded.");
            }

            // Process Audio Extraction
            if (!File.Exists(audioPath))
            {
                // FFMPEG doesn't like some file names...
                audioPath = audioDir + ValidateFileName(video.Title) + ".mp3";
                if (!File.Exists(audioPath))
                {
                    RunAudioExtraction(ffmpegPath, videoPath, audioPath);
                }
            }
            else
            {
                Console.WriteLine("Audio already processed for: {0}", audioPath);
            }
        }

        private static void RunAudioExtraction(string ffmpeg, string input, string output)
        {
            if (!File.Exists(ffmpeg))
            {
                Console.WriteLine("FFMPEG not found. Skipping audio extract.");
                return;
            }

            Process process = new Process();

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName = ffmpeg;
            process.StartInfo.Arguments = ffmpegSimpleFileArguments(input, output);
            process.Start();

            string outputText = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            Console.WriteLine("ffmpeg: " + outputText);
        }

        private static string ValidateFileName(string name)
        {
            string invalidChars = Regex.Escape(
                                        new string(Path.GetInvalidFileNameChars()) + ":"
                                    );
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return Regex.Replace(name, invalidRegStr, "_");
        }

        private static string ffmpegSimpleFileArguments(string input, string output)
        {
            return string.Format("-i file:\"{0}\" -y file:\"{1}\"", input, output);
        }
    }
}
