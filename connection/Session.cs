// Version: 1.0.0.552
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.connection
{
    public class Session
    {
        public string Token { get; set; }
        public int UserId { get; set; }
        public DateTime Expiration { get; set; }
        public byte[] AesKey { get; }
        public byte[] HmacKey { get; }
    }
}
