using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HoveringBallApp
{
    public class GroqApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GroqApiClient(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.groq.com/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> SendMessage(string message)
        {
            try
            {
                // Create request object
                var requestObject = new
                {
                    model = "llama3-8b-8192",
                    messages = new[]
                    {
                        new { role = "user", content = message }
                    },
                    temperature = 0.7,
                    max_tokens = 1024
                };

                // Serialize request
                var requestContent = new StringContent(
                    JsonSerializer.Serialize(requestObject),
                    Encoding.UTF8,
                    "application/json");

                // Send request
                var response = await _httpClient.PostAsync("openai/v1/chat/completions", requestContent);

                // Check if successful
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                    // Extract the assistant's message
                    if (responseObj.TryGetProperty("choices", out var choices) &&
                        choices.GetArrayLength() > 0 &&
                        choices[0].TryGetProperty("message", out var responseMessage) &&
                        responseMessage.TryGetProperty("content", out var responseContent))
                    {
                        return responseContent.GetString();
                    }

                    return "Received response but couldn't parse content.";
                }
                else
                {
                    return $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
                }
            }
            catch (Exception ex)
            {
                return $"API Error: {ex.Message}";
            }
        }
    }
}