// Version: 1.0.0.548
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ThmdPlayer.Core.medias;
using ThmdPlayer.Core.connection;

namespace ThmdPlayer.Core.connection
{
    public class StreamingServer
    {
        private List<Movie> _movies;
        private List<User> _users;
        private int _movieIdCounter;
        private int __userIdCounter;
        public string ServerName { get; set; }
        public string ServerLocation { get; set; }
        public string ServerVersion { get; set; }
        public string ServerStatus { get; set; }
        public string ServerDescription { get; set; }
        public string ServerIP { get; set; }
        public string ServerPort { get; set; }
        public string ServerProtocol { get; set; }
        public string ServerType { get; set; }
        public string ServerOwner { get; set; }
        public string ServerContact { get; set; }
        public string ServerLicense { get; set; }
        public string ServerCopyright { get; set; }
        public string ServerWebsite { get; set; }
        public string ServerSupport { get; set; }
        public string ServerTermsOfService { get; set; }
        public string ServerPrivacyPolicy { get; set; }
        public string ServerUserAgreement { get; set; }
        public string ServerUserManual { get; set; }


        public StreamingServer()
        {
            _movies = new List<Movie>();
            _users = new List<User>();
            _movieIdCounter = 1;
            __userIdCounter = 1;
        }

        public void AddMovie(string title, string genre, int year, decimal cost)
        {
            _movies.Add(new Movie(_movieIdCounter++, title, genre, year, cost));
        }

        public void RegisterUser(string name, string email)
        {
            _users.Add(new User(__userIdCounter++, name, email));
        }

        public User GetUserByEmail(string email)
        {
            return _users.FirstOrDefault(u => u.Email == email);
        }

        public List<Movie> SearchMoviesByTitle(string title)
        {
            return _movies.Where(m => m.Title.Contains(title)).ToList();
        }

        public List<Movie> SearchMoviesByGenre(string genre)
        {
            return _movies.Where(m => m.Genre.Equals(genre, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public bool RentMovie(int userId, int MovieId)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            var Movie = _movies.FirstOrDefault(m => m.Id == MovieId);

            if (user == null || Movie == null || !Movie.IsAvailable || user.Balance < Movie.RentalCost)                return false;

            user.Balance -= Movie.RentalCost;
            Movie.IsAvailable = false;
            user.RentedMovies.Add(Movie);
            return true;
        }

        public bool ReturnMovie(int userId, int MovieId)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            var Movie = _movies.FirstOrDefault(m => m.Id == MovieId);

            if (user == null || Movie == null || !user.RentedMovies.Contains(Movie))
                return false;

            Movie.IsAvailable = true;
            user.RentedMovies.Remove(Movie);
            return true;
        }

        public void TopUpBalance(int userId, decimal amount)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null) user.Balance += amount;
        }
    }
}
