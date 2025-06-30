// Version: 1.0.0.665
using System;
using System.ComponentModel;
using System.IO;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Vlc.DotNet.Core;
using Vlc.DotNet.Wpf;
using ThmdPlayer.Core.Interfaces;
using ThmdPlayer.Core.medias;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using ThmdPlayer.Core.helpers;

namespace ThmdPlayer.Core.controls
{
    public partial class PlayerWindow : Window, IPlayer, INotifyPropertyChanged, IDisposable
    {
        #region Deletagets
        public delegate void PlayerStatusDelegate(object sender, MediaPlayerStatus e);
        #endregion

        #region Kernel32.dll
        // Importowanie funkcji SetThreadExecutionState z kernel32.dll
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint SetThreadExecutionState(uint esFlags);

        // Flagi dla SetThreadExecutionState
        const uint ES_CONTINUOUS = 0x80000000;
        const uint ES_SYSTEM_REQUIRED = 0x00000001;
        const uint ES_DISPLAY_REQUIRED = 0x00000002;
        #endregion

        #region Private variables

        private VlcControl _vlcControl;
        private PlaylistView _playlist;
        private Media _media;
        private ControlBar _controlBar;
        private Subtitles.SubtitleManager _subtitle;
        private ControlProgressBar _progressBar;
        private BackgroundWorker _mouseNotMoveWorker;
        private WindowLastStance _lastWindowStance;
        private PlayerStatusDelegate _statusDelegate;
        private TimeSpan _position;
        private Grid _grid;

        private double _volume;
        private bool _isMuted = false;
        private bool _isPlaying = false;
        private bool _isPaused = false;
        private bool _isStoped = false;
        private bool _fullscreen = false;
        private bool _progressBarVisibility = true;
        private bool _isMouseMove = false;
        private MediaPlayerStatus _playerStatus = MediaPlayerStatus.Stop;

        /// <summary>
        /// Time when mouse device go sleeps are represented by second time span
        /// </summary>
        public int MouseSleeps
        {
            get;
            set;
        } = 7;
        #endregion


        #region Public variables
        public TimeSpan CurrentTime {
            get => _position;
            set => OnPropertyChanged(nameof(CurrentTime), ref _position, value);
        }
        
        public Media Media {
            get => _media;
            set {
                if (_media != null)
                {
                    _media.PositionChanged -= HandleMediaPositionChanged;
                    _media.VolumeChanged -= HandleMediaVolumeChanged;
                }

                _media = value;

                if (_media != null)
                {
                    _media.PositionChanged += HandleMediaPositionChanged;
                    _media.VolumeChanged += HandleMediaVolumeChanged;
                }
                OnPropertyChanged(nameof(Media), ref _media, value); 
            }
        }

        public MediaPlayerStatus Status 
        {
            get => _playerStatus;
            set
            {
                if (value == MediaPlayerStatus.Play)
                {
                    this.Play();
                }
                else if (value == MediaPlayerStatus.Stop)
                {
                    this.Stop();
                }
                else if (value == MediaPlayerStatus.Pause)
                {
                    this.Pause();
                }
                else if (value == MediaPlayerStatus.Seek)
                {
                    this.Seek(TimeSpan.FromMilliseconds(_vlcControl.SourceProvider.MediaPlayer.Position));
                }

                OnPropertyChanged(nameof(Status), ref _playerStatus, value);
            }
        }

        public PlaylistView Playlist 
        {
            get => _playlist;
            set => OnPropertyChanged(nameof(Playlist), ref _playlist, value);
        }

        public Visibility PlaylistViewVisibility
        {
            get => _playlist.Visibility;
            set=>
                _playlist.Visibility = value;
        }

        public double Volume 
        {
            get => _volume;
            set => OnPropertyChanged(nameof(Volume), ref _volume, value);
        }

        public bool IsPlaying { get => _isPlaying; set => _isPlaying = value; }

        public bool IsPaused { get => _isPaused; set => _isPaused = value; }

        public bool IsStoped { get => _isStoped; set => _isStoped = value; }

        public bool SubtitleVisibility { get; set; } = true;

        public bool Mute { get => _isMuted; set => _isMuted = value; }

        public bool FullScreen 
        { 
            get=>_fullscreen;
            set 
            {
                FullscreenOnOff();
                OnPropertyChanged(nameof(FullScreen), ref _fullscreen, value); 
            }
        }

        public bool ProgressBarVisibility {
            get => _progressBarVisibility;
            set 
            {
                if (value)
                    _progressBar.Visibility = Visibility.Collapsed;
                else 
                    _progressBar.Visibility = Visibility.Visible;
                OnPropertyChanged(nameof(ProgressBarVisibility), ref _progressBarVisibility, value);
            }
        }

