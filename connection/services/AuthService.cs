// Version: 1.0.0.531
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ThmdPlayer.Core.medias;

namespace ThmdPlayer.Core.connection.services
{
    public class AuthService
    {
        private readonly Dictionary<string, Session> _activeSessions = new();
        private readonly CryptoService _crypto;

        public AuthService(CryptoService crypto)
        {
            _crypto = crypto;
        }

        public User RegisterUser(int id, string username, string email , string password)
        {
            var salt = GenerateSalt();
            var passwordHash = HashPassword(password, salt);

            return new ThmdPlayer.Core.connection.User(id, username, email)
            {
                Id = id,
                Username = username,
                PasswordHash = passwordHash,
                Salt = salt
            };
        }

        public bool ValidateLogin(User user, string password)
        {
            var computedHash = HashPassword(password, user.Salt);
            return CompareHashes(computedHash, user.PasswordHash);
        }

        public string CreateSession(int userId)
        {
            var sessionToken = SecurityManager.GenerateSessionToken();
            var encryptedToken = EncryptSessionToken(sessionToken);

            _activeSessions[encryptedToken] = new Session
            {
                UserId = userId,
                Expiration = DateTime.UtcNow.AddMinutes(SecurityManager.SESSION_TIMEOUT_MINUTES)
            };

            return encryptedToken;
        }

        public bool ValidateSession(string sessionToken)
        {
            if (!_activeSessions.TryGetValue(sessionToken, out var session))
                return false;

            if (session.Expiration < DateTime.UtcNow)
            {
                _activeSessions.Remove(sessionToken);
                return false;
            }

            // Automatyczne przed�u�anie sesji
            session.Expiration = DateTime.UtcNow.AddMinutes(SecurityManager.SESSION_TIMEOUT_MINUTES);
            return true;
        }

        private string EncryptSessionToken(string token)
        {
            var encrypted = _crypto.Encrypt(token);
            return Convert.ToBase64String(encrypted.ciphertext)
                   + "|" + Convert.ToBase64String(encrypted.iv)
                   + "|" + Convert.ToBase64String(encrypted.hmac);
        }

        private byte[] GenerateSalt()
        {
            var salt = new byte[SecurityManager.SALT_SIZE];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return salt;
        }

        private byte[] HashPassword(string password, byte[] salt)
        {
            using var sha256 = SHA256.Create();
            var saltedPassword = Encoding.UTF8.GetBytes(password + Convert.ToBase64String(salt));
            return sha256.ComputeHash(saltedPassword);
        }

        private bool CompareHashes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }
    }

}
