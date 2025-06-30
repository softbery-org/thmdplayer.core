// Version: 1.0.0.676
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ThmdPlayer.Core.medias;
using ThmdPlayer.Core.controls;

namespace ThmdPlayer.Core.Interfaces
{

    public interface IPlayer
    {
        #region Public parameters
        /// <summary>
        /// Current media
        /// </summary>
        Media Media { get; set; }
        medias.Playlist Playlist { get; }
        controls.PlaylistView PlaylistView { get; set; }
        IntPtr Handle { get; }
        ControlBar ControlBar { get; }
        ControlProgressBar ProgressBar { get; }
        SubControl SubtitleControl { get; }
        event EventHandler<TimeSpan> OnSubtitleControlChanged;
        TimeSpan CurrentTime { get; set; }

        event EventHandler<double> PlayerPositionChanged;
        event EventHandler<MediaPlayerStatus> PlayerStatusChanged;
        event EventHandler<double> PlayerVolumeChanged;

        bool IsPlaying { get; set; }
        bool IsPaused { get; set; }
        bool IsStoped { get; set; }
        bool SubtitleVisibility { get; set; }
        /// <summary>
        /// 
        /// </summary>
        double Volume { get; set; }
        /// <summary>
        /// 
        /// </summary>
        bool Mute { get; set; }
        /// <summary>
        /// Play media
        /// </summary>
        void Play();
        void Play(Media media);
        /// <summary>
        /// Play media with uri
        /// </summary>
        /// <param name="uri">media uri</param>
        void Play(Uri uri);
        void Play(string path);
        /// <summary>
        /// Pause media
        /// </summary>
        void Pause();
        /// <summary>
        /// Stop media
        /// </summary>
        void Stop();
        /// <summary>
        /// Next media
        /// </summary>
        void Next();
        /// <summary>
        /// Previouse media
        /// </summary>
        void Preview();
        /// <summary>
        /// Seek media to time span
        /// </summary>
        void Seek(TimeSpan time);
        /// <summary>
        /// Open subtitle on path
        /// </summary>
        void Subtitle(string subtitle);
        void Dispose();
        #endregion
    }
}
