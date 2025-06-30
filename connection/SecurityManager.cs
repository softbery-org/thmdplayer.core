// Version: 1.0.0.530
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.connection
{
    public class SecurityManager
    {
        // Konfiguracja parametrów kryptograficznych
        /// <summary>
        /// Key size AES in bits.
        /// </summary>
        public const int AES_KEY_SIZE = 256;
        /// <summary>
        /// Key size HMAC in bits.
        /// </summary>
        public const int HMAC_KEY_SIZE = 256;
        /// <summary>
        /// Size of salt used for password hashing in bytes.
        /// </summary>
        public const int SALT_SIZE = 32;
        /// <summary>
        /// Size of initialization vector (IV) for AES encryption in bytes.
        /// </summary>
        public const int IV_SIZE = 16;
        /// <summary>
        /// Size of HMAC in bytes.
        /// </summary>
        public const int HMAC_SIZE = 32;
        /// <summary>
        /// Size of session token in bytes.
        /// </summary>
        public const int SESSION_TOKEN_SIZE = 64;
        /// <summary>
        /// Timeout for user session in minutes.
        /// </summary>
        public const int SESSION_TIMEOUT_MINUTES = 30;

        // Generowanie kluczy (powinny był bezpiecznie przechowywane)
        /// <summary>
        /// Generates a pair of keys for AES encryption and HMAC authentication.
        /// </summary>
        /// <returns></returns>
        public static (byte[] aesKey, byte[] hmacKey) GenerateKeys()
        {
            using var aes = Aes.Create();
            aes.KeySize = AES_KEY_SIZE;
            aes.GenerateKey();

            using var hmac = new HMACSHA256();
            hmac.Initialize();

            return (aes.Key, hmac.Key);
        }

        /// <summary>
        /// Generates a random salt for password hashing.
        /// </summary>
        /// <returns></returns>
        public static string GenerateSessionToken()
        {
            var tokenBytes = new byte[SESSION_TOKEN_SIZE];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(tokenBytes);
            return Convert.ToBase64String(tokenBytes);
        }
    }
}
