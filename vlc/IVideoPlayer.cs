// Version: 1.0.0.597
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.vlc
{
    /// <summary>
    /// Definiuje stan odtwarzacza wideo.
    /// </summary>
    public enum PlayerState
    {
        Idle,      // Nic nie za�adowano lub zatrzymano
        Opening,   // �adowanie medi�w
        Buffering, // Buforowanie
        Playing,   // Odtwarzanie
        Paused,    // Wstrzymano
        Stopped,   // Zatrzymano (ale media mog� by� za�adowane)
        Ended,     // Koniec odtwarzania
        Error      // Wyst�pi� b��d
    }

    /// <summary>
    /// Interfejs definiuj�cy podstawowe funkcjonalno�ci odtwarzacza wideo.
    /// </summary>
    public interface IVideoPlayer : IDisposable
    {
        /// <summary>
        /// �aduje medium (plik lokalny lub URL).
        /// </summary>
        /// <param name="mediaPathOrUrl">�cie�ka do pliku lub adres URL.</param>
        void LoadMedia(string mediaPathOrUrl);

        /// <summary>
        /// Rozpoczyna lub wznawia odtwarzanie.
        /// </summary>
        void Play();

        /// <summary>
        /// Wstrzymuje odtwarzanie.
        /// </summary>
        void Pause();

        /// <summary>
        /// Zatrzymuje odtwarzanie i resetuje pozycj�.
        /// </summary>
        void Stop();

        /// <summary>
        /// Przewija do okre�lonego momentu w czasie.
        /// </summary>
        /// <param name="time">Docelowy czas.</param>
        void SeekTo(TimeSpan time);

        /// <summary>
        /// Pobiera lub ustawia aktualny czas odtwarzania.
        /// Ustawienie warto�ci jest r�wnoznaczne z wywo�aniem SeekTo.
        /// </summary>
        TimeSpan CurrentTime { get; set; }

        /// <summary>
        /// Pobiera ca�kowity czas trwania za�adowanego medium.
        /// </summary>
        TimeSpan Duration { get; }

        /// <summary>
        /// Pobiera lub ustawia poziom g�o�no�ci (0-100).
        /// </summary>
        int Volume { get; set; }

        /// <summary>
        /// Pobiera lub ustawia stan wyciszenia.
        /// </summary>
        bool IsMuted { get; set; }

        /// <summary>
        /// Pobiera informacj�, czy odtwarzacz aktualnie odtwarza medium.
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Pobiera aktualny stan odtwarzacza.
        /// </summary>
        PlayerState CurrentState { get; }

        /// <summary>
        /// Pobiera lub ustawia szybko�� odtwarzania (np. 1.0 dla normalnej, 2.0 dla 2x).
        /// </summary>
        float PlaybackRate { get; set; }

        /// <summary>
        /// Pobiera kolekcję elementów playlisty (tylko do odczytu z zewnątrz, modyfikacja przez metody).
        /// </summary>
        IReadOnlyList<string> Playlist { get; }

        /// <summary>
        /// Pobiera lub ustawia indeks aktualnie odtwarzanego elementu w playliście.
        /// Ustawienie wartości spowoduje próbę odtworzenia elementu o danym indeksie.
        /// Wartość -1 oznacza, że nic z playlisty nie jest aktywne.
        /// </summary>
        int CurrentPlaylistIndex { get; set; }

        /// <summary>
        /// Dodaje ścieżkę do medium na koniec playlisty.
        /// </summary>
        /// <param name="mediaPathOrUrl">Ścieżka do pliku lub adres URL.</param>
        void AddToPlaylist(string mediaPathOrUrl);

        /// <summary>
        /// Usuwa element z playlisty o podanym indeksie.
        /// </summary>
        /// <param name="index">Indeks elementu do usunięcia.</param>
        void RemoveFromPlaylist(int index);

        /// <summary>
        /// Czyści całą playlistę.
        /// </summary>
        void ClearPlaylist();

        /// <summary>
        /// Rozpoczyna odtwarzanie elementu playlisty o podanym indeksie.
        /// </summary>
        /// <param name="index">Indeks elementu do odtworzenia.</param>
        void PlayPlaylistItem(int index);

        /// <summary>
        /// Odtwarza następny element w playliście.
        /// </summary>
        void PlayNext();

        /// <summary>
        /// Odtwarza poprzedni element w playliście.
        /// </summary>
        void PlayPrevious();

        // --- Właściwość dla WPF ---
        /// <summary>
        /// Pobiera instancję MediaPlayer używaną wewnętrznie (potrzebne do powiązania z VideoView w WPF).
        /// </summary>
        MediaPlayer MediaPlayer { get; }

        // --- Zdarzenia ---

        /// <summary>
        /// Wywo�ywane, gdy medium zostanie pomy�lnie za�adowane i jego metadane (np. czas trwania) s� dost�pne.
        /// </summary>
        event EventHandler MediaLoaded;

        /// <summary>
        /// Wywo�ywane, gdy odtwarzanie si� rozpocznie lub zostanie wznowione.
        /// </summary>
        event EventHandler PlaybackStarted;

        /// <summary>
        /// Wywo�ywane, gdy odtwarzanie zostanie wstrzymane.
        /// </summary>
        event EventHandler PlaybackPaused;

        /// <summary>
        /// Wywo�ywane, gdy odtwarzanie zostanie zatrzymane.
        /// </summary>
        event EventHandler PlaybackStopped;

        /// <summary>
        /// Wywo�ywane, gdy odtwarzanie dobiegnie ko�ca.
        /// </summary>
        event EventHandler PlaybackEnded;

        /// <summary>
        /// Wywo�ywane, gdy zmieni si� pozycja odtwarzania.
        /// </summary>
        event EventHandler<TimeSpan> PositionChanged;

        /// <summary>
        /// Wywo�ywane, gdy wyst�pi b��d podczas odtwarzania.
        /// </summary>
        event EventHandler<string> ErrorOccurred;

        /// <summary>
        /// Wywoływane, gdy zmieni się aktualnie odtwarzany element playlisty (np. przez PlayNext, PlayPrevious, PlayPlaylistItem).
        /// Zwraca nowy indeks (-1 jeśli poza playlistą).
        /// </summary>
        event EventHandler<int> PlaylistItemChanged;
    }
}
