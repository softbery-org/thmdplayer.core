// Version: 1.0.0.676
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ThmdPlayer.Core.Subtitles;

namespace ThmdPlayer.Core.controls
{
    /// <summary>
    /// Logika interakcji dla klasy SubtitleControl.xaml
    /// </summary>
    public partial class SubtitleControl : UserControl
    {
        public delegate void TimeHandlerDelegate(TimeSpan time);

        private TimeSpan _positionTime = TimeSpan.Zero;
        private SubtitleManager _subtitleManager;
        private double _fontSize = 48;
        private Brush _backgroundBrush = new SolidColorBrush(Colors.Transparent);
        private Brush _subtitleBrush = new SolidColorBrush(Colors.White);
        private bool _shadowSubtitle = true;
        private FontFamily _fontFamily = new FontFamily("SagoeUI");
        private bool _sizeToFit = true;

        public FrameworkElement TextBlock => _subtitleTextBlock;

        public bool SubtitleShadow
        {
            get => _shadowSubtitle;
            set
            {
                if (_shadowSubtitle != value)
                {
                    if (value)
                    {
                        var e = new DropShadowEffect();
                        this._subtitleTextBlock.Effect = e;
                    }
                    else
                    {
                        this._subtitleTextBlock.Effect = null;
                    }

                    _shadowSubtitle = value;
                }
            }
        }

        public FontFamily SubtitleFontFamily 
        {
            get => _fontFamily;
            set
            {
                if (_fontFamily != value)
                {
                    _fontFamily = value;
                    _subtitleTextBlock.FontFamily = value;
                    OnPropertyChanged(nameof(SubtitleFontFamily), ref _fontFamily, value);
                }
            }
        }

        public double SubtitleFontSize 
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _subtitleTextBlock.FontSize = value;
                    OnPropertyChanged(nameof(_fontSize), ref _fontSize, value);
                }
            }
        }

        public Brush SubtitleBackground {
            get => _backgroundBrush;
            set
            {
                if (_backgroundBrush != value)
                {
                    this.Background = value;
                    OnPropertyChanged(nameof(_backgroundBrush), ref _backgroundBrush, value);
                }
            }
        }

        public Brush SubtitleBrush {
            get => _subtitleBrush;
            set
            {
                if (_subtitleBrush != value)
                {
                    _subtitleTextBlock.Foreground = value;
                    OnPropertyChanged(nameof(_backgroundBrush), ref _subtitleBrush, value);
                }
            }
        }

        public string FilePath { get; set; }

        public string Text { get; set; } = string.Empty;

        public TimeSpan PositionTime 
        {
            get => _positionTime;
            set 
            {
                GetSubtitle(value);
                OnPropertyChanged(nameof(_positionTime), ref _positionTime, value);
            }
        }

        public static event TimeHandlerDelegate TimeChange;
        public event PropertyChangedEventHandler PropertyChanged;

        public SubtitleControl()
        {
            InitializeComponent();
            SubtitleBrush = new SolidColorBrush(Colors.White);
            SubtitleBackground = new SolidColorBrush(Colors.Transparent);
            SubtitleFontSize = 48;
            SubtitleFontFamily = new FontFamily("Arial");
        }

        public void UpdateSubtitleStyle()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.InvalidateVisual();
            });
        }

        public void SetSubtitleFontFamily(FontFamily fontFamily)
        {
            if (fontFamily != null)
            {
                _subtitleTextBlock.FontFamily = fontFamily;
            }
        }

        public void SetSubtitleShadow(bool shadow)
        {
            SubtitleShadow = shadow;
            if (shadow)
            {
                _subtitleTextBlock.Effect = new DropShadowEffect();
            }
        }



        protected virtual void OnTimeChange(TimeSpan time)
        {
            if (TimeChange != null)
            {
                TimeChange?.Invoke(time);
            }
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

        private void SetSubtitleFontSize(double size)
        {
            _subtitleTextBlock.FontSize = size;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (FilePath != null)
                _subtitleManager = new SubtitleManager(FilePath);

            GetSubtitle(PositionTime);

            // Create the initial formatted text string.
            /*FormattedText formattedText = new FormattedText(
                Text,
                CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight,
                new Typeface("Verdana"),
                32,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            // OnPropertyChanged a maximum width and height. If the text overflows these values, an ellipsis "..." appears.
            formattedText.MaxTextWidth = 300;
            formattedText.MaxTextHeight = 240;

            // Use a larger font size beginning at the first (zero-based) character and continuing for 5 characters.
            // The font size is calculated in terms of points -- not as device-independent pixels.
            formattedText.SetFontSize(36 * (96.0 / 72.0), 0, 5);

            // Use a Bold font weight beginning at the 6th character and continuing for 11 characters.
            formattedText.SetFontWeight(FontWeights.Bold, 6, 11);

            // Use a linear gradient brush beginning at the 6th character and continuing for 11 characters.
            formattedText.SetForegroundBrush(
                                    new LinearGradientBrush(
                                    Colors.Orange,
                                    Colors.Teal,
                                    90.0),
                                    6, 11);

            // Use an Italic font style beginning at the 28th character and continuing for 28 characters.
            formattedText.SetFontStyle(FontStyles.Italic, 28, 28);

            // Draw the formatted text string to the DrawingContext of the control.
            drawingContext.DrawText(formattedText, new Point(10, 0));*/

            base.OnRender(drawingContext);
        }

        private async void GetSubtitle(TimeSpan time)
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

                                    this._subtitleTextBlock.Text = text_lines;
                                }
                                if (time >= item.EndTime)
                                {
                                    this._subtitleTextBlock.Text = String.Empty;
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
                this._subtitleTextBlock.Width = this.ActualWidth;
                this._subtitleTextBlock.Height = this.ActualHeight;

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
                    _subtitleTextBlock.FontSize *= scaleFactor;
                }

                _lastWidth = this.ActualWidth;
            }
        }
    }
}
