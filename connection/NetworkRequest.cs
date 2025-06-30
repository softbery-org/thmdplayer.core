// Version: 1.0.0.540
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.connection
{
    public class NetworkRequest
    {
        public string Action { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }
}
