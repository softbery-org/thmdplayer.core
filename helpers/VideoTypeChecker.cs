// Version: 1.0.0.665
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThmdPlayer.Core.medias;

namespace ThmdPlayer.Core.helpers
{
    public class VideoTypeChecker
    {
        public bool IsAvi { get; }
        public bool IsMp4 { get; }
        public bool IsM3u8 { get; }
        private readonly string _filePath;

        public VideoTypeChecker(string filePath)
        {
            _filePath = filePath;
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            IsAvi = extension == ".avi";
            IsMp4 = extension == ".mp4";
            IsM3u8 = extension == ".m3u8";
        }

        public void DownloadM3u8(string outputPath)
        {
            ValidateM3u8();
            Console.WriteLine($"Downloading M3U8 stream from {_filePath} to {outputPath}");
            // Tutaj dodaj implementację pobierania
        }

        public async Task<string[]> ParseM3u8Async()
        {
            ValidateM3u8();
            Console.WriteLine($"Parsing M3U8 playlist from {_filePath}");
            var s = new List<string>();
            var streamer = new HLSStreamer();

            streamer.PlaylistParsed += segments =>
            {
                Console.WriteLine($"Loaded playlist with {segments.Count} segments");
                foreach (var item in segments)
                {
                    Console.WriteLine(item.Title, item.Duration);
                }
            };

            streamer.ErrorOccurred += error =>
                Console.WriteLine($"Error: {error}");

            streamer.StreamEnded += () =>
                Console.WriteLine("Stream ended");

            var cancellationTokenSource = new CancellationTokenSource();

            // Start streaming w tle
            _ = streamer.StartStreamingAsync(_filePath, cancellationTokenSource.Token);
            // Zatrzymaj po 30 sekundach
            await Task.Delay(30000);
            streamer.StopStreaming();
            streamer.Dispose();
            return Array.Empty<string>();
        }

        private void Stream_StreamEnded()
        {
            throw new NotImplementedException();
        }

        public Stream GetStream()
        {
            ValidateM3u8();
            Console.WriteLine($"Getting stream from {_filePath}");
            // Tutaj dodaj implementację strumieniowania
            return Stream.Null;
        }

        private void ValidateM3u8()
        {
            if (!IsM3u8)
            {
                throw new InvalidOperationException("Operation available only for M3U8 streams");
            }
        }

        public override string ToString()
        {
            return $"Video type: {(IsAvi ? "AVI" : IsMp4 ? "MP4" : IsM3u8 ? "M3U8" : "Unknown")}";
        }
    }
}
