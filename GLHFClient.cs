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
    /// Client for GLHF's LLM API
    /// </summary>
    public class GLHFClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly IMemoryManager _memoryManager;
        private readonly string _defaultModel = "llama3-70b";

        /// <summary>
        /// Initializes a new instance of the GLHFClient
        /// </summary>
        /// <param name="apiKey">GLHF API key</param>
        /// <param name="memoryManager">Memory manager for conversation history</param>
        public GLHFClient(string apiKey, IMemoryManager memoryManager)
        {
            _apiKey = apiKey;
            _memoryManager = memoryManager;

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.glhf.ai/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Gets the name of this LLM provider
        /// </summary>
        public string ProviderName => "GLHF";

        /// <summary>
        /// Sends a message to GLHF's API and returns the response
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
                throw new HttpRequestException($"GLHF API Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets a list of available models for GLHF
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
        /// Gets a list of default GLHF models
        /// </summary>
        private List<string> GetDefaultModels()
        {
            return new List<string>
            {
"hf:NousResearch/Nous-Hermes-2-Mixtral-8x7B-DPO",
"hf:meta-llama/Meta-Llama-3.1-405B-Instruct",
"hf:Qwen/Qwen1.5-110B-Chat",
"hf:google/gemma-2-9b-it"
            };
        }
    }
}