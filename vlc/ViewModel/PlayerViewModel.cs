// Version: 1.0.0.585
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.IO;
using LibVLCSharp.Shared;

namespace ThmdPlayer.Core.vlc.ViewModel
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }


    public class PlayerViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IVideoPlayer _player;
        private bool _isDisposed = false;
        private bool _isDraggingSlider = false; // Flaga do obs�ugi przeci�gania suwaka

        // --- W�a�ciwo�ci publiczne do bindowania ---

        // Bezpo�redni dost�p do playlisty z odtwarzacza
        // Potrzebujemy ObservableCollection, a IVideoPlayer zwraca IReadOnlyList<string>
        // Musimy stworzy� w�asn� kolekcj� i synchronizowa� lub zmodyfikowa� IVideoPlayer
        // Podej�cie 1: W�asna kolekcja (wymaga wi�cej synchronizacji)
        // Podej�cie 2: Zmiana IVideoPlayer (prostsze w tym przypadku)
        // Zmie�my IVideoPlayer.Playlist na ObservableCollection<string> (lub dodajmy nowe zdarzenie informuj�ce o zmianach)
        // ====> Zak�adaj�c, �e IVideoPlayer.Playlist zwraca ObservableCollection<string> (wymaga zmiany w interfejsie i klasie!)
        // Je�li nie chcesz zmienia� IVideoPlayer, musia�by� zasubskrybowa� zdarzenia dodania/usuni�cia/wyczyszczenia
        // i r�cznie aktualizowa� lokaln� ObservableCollection.

        // Za��my, �e zmodyfikowano IVideoPlayer, aby Playlist zwraca� ObservableCollection<string>
        // Je�li nie, to trzeba b�dzie doda� logik� synchronizacji.
        public ObservableCollection<string> Playlist { get; } // Zmieniono z IReadOnlyList

        private int _selectedPlaylistIndex = -1;
        public int SelectedPlaylistIndex
        {
            get => _selectedPlaylistIndex;
            set => SetProperty(ref _selectedPlaylistIndex, value); // Aktualizuj powi�zan� kontrolk�
        }

        // Skr�ty nazw plik�w dla lepszego wy�wietlania w ListBox
        public ObservableCollection<string> PlaylistDisplayNames { get; } = new ObservableCollection<string>();


        private TimeSpan _currentTime;
        public TimeSpan CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        private int _volume;
        public int Volume
        {
            get => _volume;
            set
            {
                if (SetProperty(ref _volume, value))
                {
                    _player.Volume = value; // Zaktualizuj g�o�no�� w odtwarzaczu
                }
            }
        }

        private bool _isMuted;
        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                if (SetProperty(ref _isMuted, value))
                {
                    _player.IsMuted = value;
                }
            }
        }

        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            private set => SetProperty(ref _isPlaying, value); // Prywatny setter, aktualizowany przez zdarzenia
        }

        private PlayerState _currentState;
        public PlayerState CurrentState
        {
            get => _currentState;
            private set => SetProperty(ref _currentState, value);
        }

        // W�a�ciwo�� tylko do odczytu dla VideoView
        public MediaPlayer MediaPlayer => _player.MediaPlayer;

        // --- Komendy ---
        public ICommand PlayCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand AddFilesCommand { get; }
        public ICommand PlaySelectedCommand { get; }
        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand RemoveSelectedCommand { get; }
        public ICommand SeekCommand { get; } // Komenda do przewijania (u�ywana przez suwak)
        public ICommand StartSeekCommand { get; } // Rozpocz�cie przeci�gania suwaka
        public ICommand EndSeekCommand { get; }   // Zako�czenie przeci�gania suwaka


        // --- Konstruktor ---
        public PlayerViewModel(IVideoPlayer player)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));

            // --- Inicjalizacja Playlisty ---
            // Je�li IVideoPlayer.Playlist to IReadOnlyList<string>
            // Playlist = new ObservableCollection<string>(_player.Playlist);
            // Trzeba by doda� obs�ug� zdarze� dodawania/usuwania/czyszczenia z _player

            // Za�o�enie: IVideoPlayer.Playlist to ObservableCollection<string>
            Playlist = _player.Playlist as ObservableCollection<string>; // Rzutowanie
            if (Playlist == null)
            {
                throw new InvalidOperationException("IVideoPlayer.Playlist must return ObservableCollection<string> for this ViewModel");
            }
            // Synchronizuj wy�wietlane nazwy przy zmianach w kolekcji bazowej
            Playlist.CollectionChanged += (s, e) => UpdatePlaylistDisplayNames();
            UpdatePlaylistDisplayNames(); // Inicjalna synchronizacja


            // --- Inicjalizacja W�a�ciwo�ci ---
            Volume = _player.Volume;
            IsMuted = _player.IsMuted;
            IsPlaying = _player.IsPlaying;
            CurrentState = _player.CurrentState;
            SelectedPlaylistIndex = _player.CurrentPlaylistIndex;


            // --- Subskrypcja Zdarze� Odtwarzacza ---
            // U�ywamy Dispatcher.Invoke do aktualizacji UI z w�tku odtwarzacza
            _player.PositionChanged += (s, time) => DispatcherInvoke(() => {
                if (!_isDraggingSlider) CurrentTime = time; // Aktualizuj tylko, je�li nie przeci�gamy
            });
            _player.MediaLoaded += (s, e) => DispatcherInvoke(() => Duration = _player.Duration);
            _player.PlaybackStarted += (s, e) => DispatcherInvoke(() => { IsPlaying = true; CurrentState = _player.CurrentState; });
            _player.PlaybackPaused += (s, e) => DispatcherInvoke(() => { IsPlaying = false; CurrentState = _player.CurrentState; });
            _player.PlaybackStopped += (s, e) => DispatcherInvoke(() => { IsPlaying = false; CurrentState = _player.CurrentState; CurrentTime = TimeSpan.Zero; });
            _player.PlaybackEnded += (s, e) => DispatcherInvoke(() => { IsPlaying = false; CurrentState = _player.CurrentState; /* CurrentTime mo�e by� bliskie Duration */; });
            _player.PlaylistItemChanged += (s, index) => DispatcherInvoke(() => SelectedPlaylistIndex = index); // Aktualizuj zaznaczenie w ListBox
            _player.ErrorOccurred += (s, msg) => DispatcherInvoke(() => MessageBox.Show(msg, "B��d odtwarzacza", MessageBoxButton.OK, MessageBoxImage.Error));


            // --- Inicjalizacja Komend ---
            PlayCommand = new RelayCommand(_ => _player.Play(), _ => CanPlay());
            PauseCommand = new RelayCommand(_ => _player.Pause(), _ => CanPause());
            StopCommand = new RelayCommand(_ => _player.Stop(), _ => CanStop());
            AddFilesCommand = new RelayCommand(_ => AddFiles());
            PlaySelectedCommand = new RelayCommand(PlaySelectedItem, _ => SelectedPlaylistIndex >= 0);
            NextCommand = new RelayCommand(_ => _player.PlayNext(), _ => Playlist.Count > 0);
            PreviousCommand = new RelayCommand(_ => _player.PlayPrevious(), _ => Playlist.Count > 0);
            RemoveSelectedCommand = new RelayCommand(RemoveSelectedItem, _ => SelectedPlaylistIndex >= 0);

            // Komendy suwaka
            StartSeekCommand = new RelayCommand(_ => _isDraggingSlider = true);
            EndSeekCommand = new RelayCommand(param => {
                if (_isDraggingSlider && param is double seconds)
                {
                    _player.SeekTo(TimeSpan.FromSeconds(seconds));
                }
                _isDraggingSlider = false;
            });
            // SeekCommand (nieu�ywane bezpo�rednio przez suwak, ale mo�e by� przydatne)
            SeekCommand = new RelayCommand(param => {
                if (param is double seconds)
                {
                    _player.SeekTo(TimeSpan.FromSeconds(seconds));
                }
            }, _ => CanSeek());
        }

        // --- Metody Pomocnicze dla Komend ---

        private bool CanPlay() => !_player.IsPlaying && (_player.MediaPlayer?.Media != null || Playlist.Count > 0);
        private bool CanPause() => _player.IsPlaying;
        private bool CanStop() => _player.IsPlaying || _player.CurrentState == PlayerState.Paused;
        private bool CanSeek() => _player.Duration > TimeSpan.Zero;


        private void AddFiles()
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Wybierz pliki multimedialne",
                Filter = "Pliki multimedialne|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.mp3;*.wav;*.flac;*.ogg|Wszystkie pliki|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    _player.AddToPlaylist(filename);
                }
                // Nie trzeba r�cznie aktualizowa� Playlist, bo jest to ta sama ObservableCollection
                // UpdatePlaylistDisplayNames(); // Wywo�a si� automatycznie przez CollectionChanged
            }
        }

        private void PlaySelectedItem(object parameter)
        {
            if (SelectedPlaylistIndex >= 0 && SelectedPlaylistIndex < Playlist.Count)
            {
                _player.PlayPlaylistItem(SelectedPlaylistIndex);
            }
        }

        private void RemoveSelectedItem(object parameter)
        {
            if (SelectedPlaylistIndex >= 0 && SelectedPlaylistIndex < Playlist.Count)
            {
                int indexToRemove = SelectedPlaylistIndex; // Zapisz indeks przed potencjaln� zmian�
                _player.RemoveFromPlaylist(indexToRemove);
                // Aktualizacja SelectedIndex powinna nast�pi� przez zdarzenie PlaylistItemChanged
                // lub przez r�czne ustawienie po usuni�ciu, je�li logika VlcVideoPlayer tego nie robi.
                // UpdatePlaylistDisplayNames(); // Wywo�a si� automatycznie przez CollectionChanged
            }
        }

        // --- Aktualizacja Nazw Wy�wietlanych ---
        private void UpdatePlaylistDisplayNames()
        {
            PlaylistDisplayNames.Clear();
            foreach (var path in Playlist)
            {
                try
                {
                    PlaylistDisplayNames.Add(Path.GetFileName(path));
                }
                catch
                {
                    PlaylistDisplayNames.Add(path); // W razie b��du poka� pe�n� �cie�k�/URL
                }
            }
        }


        // --- Obs�uga W�tk�w UI ---
        private void DispatcherInvoke(Action action)
        {
            Application.Current?.Dispatcher.Invoke(action);
        }

        // --- INotifyPropertyChanged ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // --- IDisposable ---
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                // Odsubskrybuj zdarzenia, aby unikn�� wyciek�w
                // (W tym przyk�adzie _player jest przekazywany z zewn�trz,
                // wi�c ViewModel nie powinien go Dispose'owa�, chyba �e jest jego w�a�cicielem)
                // Je�li ViewModel tworzy _player, powinien go tu Dispose'owa�:
                // _player?.Dispose();

                // Je�li u�ywali�my w�asnej ObservableCollection, odsubskrybuj zdarzenia
                // Playlist.CollectionChanged -= ...
            }
            _isDisposed = true;
        }
    }
}
