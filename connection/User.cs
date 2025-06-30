// Version: 1.0.0.557
using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ThmdPlayer.Core.medias;

namespace ThmdPlayer.Core.connection
{
    public partial class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] Salt { get; set; }
        public string Username { get; set; }
        public decimal Balance { get; set; }
        public List<Movie> RentedMovies { get; set; }

        public User(int id, string name, string email)
        {
            Id = id;
            Name = name;
            Email = email;
            Balance = 0;
            RentedMovies = new List<Movie>();
        }

        public void SetPassword(string password)
        {
            Salt = GenerateSalt();
            PasswordHash = HashPassword(password, Salt);
        }


        private static byte[] GenerateSalt(int size = 32)
        {
            var rng = new RNGCryptoServiceProvider();
            var buff = new byte[size];
            rng.GetBytes(buff);
            var s =  Convert.ToBase64String(buff);
            return buff;
        }

        // Metoda do walidacji has�a
        public bool ValidatePassword(string password)
        {
            if (PasswordHash == null || Salt == null) return false; // Nie ustawiono has�a
            byte[] computedHash = HashPassword(password, Salt);
            return CompareHashes(computedHash, PasswordHash);
        }

        // Metoda do hashowania has�a (uczyniona statyczn� dla u�ycia w serwerze)
        public static byte[] HashPassword(string password, byte[] salt)
        {
            using var sha256 = SHA256.Create();
            // ��czymy has�o i s�l - upewnijmy si�, �e s�l jest poprawnie kodowana/dekodowana
            // Bezpieczniej jest po��czy� bajty:
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] combinedBytes = new byte[passwordBytes.Length + salt.Length];
            Buffer.BlockCopy(passwordBytes, 0, combinedBytes, 0, passwordBytes.Length);
            Buffer.BlockCopy(salt, 0, combinedBytes, passwordBytes.Length, salt.Length);

            return sha256.ComputeHash(combinedBytes);
        }


        // Prywatna metoda do por�wnywania hashy (dla ValidatePassword)
        private bool CompareHashes(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }
    }
}
