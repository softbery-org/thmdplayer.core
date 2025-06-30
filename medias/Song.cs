// Version: 1.0.0.676
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThmdPlayer.Core.Interfaces;

namespace ThmdPlayer.Core.medias
{
    /// <summary>
    /// Represents a song.
    /// </summary>
    public class Song : IMedia
    {
        /// <summary>
        /// Gets or sets the title of the song.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the artist of the song.
        /// </summary>
        public string Artist { get; set; }

        /// <summary>
        /// Gets or sets the duration of the song.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Song"/> class.
        /// </summary>
        /// <param name="title">The title of the song.</param>
        /// <param name="artist">The artist of the song.</param>
        /// <param name="duration">The duration of the song.</param>
        public Song(string title, string artist, TimeSpan duration)
        {
            Title = title;
            Artist = artist;
            Duration = duration;
        }

        /// <summary>
        /// Returns a string representation of the song.
        /// </summary>
        /// <returns>A string that represents the song.</returns>
        public override string ToString()
        {
            return $"{Title} - {Artist} ({Duration:mm\\:ss})";
        }
    }
}
