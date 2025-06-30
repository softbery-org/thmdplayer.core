// Version: 1.0.0.657
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ThmdPlayer.Core.Interfaces;
using ThmdPlayer.Core.medias;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Xml;
using System.Windows.Markup;
using System.Data.Common;
using ThmdPlayer.Core.helpers;
using ThmdPlayer.Core.Logs;
using Newtonsoft.Json.Linq;

namespace ThmdPlayer.Core.controls
{
    /// <summary>
    /// Logika interakcji dla klasy ControlBar.xaml
    /// </summary>
    public partial class ControlBar : UserControl
    {
        public static readonly List<string> DefaultSVGImage = new List<string>() { "svg/play.svg", "svg/stop.svg" };
        private IPlayer _player;
        private Storyboard _storyboard;
        private BackgroundWorker _backgroundWorker;
        private List<DrawingImage> _svgImages = new List<DrawingImage>();
        private List<Button> _controlBarButtons = new List<Button>();
        private AsyncLogger _logger = new AsyncLogger();

        private bool _isBarMoving = true;
        public bool isPopup { get; set; }
        public double Maximum { get; set; } = 100;
        public double Minimum { get; set; } = 0;
        public bool isBarMoving { get => _isBarMoving; set => _isBarMoving = value; }
        public List<DrawingImage> SVGImage { get => _svgImages; private set => _svgImages = value; }

        #region ControlBar Buttons
        public Button BtnPlay { get => _btnPlay; }
        public Button BtnStop { get => _btnStop; }
        public Button BtnNext { get => _btnNext; }
        public Button BtnPrevious { get => _btnPrevious; }
        public Button BtnVolumeUp { get => _btnVolumeUp; }
        public Button BtnVolumeDown { get => _btnVolumeDown; }
        public Button BtnMute { get => _btnMute; }
        public Button BtnSubtitle { get => _btnSubtitle; }
        public Button BtnOptions { get => _btnOptions; }
        public Button BtnUpdate { get => _btnUpdate; }
        public Button BtnOpen { get => _btnOpen; }
        public Button BtnPlaylist { get => _btnPlaylist; }
        public Button BtnInfo { get => _btnInfo; }
        public Button BtnVideoEditor { get => _btnVideoEditor; }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public ControlBar() : base()
        {
            InitializeComponent();
            
            if (_player == null)
            {
                _player = (IPlayer)this.Parent;

                _backgroundWorker = new BackgroundWorker();
                _backgroundWorker.DoWork += OnDoWork;
                _backgroundWorker.RunWorkerAsync();

                Button[] buttons = new Button[]
                {
                    _btnPlay,
                    _btnStop,
                    _btnNext,
                    _btnPrevious,
                    _btnVolumeUp,
                    _btnVolumeDown,
                    _btnMute,
                    _btnSubtitle,
                    _btnOptions,
                    _btnUpdate,
                    _btnOpen,
                    _btnPlaylist,
                    _btnInfo,
                    _btnVideoEditor
                };

                _controlBarButtons.AddRange(buttons);
            }
            
            _logger.AddSink(new CategoryFilterSink(
                   new FileSink("Logs", "log", new TextFormatter()), new[] { "File" }));
            _logger.AddSink(new CategoryFilterSink(
                new ConsoleSink(new TextFormatter()), new[] { "Console" }));
        }

        private void OnPlayerStatusChanged(object sender, MediaPlayerStatus e)
        {
            _logger.Log(LogLevel.Info, new[] { "File", "Console" }, $"OnPlayerStatusChanged");
            if (e == MediaPlayerStatus.Play)
            {
                _logger.Log(LogLevel.Info, new[] { "File", "Console" }, $"Play");
            }
            else if(e == MediaPlayerStatus.Pause)
            {
                _logger.Log(LogLevel.Info, new[] { "File", "Console" }, $"Pause");
            }
        }

