// Version: 1.0.0.551
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.connection
{
    public class NetworkResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Result { get; set; }

        public NetworkResponse(bool success, string message, object result = null)
        {
            Success = success;
            Message = message;
            Result = result;
        }
    }
}
