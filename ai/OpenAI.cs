// Version: 1.0.0.580
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpVectors.Scripting;
using System.Net;
using System.Text.Json;
using System.IO;
using ThmdPlayer.Core.configuration;
using ThmdPlayer.Core.Logs;

namespace ThmdPlayer.Core.ai
{
    public class OpenAI
    {
        // --- Konfiguracja ---
        // !! WAŻNE: W prawdziwej aplikacji nie trzymaj klucza API w kodzie !!
        // Użyj zmiennych środowiskowych, Azure Key Vault, appsettings.json etc.
        private static string _apiKey = Environment.GetEnvironmentVariable("AI_API_KEY"); // Przykładowe wczytanie ze zmiennej środowiskowej
                                                                                                   // private static readonly string _apiKey = "twoj_super_tajny_klucz_api"; // Tylko do testów lokalnych!

        // Endpoint API - ZASTĄP adresem URL API konkretnego dostawcy AI
        // Przykład dla OpenAI Chat Completions:
        private static readonly string _apiEndpoint = "https://api.openai.com/v1/chat/completions";
        // Przykład dla Google OpenAI API (może się różnić w zależności od modelu):
        // private static readonly string _apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key=" + _apiKey;

        private static AuthenticationHeaderValue _requestHeader = new HttpClient().DefaultRequestHeaders.Authorization;
        
        private static ThmdPlayer.Core.Logs.AsyncLogger _logger = new ThmdPlayer.Core.Logs.AsyncLogger();

        private static HttpClient _httpClient = new HttpClient();

        public OpenAI()
        {
            StartLogs();

            // Ustawienia HttpClient (jeśli potrzebne)
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _logger.Log(Logs.LogLevel.Info, new[] { "Console", "File" }, $"Ustawienia HttpClient: {_httpClient.DefaultRequestHeaders.Accept}");

            Test().Wait(); // Czekamy na zakończenie metody Test
        }

        private void StartLogs()
        {
            var c = Config.Instance;

            _logger = new AsyncLogger
            {
                MinLogLevel = c.LogLevel,
                CategoryFilters =
                {
                    ["Console"] = true,
                    ["File"] = true
                },
            };

            _logger.AddSink(new CategoryFilterSink(
                new FileSink("logs", "log", new TextFormatter()), new[] { "File" }));

            _logger.AddSink(new CategoryFilterSink(
                new ConsoleSink(formatter: new TextFormatter()), new[] { "Console" }));

            _logger.Log(Core.Logs.LogLevel.Info, "File", $"Save to log files");
            _logger.Log(Core.Logs.LogLevel.Info, "Console", "Console logs just started.");
            _logger.Log(Core.Logs.LogLevel.Info, "File", _logger.GetMetrics().ToString());
            _logger.Log(Core.Logs.LogLevel.Info, "Console", _logger.GetMetrics().ToString());
        }

        public async Task Test()
        {
            var ai_api_key_environment = Environment.GetEnvironmentVariable("AI_API_KEY");
            if (String.IsNullOrEmpty(ai_api_key_environment))
            {
                _logger.Log(Logs.LogLevel.Info, new[] { "Console", "File" }, $"Tworze AI_API_KEY w zmiennych środowiskowych");
                Environment.SetEnvironmentVariable("AI_API_KEY", "");
            }
            ai_api_key_environment = Environment.GetEnvironmentVariable("AI_API_KEY");
            _apiKey = ai_api_key_environment;
            _logger.Log(Logs.LogLevel.Info, new[] { "Console", "File" }, $"Zmienna środowskowa AI_API_KEY: {ai_api_key_environment}");
            //Environment.SetEnvironmentVariable("AI_API_KEY", _apiKey);

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.Log(Logs.LogLevel.Info, new[] { "Console", "File" }, $"Błąd: Klucz API nie został ustawiony. Ustaw zmienną środowiskową AI_API_KEY.");
                return;
            }

            // Konfiguracja HttpClient (zależy od API)
            // Dla OpenAI:
            var h = new HttpClient().DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            var n = new AuthenticationHeaderValue("Bearer", _apiKey);

            // Dla OpenAI (klucz często jest w URL lub innym nagłówku - sprawdź dokumentację):
            // Niektóre API mogą wymagać innych nagłówków, np. Content-Type jest dodawany później.

            _logger.Log(Logs.LogLevel.Info, new[] { "Console", "File" }, $"{h}");
            string userInput = $"Odpowiedz na moje pytanie: Co myslisz o programie do wideo w C#?";
            _logger.Log(Logs.LogLevel.Info, new[] { "Console", "File" }, $"{userInput}");

