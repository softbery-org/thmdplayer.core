// Version: 0.1.0.124
using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ThmdPlayer.Core.medias
{
    public class MediaEditor
    {
        private Uri _videoUri;

        public MediaEditor(string video_path) 
        { 
            _videoUri = new Uri(video_path);
        }

        public MediaEditor(Uri video_uri)
        {
            _videoUri = video_uri;
        }

        /// <summary>
        /// Gets the thumbnail of the video at the specified URI.
        /// </summary>
        /// <returns>Thumbnail image</returns>
        public Image GetThumbnail()
        {
            var inputFile = new MediaFile { Filename = @$"{_videoUri.LocalPath}" };
            var outputFile = new MediaFile { Filename = Path.GetTempFileName() };
            using (var engine = new Engine())
            {
                try
                {
                    engine.GetMetadata(inputFile);
                    engine.GetThumbnail(inputFile, outputFile, new ConversionOptions { Seek = TimeSpan.FromSeconds(1) });
                    var image = new Image();
                    image.Source = new BitmapImage(new Uri(outputFile.Filename));
                    return image;
                }
                catch (Exception ex)
                {
                    Logger.Log.Log(Core.Logs.LogLevel.Error, new string[] { "File", "Console" }, $"Error getting thumbnail: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Cuts the video from startTime to endTime and saves it to outputPath.
        /// </summary>
        /// <param name="outputPath">Out file</param>
        /// <param name="startTime">Start cut time</param>
        /// <param name="endTime">End cut time</param>
        /// <returns></returns>
        public bool CutVideo(string outputPath, TimeSpan startTime, TimeSpan endTime)
        {
            Logger.Log.Log(Core.Logs.LogLevel.Info, new string[] { "File", "Console" }, $"Cutting video from {_videoUri.LocalPath} to {outputPath} from {startTime} to {endTime}");
            // Implementacja cięcia wideo
            var inputFile = new MediaFile { Filename = @$"{_videoUri.LocalPath}" };
            var outputFile = new MediaFile { Filename = @$"{outputPath}" };

            Logger.Log.Log(Core.Logs.LogLevel.Info, new string[] { "File", "Console" }, $"Run MediaToolkit engine for cutting.");
            using (var engine = new Engine())
            {
                try
                {
                    engine.GetMetadata(inputFile);
                    var options = new ConversionOptions();
                    options.CutMedia(startTime, endTime);
                    engine.Convert(inputFile, outputFile, options);

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log.Log(Core.Logs.LogLevel.Error, new string[] { "File", "Console" }, $"Error cutting video: {ex.Message}");
                    return false;
                }
            }
        }
    }
}
