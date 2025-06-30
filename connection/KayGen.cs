// Version: 1.0.0.534
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.connection
{
    public static class KayGen
    {
        public static string GenerateKey(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var key = new char[length];
            for (int i = 0; i < length; i++)
            {
                key[i] = chars[random.Next(chars.Length)];
            }

            // Generowanie kluczy (powinny by� przechowywane bezpiecznie)
            var aesKey = new byte[32];
            var hmacKey = new byte[64];
            RandomNumberGenerator.Create(aesKey.ToString());
            RandomNumberGenerator.Create(hmacKey.ToString());

            return new string(key);
        }
    }
}
