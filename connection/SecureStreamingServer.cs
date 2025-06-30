// Version: 1.0.0.548
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ThmdPlayer.Core.connection.services;

namespace ThmdPlayer.Core.connection
{
    public class SecureStreamingServer : NetworkStreamingServer
    {
        // Używamy Server z klasy bazowej
        private readonly Dictionary<string, Session> _activeSessions = new Dictionary<string, Session>(); // Przeniesione z klasy bazowej
        private readonly byte[] _aesKey;
        private readonly byte[] _hmacKey;
        private readonly AuthService _authService; // Dodajemy serwis autoryzacji
        private readonly CryptoService _cryptoService; // Dodajemy serwis krypto


        public SecureStreamingServer(StreamingServer server, int port, byte[] aesKey, byte[] hmacKey)
            : base(server, port) // Wywołanie konstruktora bazowego
        {
            _aesKey = aesKey;
            _hmacKey = hmacKey;
            Console.WriteLine($"{Convert.ToBase64String(aesKey)} {Convert.ToBase64String(hmacKey)}");
            // Inicjalizacja serwisów
            _cryptoService = new CryptoService(_aesKey, _hmacKey);
            _authService = new AuthService(_cryptoService); // AuthService może potrzebować dostępu do bazy użytkowników
        }

        // Nadpisujemy metodę do obsługi klienta
        // Uwaga: Oryginalna klasa bazowa NetworkStreamingServer nie miała tej metody jako virtual/protected.
        // Załóżmy, że możemy ją nadpisać lub zmodyfikować bazową, aby była virtual.
        // Jeśli nie, trzeba by powielić logikę Start() i wywoływać nową metodę HandleSecureClient.
        // Dla uproszczenia zakładamy, że HandleClient jest virtual lub tworzymy nową logikę.

        // Zamiast nadpisywać HandleClient (co może być niemożliwe bez zmiany klasy bazowej),
        // zmodyfikujmy logikę startową, aby wywoływała naszą bezpieczną obsługę.
        public new void Start() // Używamy 'new', aby ukryć metodę bazową
        {
            try
            {
                base.isRunning = true; // Zakładamy dostęp protected lub modyfikację pola w klasie bazowej
                base.Listener.Start(); // Zakładamy dostęp protected
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Serwer] Błąd podczas uruchamiania: {ex.Message}");
            }

            if (base.Listener!=null)
            {
                while (base.isRunning)
                {
                    try
                    {
                        Console.WriteLine("Bezpieczny Serwer uruchomiony...");
                        var client = base.Listener.AcceptTcpClient();
                        // Uruchom obsługę klienta w osobnym wątku/zadaniu, aby nie blokować pętli
                        Task.Run(() => HandleSecureClient(client));
                    }
                    catch (SocketException ex) when (!base.isRunning)
                    {
                        // Oczekiwany wyjątek podczas zatrzymywania serwera
                        Console.WriteLine("Serwer zatrzymany.");
                        Console.WriteLine($"Exception: {ex.Message}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Serwer] Błąd akceptacji klienta: {ex.Message}");
                    }
                }
            }
        }