        public ControlBar ControlBar { get => _controlBar; }

        public ControlProgressBar ProgressBar {  get => _progressBar; }
        public SubControl SubtitleControl { get; set; }
        public IntPtr Handle { get; private set; } = IntPtr.Zero;

        medias.Playlist IPlayer.Playlist => throw new NotImplementedException();

        public PlaylistView PlaylistView { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        controls.PlaylistView IPlayer.PlaylistView { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        #endregion

        /// <summary>
        /// Event for property change notifications
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Event for position change notifications
        /// </summary>
        public event EventHandler<TimeSpan> PositionChanged;
        /// <summary>
        /// Event for player status change notifications
        /// </summary>
        public event EventHandler<MediaPlayerStatus> PlayerStatusChanged;
        /// <summary>
        /// Event for player position change notifications
        /// </summary>
        public event EventHandler<double> PlayerPositionChanged;
        /// <summary>
        /// Event for volume change notifications
        /// </summary>
        public event EventHandler<Media> PlayerVolumeChanged;
        public event EventHandler<TimeSpan> OnSubtitleControlChanged;

        /// <summary>
        /// PlayerWindow constructor
        /// </summary>
        public PlayerWindow()
        {
            
            _playlist = new ThmdPlayer.Core.controls. PlaylistView();
            _playlist.Width = 400;
            _playlist.Height = 200;
            _playlist.Visibility = Visibility.Hidden;
            _playlist.SetPlayer(this);

            _grid = new Grid();
            _grid.Background = Brushes.Transparent;
            
            _progressBar = new ControlProgressBar(this);
            _progressBar.HorizontalAlignment = HorizontalAlignment.Stretch;
            _progressBar.VerticalAlignment = VerticalAlignment.Bottom;

            _controlBar = new ControlBar(this);
            _controlBar.HorizontalAlignment = HorizontalAlignment.Left;
            _controlBar.VerticalAlignment = VerticalAlignment.Top;

            _vlcControl = new VlcControl();

            var mediaOptions = new string[] { "--no-video-title-show", "--no-xlib" };
            _vlcControl.SourceProvider.CreatePlayer(new DirectoryInfo(System.IO.Path.Combine("libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64")), mediaOptions);
            _isPlaying = _vlcControl.SourceProvider.MediaPlayer.IsPlaying();

            this.Content = _grid;

            _grid.Children.Add(_vlcControl);
            _grid.Children.Add(_controlBar);
            _grid.Children.Add(_progressBar);
            _grid.Children.Add(_playlist);

            Events();

            if (_vlcControl.SourceProvider.MediaPlayer != null)
                Handle = this._vlcControl.SourceProvider.MediaPlayer.VideoHostControlHandle;
        }

        event EventHandler<double> IPlayer.PlayerVolumeChanged
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        private void OnPlayerStatusEvent(object sender, MediaPlayerStatus e)
        {
            e = _playerStatus;
        }

        private void Events()
        {
            _progressBar.MouseDown += _progressBar_MouseDown;
            _progressBar.MouseMove += _progressBar_MouseMove;

            _vlcControl.MouseDown += OnMouseDown;

            _mouseNotMoveWorker = new BackgroundWorker();
            _mouseNotMoveWorker.DoWork += _mouseNotMoveWorker_DoWork;
            _mouseNotMoveWorker.RunWorkerAsync();

            this.MouseMove += OnMouseMove;

            //_vlcControl.SourceProvider.MediaPlayer.Buffering += OnBuffering;
            _vlcControl.SourceProvider.MediaPlayer.EndReached += OnEndReached;
            _vlcControl.SourceProvider.MediaPlayer.PositionChanged += OnPositionChanged;
            _vlcControl.SourceProvider.MediaPlayer.TimeChanged += OnTimeChanged;
            _vlcControl.SourceProvider.MediaPlayer.Backward += OnBackward;
            _vlcControl.SourceProvider.MediaPlayer.Forward += OnForward;
            _vlcControl.SourceProvider.MediaPlayer.Playing += OnPlaying;
            _vlcControl.SourceProvider.MediaPlayer.Stopped += OnStopped; ;
        }

        private void OnStopped(object sender, VlcMediaPlayerStoppedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(_ => this.Stop());
        }

        private async void OnMouseMove(object sender, MouseEventArgs e)
        {
             await _controlBar.ShowByStoryboard((Storyboard)(_controlBar.FindResource("fadeInControlBar")));
            await _progressBar.ShowByStoryboard((Storyboard)(_progressBar.FindResource("fadeInProgressBar")));
            this.Cursor = Cursors.Arrow;
        }

        private void OnPlaying(object sender, VlcMediaPlayerPlayingEventArgs e)
        {
            SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (_isPlaying)
                this.Pause();
            else
                this.Play();
        }

        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.FullScreen = true;
        }

        private void OnTimeChanged(object sender, VlcMediaPlayerTimeChangedEventArgs e)
        {
            var time = TimeSpan.FromMilliseconds(e.NewTime);

            this.Dispatcher.Invoke(new Action(() =>
            {
                var progress_bar_value = (time.TotalMilliseconds / _vlcControl.SourceProvider.MediaPlayer.Length) * _progressBar.Maximum;
                Console.WriteLine((progress_bar_value));
                _progressBar.Duration = TimeSpan.FromMilliseconds(_vlcControl.SourceProvider.MediaPlayer.Length);
                var time_left = _progressBar.Duration - time;
                _progressBar.ProgressText = $"{_media.Name} \n {time.Hours:00} : {time.Minutes:00} : {time.Seconds:00} / {time_left.Hours:00} : {time_left.Minutes:00} : {time_left.Seconds:00}";

                _progressBar.Value = progress_bar_value;
                SetPosition(time.TotalMilliseconds);
            }));

            if (_vlcControl.SourceProvider.MediaPlayer.Position >= _vlcControl.SourceProvider.MediaPlayer.Length)
            {
                this.OnEndReached(sender, new VlcMediaPlayerEndReachedEventArgs());
            }
        }

        private void _progressBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ProgressBarMouseEventHandler(sender, e);
        }

