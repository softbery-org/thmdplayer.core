// Version: 1.0.0.676
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using ThmdPlayer.Core.Interfaces;

namespace ThmdPlayer.Core.medias
{
    public class FileMediaStream : IMediaStream
    {
        private readonly string _filePath;
        private Stream _stream;

        private FileStream _fileStream;

        public FileMediaStream(string path)
        {
            _fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _filePath = path;
        }

        public async Task<string> DownloadM3U8ContentAsync(string url)
        {
            using var httpClient = new HttpClient();
            return await httpClient.GetStringAsync(url);
        }

        public Task<Stream> GetStream()
        {
            return Task.FromResult<Stream>(File.OpenRead(_filePath));
        }

        public async Task<Stream> GetStreamAsync()
        {
            var stream = File.OpenRead(_filePath);
            var buffer = new byte[stream.Length];
            await stream.ReadAsync(buffer, 0, buffer.Length);
            _stream = new MemoryStream(buffer);

            return stream;
        }

        public double GetDuration()
        {
            return 120;
        }

        public void Dispose()
        {
            _fileStream?.Dispose();
        }
    }
}
