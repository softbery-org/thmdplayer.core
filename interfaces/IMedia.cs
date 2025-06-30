// Version: 1.0.0.665
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.Interfaces
{
    /// <summary>
    /// Interface for media items (songs, movies, etc.).
    /// </summary>
    public interface IMedia
    {
        /// <summary>
        /// Gets or sets the title of the media item.
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Gets or sets the duration of the media item.
        /// </summary>
        TimeSpan Duration { get; }

        /// <summary>
        /// Returns a string representation of the media item.
        /// </summary>
        /// <returns>A string that represents the media item.</returns>
        string ToString();
    }
}
