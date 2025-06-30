// Version: 1.0.0.676
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.medias
{
    public class FFmpegScreenRecorder
    {
        private Process _ffmpegProcess;

        public void StartRecording()
        {
            try
            {
                string outputPath = @"C:\test.mp4";
                string ffmpegArgs = $"-f gdigrab -framerate 20 -i desktop -c:v libx264 -preset ultrafast -crf 23 {outputPath}";

                _ffmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg/bin/ffmpeg.exe",
                        Arguments = ffmpegArgs,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    }
                };

                _ffmpegProcess.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Błąd: {e.Message}");
            }
        }

        public void StopRecording()
        {
            _ffmpegProcess?.Kill();
        }
    }
}
