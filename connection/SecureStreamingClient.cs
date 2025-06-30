// Version: 1.0.0.538
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ThmdPlayer.Core.connection.services;

namespace ThmdPlayer.Core.connection
{
    public class SecureStreamingClient : NetworkStreamingClient
    {

        private readonly byte[] _aesKey;
        private readonly byte[] _hmacKey;
        private string _currentSessionToken;
        private readonly string _serverAddress; // Dodane, aby uniknąć używania base w override
        private readonly int _port;

        public SecureStreamingClient(string serverAddress, int port, byte[] aesKey, byte[] hmacKey)
            : base(serverAddress, port)
        {
            _serverAddress = serverAddress; // Przechowaj lokalnie
            _port = port;                 // Przechowaj lokalnie
            _aesKey = aesKey;
            _hmacKey = hmacKey;
        }


        public new NetworkResponse SendRequest(NetworkRequest request)
        {
            // Dodaj token sesji jeśli istnieje i nie jest to żądanie logowania/rejestracji
            if (!string.IsNullOrEmpty(_currentSessionToken) && request.Action != "Login" && request.Action != "Register")
            {
                // Upewnij się, że Data nie jest null
                if (request.Data == null) request.Data = new Dictionary<string, object>();
                request.Data["SessionToken"] = _currentSessionToken;
            }

            try
            {
                using (var client = new TcpClient(_serverAddress, _port))
                using (var stream = client.GetStream())
                {
                    // 1. Przygotuj CryptoService i Handler
                    var cryptoService = new CryptoService(_aesKey, _hmacKey);
                    var secureHandler = new SecureCommunicationHandler(cryptoService);

                    // 2. Serializuj żądanie do JSON
                    var jsonRequest = JsonSerializer.Serialize(request);

                    // 3. Zaszyfruj i podpisz JSON
                    var secureMessage = secureHandler.PrepareSecureMessage(jsonRequest);
                    var secureData = Encoding.UTF8.GetBytes(secureMessage);

                    // 4. Wyślij zaszyfrowane dane (można użyć ChunkedDataHandler dla dużych danych)
                    // Dla uproszczenia wysyłamy bezpośrednio, zakładając że nie są zbyt duże
                    // Najpierw wyślij długość wiadomości
                    var lengthBytes = BitConverter.GetBytes(secureData.Length);
                    stream.Write(lengthBytes, 0, 4);
                    // Wyślij właściwe dane
                    stream.Write(secureData, 0, secureData.Length);


                    // 5. Odbierz odpowiedź (długość + dane)
                    var responseLengthBytes = new byte[4];
                    int bytesReadCount = stream.Read(responseLengthBytes, 0, 4);
                    if (bytesReadCount < 4) throw new IOException("Nie udało się odczytać długości odpowiedzi.");
                    int responseLength = BitConverter.ToInt32(responseLengthBytes, 0);

                    var responseBuffer = new byte[responseLength];
                    int totalBytesRead = 0;
                    while (totalBytesRead < responseLength)
                    {
                        int bytesRead = stream.Read(responseBuffer, totalBytesRead, responseLength - totalBytesRead);
                        if (bytesRead == 0) throw new IOException("Połączenie zamknięte przedwcześnie podczas odczytu odpowiedzi.");
                        totalBytesRead += bytesRead;
                    }
                    var secureResponse = Encoding.UTF8.GetString(responseBuffer, 0, totalBytesRead);


                    // 6. Zdeszyfruj i zweryfikuj odpowiedź
                    var jsonResponse = secureHandler.ProcessSecureMessage(secureResponse);


                    // 7. Deserializuj JSON do NetworkResponse
                    var networkResponse = JsonSerializer.Deserialize<NetworkResponse>(jsonResponse);

                    // 8. Jeśli logowanie się powiodło, zapisz token sesji
                    if (request.Action == "Login" && networkResponse.Success && networkResponse.Result != null)
                    {
                        _currentSessionToken = networkResponse.Result.ToString();
                    }
                    // Jeśli wylogowanie się powiodło, usuń token
                    else if (request.Action == "Logout" && networkResponse.Success)
                    {
                        _currentSessionToken = null;
                    }


                    return networkResponse;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Klient] Błąd podczas wysyłania/odbierania: {ex.Message}");
                // Zwróć generyczną odpowiedź błędu w przypadku problemów z komunikacją
                return new NetworkResponse(false, $"Błąd komunikacji: {ex.Message}", null);
            }
        }

        public bool Login(string email, string password) // Zakładamy logowanie po emailu, ale klasa User ma Username - dostosuj wg potrzeb
        {
            var request = new NetworkRequest
            {
                Action = "Login",
                Data = new Dictionary<string, object>
                {
                    { "Email", email },     // lub "Username" w zależności od logiki serwera
                    { "Password", password }
                }
            };
            // Wywołanie nadpisanej metody SendRequest
            var response = this.SendRequest(request);
            return response.Success; // Token sesji jest zapisywany w SendRequest
        }

        // Dodajmy metodę Logout
        public bool Logout()
        {
            if (string.IsNullOrEmpty(_currentSessionToken))
            {
                Console.WriteLine("[Klient] Nie jesteś zalogowany.");
                return true; // Już wylogowany
            }

            var request = new NetworkRequest
            {
                Action = "Logout",
                // Token sesji zostanie dodany automatycznie przez SendRequest
            };
            var response = this.SendRequest(request);
            if (!response.Success)
            {
                Console.WriteLine($"[Klient] Błąd podczas wylogowywania: {response.Message}");
            }
            // Token jest usuwany w SendRequest po pomyślnej odpowiedzi Logout
            return response.Success;
        }
    }
}
