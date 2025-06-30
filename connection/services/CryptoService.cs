// Version: 1.0.0.543
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.connection.services
{
    public class CryptoService
    {
        private readonly byte[] _aesKey;
        private readonly byte[] _hmacKey;

        public CryptoService(byte[] aesKey, byte[] hmacKey)
        {
            _aesKey = aesKey ?? throw new ArgumentNullException(nameof(aesKey));
            _hmacKey = hmacKey ?? throw new ArgumentNullException(nameof(hmacKey));
        }

        public (byte[] ciphertext, byte[] iv, byte[] hmac) Encrypt(string plaintext)
        {
            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.GenerateIV();
            var iv = aes.IV;

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plaintext);
            }

            var ciphertext = ms.ToArray();
            var hmac = ComputeHMAC(ciphertext, iv);

            return (ciphertext, iv, hmac);
        }

        public string Decrypt(byte[] ciphertext, byte[] iv, byte[] receivedHmac)
        {
            var computedHmac = ComputeHMAC(ciphertext, iv);
            if (!CompareHmac(computedHmac, receivedHmac))
                throw new SecurityException("HMAC validation failed");

            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(ciphertext);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }

        private byte[] ComputeHMAC(byte[] ciphertext, byte[] iv)
        {
            using var hmac = new HMACSHA256(_hmacKey);
            var combinedData = new byte[ciphertext.Length + iv.Length];
            Buffer.BlockCopy(ciphertext, 0, combinedData, 0, ciphertext.Length);
            Buffer.BlockCopy(iv, 0, combinedData, ciphertext.Length, iv.Length);
            return hmac.ComputeHash(combinedData);
        }

        private bool CompareHmac(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }
    }
}
