using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HoveringBallApp
{
    public class OllamaClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _systemPrompt;

        public OllamaClient(string baseUrl = "http://localhost:11434")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_baseUrl);

            // Create a system prompt with current date/time awareness
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
1. You can help with a wide range of tasks including answering questions, drafting content, solving problems, and more.
2. You format code responses neatly with proper syntax highlighting using markdown triple backticks with the language specified (```language).
3. You're running locally on the user's device using Ollama.
4. You're part of an Avalonia C# desktop application with a modern, oak-wood themed interface.

Guidelines:
- Be concise but helpful.
- Format code neatly with proper language tags.
- Include examples when helpful.
- Be friendly and conversational.
- When uncertain, mention that limitation rather than providing potentially incorrect information.

The user communicates with you by clicking on the floating ball and typing in their message.";
        }

        public async Task<string> SendMessage(string message, string model = "deepseek-r1:1.5b")
        {
            try
            {
                // Create request object
                var requestObject = new
                {
                    model = model,
                    prompt = message,
                    system = _systemPrompt,
                    stream = false
                };

                // Serialize request
                var requestContent = new StringContent(
                    JsonSerializer.Serialize(requestObject),
                    Encoding.UTF8,
                    "application/json");

                // Send request
                var response = await _httpClient.PostAsync("api/generate", requestContent);

                // Check if successful
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonSerializer.Deserialize<OllamaResponse>(jsonResponse);
                    return responseObj?.Response ?? "No response received.";
                }
                else
                {
                    return $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
                }
            }
            catch (Exception ex)
            {
                return $"Ollama API Error: {ex.Message}";
            }
        }

        // Get list of available models
        public async Task<List<string>> GetAvailableModels()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/tags");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(jsonResponse);

                    var modelNames = new List<string>();
                    if (modelsResponse?.Models != null)
                    {
                        foreach (var model in modelsResponse.Models)
                        {
                            modelNames.Add(model.Name);
                        }
                    }

                    return modelNames;
                }

                return new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }

    public class OllamaResponse
    {
        public string Response { get; set; }
    }

    public class OllamaModelsResponse
    {
        public List<OllamaModel> Models { get; set; } = new List<OllamaModel>();
    }

    public class OllamaModel
    {
        public string Name { get; set; }
    }
}