        private void HandleSecureClient(TcpClient client)
        {
            Console.WriteLine("[Serwer] Połączono nowego klienta.");
            using (client) // Upewnij się, że klient zostanie zamknięty
            using (var stream = client.GetStream())
            {
                NetworkRequest decryptedRequest = null;
                string sessionToken = null; // Przechowamy token sesji, jeśli zostanie znaleziony
                try
                {
                    // 1. Odbierz dane (długość + dane)
                    var lengthBytes = new byte[4];
                    int bytesReadCount = stream.Read(lengthBytes, 0, 4);
                    if (bytesReadCount < 4) throw new IOException("Nie udało się odczytać długości żądania.");
                    int length = BitConverter.ToInt32(lengthBytes, 0);

                    var buffer = new byte[length];
                    int totalBytesRead = 0;
                    while (totalBytesRead < length)
                    {
                        int bytesRead = stream.Read(buffer, totalBytesRead, length - totalBytesRead);
                        if (bytesRead == 0) throw new IOException("Połączenie zamknięte przedwcześnie podczas odczytu żądania.");
                        totalBytesRead += bytesRead;
                    }
                    var secureRequest = Encoding.UTF8.GetString(buffer, 0, totalBytesRead);


                    // 2. Zdeszyfruj i zweryfikuj żądanie
                    // Używamy _cryptoService i SecureCommunicationHandler
                    var secureHandler = new SecureCommunicationHandler(_cryptoService);
                    var jsonRequest = secureHandler.ProcessSecureMessage(secureRequest);


                    // 3. Deserializuj JSON do NetworkRequest
                    decryptedRequest = JsonSerializer.Deserialize<NetworkRequest>(jsonRequest);
                    if (decryptedRequest == null) throw new JsonException("Nie udało się zdeserializować żądania.");


                    // 4. Sprawdź token sesji (jeśli akcja tego wymaga)
                    bool requiresAuth = RequiresAuthentication(decryptedRequest.Action);
                    bool sessionValid = false;
                    if (requiresAuth)
                    {
                        if (decryptedRequest.Data != null && decryptedRequest.Data.TryGetValue("SessionToken", out var tokenObj) && tokenObj is JsonElement tokenElement && tokenElement.ValueKind == JsonValueKind.String)
                        {
                            sessionToken = tokenElement.GetString(); // Odczytaj token
                                                                     // Używamy lokalnej implementacji ValidateSession zamiast AuthService
                            sessionValid = ValidateSession(sessionToken); // Używamy sessionToken odczytanego z żądania
                            if (!sessionValid)
                            {
                                throw new SecurityException("Nieprawidłowa lub wygasła sesja.");
                            }
                        }
                        else
                        {
                            throw new SecurityException("Brak tokenu sesji dla wymaganej akcji.");
                        }
                    }


                    // 5. Przetwórz żądanie (wywołaj logikę z StreamingServer poprzez metodę bazową)
                    // Uwaga: ProcessRequest z NetworkStreamingServer powinien być wywołany
                    NetworkResponse response = ProcessDecryptedRequest(decryptedRequest, sessionToken); // Przekazujemy token dla akcji typu Logout


                    // 6. Serializuj odpowiedź do JSON
                    var jsonResponse = JsonSerializer.Serialize(response);

                    // 7. Zaszyfruj i podpisz odpowiedź
                    var secureResponse = secureHandler.PrepareSecureMessage(jsonResponse);
                    var secureResponseData = Encoding.UTF8.GetBytes(secureResponse);


                    // 8. Wyślij zaszyfrowaną odpowiedź (długość + dane)
                    var responseLengthBytes = BitConverter.GetBytes(secureResponseData.Length);
                    stream.Write(responseLengthBytes, 0, 4);
                    stream.Write(secureResponseData, 0, secureResponseData.Length);

                    Console.WriteLine($"[Serwer] Obsłużono żądanie: {decryptedRequest.Action}, Sukces: {response.Success}");

                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"[Serwer] Błąd deserializacji: {jsonEx.Message}");
                    TrySendErrorResponse(stream, $"Błąd formatu danych: {jsonEx.Message}");
                }
                catch (SecurityException secEx)
                {
                    Console.WriteLine($"[Serwer] Błąd bezpieczeństwa: {secEx.Message}");
                    // Nie wysyłaj szczegółów błędu bezpieczeństwa do klienta
                    TrySendErrorResponse(stream, "Błąd bezpieczeństwa.");
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"[Serwer] Błąd I/O: {ioEx.Message}");
                    // Nie można wysłać odpowiedzi, jeśli strumień jest zamknięty
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Serwer] Nieoczekiwany błąd: {ex.Message}");
                    // Spróbuj wysłać generyczną odpowiedź błędu
                    TrySendErrorResponse(stream, $"Wystąpił wewnętrzny błąd serwera.");
                }
                finally
                {
                    Console.WriteLine("[Serwer] Zakończono obsługę klienta.");
                }
            }
        }

        // Prywatna metoda do obsługi już zdeszyfrowanego żądania
        private NetworkResponse ProcessDecryptedRequest(NetworkRequest request, string sessionToken)
        {
            // Logika logowania i rejestracji jest specyficzna i nie wymaga sesji
            if (request.Action == "Register")
            {
                try
                {
                    // Walidacja danych wejściowych
                    if (request.Data == null || !request.Data.ContainsKey("Name") || !request.Data.ContainsKey("Email") || !request.Data.ContainsKey("Password"))
                        return new NetworkResponse(false, "Brak wymaganych danych do rejestracji.");

                    string name = request.Data["Name"]?.ToString();
                    string email = request.Data["Email"]?.ToString();
                    string password = request.Data["Password"]?.ToString();

                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                        return new NetworkResponse(false, "Nazwa, email i hasło nie mogą być puste.");

                    // Sprawdź, czy użytkownik już istnieje
                    if (base.Server.GetUserByEmail(email) != null) // Używamy Server z klasy bazowej
                        return new NetworkResponse(false, "Użytkownik o tym adresie email już istnieje.");

                    // Zarejestruj użytkownika (metoda w StreamingServer powinna zostać rozszerzona o hasło)
                    // TODO: Zmodyfikuj `StreamingServer.RegisterUser`, aby przyjmowała hasło i je hashowała.
                    // Na razie symulujemy:
                    base.Server.RegisterUser(name, email); // Ta metoda musi zostać zaktualizowana
                    // Pobierz nowo utworzonego użytkownika, aby ustawić hasło
                    var newUser = base.Server.GetUserByEmail(email);
                    if (newUser != null)
                    {
                        newUser.SetPassword(password); // Ustaw hasło (hashowanie wewnątrz User)
                        Console.WriteLine($"[Serwer] Zarejestrowano użytkownika: {email}");
                        return new NetworkResponse(true, "Rejestracja udana");
                    }
                    else
                    {
                        return new NetworkResponse(false, "Błąd podczas tworzenia użytkownika.");
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Serwer] Błąd rejestracji: {ex.Message}");
                    return new NetworkResponse(false, $"Błąd podczas rejestracji: {ex.Message}");
                }
            }
            else if (request.Action == "Login")
            {
                try
                {
                    if (request.Data == null || !request.Data.ContainsKey("Email") || !request.Data.ContainsKey("Password"))
                        return new NetworkResponse(false, "Brak danych logowania.");

                    string email = request.Data["Email"]?.ToString();
                    string password = request.Data["Password"]?.ToString();

                    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                        return new NetworkResponse(false, "Email i hasło nie mogą być puste.");

                    // Znajdź użytkownika
                    var user = base.Server.GetUserByEmail(email); // Używamy Server z klasy bazowej
                    if (user == null)
                        return new NetworkResponse(false, "Nie znaleziono użytkownika o podanym adresie email.");


                    // TODO: W klasie User powinna być metoda do walidacji hasła
                    // public bool ValidatePassword(string password) { ... }
                    // Na razie symulujemy walidację:
                    // if (user.ValidatePassword(password)) // Prawidłowa implementacja
                    // Symulacja: Załóżmy, że SetPassword zahashowało i ValidatePassword porównuje hashe
                    byte[] salt = user.Salt;
                    byte[] expectedHash = user.PasswordHash;
                    byte[] actualHash = User.HashPassword(password, salt); // Używamy statycznej metody z User

                    if (CompareHashes(expectedHash, actualHash)) // Porównanie hashy
                    {
                        // Logowanie udane - utwórz sesję
                        string newSessionToken = CreateNewSession(user.Id);
                        Console.WriteLine($"[Serwer] Użytkownik {email} zalogowany. Token: {newSessionToken}");
                        return new NetworkResponse(true, "Logowanie udane", newSessionToken); // Zwróć token
                    }
                    else
                    {
                        return new NetworkResponse(false, "Nieprawidłowe hasło.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Serwer] Błąd logowania: {ex.Message}");
                    return new NetworkResponse(false, $"Błąd podczas logowania: {ex.Message}");
                }
            }
            else if (request.Action == "Logout")
            {
                if (!string.IsNullOrEmpty(sessionToken) && _activeSessions.ContainsKey(sessionToken))
                {
                    _activeSessions.Remove(sessionToken);
                    Console.WriteLine($"[Serwer] Wylogowano sesję z tokenem: {sessionToken}");
                    return new NetworkResponse(true, "Wylogowano pomyślnie.");
                }
                else
                {
                    return new NetworkResponse(false, "Nieprawidłowy lub brak tokenu sesji do wylogowania.");
                }
            }
            else if(request.Action == "Ping")
            {
                // Odpowiedz na ping
                return new NetworkResponse(true, "Pong");
            }


                // Dla innych akcji, które wymagają uwierzytelnienia (i przeszły walidację sesji)
                // Wywołaj metodę z klasy bazowej NetworkStreamingServer, która deleguje do StreamingServer
                // Upewnij się, że ValidateSession zostało wywołane wcześniej w HandleSecureClient
                // Musimy dodać mapowanie UserId z sesji do żądania, jeśli oryginalne API tego wymagało
                if (_activeSessions.TryGetValue(sessionToken, out var session))
            {
                // Dodaj UserId do danych żądania, jeśli go tam nie ma,
                // aby metody w StreamingServer działały poprawnie.
                if (request.Data != null && !request.Data.ContainsKey("UserId"))
                {
                    request.Data["UserId"] = session.UserId;
                }

                // Wywołujemy oryginalną metodę przetwarzania z klasy bazowej,
                // która zawiera switch dla akcji takich jak "RentMovie".
                // Metoda base.ProcessRequest powinna być protected lub public.
                // Jeśli jest private, musimy skopiować jej logikę switch tutaj.
                // Zakładamy, że jest dostępna:
                return base.ProcessRequest(request); // Wywołanie metody z NetworkStreamingServer
            }
            else
            {
                // To nie powinno się zdarzyć, jeśli ValidateSession działa poprawnie
                return new NetworkResponse(false, "Błąd sesji.");
            }
        }


        // Metoda pomocnicza do wysyłania błędu (jeśli to możliwe)
        private void TrySendErrorResponse(NetworkStream stream, string errorMessage)
        {
            try
            {
                if (stream.CanWrite)
                {
                    var errorResponse = new NetworkResponse(false, errorMessage);
                    var jsonResponse = JsonSerializer.Serialize(errorResponse);
                    // Używamy tego samego handlera do zaszyfrowania odpowiedzi błędu
                    var secureHandler = new SecureCommunicationHandler(_cryptoService);
                    var secureMessage = secureHandler.PrepareSecureMessage(jsonResponse);
                    var secureData = Encoding.UTF8.GetBytes(secureMessage);

                    var lengthBytes = BitConverter.GetBytes(secureData.Length);
                    stream.Write(lengthBytes, 0, 4);
                    stream.Write(secureData, 0, secureData.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Serwer] Nie można wysłać odpowiedzi błędu: {ex.Message}");
            }
        }


        // Usunięto metody DecryptAndVerifyRequest i EncryptAndSignResponse
        // Logika została zintegrowana w HandleSecureClient i ProcessDecryptedRequest


        // Implementacja walidacji sesji (przeniesiona lub powielona z oryginalnego kodu)
        private bool ValidateSession(string sessionToken)
        {
            if (string.IsNullOrEmpty(sessionToken)) return false; // Dodane sprawdzenie null/empty


            if (!_activeSessions.TryGetValue(sessionToken, out var session))
                return false;

            if (session.Expiration < DateTime.Now) // Użyj DateTime.Now dla spójności
            {
                _activeSessions.Remove(sessionToken);
                Console.WriteLine($"[Serwer] Sesja wygasła: {sessionToken}");
                return false;
            }

            // Przedłuż sesję
            session.Expiration = DateTime.Now.AddMinutes(SecurityManager.SESSION_TIMEOUT_MINUTES); // Użyj stałej
            Console.WriteLine($"[Serwer] Sesja ważna: {sessionToken}, przedłużono do {session.Expiration}");
            return true;
        }

        // Implementacja sprawdzania, czy akcja wymaga autentykacji
        private bool RequiresAuthentication(string action)
        {
            // Logowanie i Rejestracja nie wymagają istniejącej sesji
            return action switch
            {
                "Login" or "Register" => false,
                _ => true // Wszystkie inne akcje wymagają zalogowania (ważnej sesji)
            };
        }

        // Implementacja tworzenia nowej sesji
        private string CreateNewSession(int userId)
        {
            var token = SecurityManager.GenerateSessionToken(); // Użyj metody z SecurityManager
            _activeSessions[token] = new Session
            {
                Token = token,
                UserId = userId,
                Expiration = DateTime.Now.AddMinutes(SecurityManager.SESSION_TIMEOUT_MINUTES) // Użyj stałej
            };
            return token;
        }


        // Prywatna metoda do porównywania hashy (może być w klasie User lub AuthService)
        private bool CompareHashes(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }


        // Musimy też dodać metodę HashPassword statycznie lub w User/AuthService
        // Zakładamy, że istnieje w klasie User: public static byte[] HashPassword(string password, byte[] salt)

        // Dodajmy też metodę Stop z klasy bazowej, jeśli jest potrzebna
        public new void Stop() // Używamy 'new'
        {
            base.Stop(); // Wywołaj metodę bazową
            _activeSessions.Clear(); // Wyczyść aktywne sesje
            Console.WriteLine("Bezpieczny serwer zatrzymany.");
        }
    }
}
