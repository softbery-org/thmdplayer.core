// Version: 0.1.0.104
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThmdPlayer.Core.configuration;
using ThmdPlayer.Core.Subtitles;

namespace ThmdPlayer.Core.controls
{
    /// <summary>
    /// Logika interakcji dla klasy SubControl.xaml
    /// </summary>
    public partial class SubControl : TextBlock
    {
        public delegate void TimeHandlerDelegate(object sender, TimeSpan time);

        private TimeSpan _currentTime = TimeSpan.Zero;
        private SubtitleManager _subtitleManager;
        private Shadow _shadow = new Shadow();
        private bool _sizeToFit = true;
        private string _filePath = null;

        public bool ShowShadow
        {
            get => _shadow.Visible;
            set
            {
                if (_shadow.Visible != value)
                {
                    if (value)
                    {
                        var e = new DropShadowEffect
                        {
                            Color = _shadow.Color,
                            BlurRadius = _shadow.BlurRadius,
                            ShadowDepth = _shadow.ShadowDepth,
                            Opacity = Shadow.Opacity
                        };
                        this.Effect = e;
                    }
                    else
                    {
                        this.Effect = null;
                    }

                    _shadow.Visible = value;
                }
            }
        }

        public Shadow Shadow
        {
            get => _shadow;
            set
            {
                _shadow = value;
                OnPropertyChanged(nameof(Shadow), ref _shadow, value);
            }
        }

        public string FilePath
        {
            get => _filePath;
            set
            {
                _subtitleManager = new SubtitleManager(value);
                OnPropertyChanged(nameof(FilePath), ref _filePath, value);
            }
        }

        public TimeSpan CurrentTime
        {
            get => _currentTime;
            set
            {
                GetSubtitleLine(value);
                this.InvalidateVisual();
                OnPropertyChanged(nameof(CurrentTime), ref _currentTime, value);
            }
        }

        public event TimeHandlerDelegate TimeChanged;
        public event PropertyChangedEventHandler PropertyChanged;


        public SubControl()
        {
            InitializeComponent();

            this.Foreground = new SolidColorBrush(Colors.White);
            this.Background = new SolidColorBrush(Colors.Transparent);
            this.FontFamily = new FontFamily("SagoeUI");
            this.TextAlignment = TextAlignment.Center;
            this.TextWrapping = TextWrapping.Wrap;
            this.HorizontalAlignment = HorizontalAlignment.Center;
            this.VerticalAlignment = VerticalAlignment.Bottom;
            this.Effect = new DropShadowEffect
            {
                Color = Colors.Red,
                BlurRadius = 15,
                ShadowDepth = 0,
                Opacity = 0.5
            };
        }

        public SubControl(string path) : this()
        {
            if (path != null)
            {
                _subtitleManager = new SubtitleManager(path);
                _filePath = path;
                Player.OnCurrentTimeChanged += (s, e) => {
                    _currentTime = e;
                    GetSubtitleLine(e);
                };
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            this.Loaded += OnLoaded;
            this.SizeChanged += (s, a) => SizeToFit();
        }

        public void UpdateSubtitleStyle()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.InvalidateVisual();
            });
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void OnPropertyChanged<T>(string propertyName, ref T field, T value)
        {
            if ((field == null && value != null) || (field != null && !field.Equals(value)))
            {
                field = value;
                PropertyChanged?.Invoke(field, new PropertyChangedEventArgs(propertyName));
            }
        }

        private async void GetSubtitleLine(TimeSpan time)
        {
            var task = Task.Run(() =>
            {
                if (_subtitleManager != null)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        if (_subtitleManager != null)
                        {
                            foreach (var item in _subtitleManager.GetSubtitles())
                            {
                                if (time >= item.StartTime)
                                {
                                    var text_lines = "";

                                    foreach (var txt in item.Text)
                                    {
                                        text_lines += txt;
                                    }

                                    this.Text = text_lines;
                                }
                                if (time >= item.EndTime)
                                {
                                    this.Text = String.Empty;
                                }
                            }
                        }
                    });
                }
            });
            await Task.FromResult(task);
        }

        private double? _lastWidth;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.SizeToFit();
        }

        private void SizeToFit()
        {
            if (_sizeToFit)
            {
                this.Width = this.ActualWidth;
                this.Height = this.ActualHeight;

                // 1920x1080=72
                // 1280x720=62
                // 640x360=52
                // 320x180=42
                // 160x90=32
                // 80x45=22
                // 40x22=12

                // width 1920 = font size 72

                if (_lastWidth.HasValue && this.ActualWidth != _lastWidth.Value)
                {
                    double scaleFactor = this.ActualWidth / _lastWidth.Value;
                    this.FontSize *= scaleFactor;
                }

                _lastWidth = this.ActualWidth;
            }
        }
    }
}
