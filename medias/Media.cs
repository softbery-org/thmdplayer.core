// Version: 1.0.0.674
using MediaInfo;
using MediaToolkit;
using MediaToolkit.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ThmdPlayer.Core.helpers;
using ThmdPlayer.Core.Interfaces;
using ThmdPlayer.Core.logs;

namespace ThmdPlayer.Core.medias
{
    // m3u8 file url https://dd315o.cloudatacdn.com/u5kj7sz47xe3sdgge5fawjiailakzznah7nuqgivyaqst6e73e6oueuexgjq/g6nozz097y~VBuF4TyCj3?token=tgd9ll67296hte7tofvmpq6h&expiry=1743356456552

    /// <summary>
    /// Provides data for media-related events
    /// </summary>
    /// <remarks>
    /// Przechowuje dane zdarzeń związanych z odtwarzaniem multimediów
    /// </remarks>
    public class MediaEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the associated media object
        /// </summary>
        /// <remarks>Powiązany obiekt multimedialny</remarks>
        public Media Media { get; private set; }

        /// <summary>
        /// Gets the player instance
        /// </summary>
        /// <remarks>Instancja odtwarzacza</remarks>
        public IPlayer Player { get; private set; }

        /// <summary>
        /// Gets the current playback position
        /// </summary>
        /// <remarks>Aktualna pozycja odtwarzania</remarks>
        public TimeSpan Position { get; private set; }

