// Version: 1.0.0.548
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ThmdPlayer.Core.medias;

namespace ThmdPlayer.Core.connection
{
    public class NetworkStreamingClient
    {
        private readonly string _serverAddress;
        private readonly int _port;

        public NetworkStreamingClient(string serverAddress, int port)
        {
            _serverAddress = serverAddress;
            _port = port;
        }

        public NetworkResponse SendRequest(NetworkRequest request)
        {
            using (var client = new TcpClient(_serverAddress, _port))
            using (var stream = client.GetStream())
            {
                var requestData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
                stream.Write(requestData, 0, requestData.Length);

                var buffer = new byte[1024];
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                return JsonSerializer.Deserialize<NetworkResponse>(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            }
        }
    }
}
