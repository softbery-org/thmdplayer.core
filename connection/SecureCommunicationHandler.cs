// Version: 1.0.0.541
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using ThmdPlayer.Core.connection.services;

namespace ThmdPlayer.Core.connection
{
    public class SecureCommunicationHandler
    {
        private readonly CryptoService _crypto;

        public SecureCommunicationHandler(CryptoService crypto)
        {
            _crypto = crypto;
        }

        public string PrepareSecureMessage(string message)
        {
            var (ciphertext, iv, hmac) = _crypto.Encrypt(message);
            return $"{Convert.ToBase64String(ciphertext)}|{Convert.ToBase64String(iv)}|{Convert.ToBase64String(hmac)}";
        }

        public string ProcessSecureMessage(string secureMessage)
        {
            var parts = secureMessage.Split('|');
            if (parts.Length != 3) throw new SecurityException("Invalid message format");

            var ciphertext = Convert.FromBase64String(parts[0]);
            var iv = Convert.FromBase64String(parts[1]);
            var hmac = Convert.FromBase64String(parts[2]);

            return _crypto.Decrypt(ciphertext, iv, hmac);
        }
    }
}