        private async void OnDoWork(object sender, DoWorkEventArgs e)
        {
            var token = new CancellationTokenSource();
            CancellationToken ct = token.Token;

            var task = Task.Run(() =>
            {
                if (ct.IsCancellationRequested)
                    ct.ThrowIfCancellationRequested();
                else
                {
                    token.Cancel();
                    ct.ThrowIfCancellationRequested();
                }
            var val = true;
                while (val)
                {
                    if (!ct.IsCancellationRequested)
                    {
                        if (_player.Media != null)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                _mediaElementLabel.Content = _player.Media.Name;
                                _timer.Content = _player.CurrentTime;
                            });
                        }
                    }
                    else
                    {
                        val = false;
                        ct.ThrowIfCancellationRequested();
                    }
                }
            }, token.Token);
            token.Cancel();
            try
            {
                await task;
            }
            catch (OperationCanceledException ex)
            {
                _logger.Log(LogLevel.Error, new[] { "File", "Console" }, $"{nameof(OperationCanceledException)} {ex.Message}");
            }
            finally
            {
                task.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        public ControlBar(IPlayer player) : this()
        {
            _player = player;

            _volumeProgressBar.Value = _player.Volume;
            _volumeProgressBar.ProgressText = _player.Volume.ToString();

            _volumeProgressBar.Maximum = 100;
            _volumeProgressBar.Minimum = 0;
            _volumeProgressBar.MouseDown += _volumeProgressBar_MouseDown; ;
            _volumeProgressBar.MouseMove += _volumeProgressBar_MouseMove;
            _volumeProgressBar.MouseUp += _volumeProgressBar_MouseUp; ;

            this.MouseDown += helpers.SizeingMouseEventsHelper.OnControlMouseDown;
            this.MouseMove += helpers.SizeingMouseEventsHelper.OnControlMouseMove;
            this.MouseUp += helpers.SizeingMouseEventsHelper.OnControlMouseUp;

            _player.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        private void _volumeProgressBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void _volumeProgressBar_MouseMove(object sender, MouseEventArgs e)
        {
            var mouse = e.GetPosition(sender as ControlProgressBar);
            var value = (int)(mouse.X / ((ControlProgressBar)sender).ActualWidth * 100);
            _volumeProgressBar.PopupText = value.ToString();
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _volumeProgressBar_MouseMove(sender, e);
            }
        }

        private void _volumeProgressBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var mouse = e.GetPosition(sender as ControlProgressBar);
            var value = (int)(mouse.X / ((ControlProgressBar)sender).ActualWidth * 100);
            _volumeProgressBar.Value = value;
            _volumeProgressBar.ProgressText = value.ToString();
            _volumeProgressBar.PopupText = value.ToString();
            _player.Volume = value;
        }

        public void SetPlayer(IPlayer player)
        {
            _player = player;
        }

        private void _btnVolumeDown_Click(object sender, RoutedEventArgs e)
        {
            _player.Volume--;
            _volumeProgressBar.ProgressText = _player.Volume.ToString();
        }

        private void _btnOptions_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void _btnMute_Click(object sender, RoutedEventArgs e)
        {
            if (_player.Mute)
            {
                _player.Mute = false;
                _volumeProgressBar.ProgressText = _player.Volume.ToString();
                this._btnMute.Content = (object)this.FindResource("Unmute");
                _logger.Log(LogLevel.Info, new[] { "File", "Console" }, $"Unmute media");
            }
            else
            {
                _player.Mute = true;
                _volumeProgressBar.Value = _player.Mute ? 0 : _player.Volume;
                _volumeProgressBar.ProgressText = "Muted";
                this._btnMute.Content = (object)this.FindResource("Mute");
                _logger.Log(LogLevel.Info, new[] { "File", "Console" }, $"Mute media");
            }
        }

        private void _btnVolumeUp_Click(object sender, RoutedEventArgs e)
        {
            _player.Volume++;
            _volumeProgressBar.ProgressText = _player.Volume.ToString();
            _volumeProgressBar.Value = _player.Volume;
            _volumeProgressBar.PopupText = _player.Volume.ToString();
        }

        private void _btnSubtitle_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                _player.Subtitle(ofd.FileName);
            }
        }
        /*
        private void _btnRepeat_Click(object sender, RoutedEventArgs e)
        {
            if (_player.IsPlaying)
            {
                _player.Repeat = true;
                this._btnRepeat.Content = (object)this.FindResource("Repeat");
            }
            else
            {
                _player.Repeat = false;
                this._btnRepeat.Content = (object)this.FindResource("Unrepeat");
            }
        }
        private void _btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            _player.Previous();
        }
        private void _btnPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (_player.Playlist.Visibility == Visibility.Visible)
            {
                _player.Playlist.Visibility = Visibility.Hidden;
            }
            else
            {
                _player.Playlist.Visibility = Visibility.Visible;
            }
        }
        private void _btnPlaylistAdd_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                _player.Playlist.Add(new Media(ofd.FileName, _player));
            }
        }
        private void _btnPlaylistRemove_Click(object sender, RoutedEventArgs e)
        {
            if (_player.Playlist.SelectedItem != null)
            {
                _player.Playlist.Remove(_player.Playlist.SelectedItem);
            }
        }
        private void _btnPlaylistClear_Click(object sender, RoutedEventArgs e)
        {
            _player.Playlist.Clear();
        }
        private void _btnPlaylistMoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (_player.Playlist.SelectedItem != null)
            {
                _player.Playlist.MoveUp(_player.Playlist.SelectedItem);
            }
        }
        private void _btnPlaylistMoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (_player.Playlist.SelectedItem != null)
            {
                _player.Playlist.MoveDown(_player.Playlist.SelectedItem);
            }
        }
        private void _btnPlaylistPlay_Click(object sender, RoutedEventArgs e)
        {
            if (_player.Playlist.SelectedItem != null)
            {
                _player.Play(_player.Playlist.SelectedItem);
            }
        }
        private void _btnPlaylistPlayAll_Click(object sender, RoutedEventArgs e)
        {
            if (_player.Playlist.Items.Count > 0)
            {
                _player.Play(_player.Playlist.Items[0]);
            }
        }
        private void _btnPlaylistPlayNext_Click(object sender, RoutedEventArgs e)
        {
            if (_player.Playlist.SelectedItem != null)
            {
                _player.Play(_player.Playlist.SelectedItem);
            }
        }
        private void _btnPlaylistPlayPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_player.Playlist.SelectedItem != null)
            {
                _player.Play(_player.Playlist.SelectedItem);
            }
        }
        private void _btnPlaylistPlayRandom_Click(object sender, RoutedEventArgs e)
        {
            if (_player.Playlist.Items.Count > 0)
            {
                _player.Play(_player.Playlist.Items[0]);
            }
        }
        private void _btnPlaylistPlayRepeat_Click(object sender, RoutedEventArgs e)
        {
            if (_player.Playlist.Items.Count > 0)
            {
                _player.Play(_player.Playlist.Items[0]);
            }
        }
        private void _btnPlaylistPlayShuffle_Click(object sender, RoutedEventArgs e)
        {
            if (_player.Playlist.Items.Count > 0)
            {
                _player.Play(_player.Playlist.Items[0]);
            }
        }
        */
        private void _btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void _btnOpen_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                _player.Playlist.Add(new Media(ofd.FileName, _player));
            }
        }

        private void _btnStop_Click(object sender, RoutedEventArgs e)
        {
            _player.Stop();
        }

        private void _btnNext_Click(object sender, RoutedEventArgs e)
        {
            _player.Next();
        }

        private void _btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (_player.IsPlaying)
                _player.Pause();
            else
                _player.Play();
        }

        private void _btnPrieview_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MouseSleep_progressChanged(object sender, ProgressChangedEventArgs e)
        {
            
        }

        private void _btnPlaylistShowHide(object sender, RoutedEventArgs e)
        {
            if (_player.PlaylistView.Visibility == Visibility.Visible)
            {
                _player.PlaylistView.Visibility = Visibility.Hidden;
            }
            else
            {
                _player.PlaylistView.Visibility = Visibility.Visible;
            }
        }

        private void _btnCloseApp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Logger.Log.Log(Core.Logs.LogLevel.Error, new[] { "Console", "File" }, $"Error: {ex.Message}");
            }
        }

        private async void _btnHideControlBar_Click(object sender, RoutedEventArgs e)
        {
            await this.HideByStoryboard(_storyboard);
        }

        private void _btnInfo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _btnVideoEdit_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
