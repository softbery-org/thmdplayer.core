// Version: 1.0.0.647
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ThmdPlayer.Core.helpers;
using ThmdPlayer.Core.Interfaces;
using ThmdPlayer.Core.medias;
using Vlc.DotNet.Core;
using Vlc.DotNet.Wpf;

namespace ThmdPlayer.Core.controls
{
    public class PlayerEventArgs : EventArgs
    {
        public PlaylistView PlaylistView { get; private set; }
        public ControlBar ControlBar { get; private set; }
        public ControlProgressBar ProgressBar { get; private set; }
        public SubControl SubtitleControl { get; private set; }
        public Media Media { get; private set; }
        public int Count { get => this.PlaylistView.Items.Count; }

        public PlayerEventArgs(IPlayer player)
        {
            PlaylistView = player.PlaylistView ?? throw new ArgumentNullException(nameof(player.PlaylistView));
            ControlBar = player.ControlBar ?? throw new ArgumentNullException(nameof(player.ControlBar));
            ProgressBar = player.ProgressBar ?? throw new ArgumentNullException(nameof(player.ProgressBar));
            Media = player.Media ?? throw new ArgumentNullException(nameof(player.Media));
            SubtitleControl = player.SubtitleControl ?? throw new ArgumentNullException(nameof(player.SubtitleControl));
        }
    }


    public class PlayerEventHandler : ContentControl
    {
        public event EventHandler<PlayerEventArgs> MediaPlayed;
        public event EventHandler<PlayerEventArgs> MediaPaused;
        public event EventHandler<PlayerEventArgs> MediaStopped;
        public event EventHandler<PlayerEventArgs> MediaChanged;

        public void OnMediaPlayed(IPlayer player)
        {
            MediaPlayed?.Invoke(this, new PlayerEventArgs(player));
        }

        public void OnMediaPaused(IPlayer player)
        {
            MediaPaused?.Invoke(this, new PlayerEventArgs(player));
        }

        public void OnMediaStopped(IPlayer player)
        {
            MediaStopped?.Invoke(this, new PlayerEventArgs(player));
        }

        public void OnMediaChanged(IPlayer player)
        {
            MediaChanged?.Invoke(this, new PlayerEventArgs(player));
        }
    }

    public partial class Player : UserControl, IPlayer, INotifyPropertyChanged, IDisposable
    {
        #region Deletagets
        public delegate void PlayerStatusDelegate(object sender, MediaPlayerStatus e);
        public delegate void PlayerCurrentTimeDelegate(object sender, TimeSpan e);
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
        private medias.Playlist _playlist;
        private controls.PlaylistView _playlistView;
        private Media _media;
        private ControlBar _controlBar;
        private ControlProgressBar _progressBar;
        private SubControl _subtitleControl = new SubControl();
        private BackgroundWorker _mouseNotMoveWorker;
        private WindowLastStance _lastWindowStance;
        private TimeSpan _currentTime;
        private Grid _grid;
        private AudioSpectrumControl _spectrumControl;

        private double _volume = 100;
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

        /// <summary>
        /// Event for
        /// </summary>
        #region Public variables

        /// Event for current time changed
        public static event PlayerCurrentTimeDelegate OnCurrentTimeChanged;

        private void OnCurrentTimeChangedHandler(TimeSpan time)
        {
            if (OnCurrentTimeChanged!=null)
            {
                _subtitleControl.CurrentTime = time;
                OnCurrentTimeChanged?.Invoke(this, time);
            }
        }

        public TimeSpan CurrentTime
        {
            get => _currentTime;
            set => OnPropertyChanged(nameof(CurrentTime), ref _currentTime, value);
        }

