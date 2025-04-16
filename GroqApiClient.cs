using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HoveringBallApp
{
    public class GroqApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _systemPrompt;

        public GroqApiClient(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.groq.com/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Create a system prompt with current date/time and web access awareness
            _systemPrompt = GenerateSystemPrompt();
        }

        private string GenerateSystemPrompt()
        {
            // Get current date and time
            var now = DateTime.Now;
            var formattedDateTime = now.ToString("dddd, MMMM d, yyyy HH:mm");

            return $@"You are an intelligent assistant embedded in a floating ball application on the user's desktop.
Current time and date: {formattedDateTime}

Key capabilities:
1. You have access to web search through SearXNG integration to provide up-to-date information.
2. You can help with a wide range of tasks including answering questions, drafting content, solving problems, and more.
3. When you detect that the user is asking for information that might be time-sensitive or recent, you can use web search to find the latest data.
4. You format code responses neatly with proper syntax highlighting using markdown triple backticks with the language specified (```language).
5. You're running on an Avalonia C# desktop application with a modern, oak-wood themed interface.

Guidelines:
- Be concise but helpful.
- For recent information, mention you're using web search capabilities.
- Format code neatly with proper language tags.
- Include examples when helpful.
- Be friendly and conversational.
- When uncertain, mention that limitation rather than providing potentially incorrect information.

The user communicates with you by clicking on the floating ball and typing in their message.";
        }

        public async Task<string> SendMessage(string message, string model = "llama3-8b-8192")
        {
            try
            {
                // Create request object
                var requestObject = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = _systemPrompt },
                        new { role = "user", content = message }
                    },
                    temperature = 0.7,
                    max_tokens = 2048
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
                throw new HttpRequestException($"API Error: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GetAvailableModels()
        {
            try
            {
                // Send request to list models
                var response = await _httpClient.GetAsync("openai/v1/models");

                // Check if successful
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                    var models = new List<string>();

                    // Extract model IDs
                    if (responseObj.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var model in data.EnumerateArray())
                        {
                            if (model.TryGetProperty("id", out var id))
                            {
                                var modelId = id.GetString();

                                // Only add Groq-supported models
                                if (modelId.StartsWith("llama") ||
                                    modelId.StartsWith("mixtral") ||
                                    modelId.StartsWith("gemma"))
                                {
                                    models.Add(modelId);
                                }
                            }
                        }
                    }

                    return models.Count > 0 ? models : GetDefaultModels();
                }

                return GetDefaultModels();
            }
            catch
            {
                // Return default models if anything goes wrong
                return GetDefaultModels();
            }
        }

        private List<string> GetDefaultModels()
        {
            // Return list of known Groq models
            return new List<string>
            {
                "llama3-8b-8192",
                "llama3-70b-8192",
                "mixtral-8x7b-32768",
                "gemma-7b-it"
            };
        }
    }
}