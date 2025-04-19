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
    /// Client for Cohere's LLM API
    /// </summary>
    public class CohereClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly IMemoryManager _memoryManager;
        private readonly string _defaultModel = "command-r-plus";

        /// <summary>
        /// Initializes a new instance of the CohereClient
        /// </summary>
        /// <param name="apiKey">Cohere API key</param>
        /// <param name="memoryManager">Memory manager for conversation history</param>
        public CohereClient(string apiKey, IMemoryManager memoryManager)
        {
            _apiKey = apiKey;
            _memoryManager = memoryManager;

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.cohere.ai/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Gets the name of this LLM provider
        /// </summary>
        public string ProviderName => "Cohere";

        /// <summary>
        /// Sends a message to Cohere's API and returns the response
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

                // Get recent memories to build conversation history
                var recentMemories = await _memoryManager.GetRecentMemoriesAsync(sessionId, 10);

                // Build message history in Cohere format
                var chatHistory = new List<object>();
                foreach (var memory in recentMemories)
                {
                    chatHistory.Add(new { role = "USER", message = memory.UserInput });
                    chatHistory.Add(new { role = "CHATBOT", message = memory.AssistantResponse });
                }

                // Add current message
                chatHistory.Add(new { role = "USER", message = message });

                // Create request object (Cohere has a different API format)
                var requestObject = new
                {
                    model = modelToUse,
                    message = message,
                    chat_history = chatHistory.Count > 0 ? chatHistory.ToArray() : null,
                    preamble = systemPrompt,
                    temperature = 0.7,
                    max_tokens = 2048
                };

                // Serialize request
                var requestContent = new StringContent(
                    JsonSerializer.Serialize(requestObject),
                    Encoding.UTF8,
                    "application/json");

                // Send request (Cohere uses a different endpoint)
                var response = await _httpClient.PostAsync("v1/chat", requestContent);

                // Check if successful
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                    // Extract the assistant's message (Cohere format is different)
                    if (responseObj.TryGetProperty("text", out var responseText))
                    {
                        string assistantResponse = responseText.GetString();

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
                throw new HttpRequestException($"Cohere API Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets a list of available models for Cohere
        /// </summary>
        public async Task<List<string>> GetAvailableModelsAsync()
        {
            // Cohere doesn't have a models endpoint, return default models
            return GetDefaultModels();
        }

        /// <summary>
        /// Gets a list of default Cohere models
        /// </summary>
        private List<string> GetDefaultModels()
        {
            return new List<string>
            {
                "command-r-plus",
                "command-r",
                "command-light"
            };
        }
    }
}