        private void _progressBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ProgressBarMouseEventHandler(sender, e);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this._progressBar.Margin = new Thickness(0, 0, this.ActualWidth, this.ActualHeight - _progressBar.Height);
            this._progressBar.Width = this.ActualWidth;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            this._progressBar.Margin = new Thickness(0, 0, this.ActualWidth, this.ActualHeight - _progressBar.Height);
            this._progressBar.Width = this.ActualWidth;
        }

        private void ProgressBarMouseEventHandler(object sender, MouseEventArgs e)
        {
            var click_position = e.GetPosition(sender as ControlProgressBar).X;
            var width = (sender as ControlProgressBar).ActualWidth;
            var result = (click_position / width) * (sender as ControlProgressBar).Maximum;

            (sender as ControlProgressBar).Value = result;

            // Video jump to time
            var jump_to_time = (_vlcControl.SourceProvider.MediaPlayer.Length * result) / (sender as ControlProgressBar).Maximum;
            _vlcControl.SourceProvider.MediaPlayer.Time = (long)jump_to_time;
        }

        private async void _mouseNotMoveWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var val = true;
            while (val)
            {
                await IfMouseMoved();

                if (e.Cancel)
                {
                    val = false;
                }
            }
        }

        private async Task<bool> IfMouseMoved()
        {
            void MouseMovedCallback(object sender, MouseEventArgs e) 
            { 
                _isMouseMove = true;
            };

            MouseMove += MouseMovedCallback;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(MouseSleeps));
                return _isMouseMove;
            }
            finally
            {
                MouseMove -= MouseMovedCallback;

                await _controlBar.Dispatcher.Invoke(async() =>
                {
                    await _controlBar.HideByStoryboard((Storyboard)(_controlBar.FindResource("fadeOutControlBar")));
                    await _progressBar.HideByStoryboard((Storyboard)(_progressBar.FindResource("fadeOutProgressBar")));
                    this.Cursor = Cursors.None;
                });

                _isMouseMove = false;
            }
        }

        private void OnForward(object sender, VlcMediaPlayerForwardEventArgs e)
        {
            
        }

        private void OnBackward(object sender, VlcMediaPlayerBackwardEventArgs e)
        {
            
        }

        private void OnPositionChanged(object sender, VlcMediaPlayerPositionChangedEventArgs e)
        {
            this._position = TimeSpan.FromMilliseconds(e.NewPosition);
            _media.Position = e.NewPosition;
        }

        private void OnEndReached(object sender, VlcMediaPlayerEndReachedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(_ => this.Stop());
            SetThreadExecutionState(ES_CONTINUOUS);
        }

        private void OnBuffering(object sender, VlcMediaPlayerBufferingEventArgs e)
        {
            _progressBar.BufforBarValue = e.NewCache;
        }

        public void Play()
        {
            if (_vlcControl.SourceProvider.MediaPlayer.IsPlaying())
            {
                this.Pause();
            }
            else
            {
                if (_media != null)
                {
                    this._Play();
                }
            }

            _isPlaying = _vlcControl.SourceProvider.MediaPlayer.IsPlaying();
        }

        public void SetPosition(double position)
        {
            _media.Position = position;
            _controlBar._timer.Content = TimeSpan.FromMilliseconds(Media.Position);
        }

        public void SetVolume(double volume)
        {
            Media.Volume = volume;
        }

        private void HandleMediaPositionChanged(object sender, double newPosition)
        {
            Console.WriteLine($"Pozycja odtwarzania zmieniona: {newPosition}");
            newPosition = _position.TotalMilliseconds;
        }

        private void HandleMediaVolumeChanged(object sender, double newVolume)
        {
            Console.WriteLine($"Głośność zmieniona: {newVolume}");
             newVolume = _vlcControl.SourceProvider.MediaPlayer.Audio.Volume;
        }

        private void _Play()
        {
            Console.WriteLine("Runing media: "+_media.Name+"\n[on handle] : "+_media.Player.Handle);

            _controlBar._mediaElementLabel.Content = _media.Name;
            _controlBar._timer.Content = TimeSpan.FromMilliseconds(_media.Position);
            _vlcControl.SourceProvider.MediaPlayer?.Play(_media.MediaStream.GetStream().Result);//_media.MediaStream.GetStreamAsync().Result);
            _playerStatus = MediaPlayerStatus.Play;
            _isPlaying = true;
            _isPaused = false;
            _isStoped = false;
        }

        public void Play(Media media)
        {
            _media = media;
            _Play();
        }

        public void Play(Uri uri)
        {
            _media = new Media(uri.AbsoluteUri, this);
            _Play();
        }

        public void Play(string path)
        {
            _media = new Media(path, this);
            _Play();
        }

        public void Stop()
        {
            _vlcControl.SourceProvider.MediaPlayer?.Stop();
            _playerStatus = MediaPlayerStatus.Stop;
            _isPlaying = false;
            _isPaused = false;
            _isStoped = true;
        }

        public void Pause()
        {
            _vlcControl.SourceProvider.MediaPlayer?.Pause();
            _playerStatus = MediaPlayerStatus.Pause;
            _isPlaying = false;
            _isPaused = true;
            _isStoped = false;
        }

        public void Next()
        {
            _playlist.Next.Play();
        }

        public void Preview()
        {
            _playlist.Items.MoveCurrentToPrevious();
            (_playlist.Items.CurrentItem as Media).Play();
        }

        public void Seek(TimeSpan time)
        {
            if (_vlcControl != null)
            {
                _vlcControl.SourceProvider.MediaPlayer.Time = (long)time.TotalMilliseconds;
                _playerStatus = MediaPlayerStatus.Seek;
            }
        }

        public void Seek(TimeSpan time, SeekDirection direction)
        {
            if (_vlcControl != null)
            {
                switch (direction)
                {
                    case SeekDirection.Forward:
                        _vlcControl.SourceProvider.MediaPlayer.Time += (long)time.TotalMilliseconds;
                        break;
                    case SeekDirection.Backward:
                        _vlcControl.SourceProvider.MediaPlayer.Time -= (long)time.TotalMilliseconds;
                        break;
                }
                _playerStatus = MediaPlayerStatus.Seek;
            }
        }

        public void Subtitle(string subtitle)
        {
            
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

        /// <summary>
        /// 
        /// </summary>
        public void Fullscreen()
        {
            FullscreenOnOff();
        }
        /// <summary>
        /// 
        /// </summary>
        private void FullscreenOnOff()
        {
            var window = Window.GetWindow(this);

            if (window != null) {
                if (window.WindowStyle == WindowStyle.None)
                {
                    // Exit fullscreen
                    if (this._lastWindowStance != null)
                    {
                        window.ResizeMode = this._lastWindowStance.Mode;
                        window.WindowStyle = this._lastWindowStance.Style;
                        window.WindowState = this._lastWindowStance.State;

                        this._fullscreen = false;

                        return;
                    }
                    else
                    {
                        // Default
                        window.ResizeMode = ResizeMode.CanResize;
                        window.WindowStyle = WindowStyle.SingleBorderWindow;
                        window.WindowState = WindowState.Normal;

                        this._fullscreen = false;
                    }
                }
            }

            // Enter fullscreen
            this._lastWindowStance = new WindowLastStance() { Mode = window.ResizeMode, State = window.WindowState, Style = window.WindowStyle };

            window.ResizeMode = ResizeMode.NoResize;
            window.WindowStyle = WindowStyle.None;
            window.WindowState = WindowState.Normal;
            window.WindowState = WindowState.Maximized;
            this._fullscreen = true;
        }

        public void Dispose()
        {
            
        }
    }
}
