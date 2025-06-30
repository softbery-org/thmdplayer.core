// Version: 1.0.0.663
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ThmdPlayer.Core.Interfaces;
using ThmdPlayer.Core.medias;
using ThmdPlayer.Core.controls;
using System.IO;

namespace ThmdPlayer.Core.controls
{
    public partial class PlaylistView : ListView
    {
        /// <summary>
        /// Tracks the current index of media item in the playlist
        /// </summary>
        private int _currentIndex = 0;

        /// <summary>
        /// Shared playlist storage across all instances (Note: Static implementation)
        /// </summary>
        private medias.Playlist _playlist = new medias.Playlist("New list","");

        /// <summary>
        /// Event for requesting playlist data
        /// </summary>
        private static event Action GetPlaylist;

        /// <summary>
        /// Event triggered when playlist contents change
        /// </summary>
        private static event Action PlaylistChanged;

        private IPlayer _player;
        private GridView _gridView = new GridView();
        private List<GridViewColumn> _columns = new List<GridViewColumn>();

        /// <summary>
        /// Event for property change notifications
        /// </summary>
        public static event PropertyChangedEventHandler PropertyChanged;

        public int CurrentIndex
        {
            get
            {
                // Zoptymalizowany dostęp do Items.Count
                int itemCount = this.Items.Count;
                if (_currentIndex >= itemCount) _currentIndex = 0;
                if (_currentIndex < 0) _currentIndex = itemCount - 1;
                return _currentIndex;
            }
            set => _currentIndex = value;
        }

        /// <summary>
        /// Gets next index with wrap-around
        /// </summary>
        public int NextIndex
        {
            get
            {
                // Zoptymalizowany dostęp do Items.Count
                return _currentIndex == this.Items.Count - 1 ? 0 : _currentIndex + 1;
            }
        }

        /// <summary>
        /// Gets previous index with wrap-around
        /// </summary>
        public int PreviousIndex
        {
            get
            {
                // Zoptymalizowany dostęp do Items.Count
                return _currentIndex == 0 ? this.Items.Count - 1 : _currentIndex - 1;
            }
        }

        /// <summary>
        /// Gets next media item
        /// </summary>
        public Media Next
        {
            get
            {
                // Zoptymalizowany dostęp do NextIndex i Items
                return GetMediaFromListItem(_playlist[NextIndex] as ListViewItem);
            }
        }

        /// <summary>
        /// Gets previous media item
        /// </summary>
        public Media Previous
        {
            get
            {
                // Zoptymalizowany dostęp do PreviousIndex i Items
                return GetMediaFromListItem(_playlist[PreviousIndex] as ListViewItem);
            }
        }

        /// <summary>
        /// Moves to and returns next media item
        /// </summary>
        public Media MoveNext
        {
            get
            {
                _currentIndex = NextIndex;
                // Zoptymalizowany dostęp do NextIndex i Items
                return GetMediaFromListItem(_playlist[NextIndex] as ListViewItem);
            }
        }

        /// <summary>
        /// Moves to and returns previous media item
        /// </summary>
        public Media MovePrevious
        {
            get
            {
                _currentIndex = PreviousIndex;
                // Zoptymalizowany dostęp do NextIndex i Items
                return GetMediaFromListItem(_playlist[NextIndex] as ListViewItem);
            }
        }

        /// <summary>
        /// Gets current media item
        /// </summary>
        public Media Current
        {
            get
            {
                // Zoptymalizowany dostęp do CurrentIndex i Items
                return GetMediaFromListItem(this.Items[CurrentIndex] as ListViewItem);
            }
        }

        /// <summary>
        /// Gets or sets the background color of the PlaylistView
        /// </summary>
        public new System.Windows.Media.SolidColorBrush Background { get; set; } = new System.Windows.Media.SolidColorBrush(new System.Windows.Media.Color() { A = 95, R = 25, G = 55, B = 15 });

        /// <summary>
        /// Gets the GridView used for displaying media items in a grid format
        /// </summary>
        public GridView GridView
        {
            get => _gridView;
        }

        /// <summary>
        /// Indexer to access media items by index
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Media this[int id]
        {
            get => this.Items[id] as Media;
            set => this.Items[id] = value;
        }

        /// <summary>
        /// Constructor for PlaylistView
        /// </summary>
        public PlaylistView()
        {
            this.Width = 300;
            this.Height = 200;
            this.MinWidth = 150;
            this.MinHeight = 100;

            this.Background = new SolidColorBrush(new Color() { A = 30, R = 128, G = 128, B = 128 });

            this.ScrollIntoView(this);
            this.BorderThickness = new Thickness(2);

            this.HorizontalAlignment = HorizontalAlignment.Left;
            this.VerticalAlignment = VerticalAlignment.Top;

            var r = new Core.helpers.ResizeControlHelper(this);//Core.helpers.SizeingMouseEventsHelper.OnControlMouseDown;
            //this.MouseUp += Core.helpers.SizeingMouseEventsHelper.OnControlMouseUp;
            //this.MouseMove += Core.helpers.SizeingMouseEventsHelper.OnControlMouseMove;
            //this.MouseDoubleClick += PlaylistView_MouseDoubleClick;

            PlaylistChanged += OnPlaylistChanged;
            GetPlaylist += OnGetPlaylist;

            // Rozważenie ObservableCollection
            //_playlist.Items = new ObservableCollection<ListViewItem>();
        }

