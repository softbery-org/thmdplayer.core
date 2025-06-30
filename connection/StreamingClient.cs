// Version: 1.0.0.553
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThmdPlayer.Core.medias;

namespace ThmdPlayer.Core.connection
{
    public class StreamingClient
    {
        private StreamingServer server;
        public User CurrentUser { get; private set; }

        public StreamingClient(StreamingServer server)
        {
            this.server = server;
        }

        public void Register(string name, string email)
        {
            server.RegisterUser(name, email);
        }

        public bool Login(string email)
        {
            CurrentUser = server.GetUserByEmail(email);
            return CurrentUser != null;
        }

        public void Logout()
        {
            CurrentUser = null;
        }

        public List<Movie> SearchMoviesByTitle(string title)
        {
            return server.SearchMoviesByTitle(title);
        }

        public List<Movie> SearchMoviesByGenre(string genre)
        {
            return server.SearchMoviesByGenre(genre);
        }

        public bool RentMovie(int movieId)
        {
            if (CurrentUser == null) return false;
            return server.RentMovie(CurrentUser.Id, movieId);
        }

        public bool ReturnMovie(int movieId)
        {
            if (CurrentUser == null) return false;
            return server.ReturnMovie(CurrentUser.Id, movieId);
        }

        public void DisplayUserInfo()
        {
            if (CurrentUser == null)
            {
                Console.WriteLine("Nie jeste� zalogowany!");
                return;
            }

            Console.WriteLine($"U�ytkownik: {CurrentUser.Name}");
            Console.WriteLine($"Email: {CurrentUser.Email}");
            Console.WriteLine($"Saldo: {CurrentUser.Balance:C}");
            Console.WriteLine("Wypo�yczone filmy:");
            foreach (var movie in CurrentUser.RentedMovies)
            {
                Console.WriteLine($"- {movie.Title} ({movie.Year})");
            }
        }

        public void TopUpBalance(decimal amount)
        {
            if (CurrentUser == null) return;
            server.TopUpBalance(CurrentUser.Id, amount);
        }
    }
}