        /// <summary>
        /// Initializes a new instance of the MediaEventArgs class
        /// </summary>
        /// <param name="player">The player instance</param>
        /// <remarks>
        /// Inicjalizuje nową instancję klasy MediaEventArgs
        /// </remarks>
        public MediaEventArgs(IPlayer player)
        {
            if (player != null)
            {
                Player = player;
                Media = player.Media;
                Position = player.CurrentTime;
                
                /*

                .OnCurrentTimeChanged += (s, e) => {
                    _currentTime = e;
                    GetSubtitleLine(e);
                };*/
            }
        }
    }

    /// <summary>
    /// Represents a media file with playback capabilities
    /// </summary>
    /// <remarks>
    /// Reprezentuje plik multimedialny z możliwością odtwarzania
    /// </remarks>
    [Serializable]
    public class Media
    {
        #region Private fields
        private Uri _uri;
        private string _name;
        private double _position = 0;
        private double _volume = 1.0;
        private double _duration;
        private IPlayer _player;
        private double _fps;
        private MediaToolkit.Model.Metadata _metadataMediaToolkit;
        private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
        #endregion

        #region Public variables
        public object MediaType { get; private set; }
        /// <summary>
        /// Gets or sets the display name of the media
        /// </summary>
        /// <remarks>Nazwa wyświetlana multimediów</remarks>
        public string Name
        {
            get => _name;
            set => new Uri(_uri, value);
        }

        /// <summary>
        /// Gets the URI of the media file
        /// </summary>
        /// <remarks>URI pliku multimedialnego</remarks>
        public Uri Uri => _uri;

        /// <summary>
        /// Gets the total duration of the media
        /// </summary>
        /// <remarks>Całkowity czas trwania multimediów</remarks>
        public TimeSpan Duration
        {
            get
            {
                var time = TimeSpan.FromMilliseconds(_duration);
                var stringTime = time.ToString(@"hh\:mm\:ss");
                //var stringTime = time.ToString(@"hh\:mm\:ss\.fff");

                return TimeSpan.Parse(stringTime);
            }
        }

        /// <summary>
        /// Gets the associated player instance
        /// </summary>
        /// <remarks>Powiązana instancja odtwarzacza</remarks>
        public IPlayer Player => _player;

        public double Fps => _fps;

        /// <summary>
        /// Gets the media stream instance
        /// </summary>
        /// <remarks>Instancja strumienia multimedialnego</remarks>
        public IMediaStream MediaStream { get; private set; }

        /// <summary>
        /// Gets or sets the current playback position in milliseconds
        /// </summary>
        /// <remarks>Aktualna pozycja odtwarzania w milisekundach</remarks>
        public double Position
        {
            get { return _position; }
            set
            {
                if (value >= 0 && value <= Duration.TotalMilliseconds)
                {
                    _position = value;
                    OnPositionChanged(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the playback volume (0.0 to 1.0)
        /// </summary>
        /// <remarks>Głośność odtwarzania (0.0 do 1.0)</remarks>
        public double Volume
        {
            get { return _volume; }
            set
            {
                if (value >= 0 && value <= 1)
                {
                    _volume = value;
                    OnVolumeChanged(value);
                }
            }
        }
        #endregion

        #region Public events
        /// <summary>
        /// CurrentTime change event
        /// </summary>
        /// <remarks>Aktualna pozycja podczas odtwarzania</remarks>
        public event EventHandler<double> PositionChanged;
        /// <summary>
        /// Volume change event
        /// </summary>
        /// <remarks>Zmiana głośności</remarks>
        public event EventHandler<double> VolumeChanged;
        /// <summary>
        /// Set player change event
        /// </summary>
        /// <remarks>Zmiana lub ustawianie playera</remarks>
        public event EventHandler<IPlayer> PlayerChanged;
        #endregion

        #region Events methods
        protected virtual void OnPositionChanged(double newPosition)
        {
            PositionChanged?.Invoke(this, newPosition);
            // Log
           Logger.Log.Log(logs.LogLevel.Info, "Console", $"CurrentTime change event: {newPosition}");
        }

        protected virtual void OnVolumeChanged(double newVolume)
        {
            VolumeChanged?.Invoke(this, newVolume);
            // Log
           Logger.Log.Log(logs.LogLevel.Info, "Console", $"Volume change: {newVolume}");
        }

        protected virtual void OnPlayerChanged(IPlayer player)
        {
            PlayerChanged?.Invoke(this, player);
            // Log
           Logger.Log.Log(logs.LogLevel.Info, "Console", $"Player change event: {player}");
           Logger.Log.Log(logs.LogLevel.Info, "File", $"Player change event: {player}");
        }
        #endregion

        #region Costructor
        private Media()
        {

        }

        /// <summary>
        /// Initializes a new Media instance with specified path
        /// </summary>
        /// <param name="path">Path to media file</param>
        /// <remarks>
        /// Inicjalizuje nową instancję klasy Media z podaną ścieżką
        /// </remarks>
        public Media(string path):this()
        {
            _uri = new Uri(path);
            _name = new FileInfo(_uri.LocalPath).Name;
            _metadataMediaToolkit = GetMetadata();
            if (_metadataMediaToolkit != null)
            {
                _fps = GetFPS();
                _duration = GetDuration();
            }
            else 
            {                 
                _fps = 0;
                _duration = 0;
            }
            ReadFileAsync(_uri.LocalPath).Wait();
        }

        /// <summary>
        /// Initializes a new Media instance with specified path
        /// </summary>
        /// <param name="path">Path to media file</param>
        /// <param name="player">Player with IPlayer interface</param>
        /// <remarks>
        /// Inicjalizuje nową instancję klasy Media z podaną ścieżką
        /// </remarks>
        public Media(string path, IPlayer player) : this(path)
        {
            _player = player;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Asynchronously reads the media file
        /// </summary>
        /// <param name="path">Path to media file</param>
        /// <returns>Task representing the operation</returns>
        /// <remarks>
        /// Asynchronicznie odczytuje plik multimedialny
        /// </remarks>
        private async Task ReadFileAsync(string path)
        {
            await Semaphore.WaitAsync();

            try
            {
                var m3u8Checker = new VideoTypeChecker(path);
                
                m3u8Checker.DownloadM3u8("download/local_copy.m3u8");
                var p = m3u8Checker.ParseM3u8Async();
                Console.WriteLine(p);
                if (m3u8Checker.IsM3u8)
                {
                    try
                    {
                        if (!Directory.Exists("download/"))
                        {
                            Directory.CreateDirectory("download/");
                        }
                        if (!File.Exists("download/local_copy.m3u8"))
                        {
                            var d = File.Create("download/local_copy.m3u8");
                        }
                        //m3u8Checker.DownloadM3u8("download/local_copy.m3u8");
                        var playlist = m3u8Checker.ParseM3u8Async();


                        Console.WriteLine(playlist);
                        //Console.WriteLine(m3u8Checker.ParseM3u8Async().Length);

                        MediaStream = new FileMediaStream("download/local_copy.m3u8");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error: {e.Message}");
                    }
                }
                else
                {
                    FileMediaStream file_stream = new FileMediaStream(_uri.AbsolutePath);
                    MediaStream = file_stream;
                }
                // Log
               Logger.Log.Log(logs.LogLevel.Info, "Console", $"[{this.GetType().Name}]: Reading file: {_uri.AbsoluteUri}");
               Logger.Log.Log(logs.LogLevel.Info, "File", $"[{this.GetType().Name}]: Reading file: {_uri.AbsoluteUri}");
            }
            catch (Exception ex)
            {
                // Log 
               Logger.Log.Log(logs.LogLevel.Error, new[] { "Console", "File" }, $"[{this.GetType().Name}]: Can't read m3u8", ex);
            }
            finally
            {
                // Release semaphore field
                Semaphore.Release();
            }
        }

        /// <summary>
        /// Releases all resources used by the Media object
        /// </summary>
        /// <remarks>
        /// Zwalnia wszystkie zasoby używane przez obiekt Media
        /// </remarks>
        public void Dispose()
        {
            _player.Dispose();
            // Log
            Logger.Log.Log(logs.LogLevel.Info, new string[]{"Console", "File"}, $"[{this.GetType().Name}]: Disposing: {Name}");
        }

        /// <summary>
        /// Sets the player for the media instance
        /// </summary>
        /// <param name="player"></param>
        public void SetPlayer(IPlayer player)
        {
            try
            {
                _player = player;
            }
            catch (Exception ex)
            {
               Logger.Log.Log(logs.LogLevel.Error, new[] { "File", "Console" }, $"Error with player set in media class. In media: {this.Uri}. {ex.Message}");
            }
        }

        /// <summary>
        /// Starts media playback
        /// </summary>
        /// <remarks>
        /// Rozpoczyna odtwarzanie multimediów
        /// </remarks>
        public void Play()
        {
            _player.Play(this);
            // Log
           Logger.Log.Log(logs.LogLevel.Info, "Console", $"[{this.GetType().Name}]: Playing media {Name}");
           Logger.Log.Log(logs.LogLevel.Info, "File", $"[{this.GetType().Name}]: Playing media {Name}");
        }

        /// <summary>
        /// Pauses media playback
        /// </summary>
        /// <remarks>
        /// Wstrzymuje odtwarzanie multimediów
        /// </remarks>
        public void Pause()
        {
            _player.Pause();
            // Log
           Logger.Log.Log(logs.LogLevel.Info, "Console", $"[{this.GetType().Name}]: Pause media {Name}");
           Logger.Log.Log(logs.LogLevel.Info, "File", $"[{this.GetType().Name}]: Pause media {Name}");
        }

        /// <summary>
        /// Stops media playback and resets position
        /// </summary>
        /// <remarks>
        /// Zatrzymuje odtwarzanie i resetuje pozycję
        /// </remarks>
        public void Stop()
        {
            _player.Stop();
            Position = 0;
            // Log
           Logger.Log.Log(logs.LogLevel.Info, "Console", $"[{this.GetType().Name}]: Stopped media {Name}");
           Logger.Log.Log(logs.LogLevel.Info, "File", $"[{this.GetType().Name}]: Stopped media {Name}");
        }

        /// <summary>
        /// Fast forwards the playback by specified seconds
        /// </summary>
        /// <param name="seconds">Number of seconds to forward</param>
        /// <remarks>
        /// Przewija do przodu o podaną liczbę sekund
        /// </remarks>
        public void Forward(double seconds)
        {
            Position += seconds;
            // Log
           Logger.Log.Log(logs.LogLevel.Info, "Console", $"[{this.GetType().Name}]: Change position to forward with +{seconds} second(s)");
           Logger.Log.Log(logs.LogLevel.Info, "File", $"[{this.GetType().Name}]: Change position to forward with +{seconds} second(s)");
        }

        /// <summary>
        /// Rewinds the playback by specified seconds
        /// </summary>
        /// <param name="seconds">Number of seconds to rewind</param>
        /// <remarks>
        /// Przewija do tyłu o podaną liczbę sekund
        /// </remarks>
        public void Rewind(double seconds)
        {
            Position -= seconds;
            // Log
            Logger.Log.Log(logs.LogLevel.Info, "Console", $"[{this.GetType().Name}]: Rewind position by -{seconds} second(s)");
            Logger.Log.Log(logs.LogLevel.Info, "File", $"[{this.GetType().Name}]: Rewind position by -{seconds} second(s)");
        }

        private MediaToolkit.Model.Metadata GetMetadata()
        {
            if(_uri == null || string.IsNullOrEmpty(_uri.LocalPath) || !File.Exists(_uri.LocalPath))
            {
                // Log error
                Logger.Log.Log(logs.LogLevel.Error, new[] { "Console", "File" }, $"[{this.GetType().Name}]: Invalid media file path: {_uri?.LocalPath}");
                return null;
            }

            try
            {
                var inputFile = new MediaFile { Filename = _uri.LocalPath };

                using (var engine = new Engine())
                {
                    engine.GetMetadata(inputFile);
                }

                // Log
                Logger.Log.Log(logs.LogLevel.Info, new[] { "Console", "File" }, $"[{this.GetType().Name}]: Get metadata for media: {_uri.LocalPath}");

                return inputFile.Metadata;
            }
            catch (Exception ex)
            {
                // Log error
                Logger.Log.Log(logs.LogLevel.Error, new[] { "Console", "File" }, $"[{this.GetType().Name}]: Error getting metadata for media: {_uri.LocalPath}", ex);
                return null;
            }
        }

        /// <summary>
        /// Gets the duration of the media file
        /// </summary>
        /// <returns>Duration in milliseconds</returns>
        /// <remarks>
        /// Pobiera długość pliku multimedialnego
        /// </remarks>
        private double GetDuration()
        {
            // Get metadata
            var duration = _metadataMediaToolkit.Duration.TotalMilliseconds;
            
            // Log
            Logger.Log.Log(logs.LogLevel.Info, new[] { "Console", "File" }, $"[{this.GetType().Name}]: Get media {TimeSpan.FromMilliseconds(duration)} duration");

            return duration;
        }

        private double GetFPS() 
        {
            // Get metadata
            var fps = _metadataMediaToolkit.VideoData.Fps;
            
            // Log
            Logger.Log.Log(logs.LogLevel.Info, new[] { "Console", "File" }, $"[{this.GetType().Name}]: Get media {fps} fps");

            return fps;
        }
        #endregion
    }
}
