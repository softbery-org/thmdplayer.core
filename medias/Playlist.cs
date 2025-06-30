// Version: 1.0.0.665
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using ThmdPlayer.Core.Interfaces;

namespace ThmdPlayer.Core.medias
{
    
    /// <summary>
    /// Represents a playlist of songs.
    /// </summary>
    public class Playlist : List<IMedia>, IMedia
    {

        private Media _current;
        private Media _next;
        private Media _previous;

        /// <summary>
        /// Gets or sets the name of the playlist.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the playlist.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets the list of songs in the playlist.
        /// </summary>
        public List<Media> Media { get; private set; }

        /// <summary>
        /// Gets or sets the current, next, and previous media items in the playlist.
        /// </summary>
        public Media Current { get => _current; private set=>_current = value; }

        /// <summary>
        /// Gets or sets the next and previous media items in the playlist.
        /// </summary>
        public Media Next { get => _next; private set => _next = value; }

        /// <summary>
        /// Gets or sets the previous media item in the playlist.
        /// </summary>
        public Media Previous { get => _previous; private set => _previous = value; }

        /// <summary>
        /// Gets the index of the current media item in the playlist.
        /// </summary>
        public int CurrentIndex
        {
            get
            {
                return Media.IndexOf(Current);
            }
        }

        /// <summary>
        /// Gets the index of the next media item in the playlist.
        /// </summary>
        public int NextIndex
        {
            get
            {
                int index = Media.IndexOf(Current) + 1;
                if (index >= Media.Count)
                {
                    return -1; // No next item
                }
                return index;
            }
        }

        /// <summary>
        /// Gets the index of the previous media item in the playlist.
        /// </summary>
        public int PreviousIndex
        {
            get
            {
                int index = Media.IndexOf(Current) - 1;
                if (index < 0)
                {
                    return -1; // No previous item
                }
                return index;
            }
        }

        /// <summary>
        /// Gets the creation date of the playlist.
        /// </summary>
        public DateTime CreationDate { get; private set; }

        /// <summary>
        /// Gets or sets the title of the media item.
        /// </summary>
        public string Title { get => Name; set => Name = value; }

        private TimeSpan _playlistDuration = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the duration of the media item.
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                return _playlistDuration;
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="Playlist"/> class.
        /// </summary>
        /// <param name="name">The name of the playlist.</param>
        /// <param name="description">The description of the playlist (optional).</param>
        public Playlist(string name, string description = "")
        {
            Name = name;
            Description = description;
            Media = new List<Media>();
            CreationDate = DateTime.Now;
        }

        /// <summary>
        /// Adds a media item to the playlist.
        /// </summary>
        /// <param name="media"></param>
        public void Add(Media media)
        {
            if (media != null)
            {
                Media.Add(media);
                Console.WriteLine($"Media \"{media.Name}\" has been added to the playlist \"{Name}\".");
            }
            else
            {
                Console.WriteLine("Cannot add a null media.");
            }
        }

        /// <summary>
        /// Removes a mediia from the playlist.
        /// </summary>
        /// <param name="media">The mediia to remove.</param>
        public void RemoveMedia(Media media)
        {
            if (media != null)
            {
                if (Media.Remove(media))
                {
                    Console.WriteLine($"Song \"{media.Name}\" has been removed from the playlist \"{Name}\".");
                }
                else
                {
                    Console.WriteLine($"Song \"{media.Name}\" was not found in the playlist \"{Name}\".");
                }
            }
            else
            {
                Console.WriteLine("Cannot remove a null mediia.");
            }
        }

        /// <summary>
        /// Displays the list of songs in the playlist.
        /// </summary>
        public void DisplayMedia()
        {
            if (Media.Count == 0)
            {
                Console.WriteLine($"Playlist \"{Name}\" is empty.");
                return;
            }

            Console.WriteLine($"Media in playlist \"{Name}\":");
            foreach (var mediia in Media)
            {
                Console.WriteLine($"- ({mediia.ToString()})");
            }
        }

        /// <summary>
        /// Gets the number of songs in the playlist.
        /// </summary>
        /// <returns>The number of songs.</returns>
        public int GetMediaCount()
        {
            return Media.Count;
        }

        /// <summary>
        /// Gets the total duration of all songs in the playlist.
        /// </summary>
        /// <returns>The total duration.</returns>
        public TimeSpan GetTotalDuration()
        {
            TimeSpan totalDuration = TimeSpan.Zero;
            foreach (var media in Media)
            {
                totalDuration += media.Duration;
            }
            return totalDuration;
        }
    }
}
