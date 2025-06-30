// Version: 0.1.0.34
using NAudio.Wave;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ThmdPlayer.Core.controls
{
    public partial class AudioSpectrumControl : UserControl
    {
        private WaveInEvent waveIn;
        private IWaveProvider waveProvider;
        private WaveOutEvent waveOut;
        private BufferedWaveProvider bufferedWaveProvider;
        private float[] fftBuffer;
        private const int FFT_LENGTH = 1024;
        private const int SAMPLE_RATE = 44100;
        private const int BUFFER_SIZE = 65536; // Buffer size to prevent overflow
        private SampleAggregator sampleAggregator;

        public AudioSpectrumControl()
        {
            InitializeComponent();
            InitializeAudio();

            // Timer for visualization refresh
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(33); // ~30 FPS
            timer.Tick += (s, e) => spectrumCanvas.InvalidateVisual();
            timer.Start();
        }

        private void InitializeAudio()
        {
            // Initialize playback
            waveOut = new WaveOutEvent();
            bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(SAMPLE_RATE, 16, 2))
            {
                BufferLength = BUFFER_SIZE,
                DiscardOnBufferOverflow = true // Prevent buffer overflow
            };
            waveOut.Init(bufferedWaveProvider);

            // Initialize input
            waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(SAMPLE_RATE, 16, 2);
            waveIn.DataAvailable += WaveIn_DataAvailable;

            // Initialize FFT processing
            fftBuffer = new float[FFT_LENGTH];
            sampleAggregator = new SampleAggregator(FFT_LENGTH);
            sampleAggregator.FftCalculated += SampleAggregator_FftCalculated;
            sampleAggregator.PerformFFT = true;
        }

        private void SampleAggregator_FftCalculated(object sender, FftEventArgs e)
        {
            // Copy FFT results to buffer for visualization
            for (int i = 0; i < FFT_LENGTH / 2; i++)
            {
                float real = e.Result[i].X;
                float imag = e.Result[i].Y;
                fftBuffer[i] = (float)Math.Sqrt(real * real + imag * imag);
            }
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            // Check buffer space
            if (bufferedWaveProvider.BufferedBytes + e.BytesRecorded <= bufferedWaveProvider.BufferLength)
            {
                bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
            }

            // Process samples for FFT
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = BitConverter.ToInt16(e.Buffer, i);
                sampleAggregator.Add(sample / 32768.0f);
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            // Draw spectrum on Canvas
            dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, spectrumCanvas.ActualWidth, spectrumCanvas.ActualHeight));

            double width = spectrumCanvas.ActualWidth / (FFT_LENGTH / 2);
            for (int i = 0; i < FFT_LENGTH / 2; i++)
            {
                double height = Math.Min(fftBuffer[i] * spectrumCanvas.ActualHeight * 0.5, spectrumCanvas.ActualHeight);
                dc.DrawRectangle(Brushes.Green, null, new Rect(i * width, spectrumCanvas.ActualHeight - height, width, height));
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Audio files (*.wav, *.mp3)|*.wav;*.mp3|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Clean up previous resources
                    StopAndDispose();

                    // Determine file type and initialize appropriate reader
                    string extension = Path.GetExtension(openFileDialog.FileName).ToLower();
                    if (extension == ".mp3")
                    {
                        waveProvider = new Mp3FileReader(openFileDialog.FileName);
                    }
                    else if (extension == ".wav")
                    {
                        waveProvider = new WaveFileReader(openFileDialog.FileName);
                    }
                    else
                    {
                        MessageBox.Show("Unsupported file format. Please select a WAV or MP3 file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Convert to target format (44.1 kHz, 16-bit, stereo)
                    var targetFormat = new WaveFormat(SAMPLE_RATE, 16, 2);
                    waveProvider = WaveFormatConversionStream.CreatePcmStream((WaveStream)waveProvider);
                    waveProvider = new BlockAlignReductionStream((WaveStream)waveProvider);

                    // Read and buffer audio
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    while ((bytesRead = waveProvider.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (bufferedWaveProvider.BufferedBytes + bytesRead <= bufferedWaveProvider.BufferLength)
                        {
                            bufferedWaveProvider.AddSamples(buffer, 0, bytesRead);
                        }
                    }

                    waveOut.Play();
                    waveIn.StartRecording();
                    pauseButton.IsEnabled = true;
                    resumeButton.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading audio file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (waveOut != null && waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Pause();
                waveIn.StopRecording();
                pauseButton.IsEnabled = false;
                resumeButton.IsEnabled = true;
            }
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (waveOut != null && waveOut.PlaybackState == PlaybackState.Paused)
            {
                waveOut.Play();
                waveIn.StartRecording();
                pauseButton.IsEnabled = true;
                resumeButton.IsEnabled = false;
            }
        }

        private void StopAndDispose()
        {
            waveIn?.StopRecording();
            waveOut?.Stop();
            waveProvider = null;
            bufferedWaveProvider.ClearBuffer();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StopAndDispose();
            waveIn?.Dispose();
            waveOut?.Dispose();
        }
    }

    // SampleAggregator class for FFT processing (from NAudio examples)
    public class SampleAggregator
    {
        private readonly float[] fftBuffer;
        private readonly Complex[] fftComplex;
        private int fftPos;
        private readonly int fftLength;
        public bool PerformFFT { get; set; }
        public event EventHandler<FftEventArgs> FftCalculated;

        public SampleAggregator(int fftLength)
        {
            this.fftLength = fftLength;
            fftBuffer = new float[fftLength];
            fftComplex = new Complex[fftLength];
        }

        public void Add(float value)
        {
            if (!PerformFFT || FftCalculated == null) return;

            fftBuffer[fftPos] = value;
            fftComplex[fftPos] = new Complex(value, 0);
            fftPos++;

            if (fftPos >= fftLength)
            {
                fftPos = 0;
                FastFourierTransform.FFT(true, (int)Math.Log(fftLength, 2.0), fftComplex);
                FftCalculated?.Invoke(this, new FftEventArgs(fftComplex));
            }
        }
    }

    public class FftEventArgs : EventArgs
    {
        public Complex[] Result { get; private set; }
        public FftEventArgs(Complex[] result)
        {
            Result = result;
        }
    }

    // Simplified Complex struct for FFT
    public struct Complex
    {
        public float X;
        public float Y;
        public Complex(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    // Simplified FFT implementation (from NAudio)
    public static class FastFourierTransform
    {
        public static void FFT(bool forward, int m, Complex[] data)
        {
            int n = 1 << m;
            for (int i = 0; i < n; i++)
            {
                int j = 0;
                for (int k = 0; k < m; k++)
                    j = (j << 1) | ((i >> k) & 1);
                if (j > i)
                {
                    Complex temp = data[i];
                    data[i] = data[j];
                    data[j] = temp;
                }
            }

            for (int i = 2; i <= n; i <<= 1)
            {
                for (int j = 0; j < n; j += i)
                {
                    for (int k = 0; k < i / 2; k++)
                    {
                        int index1 = j + k;
                        int index2 = index1 + i / 2;
                        float angle = (float)(2 * Math.PI * k / i * (forward ? -1 : 1));
                        float wReal = (float)Math.Cos(angle);
                        float wImag = (float)Math.Sin(angle);
                        Complex w = new Complex(wReal, wImag);
                        Complex t = new Complex(
                            data[index2].X * w.X - data[index2].Y * w.Y,
                            data[index2].X * w.Y + data[index2].Y * w.X);
                        Complex u = data[index1];
                        data[index1] = new Complex(u.X + t.X, u.Y + t.Y);
                        data[index2] = new Complex(u.X - t.X, u.Y - t.Y);
                    }
                }
            }
        }
    }
}
