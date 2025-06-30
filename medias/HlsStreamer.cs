// Version: 1.0.0.665
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.medias
{
    public static class HlsPlaylistParser
    {
        public static List<HlsSegment> ParsePlaylist(string m3u8Content, Uri baseUri)
        {
            var segments = new List<HlsSegment>();
            var lines = m3u8Content.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            HlsSegment currentSegment = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("#EXTINF:"))
                {
                    currentSegment = new HlsSegment();
                    var durationPart = trimmedLine.Split(':')[1].Split(',')[0];
                    currentSegment.Duration = TimeSpan.FromMilliseconds(double.Parse(durationPart));
                }
                else if (trimmedLine.StartsWith("#EXT-X-DISCONTINUITY"))
                {
                    if (currentSegment != null)
                    {
                        currentSegment.IsDiscontinuity = true;
                    }
                }
                else if (trimmedLine.StartsWith("#"))
                {
                    // Handle other tags (EXT-X-VERSION, EXT-X-KEY etc.)
                    if (currentSegment != null)
                    {
                        var parts = trimmedLine.Split(new[] { ':' }, 2);
                        if (parts.Length > 1)
                        {
                            currentSegment.Tags[parts[0]] = parts[1];
                        }
                    }
                }
                else if (!trimmedLine.StartsWith("#") && currentSegment != null)
                {
                    currentSegment.Uri = new Uri(baseUri, trimmedLine).AbsoluteUri;
                    segments.Add(currentSegment);
                    currentSegment = null;
                }
            }

            return segments;
        }
    }

    public class HLSStreamer : IDisposable
    {
        private readonly HttpClient _httpClient;
        private CancellationTokenSource _cts;
        private List<HlsSegment> _segments;
        private Uri _playlistUri;

        public event Action<byte[]> SegmentDownloaded;
        public event Action<List<HlsSegment>> PlaylistParsed;
        public event Action<string> ErrorOccurred;
        public event Action StreamEnded;

        public HLSStreamer()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "HLS-Streamer/1.0");
        }

        public async Task StartStreamingAsync(string m3u8Url, CancellationToken cancellationToken = default)
        {
            try
            {
                _playlistUri = new Uri(m3u8Url);
                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                while (!_cts.IsCancellationRequested)
                {
                    var playlistContent = await DownloadPlaylistAsync(_playlistUri);
                    _segments = HlsPlaylistParser.ParsePlaylist(playlistContent, _playlistUri);
                    PlaylistParsed?.Invoke(_segments);

                    foreach (var segment in _segments)
                    {
                        if (_cts.IsCancellationRequested) break;

                        try
                        {
                            //var segmentData = await _httpClient.GetByteArrayAsync(segment.Uri, _cts.Token);
                            var segmentData = await _httpClient.GetByteArrayAsync(_playlistUri);
                            SegmentDownloaded?.Invoke(segmentData);
                        }
                        catch (Exception ex)
                        {
                            ErrorOccurred?.Invoke($"Error downloading segment: {ex.Message}");
                        }
                    }

                    if (_cts.IsCancellationRequested) break;

                    // Dla strumieni na żywo: dodać logikę odświeżania playlisty
                    await Task.Delay(TimeSpan.FromSeconds(_segments.FirstOrDefault()?.Duration.TotalSeconds ?? 10), _cts.Token);
                }

                StreamEnded?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Streaming failed: {ex.Message}");
            }
        }

        public void StopStreaming()
        {
            _cts?.Cancel();
        }

        private async Task<string> DownloadPlaylistAsync(Uri uri)
        {
            try
            {
                return await _httpClient.GetStringAsync(uri);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Playlist download error: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _httpClient?.Dispose();
        }
    }
}
