// Version: 1.0.0.665
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using ThmdPlayer.Core.Logs;

namespace ThmdPlayer.Core.Updates
{
    public class Updater : IDisposable
    {
        private readonly HttpClient _httpClient;
        private bool _disposed = false;
        private AsyncLogger _logger = new AsyncLogger();

        public Version CurrentVersion { get; private set; }
        public Version LatestVersion { get; private set; }
        public string UpdateManifestUrl { get; }
        public string TempFilePath { get; private set; }

        public event EventHandler<UpdateAvailableEventArgs> UpdateAvailable;
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        public event EventHandler UpdateCompleted;
        public event EventHandler<Exception> UpdateFailed;

        public Updater(string updateManifestUrl)
        {
            _httpClient = new HttpClient();
            UpdateManifestUrl = updateManifestUrl;
            CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }

        public async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(UpdateManifestUrl);
                LatestVersion = ParseVersionFromManifest(response);

                if (LatestVersion > CurrentVersion)
                {
                    UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs(LatestVersion));
                    _logger.Log(LogLevel.Info, new[] { "Console", "File" }, $"New version available: {LatestVersion}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                UpdateFailed?.Invoke(this, ex);
                _logger.Log(LogLevel.Error, new[] { "Console", "File" }, $"Error checking for updates: {ex.Message}");
                return false;
            }
        }

        public async Task DownloadUpdateAsync(string downloadUrl)
        {
            try
            {
                if (!Directory.Exists("update"))
                {
                    try
                    {
                        Directory.CreateDirectory("update");
                        _logger.Log(LogLevel.Error, new[] { "Console", "File" }, $"Directory created: update");
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, new[] { "Console", "File" }, $"Error creating update directory: {ex.Message}");
                    }
                }

                TempFilePath = Path.Combine(Path.GetFullPath("update/update")); //Path.GetTempFileName();

                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                using (var streamToRead = await response.Content.ReadAsStreamAsync())
                using (var streamToWrite = File.OpenWrite(TempFilePath))
                {
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var buffer = new byte[8192];
                    var totalBytesRead = 0L;
                    var bytesRead = 0;

                    while ((bytesRead = await streamToRead.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await streamToWrite.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                        ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(totalBytesRead, totalBytes));
                    }
                }
                UpdateCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                UpdateFailed?.Invoke(this, ex);
                _logger.Log(LogLevel.Error, new[] { "Console", "File"}, $"Error downloading update: {ex.Message}");
            }
        }

        public void ApplyUpdate()
        {
            if (!File.Exists(TempFilePath))
            {
                _logger.Log(LogLevel.Error, new[] { "Console", "File" }, "Update package not found", new FileNotFoundException("Update package not found"));
            }

            var startInfo = new ProcessStartInfo(TempFilePath)
            {
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(startInfo);
            Environment.Exit(0);
        }

        private Version ParseVersionFromManifest(string manifestContent)
        {
            // Implement custom parsing logic according to your manifest format
            return Version.Parse(manifestContent.Trim());
        }
    }

    public class UpdateAvailableEventArgs : EventArgs
    {
        public Version NewVersion { get; }

        public UpdateAvailableEventArgs(Version newVersion)
        {
            NewVersion = newVersion;
        }
    }

    public class ProgressChangedEventArgs : EventArgs
    {
        public long BytesReceived { get; }
        public long TotalBytes { get; }
        public int ProgressPercentage => (int)((BytesReceived * 100) / (TotalBytes > 0 ? TotalBytes : 1));

        public ProgressChangedEventArgs(long bytesReceived, long totalBytes)
        {
            BytesReceived = bytesReceived;
            TotalBytes = totalBytes;
        }
    }
}
