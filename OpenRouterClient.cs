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
    /// Client for OpenRouter's LLM API
    /// </summary>
    public class OpenRouterClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly IMemoryManager _memoryManager;
        private readonly string _defaultModel = "openai/gpt-4";

        /// <summary>
        /// Initializes a new instance of the OpenRouterClient
        /// </summary>
        /// <param name="apiKey">OpenRouter API key</param>
        /// <param name="memoryManager">Memory manager for conversation history</param>
        public OpenRouterClient(string apiKey, IMemoryManager memoryManager)
        {
            _apiKey = apiKey;
            _memoryManager = memoryManager;

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://openrouter.ai/api/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // OpenRouter specific headers
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://hoveringballapp.com");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "Hovering Ball Assistant");
        }

        /// <summary>
        /// Gets the name of this LLM provider
        /// </summary>
        public string ProviderName => "OpenRouter";

        /// <summary>
        /// Sends a message to OpenRouter's API and returns the response
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
                    max_tokens = 2048
                };

                // Serialize request
                var requestContent = new StringContent(
                    JsonSerializer.Serialize(requestObject),
                    Encoding.UTF8,
                    "application/json");

                // Send request
                var response = await _httpClient.PostAsync("v1/chat/completions", requestContent);

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
                throw new HttpRequestException($"OpenRouter API Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets a list of available models for OpenRouter
        /// </summary>
        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                // Send request to list models
                var response = await _httpClient.GetAsync("v1/models");

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
        /// Gets a list of default OpenRouter models
        /// </summary>
        private List<string> GetDefaultModels()
        {
            return new List<string>
            {
"google/gemma-2-9b-it:free",
"meta-llama/llama-3-8b-instruct:free",
"qwen/qwen-2-7b-instruct:free",
"nousresearch/hermes-3-llama-3.1-405b"
            };
        }
    }
}