        private void PlaylistView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Logger.Log.Log(Core.Logs.LogLevel.Info, new string[] { "Console" }, "PlaylistView mouse double click event triggered.");
        }

        /// <summary>
        /// Constructor for PlaylistView with initial media items
        /// </summary>
        /// <param name="Medias"></param>
        public PlaylistView(params object[] Medias) : this()
        {
            try
            {
                this.Add(Medias);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Sets the player instance for the playlist view
        /// </summary>
        /// <param name="player"></param>
        public void SetPlayer(IPlayer player)
        {
            _player = player;
        }

        /// <summary>
        /// Sets the columns for the GridView used in the playlist view
        /// </summary>
        /// <param name="columns"></param>
        public void SetGridColumns(string[] columns)
        {
            foreach (var item in columns)
            {
                GridViewColumn column = new GridViewColumn();
                column.Header = item;
                column.DisplayMemberBinding = new Binding(item);
                column.Width = item.Length * 10;
                Logger.Log.Log(Core.Logs.LogLevel.Info, "Console", $"Adding column {column} for playlist.");
                this._gridView.Columns.Add(column);
            }

            this.View = this.GridView;
        }

        private ListViewItem CreateListItem(Media media)
        {
            try
            {
                var item = new ListViewItem();
                if (_player.Media == null)
                {
                    _player.Media = media;
                }

                item.Content = media;
                item.MouseDoubleClick += OnPlaylistItemMouseDoubleClick;
                item.MouseDown += OnListItemMouseClick;

                Logger.Log.Log(Core.Logs.LogLevel.Info, "Console", $"Add media: {media.Name} to playlist.");

                return item;
            }
            catch (Exception ex)
            {
                Logger.Log.Log(Core.Logs.LogLevel.Error, "Console", $"{ex.Message}");
                Logger.Log.Log(Logs.LogLevel.Warning, new[] { "Console", "File" }, $"Create file:");
                
                if (!File.Exists("download/local_copy.m3u8"))
                {
                    File.Create("download/local_copy.m3u8");
                }

                return null;
            }
        }

        private Media GetMediaFromListItem(ListViewItem item)
        {
            var media = item.Content as Media;
            return media;
        }

        private void OnPlaylistChanged()
        {
            if (PlaylistChanged != null)
            {
                PlaylistChanged?.Invoke();
            }
        }

        private void OnGetPlaylist()
        {
            _playlist = (medias.Playlist)this.ItemsSource.Cast<Playlist>();
        }

        private static void OnPropertyChanged<T>(string propertyName, ref T field, T value)
        {
            if ((field == null && value != null) || (field != null && !field.Equals(value)))
            {
                field = value;
                PropertyChanged?.Invoke(field, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void OnPlaylistItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.SelectedItem != null)
            {
                var item = this.SelectedItem as ListViewItem;
                var media = GetMediaFromListItem(item);
                _player.Media = media;
                _player.Play(media);
                _currentIndex = this.SelectedIndex;
            }
        }

        private void OnListItemMouseClick(object sender, MouseButtonEventArgs e)
        {
            if (this.SelectedItem != null && e.RightButton == MouseButtonState.Pressed)
            {
                // Todo: Uruchom metode np. prawy przycisk myszki, wyświetl context menu
            }
        }

        private void OnControlClose(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Clears all media items
        /// </summary>
        public void ClearTracks()
        {
            this.Items.Clear();
        }

        /// <summary>
        /// Checks if playlist contains media item
        /// </summary>
        /// <param name="media">media item to check</param>
        /// <returns>True if item exists</returns>
        public bool Contains(Media media)
        {
            return this.Items.Contains(media);
        }

        /// <summary>
        /// Removes media item from playlist
        /// </summary>
        /// <param name="media">media item to remove</param>
        /// <returns>Removed media item</returns>
        public Media Remove(Media media)
        {
            Items.Remove(media);
            return media;
        }

        /// <summary>
        /// Creates new empty playlist
        /// </summary>
        /// <remarks>
        /// Currently ignores name and description parameters
        /// </remarks>
        /// <param name="name">PlaylistView name (unused)</param>
        /// <param name="description">PlaylistView description (unused)</param>
        public void New(string name, string description)
        {
            ClearTracks();
        }

        public void Add(Media media)
        {
            var m = media;
            _currentIndex++;
            Items.Add(CreateListItem(m));
        }

        /// <summary>
        /// Adds multiple media items to the playlist
        /// </summary>
        /// <param name="medias"></param>
        public void Add(Media[] medias)
        {
            foreach (var media in medias)
            {
                var m = media;
                _currentIndex++;
                Items.Add(CreateListItem(m));
            }
        }

        /// <summary>
        /// Adds multiple media items to the playlist using params
        /// </summary>
        /// <param name="medias"></param>
        public void Add(params object[] medias)
        {
            if (medias == null) return;

            Parallel.ForEach(medias, media =>
            {
                if (media is Media m)
                {
                    // Użycie Dispatcher.Invoke, jeśli aktualizacja UI jest wymagana
                     this.Dispatcher.Invoke(() =>
                     {
                         _currentIndex++;
                         Items.Add(CreateListItem(m));
                    });
                }
            });
        }
    }
}
