using System;
using HoveringBallApp.Memory;

namespace HoveringBallApp.LLM
{
    /// <summary>
    /// Enumeration of supported LLM providers
    /// </summary>
    public enum LLMProvider
    {
        Groq,
        GLHF,
        OpenRouter,
        Cohere
    }

    /// <summary>
    /// Factory for creating LLM clients based on the selected provider
    /// </summary>
    public class LLMClientFactory
    {
        private readonly IMemoryManager _memoryManager;
        private readonly ConfigurationManager _config;

        /// <summary>
        /// Initializes a new instance of the LLMClientFactory
        /// </summary>
        /// <param name="memoryManager">Memory manager for conversation history</param>
        /// <param name="config">Configuration manager for API keys</param>
        public LLMClientFactory(IMemoryManager memoryManager, ConfigurationManager config)
        {
            _memoryManager = memoryManager;
            _config = config;
        }

        /// <summary>
        /// Creates an LLM client for the specified provider
        /// </summary>
        /// <param name="provider">The LLM provider to use</param>
        /// <returns>An instance of the appropriate LLM client</returns>
        public ILLMClient CreateClient(LLMProvider provider)
        {
            return provider switch
            {
                LLMProvider.Groq => new GroqClient(_config.GroqApiKey, _memoryManager),
                LLMProvider.GLHF => new GLHFClient(_config.GLHFApiKey, _memoryManager),
                LLMProvider.OpenRouter => new OpenRouterClient(_config.OpenRouterApiKey, _memoryManager),
                LLMProvider.Cohere => new CohereClient(_config.CohereApiKey, _memoryManager),
                _ => throw new ArgumentException($"Unsupported LLM provider: {provider}")
            };
        }

        /// <summary>
        /// Gets the default model name for the specified provider
        /// </summary>
        /// <param name="provider">The LLM provider</param>
        /// <returns>The default model name</returns>
        public static string GetDefaultModel(LLMProvider provider)
        {
            return provider switch
            {
                LLMProvider.Groq => "llama3-70b-8192",
                LLMProvider.GLHF => "llama3-70b",
                LLMProvider.OpenRouter => "openai/gpt-4",
                LLMProvider.Cohere => "command-r-plus",
                _ => throw new ArgumentException($"Unsupported LLM provider: {provider}")
            };
        }

        /// <summary>
        /// Gets available models for the specified provider
        /// </summary>
        /// <param name="provider">The LLM provider</param>
        /// <returns>Array of available model names</returns>
        public static string[] GetAvailableModels(LLMProvider provider)
        {
            return provider switch
            {
                LLMProvider.Groq => new[] {
                    "llama3-70b-8192",
                    "llama3-8b-8192",
                    "mixtral-8x7b-32768",
                    "gemma-7b-it"
                },
                LLMProvider.GLHF => new[] {
"hf:NousResearch/Nous-Hermes-2-Mixtral-8x7B-DPO",
"hf:meta-llama/Meta-Llama-3.1-405B-Instruct",
"hf:Qwen/Qwen1.5-110B-Chat",
"hf:google/gemma-2-9b-it"
                },
                LLMProvider.OpenRouter => new[] {
"google/gemma-2-9b-it:free",
"meta-llama/llama-3-8b-instruct:free",
"qwen/qwen-2-7b-instruct:free",
"nousresearch/hermes-3-llama-3.1-405b"
                },
                LLMProvider.Cohere => new[] {
                    "command-r-plus",
                    "command-r",
                    "command-light"
                },
                _ => new string[0]
            };
        }
    }
}