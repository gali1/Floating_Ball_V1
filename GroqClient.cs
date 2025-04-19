using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HoveringBallApp.Memory;

namespace HoveringBallApp.LLM
{
    /// <summary>
    /// Client for Groq's LLM API
    /// </summary>
    public class GroqClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly IMemoryManager _memoryManager;
        private readonly string _defaultModel = "llama3-70b-8192";

        /// <summary>
        /// Initializes a new instance of the GroqClient
        /// </summary>
        /// <param name="apiKey">Groq API key</param>
        /// <param name="memoryManager">Memory manager for conversation history</param>
        public GroqClient(string apiKey, IMemoryManager memoryManager)
        {
            _apiKey = apiKey;
            _memoryManager = memoryManager;

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.groq.com/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Gets the name of this LLM provider
        /// </summary>
        public string ProviderName => "Groq";

        /// <summary>
        /// Sends a message to Groq's API and returns the response
        /// </summary>
        public async Task<string> SendMessageAsync(string message, Guid sessionId, string model = null)
        {
            try
            {
                // Use specified model or default
                string modelToUse = string.IsNullOrEmpty(model) ? _defaultModel : model;

                // Extract topic from the user's message
                string topic = _memoryManager.ExtractTopicFromInput(message);

                // Build system prompt with memory context
                var promptBuilder = new SystemPromptBuilder(_memoryManager, sessionId)
                    .WithProviderCapabilities(ProviderName);

                string systemPrompt = await promptBuilder.BuildSystemPromptAsync();

                // Create request object
                var requestObject = new
                {
                    model = modelToUse,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = message }
                    },
                    temperature = 0.7,
                    max_tokens = 4096
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
                        string assistantResponse = responseContent.GetString();

                        // Save to memory
                        await _memoryManager.SaveMemoryAsync(sessionId, topic, message, assistantResponse);

                        return assistantResponse;
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
                throw new HttpRequestException($"Groq API Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets a list of available models for Groq
        /// </summary>
        public async Task<List<string>> GetAvailableModelsAsync()
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
                                models.Add(id.GetString());
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

        /// <summary>
        /// Gets a list of default Groq models
        /// </summary>
        private List<string> GetDefaultModels()
        {
            return new List<string>
            {
                "llama3-70b-8192",
                "llama3-8b-8192",
                "mixtral-8x7b-32768",
                "gemma-7b-it"
            };
        }
    }
}