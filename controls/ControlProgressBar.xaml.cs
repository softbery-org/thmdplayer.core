// Version: 1.0.0.661
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ThmdPlayer.Core.Interfaces;

namespace ThmdPlayer.Core.controls
{
    /// <summary>
    /// Logika interakcji dla klasy ControlProgressBar.xaml
    /// </summary>
    public partial class ControlProgressBar : UserControl, INotifyPropertyChanged
    {
        #region Delegates

        #endregion

        #region Private properties
        private IPlayer _player;
        private String _progressBarText = "";
        private TimeSpan _duration;
        private bool _popupVisibility = false;
        private Brush _backgroundBrush = Brushes.Black;
        private Brush _foregroundBrush = Brushes.DarkOrange;
        private double _value = 0;
        private double _buffor = 0;
        #endregion

        #region Events declaration
        /// <summary>
        /// Property binding changed
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Public properties
        /// <summary>
        /// Foreground text color in popup
        /// </summary>
        public Brush TextForgroundBrush
        {
            get => this._foregroundBrush;
            set => this._foregroundBrush = value;
        }
        /// <summary>
        /// Background text color in popup
        /// </summary>
        public Brush TextBackgroundBrush
        {
            get => this._backgroundBrush;
            set => this._backgroundBrush = value;
        }
        /// <summary>
        /// Time calculated from progress value
        /// </summary>
        public double Time
        {
            get => this._player.CurrentTime.Milliseconds;
            //set => this._player.PositionChange = TimeSpan.FromMilliseconds(value);
        }
        /// <summary>
        /// Progress bar maximum value
        /// </summary>
        public double Maximum
        {
            get => this._progressBar.Maximum;
            set => this._progressBar.Maximum = value;
        }
        /// <summary>
        /// Progress bar minimum value
        /// </summary>
        public double Minimum
        {
            get => this._progressBar.Minimum;
            set => this._progressBar.Minimum = value;
        }
        /// <summary>
        /// Progress bar value
        /// </summary>
        public double Value
        {
            get => _value;
            set
            {
                var result = new double();
                var v = double.TryParse(value.ToString(), out result);
                if (v)
                {
                    Console.WriteLine(result);
                }
                _value = value;
                try
                {
                    this._progressBar.Value = value;
                }
                catch (Exception ex)
                {
                    this._progressBar.Value = 0;
                    Logger.Log.Log(Logs.LogLevel.Error, new[] { "Console", "File" }, $"{ex.Message}");
                }
                
                this.OnPropertyChanged(nameof(Value), ref _value, value);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public Color BufforBarColor
        {
            get => BufforBarColor;
            set
            {
                this._rectangleBufferMedia.Fill = new SolidColorBrush(value);
            }
        } 
        /// <summary>
        /// 
        /// </summary>
        public double BufforBarValue
        {
            get
            {
                return _buffor;
            }
            set
            {
                this.Dispatcher.Invoke(() => {
                    this._rectangleBufferMedia.Width = (value*this.ActualWidth)/100;// / this.ActualWidth;
                });
                OnPropertyChanged(nameof(BufforBarValue), ref _buffor, value);
            }
        }
        /// <summary>
        /// Duration for progress bar time text
        /// </summary>
        public TimeSpan Duration
        {
            get => this._duration;
            set => this._duration = value;
        }
        /// <summary>
        /// Gets or sets true
        /// </summary>
        public string ProgressText
        {
            get => this._progressBarText;
            set
            {
                if (this._progressBarText != value)
                {
                    this._progressBarText = value;
                    this.OnPropertyChanged("ProgressText");
                }
            }
        }
        /// <summary>
        /// Popup visibility
        /// </summary>
        public bool PopupVisibility
        {
            get
            {
                return this._popupVisibility;
            }
            set
            {
                if (this._popupVisibility != value)
                {
                    this._popupVisibility = value;
                    this._popup.IsOpen = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets true
        /// </summary>
        public string PopupText
        {
            get => this._popuptext.Text;
            set
            {
                if (this._popuptext.Text != value)
                {
                    this._popuptext.Text = value;
                    //this.OnPropertyChanged(nameof(this._popuptext), ref this._popuptext, value);
                }
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Class constructor
        /// </summary>
        public ControlProgressBar()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Maximum = 100;
            this._rectangleMouseOverPoint.Visibility = Visibility.Hidden;
            this._popup.IsOpen = false;
            this._popuptext.Width = 100;
            this._popuptext.Background = new DrawingBrush(DrawingBackground());
            this._popuptext.Effect = new DropShadowEffect();

            this._progressBar.ValueChanged += _progressBar_ValueChanged;

            this.BufforBarColor = Color.FromArgb(45, 138, 43, 226);
        }

        public ControlProgressBar(IPlayer player) : this()
        {
            _player = player;
        }

        private void _progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _value = (double)e.NewValue;
        }
        #endregion

        #region Event methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
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
        #endregion

        #region Mouse actions on progress bar
        private void _grid_MouseMove(object sender, MouseEventArgs e)
        {
            // Get mouse position point
            var mouse_position = e.GetPosition(this._progressBar);
            // Progress width
            var width = this._progressBar.ActualWidth;
            // Mouse position
            var position = (mouse_position.X / width) * this._progressBar.Maximum;

            // Caltulateing time in TimeSpan type from mouse position
            var time_in_ms = (this._duration.TotalMilliseconds * position) / this._progressBar.Maximum;
            var time = TimeSpan.FromMilliseconds(time_in_ms);

            // Show position line on bar
            this._rectangleMouseOverPoint.Visibility = Visibility.Visible;
            this._rectangleMouseOverPoint.Stroke = Brushes.DarkOrange;
            this._rectangleMouseOverPoint.StrokeThickness = 3;
            this._rectangleMouseOverPoint.Margin = new Thickness(e.GetPosition(this).X, 0, 0, 0);

            // Popup text for time where mouse is over with text time
            if (!this._popup.IsOpen)
                this.PopupVisibility = true;

            this._popup.HorizontalOffset = mouse_position.X + 20;
            this._popup.VerticalOffset = mouse_position.Y;
        }

        private void _grid_MouseLeave(object sender, MouseEventArgs e)
        {
            this._rectangleMouseOverPoint.Visibility = Visibility.Hidden;
            this.PopupVisibility = false;
        }
        #endregion

        #region Drawing background with shape
        private Drawing DrawingBackground()
        {
            var drawing_group = new DrawingGroup();
            using (DrawingContext context = drawing_group.Open())
            {
                context.DrawEllipse(this._backgroundBrush, new Pen(this._foregroundBrush, 5), new Point(0, 0), 35, 35);
            }
            return drawing_group;
        }
        #endregion
    }
}