            try
            {
                Console.WriteLine($"{userInput}");
                string aiResponse = await GetAiResponse(userInput);
                _logger.Log(Logs.LogLevel.Info, new[] { "Console", "File" }, $"{aiResponse}");
                //Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nOdpowiedź AI:");
                Console.WriteLine(aiResponse);
                Console.ResetColor();
                Console.WriteLine("\n---"); // Separator
                Console.WriteLine("Wpisz kolejne pytanie (lub 'wyjście'):");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                _logger.Log(Logs.LogLevel.Error, new[] { "Console", "File" }, $"\nWystąpił błąd: {ex.Message}");
                // W bardziej rozbudowanej aplikacji loguj szczegły błędu (ex.ToString())
                //Console.ResetColor();
                Console.WriteLine("\n---");
                Console.WriteLine("Spróbuj ponownie lub wpisz 'wyjscie':");
            }
        }
        

        private static async Task<string> GetAiResponse(string prompt)
        {
            // --- Przygotowanie danych do wysłania (Request Body) ---
            // Struktura danych zależy od konkretnego API!
            // Przykład dla OpenAI Chat Completions:
            var requestData = new
            {
                model = "gpt-4.1", // lub inny model np. gpt-4
                messages = new[] {
                new { role = "user", input = prompt }
                /*"model": "gpt-4.1",
  "input": [],
  "text": {
                "format": {
                    "type": "text"
                }
            },
  "reasoning": { },
  "tools": [],
  "temperature": 1,
  "max_output_tokens": 2048,
  "top_p": 1,
  "store": true*/
                // Można dodać więcej wiadomości dla kontekstu:
                // new { role = "system", content = "Jesteś pomocnym asystentem." },
            },
                temperature = 0.7, // Kreatywność odpowiedzi (0.0 - 2.0)
                max_tokens = 150 // Maksymalna długość odpowiedzi
            };

            // Serializacja obiektu C# do JSON
            string jsonPayload = System.Text.Json.JsonSerializer.Serialize(requestData);
            _logger.Log(Logs.LogLevel.Info, new[] { "Console", "File" }, $"{requestData}");
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // --- Wysłanie żądania POST ---
            Console.WriteLine("Wysyłanie zapytania do AI...");
            HttpResponseMessage response = await _httpClient.PostAsync(_apiEndpoint, httpContent);

            // --- Obsługa odpowiedzi ---
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Rzucenie wyjątku z informacją o błędzie z API
                throw new HttpRequestException($"Błąd API: {response.StatusCode} - {responseContent}");
            }

            Console.WriteLine("Otrzymano odpowiedź.");

            // --- Przetwarzanie odpowiedzi JSON ---
            // Struktura odpowiedzi również zależy od API!
            // Przykład dla OpenAI Chat Completions:
            using (JsonDocument document = JsonDocument.Parse(responseContent))
            {
                // Sprawdzamy, czy odpowiedź ma oczekiwaną strukturę
                if (document.RootElement.TryGetProperty("choices", out JsonElement choicesElement) &&
                    choicesElement.ValueKind == JsonValueKind.Array &&
                choicesElement.GetArrayLength() > 0 &&
                    choicesElement[0].TryGetProperty("message", out JsonElement messageElement) &&
                    messageElement.TryGetProperty("content", out JsonElement contentElement))
                {
                    return contentElement.GetString() ?? "Nie otrzymano treści odpowiedzi.";
                }
                else
                {
                    // Jeśli struktura jest inna, logujemy całą odpowiedź do analizy
                    _logger.Log(Logs.LogLevel.Info, new[] { "Console", "File" }, $"Nieoczekiwana struktura odpowiedzi JSON:");
                    Console.WriteLine(responseContent);
                    return "Nie udało się przetworzyć odpowiedzi AI.";
                }
            }

            // Przykład dla Google OpenAI (struktura może być inna, np.):
            /*
            using (JsonDocument document = JsonDocument.Parse(responseContent))
            {
                if (document.RootElement.TryGetProperty("candidates", out JsonElement candidatesElement) &&
                    candidatesElement.ValueKind == JsonValueKind.Array && candidatesElement.GetArrayLength() > 0 &&
                    candidatesElement[0].TryGetProperty("content", out JsonElement contentElement) &&
                    contentElement.TryGetProperty("parts", out JsonElement partsElement) &&
                    partsElement.ValueKind == JsonValueKind.Array && partsElement.GetArrayLength() > 0 &&
                    partsElement[0].TryGetProperty("text", out JsonElement textElement))
                {
                    return textElement.GetString() ?? "Nie otrzymano treści odpowiedzi.";
                }
                // ... obsługa błędów i innej struktury
            }
            */
        }

        public void DisplayInfo()
        {
            
        }
    }
}
