// Version: 1.0.0.665
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThmdPlayer.Core.Interfaces;

namespace ThmdPlayer.Core.medias
{
    /// <summary>
    /// Represents a movie.
    /// </summary>
    public class Movie : IMedia
    {
        private int v;
        private int year;
        private decimal cost;

        /// <summary>
        /// Gets or sets the unique identifier for the movie.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the title of the movie.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the director of the movie.
        /// </summary>
        public string Director { get; set; }

        /// <summary>
        /// Gets or sets the duration of the movie.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets the release year of the movie.
        /// </summary>
        public int ReleaseYear { get; set; }

        /// <summary>
        /// Gets or sets the genre of the movie.
        /// </summary>
        public string Genre { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the movie is available for rental.
        /// </summary>
        public bool IsAvailable { get; internal set; }
        /// <summary>
        /// Gets or sets the rental cost of the movie.
        /// </summary>
        public decimal RentalCost { get; internal set; }
        /// <summary>
        /// Gets or sets the cost of the movie.
        /// </summary>
        public object Year { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Movie"/> class.
        /// </summary>
        /// <param name="title">The title of the movie.</param>
        /// <param name="director">The director of the movie.</param>
        /// <param name="duration">The duration of the movie.</param>
        /// <param name="releaseYear">The release year of the movie.</param>
        /// <param name="genre">The genre of the movie.</param>
        public Movie(int id, string title, string director, TimeSpan duration, int releaseYear, string genre)
        {
            Title = title;
            Director = director;
            Duration = duration;
            ReleaseYear = releaseYear;
            Genre = genre;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Movie"/> class with additional properties.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="title"></param>
        /// <param name="genre"></param>
        /// <param name="year"></param>
        /// <param name="cost"></param>
        public Movie(int id, string title, string genre, int year, decimal cost)
        {
            this.v = id;
            Title = title;
            Genre = genre;
            this.year = year;
            this.cost = cost;
            RentalCost = cost;
        }

        /// <summary>
        /// Returns a string representation of the movie.
        /// </summary>
        /// <returns>A string that represents the movie.</returns>
        public override string ToString()
        {
            return $"{Title} ({ReleaseYear}) - {Director}, {Genre} ({Duration:hh\\:mm\\:ss})";
        }
    }
}
