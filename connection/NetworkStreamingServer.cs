// Version: 1.0.0.540
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ThmdPlayer.Core.connection;
using ThmdPlayer.Core.medias;
using System.Text.Json;

namespace ThmdPlayer.Core.connection
{
    public class NetworkStreamingServer
    {
        protected readonly StreamingServer Server;
        protected readonly TcpListener Listener;
        protected bool isRunning = false;
        protected readonly Dictionary<string, string> _users = new Dictionary<string, string>();
        protected readonly Dictionary<string, string> _movies = new Dictionary<string, string>();
        protected readonly byte[] AesKey;
        protected readonly byte[] HmacKey;

        public int Port { get; }

        public NetworkStreamingServer(StreamingServer server, int port)
        {
            Server = server;
            Port = port;
            Listener = new TcpListener(IPAddress.Any, port);
        }

        public NetworkStreamingServer(StreamingServer server, int port, byte[] aaes, byte[] hmackey) : this(server, port)
        {
            AesKey = aaes;
            HmacKey = hmackey;
        }

        public void Start()
        {
            try
            {
                isRunning = true;
                Listener.Start();
                Console.WriteLine("Serwer uruchomiony...");
            }
            catch (Exception ex)
            {
                isRunning = false;
                Console.WriteLine($"Błąd uruchamiania serwera: {ex.Message}");
            }

            while (isRunning)
            {
                var client = Listener.AcceptTcpClient();
                HandleClient(client);
            }
        }

        private void HandleClient(TcpClient client)
        {
            using (var stream = client.GetStream())
            {
                var buffer = new byte[1024];
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                var request = JsonSerializer.Deserialize<NetworkRequest>(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                var response = ProcessRequest(request);

                var responseData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
                stream.Write(responseData, 0, responseData.Length);
            }
        }

        protected NetworkResponse ProcessRequest(NetworkRequest request)
        {
            try
            {
                switch (request.Action)
                {
                    case "Register":
                        Server.RegisterUser((string)request.Data["Name"], (string)request.Data["Email"]);
                        return new NetworkResponse(true, "Rejestracja udana");

                    case "RentMovie":
                        var result = Server.RentMovie((int)request.Data["UserId"], (int)request.Data["MovieId"]);
                        return new NetworkResponse(result, result ? "Wypożyczono film" : "Błąd wypożyczenia");

                    case "Ping":
                        return new NetworkResponse(true, "Pong");

                    default:
                        return new NetworkResponse(false, "Nieznana akcja");
                }
            }
            catch (Exception ex)
            {
                return new NetworkResponse(false, $"Błąd: {ex.Message}");
            }
        }

        public void GetKeys()
        {
            Console.WriteLine($"{Convert.ToBase64String(AesKey)} {Convert.ToBase64String(HmacKey)}");
        }

        public void Stop()
        {
            isRunning = false;
            Listener.Stop();
        }
    }
}
