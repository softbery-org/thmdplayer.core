// Version: 1.0.0.585
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.vlc
{
    /// <summary>
    /// Implementacja odtwarzacza wideo wykorzystuj�ca LibVLCSharp.
    /// </summary>
    public class VlcVideoPlayer : IVideoPlayer // Implementuje interfejs IVideoPlayer i IDisposable
    {
        private readonly LibVLC _libVLC;
        private readonly MediaPlayer _mediaPlayer;
        private Media _currentMedia;
        private PlayerState _currentState = PlayerState.Idle;
        private bool _isDisposed = false;

        /// <summary>
        /// Inicjalizuje nową instancję VlcVideoPlayer.
        /// Wymaga zainicjalizowania Core.Initialize() przed użyciem.
        /// </summary>
        public VlcVideoPlayer()
        {
            // Upewnij się, że LibVLC jest zainicjalizowane (najlepiej zrobił to raz na start aplikacji)
            // Core.Initialize(); // Odkomentuj, jeśli nie robisz tego gdzie indziej

            _libVLC = new LibVLC(); // można przekazać opcje, np. ścieżki do pluginów
            _mediaPlayer = new MediaPlayer(_libVLC);

            // Subskrypcja zdarze� MediaPlayer z LibVLCSharp
            _mediaPlayer.Playing += MediaPlayer_Playing;
            _mediaPlayer.Paused += MediaPlayer_Paused;
            _mediaPlayer.Stopped += MediaPlayer_Stopped;
            _mediaPlayer.EndReached += MediaPlayer_EndReached;
            _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
            _mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
            _mediaPlayer.LengthChanged += MediaPlayer_LengthChanged;
            _mediaPlayer.MediaChanged += MediaPlayer_MediaChanged;
            _mediaPlayer.Buffering += MediaPlayer_Buffering;
            _mediaPlayer.Opening += MediaPlayer_Opening;
        }

        // --- Implementacja IVideoPlayer ---

        public void LoadMedia(string mediaPathOrUrl)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(VlcVideoPlayer));
            if (string.IsNullOrEmpty(mediaPathOrUrl)) throw new ArgumentNullException(nameof(mediaPathOrUrl));

            // Zatrzymaj poprzednie odtwarzanie i zwolnij stare media
            StopInternal();
            _currentMedia?.Dispose();

            try
            {
                _currentMedia = new Media(_libVLC, new Uri(mediaPathOrUrl));
                // Opcjonalnie: _currentMedia.AddOption(":no-video"); // dla audio
                _mediaPlayer.Media = _currentMedia;
                // Stan zmieni się na Opening -> Buffering -> Playing (jeśli auto-play) lub Paused/Stopped
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Błąd ładowania medium '{mediaPathOrUrl}': {ex.Message}");
                _currentMedia?.Dispose();
                _currentMedia = null;
                _mediaPlayer.Media = null; // Upewnij się, że media player nie ma referencji
            }
        }

        public void Play()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(VlcVideoPlayer));
            if (_mediaPlayer.Media != null)
            {
                _mediaPlayer.Play();
                // Stan zostanie zaktualizowany przez zdarzenie MediaPlayer_Playing
            }
        }

        public void Pause()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(VlcVideoPlayer));
            if (_mediaPlayer.CanPause)
            {
                _mediaPlayer.Pause();
                // Stan zostanie zaktualizowany przez zdarzenie MediaPlayer_Paused
            }
        }

        public void Stop()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(VlcVideoPlayer));
            StopInternal();
        }

        private void StopInternal()
        {
            if (_mediaPlayer.IsPlaying || _mediaPlayer.State == VLCState.Paused)
            {
                _mediaPlayer.Stop(); // To wywoła zdarzenie MediaPlayer_Stopped
            }
            // Reset pozycji jest zwykle obsługiwany przez samo Stop() w VLC
            _mediaPlayer.Time = 0; // można dodać, jeśli potrzebne jawne resetowanie
        }


        public void SeekTo(TimeSpan time)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(VlcVideoPlayer));
            if (_mediaPlayer.Media != null && _mediaPlayer.IsSeekable)
            {
                _mediaPlayer.Time = (long)time.TotalMilliseconds;
            }
        }

        public TimeSpan CurrentTime
        {
            get => TimeSpan.FromMilliseconds(_mediaPlayer.Time);
            set => SeekTo(value);
        }

        // Długość jest aktualizowana przez zdarzenie LengthChanged
        public TimeSpan Duration => TimeSpan.FromMilliseconds(_mediaPlayer.Length);

        public int Volume
        {
            get => _mediaPlayer.Volume;
            set
            {
                if (_isDisposed) throw new ObjectDisposedException(nameof(VlcVideoPlayer));
                // Ogranicz wartość do zakresu 0-100 (lub innego obsługiwanego przez VLC, często 0-200)
                _mediaPlayer.Volume = Math.Max(0, Math.Min(value, 100));
            }
        }

        public bool IsMuted
        {
            get => _mediaPlayer.Mute;
            set
            {
                if (_isDisposed) throw new ObjectDisposedException(nameof(VlcVideoPlayer));
                _mediaPlayer.Mute = value;
            }
        }

        public bool IsPlaying => _mediaPlayer.IsPlaying;

        // Aktualny stan jest zarządzany przez handlery zdarzeń
        public PlayerState CurrentState => _currentState;

        public float PlaybackRate
        {
            get => _mediaPlayer.Rate;
            set
            {
                if (_isDisposed) throw new ObjectDisposedException(nameof(VlcVideoPlayer));
                if (_mediaPlayer.Media != null)
                {
                    _mediaPlayer.SetRate(value);
                }
            }
        }

        public IReadOnlyList<string> Playlist => throw new NotImplementedException();

        public int CurrentPlaylistIndex { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public MediaPlayer MediaPlayer => throw new NotImplementedException();


        // --- Zdarzenia interfejsu ---
        #region Events
        public event EventHandler MediaLoaded;
        public event EventHandler PlaybackStarted;
        public event EventHandler PlaybackPaused;
        public event EventHandler PlaybackStopped;
        public event EventHandler PlaybackEnded;
        public event EventHandler<TimeSpan> PositionChanged;
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler<int> PlaylistItemChanged;
        #endregion

        // --- Handlery zdarzeń MediaPlayer ---
        #region Event Handlers
        private void MediaPlayer_Opening(object sender, EventArgs e)
        {
            UpdateState(PlayerState.Opening);
        }

        private void MediaPlayer_Buffering(object sender, MediaPlayerBufferingEventArgs e)
        {
            // Możesz doda� logik� np. do wy�wietlania wska�nika buforowania
            // e.NewCache zawiera procent buforowania (0-100)
            UpdateState(PlayerState.Buffering);
        }

        private void MediaPlayer_MediaChanged(object sender, MediaPlayerMediaChangedEventArgs e)
        {
            // Wywo�ywane po przypisaniu nowego obiektu Media do MediaPlayer
            // Dobre miejsce do resetowania stanu, jeśli potrzebne
            UpdateState(PlayerState.Idle); // Pocz�tkowy stan po zmianie medium
                                           // _currentMedia = e.Media; // Aktualizacja referencji, jeśli zarz�dzana tutaj
        }

        private void MediaPlayer_LengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            // D�ugo�� jest dost�pna, medium prawdopodobnie za�adowane
            // Czasami może by� wywo�ane wielokrotnie
            if (_currentState != PlayerState.Playing && _currentState != PlayerState.Paused) // Unikaj wywo�ywania po rozpocz�ciu odtwarzania
            {
                OnMediaLoaded();
            }
        }

        private void MediaPlayer_Playing(object sender, EventArgs e)
        {
            UpdateState(PlayerState.Playing);
            PlaybackStarted?.Invoke(this, EventArgs.Empty);
        }

        private void MediaPlayer_Paused(object sender, EventArgs e)
        {
            UpdateState(PlayerState.Paused);
            PlaybackPaused?.Invoke(this, EventArgs.Empty);
        }

        private void MediaPlayer_Stopped(object sender, EventArgs e)
        {
            // Sprawd�, czy zatrzymanie nie jest spowodowane ko�cem pliku
            if (_currentState != PlayerState.Ended)
            {
                UpdateState(PlayerState.Stopped);
                PlaybackStopped?.Invoke(this, EventArgs.Empty);
                PositionChanged?.Invoke(this, TimeSpan.Zero); // Resetuj pozycj� w UI
            }
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            UpdateState(PlayerState.Ended);
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
            // Po EndReached stan VLC często wraca do Idle/Stopped
            // Możemy jawnie ustawi� Stopped, jeśli taka logika jest preferowana
            // UpdateState(PlayerState.Stopped);
        }

        private void MediaPlayer_EncounteredError(object sender, EventArgs e)
        {
            // LibVLC nie zawsze dostarcza szczegłowych informacji o błędzie tutaj
            // można spr�bowa� pobra� ostatni Błąd z log�w LibVLC, jeśli s� w��czone
            OnErrorOccurred("Wyst�pi� nieokre�lony Błąd odtwarzacza VLC.");
        }

        private void MediaPlayer_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            // e.Time zawiera czas w milisekundach
            PositionChanged?.Invoke(this, TimeSpan.FromMilliseconds(e.Time));
        }
        #endregion


        // --- Metody pomocnicze ---
        #region Helper Methods
        private void UpdateState(PlayerState newState)
        {
            if (_currentState != newState)
            {
                _currentState = newState;
                // Tutaj można doda� logowanie zmiany stanu, jeśli potrzebne
                // Console.WriteLine($"Player State Changed: {newState}");
            }
        }

        private void OnMediaLoaded()
        {
            // Wywołaj zdarzenie tylko raz po załadowaniu medium
            // Można dodać flagę, aby upewnić się, że jest wywoływane tylko raz na medium
            MediaLoaded?.Invoke(this, EventArgs.Empty);
            // Po załadowaniu, stan często staje się Paused lub Stopped, zależnie od konfiguracji VLC
            // Ustal stan na podstawie aktualnego stanu MediaPlayer
            UpdateState(VlcStateToPlayerState(_mediaPlayer.State));
        }

        private void OnErrorOccurred(string errorMessage)
        {
            UpdateState(PlayerState.Error);
            ErrorOccurred?.Invoke(this, errorMessage);
        }
        #endregion

        // Konwersja stanu VLC na nasz enum PlayerState
        private PlayerState VlcStateToPlayerState(VLCState vlcState)
        {
            switch (vlcState)
            {
                case VLCState.NothingSpecial:
                    return PlayerState.Idle;
                case VLCState.Opening:
                    return PlayerState.Opening;
                case VLCState.Buffering:
                    return PlayerState.Buffering;
                case VLCState.Playing:
                    return PlayerState.Playing;
                case VLCState.Paused:
                    return PlayerState.Paused;
                case VLCState.Stopped:
                    return PlayerState.Stopped;
                case VLCState.Ended:
                    return PlayerState.Ended;
                case VLCState.Error:
                    return PlayerState.Error;
                default:
                    return PlayerState.Idle; // Domy�lny stan na wszelki wypadek
            }
        }

        // --- Implementacja IDisposable ---

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Zapobiegaj wywo�aniu finalizatora
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                // Zwolnij zarz�dzane zasoby

                // Odsubskrybuj zdarzenia, aby unikn�� wyciek�w pami�ci
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Stop(); // Najpierw zatrzymaj
                    _mediaPlayer.Playing -= MediaPlayer_Playing;
                    _mediaPlayer.Paused -= MediaPlayer_Paused;
                    _mediaPlayer.Stopped -= MediaPlayer_Stopped;
                    _mediaPlayer.EndReached -= MediaPlayer_EndReached;
                    _mediaPlayer.EncounteredError -= MediaPlayer_EncounteredError;
                    _mediaPlayer.TimeChanged -= MediaPlayer_TimeChanged;
                    _mediaPlayer.LengthChanged -= MediaPlayer_LengthChanged;
                    _mediaPlayer.MediaChanged -= MediaPlayer_MediaChanged;
                    _mediaPlayer.Buffering -= MediaPlayer_Buffering;
                    _mediaPlayer.Opening -= MediaPlayer_Opening;

                    _mediaPlayer.Dispose();
                }

                _currentMedia?.Dispose();
                _libVLC?.Dispose();
            }

            // Zwolnij niezarz�dzane zasoby (jeśli istniej�) - LibVLCSharp robi to wewn�trznie

            _isDisposed = true;
        }

        public void AddToPlaylist(string mediaPathOrUrl)
        {
            throw new NotImplementedException();
        }

        public void RemoveFromPlaylist(int index)
        {
            throw new NotImplementedException();
        }

        public void ClearPlaylist()
        {
            throw new NotImplementedException();
        }

        public void PlayPlaylistItem(int index)
        {
            throw new NotImplementedException();
        }

        public void PlayNext()
        {
            throw new NotImplementedException();
        }

        public void PlayPrevious()
        {
            throw new NotImplementedException();
        }

        // Finalizator (opcjonalny, ale dobry zwyczaj dla klas z IDisposable)
        ~VlcVideoPlayer()
        {
            Dispose(false);
        }
    }
}