        public Media Media
        {
            get => _media;
            set
            {
                if (_media != null)
                {
                    _media.PositionChanged -= HandleMediaPositionChanged;
                    _media.VolumeChanged -= HandleMediaVolumeChanged;
                    //_media.PositionChanged -= _media_PositionChanged;
                }

                _media = value;

                if (_media != null)
                {
                    _media.PositionChanged += HandleMediaPositionChanged;
                    _media.VolumeChanged += HandleMediaVolumeChanged;
                    //_media.PositionChanged += _media_PositionChanged;
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

        public PlaylistView PlaylistView
        {
            get => _playlistView;
            set => OnPropertyChanged(nameof(PlaylistView), ref _playlistView, value);
        }

        public medias.Playlist Playlist
        {
            get => _playlist;
            set => OnPropertyChanged(nameof(Playlist), ref _playlist, value);
        }

        public double Volume
        {
            get => _volume;
            set
            {
                if (value < 0)
                    value = 0;
                else if (value > 100)
                    value = 100;
                _vlcControl.SourceProvider.MediaPlayer.Audio.Volume = (int)value;
                OnPropertyChanged(nameof(Volume), ref _volume, value);
            }
        }

        public bool IsPlaying { get => _isPlaying; set => _isPlaying = value; }

        public bool IsPaused { get => _isPaused; set => _isPaused = value; }

        public bool IsStoped { get => _isStoped; set => _isStoped = value; }

        public bool SubtitleVisibility { get; set; } = true;

        public bool Mute
        {
            get => _isMuted;
            set
            {
                if (value)
                {
                    _isMuted = true;
                    _vlcControl.SourceProvider.MediaPlayer.Audio.Volume = 0;
                }
                else
                {
                    _isMuted = false;
                    _vlcControl.SourceProvider.MediaPlayer.Audio.Volume = (int)_volume;
                }
                OnPropertyChanged(nameof(_isMuted), ref _isMuted, value);
            }
        }

        public bool FullScreen
        {
            get => _fullscreen;
            set
            {
                FullscreenOnOff();
                OnPropertyChanged(nameof(FullScreen), ref _fullscreen, value);
            }
        }

        public bool ProgressBarVisibility
        {
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

        public ControlProgressBar ProgressBar { get => _progressBar; }

        public SubControl SubtitleControl { get => _subtitleControl; private set => _subtitleControl = value; }

        public IntPtr Handle { get; private set; } = IntPtr.Zero;
        public AudioSpectrumControl SpectrumControl
        {
            get => _spectrumControl;
            set
            {
                if (_spectrumControl != null)
                {
                    _grid.Children.Remove(_spectrumControl);
                }
                _spectrumControl = value;
                if (_spectrumControl != null)
                {
                    _grid.Children.Add(_spectrumControl);
                }
            }
        }
        #endregion

        #region Public Events
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<MediaPlayerStatus> PlayerStatusChanged;
        public event EventHandler<double> PlayerPositionChanged;
        public event EventHandler<double> PlayerVolumeChanged;
        public event EventHandler<TimeSpan> OnSubtitleControlChanged;
        #endregion

        public Player()
        {
            _playerStatus = MediaPlayerStatus.None;
            _playlist = new medias.Playlist("New list 1", "Description for new list 1.");
            _playlistView = new PlaylistView(_playlist);
            _playlistView.Width = 400;
            _playlistView.Height = 200;
            _playlistView.Visibility = Visibility.Hidden;
            _playlistView.SetPlayer(this);

            _spectrumControl = new AudioSpectrumControl();
            _spectrumControl.Visibility = Visibility.Hidden;

            _grid = new Grid();
            _grid.Background = Brushes.Transparent;

            _progressBar = new ControlProgressBar(this);
            _progressBar.HorizontalAlignment = HorizontalAlignment.Stretch;
            _progressBar.VerticalAlignment = VerticalAlignment.Bottom;
            Logger.InitLogs();

            Logger.Log.Log(Logs.LogLevel.Info, "File", $"ProgressBar control was created: {_progressBar}");
            Logger.Log.Log(Logs.LogLevel.Info, "Console", $"ProgressBar control was created: {_progressBar}");

            _controlBar = new ControlBar(this);
            _controlBar.HorizontalAlignment = HorizontalAlignment.Left;
            _controlBar.VerticalAlignment = VerticalAlignment.Top;
            Logger.Log.Log(Logs.LogLevel.Info, "File", $"ControlBar control was created: {_controlBar}");
            Logger.Log.Log(Logs.LogLevel.Info, "Console", $"ControlBar control was created: {_controlBar}");

            _subtitleControl = new SubControl();
            _subtitleControl.HorizontalAlignment = HorizontalAlignment.Stretch;
            _subtitleControl.FontSize = Logger.Config.SubtitleConfig.FontSize;
            _subtitleControl.FontFamily = Logger.Config.SubtitleConfig.FontFamily;
            _subtitleControl.Foreground = Logger.Config.SubtitleConfig.FontColor;

            SubtitleControl = _subtitleControl;

            _vlcControl = new VlcControl();

            var mediaOptions = new string[] { "--no-video-title-show", "--no-xlib" };

            _vlcControl.SourceProvider.CreatePlayer(new DirectoryInfo(System.IO.Path.Combine(Logger.Config.LibVlcPath, IntPtr.Size == 4 ? "win-x86" : "win-x64")), mediaOptions);
            _isPlaying = _vlcControl.SourceProvider.MediaPlayer.IsPlaying();

            this.Content = _grid;

            Logger.Log.Log(Logs.LogLevel.Info, "File", $"Vlc control was created: {_controlBar}");
            Logger.Log.Log(Logs.LogLevel.Info, "Console", $"Vlc control was created: {_controlBar}");

            _grid.Children.Add(_vlcControl);
            _grid.Children.Add(_subtitleControl);
            _grid.Children.Add(_controlBar);
            _grid.Children.Add(_progressBar);
            _grid.Children.Add(_playlistView);
            _grid.Children.Add(_spectrumControl);

            Logger.Log.Log(Logs.LogLevel.Info, "File", $"controls was added to: {this}");
            Logger.Log.Log(Logs.LogLevel.Info, "Console", $"controls was added to: {this}");

            Events();

            if (_vlcControl.SourceProvider.MediaPlayer != null)
                Handle = this._vlcControl.SourceProvider.MediaPlayer.VideoHostControlHandle;

            Subtitle("");
        }

        public void UpdateSubtitleStyle()
        {
            this.Dispatcher.Invoke(() =>
            {
                //_subtitleControl.FontFamily = fontFamily;
                _subtitleControl.UpdateLayout();
            });
        }

        private void OnPlayerStatusEvent(object sender, MediaPlayerStatus e)
        {
            e = _playerStatus;
            PlayerStatusChanged?.Invoke(this, e);
        }

        private void Events()
        {
            _progressBar.MouseDown += _progressBar_MouseDown;
            _progressBar.MouseMove += _progressBar_MouseMove;

            _vlcControl.MouseDown += OnMouseDown;
            _vlcControl.MouseDoubleClick += OnMouseDoubleClick;

            _mouseNotMoveWorker = new BackgroundWorker();
            _mouseNotMoveWorker.DoWork += _mouseNotMoveWorker_DoWork;
            _mouseNotMoveWorker.RunWorkerAsync();

            this.MouseMove += OnMouseMove;

            _vlcControl.SourceProvider.MediaPlayer.Buffering += OnBuffering;
            _vlcControl.SourceProvider.MediaPlayer.EndReached += OnEndReached;
            _vlcControl.SourceProvider.MediaPlayer.PositionChanged += OnPositionChanged;
            _vlcControl.SourceProvider.MediaPlayer.TimeChanged += OnTimeChanged;
            _vlcControl.SourceProvider.MediaPlayer.Backward += OnBackward;
            _vlcControl.SourceProvider.MediaPlayer.Forward += OnForward;
            _vlcControl.SourceProvider.MediaPlayer.Playing += OnPlaying;
            _vlcControl.SourceProvider.MediaPlayer.Stopped += OnStopped;
            _vlcControl.SourceProvider.MediaPlayer.Paused += OnPaused;
            _vlcControl.SourceProvider.MediaPlayer.Muted += OnMuted;

            this.PlayerStatusChanged += OnPlayerStatusEvent;
            this.PlayerVolumeChanged += OnPlayerVolumeChanged;
        }

        private void OnPlayerVolumeChanged(object sender, double e)
        {
            e = _volume;
            PlayerVolumeChanged?.Invoke(this, e);
        }

        private void OnMuted(object sender, EventArgs e)
        {

        }

        private void OnStopped(object sender, VlcMediaPlayerStoppedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(_ => this.Stop());
            _playerStatus = MediaPlayerStatus.Stop;
        }

        private void OnPaused(object sender, VlcMediaPlayerPausedEventArgs e)
        {
            SetThreadExecutionState(ES_CONTINUOUS);
            _playerStatus = MediaPlayerStatus.Pause;
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
            _playerStatus = MediaPlayerStatus.Play;
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
                _progressBar.Duration = TimeSpan.FromMilliseconds(_vlcControl.SourceProvider.MediaPlayer.Length);
                var time_left = _progressBar.Duration - time;

                _progressBar.ProgressText = $"{_media.Name} \n {time.Hours:00} : {time.Minutes:00} : {time.Seconds:00} / {time_left.Hours:00} : {time_left.Minutes:00} : {time_left.Seconds:00}";

                _progressBar.Value = progress_bar_value;

                SetPosition(time.TotalMilliseconds);

                _subtitleControl.CurrentTime = time;
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
            // Get mouse position point
            var mouse_position = e.GetPosition(this._progressBar);
            // Progress width
            var width = this._progressBar.ActualWidth;
            // Mouse position
            var position = (mouse_position.X / width) * this._progressBar.Maximum;

            // Caltulate time in TimeSpan type from mouse position
            var time_in_ms = (this._vlcControl.SourceProvider.MediaPlayer.Length * position) / this._progressBar.Maximum;
            var time = TimeSpan.FromMilliseconds(time_in_ms);
            _progressBar.PopupText = $"{time.Hours:00} : {time.Minutes:00} : {time.Seconds:00}";

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
            var position = e.GetPosition(sender as ControlProgressBar).X;
            var width = (sender as ControlProgressBar).ActualWidth;
            var result = (position / width) * (sender as ControlProgressBar).Maximum;

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
            }
            ;

            MouseMove += MouseMovedCallback;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(MouseSleeps));
                return _isMouseMove;
            }
            finally
            {
                MouseMove -= MouseMovedCallback;

                await _controlBar.Dispatcher.Invoke(async () =>
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
            _playerStatus = MediaPlayerStatus.Seek;
        }

        private void OnBackward(object sender, VlcMediaPlayerBackwardEventArgs e)
        {
            _playerStatus = MediaPlayerStatus.Seek;
        }

        private void OnPositionChanged(object sender, VlcMediaPlayerPositionChangedEventArgs e)
        {
            this._currentTime = TimeSpan.FromMilliseconds(e.NewPosition);
            _media.Position = e.NewPosition;
        }

        private void OnEndReached(object sender, VlcMediaPlayerEndReachedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(_ => this.Stop());
            SetThreadExecutionState(ES_CONTINUOUS);
        }

        private void OnBuffering(object sender, VlcMediaPlayerBufferingEventArgs e)
        {
            Console.Write($"* ");
            _progressBar.BufforBarValue = e.NewCache;
        }

        public void Play()
        {
            if (_vlcControl.SourceProvider.MediaPlayer.IsPlaying())
            {
                _playerStatus = MediaPlayerStatus.Pause;
                this.Pause();
            }
            else
            {
                if (_media != null)
                {
                    _playerStatus = MediaPlayerStatus.Play;
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
            newPosition = _currentTime.TotalMilliseconds;
        }

        private void HandleMediaVolumeChanged(object sender, double newVolume)
        {
            newVolume = _vlcControl.SourceProvider.MediaPlayer.Audio.Volume;
        }

        private void _Play()
        {
            try
            {
                this.Media = _media;
                this.Media.SetPlayer(this);
                Console.WriteLine("Runing media: " + _media.Name + "\n[on handle] : " + _media.Player.Handle);
                _controlBar._mediaElementLabel.Content = _media.Name;
                _controlBar._timer.Content = TimeSpan.FromMilliseconds(_media.Position);
                _vlcControl.SourceProvider.MediaPlayer?.Play(_media.Uri);
                _isPlaying = true;
                _isPaused = false;
                _isStoped = false;
                // Log
                Logger.Log.Log(Logs.LogLevel.Info, "Console", $"Play media: {_media.Uri}; on handle: {_media.Player.Handle};");
                Logger.Log.Log(Logs.LogLevel.Info, "File", $"Play media: {_media.Uri}; on handle: {_media.Player.Handle};");
            }
            catch (Exception ex)
            {
                Logger.Log.Log(Logs.LogLevel.Error, "Console", $"Play media error: {_media.Uri}; on handle: {_media.Player.Handle} - {ex.Message}");
            }
        }

        public void Play(Media media)
        {
            if (_vlcControl.SourceProvider.MediaPlayer != null)
            {
                _media = media;
                _Play();
            }
        }

        public void Play(Uri uri)
        {
            if (_vlcControl.SourceProvider.MediaPlayer != null)
            {
                _media = new Media(uri.AbsoluteUri, this);
                _Play();
            }
        }

        public void Play(string path)
        {
            if (_vlcControl.SourceProvider.MediaPlayer != null)
            {
                _media = new Media(path, this);
                _Play();
            }
        }

        public void Stop()
        {
            try
            {
                ThreadPool.QueueUserWorkItem(_ => _vlcControl.SourceProvider.MediaPlayer?.Stop());
                SetThreadExecutionState(ES_CONTINUOUS);
                Logger.Log.Log(Logs.LogLevel.Debug, new[] { "File", "Console" }, $"{_vlcControl.SourceProvider.MediaPlayer.GetMedia().Title}");
                _playerStatus = MediaPlayerStatus.Stop;
                _isPlaying = false;
                _isPaused = false;
                _isStoped = true;
            }
            catch (Exception ex)
            {
                Logger.Log.Log(Logs.LogLevel.Error, new[] { "File", "Console" }, $"{ex.Message}");
                Logger.Log.Log(Logs.LogLevel.Error, new[] { "File", "Console" }, $"Stop media error: {_media.Uri}; on handle: {_media.Player.Handle} - {ex.Message}");
            }
        }

        public void Pause()
        {
            if (_vlcControl.SourceProvider.MediaPlayer != null)
            {
                _vlcControl.SourceProvider.MediaPlayer?.Pause();
                _playerStatus = MediaPlayerStatus.Pause;
                _isPlaying = false;
                _isPaused = true;
                _isStoped = false;
            }
        }

        public void Next()
        {
            _playlist.Next.Play();
        }

        public void Preview()
        {
            _playlist.GetTotalDuration();//.Items.MoveCurrentToPrevious();
            (_playlist.Current as Media).Play();
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
            if (!File.Exists(subtitle))
                subtitle = @"F:\Filmy\Futurama\Futurama S07E26 PL 720p.srt";

            _subtitleControl.FilePath = subtitle;
            _subtitleControl.TimeChanged += (sender, time) =>
            {
                if (_vlcControl.SourceProvider.MediaPlayer != null)
                {
                    _vlcControl.SourceProvider.MediaPlayer.Time = (long)time.TotalMilliseconds;
                }
            };
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
        /// Fullscreen mode
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

            if (window != null)
            {
                if (window.WindowStyle == WindowStyle.None)
                {
                    // Exit fullscreen
                    if (this._lastWindowStance != null)
                    {
                        window.ResizeMode = this._lastWindowStance.Mode;
                        window.WindowStyle = this._lastWindowStance.Style;
                        window.WindowState = this._lastWindowStance.State;

                        this._fullscreen = false;

                        Logger.Log.Log(Logs.LogLevel.Info, new string[] { "Console", "File" }, $"Exit fullscreen: Change video screen from fullscreen to last stance {this._lastWindowStance.State}");

                        return;
                    }
                    else
                    {
                        // Default
                        window.ResizeMode = ResizeMode.CanResize;
                        window.WindowStyle = WindowStyle.SingleBorderWindow;
                        window.WindowState = WindowState.Normal;

                        this._fullscreen = false;

                        Logger.Log.Log(Logs.LogLevel.Info, new string[] { "Console", "File" }, $"Exit fullscreen: Change video screen from fullscreen to default stance {this._lastWindowStance.State}");
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

            Logger.Log.Log(Logs.LogLevel.Info, new string[] { "Console", "File" }, $"Enter fullscreen: Change video screen to fullscreen from last stance {this._lastWindowStance.State}");
        }

        public void Dispose()
        {
            this.Stop();
            Logger.Log.Dispose();
            _vlcControl.Dispose();
        }

        private void _btnRefresh_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
