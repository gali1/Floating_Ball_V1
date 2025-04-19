using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HoveringBallApp.LLM
{
    /// <summary>
    /// Interface for all LLM API clients
    /// </summary>
    public interface ILLMClient
    {
        /// <summary>
        /// Sends a message to the LLM and returns the response
        /// </summary>
        /// <param name="message">The user's message</param>
        /// <param name="sessionId">The session identifier</param>
        /// <param name="model">The model name to use (optional)</param>
        /// <returns>The LLM's response</returns>
        Task<string> SendMessageAsync(string message, Guid sessionId, string model = null);

        /// <summary>
        /// Gets a list of available models for this provider
        /// </summary>
        /// <returns>List of available model names</returns>
        Task<List<string>> GetAvailableModelsAsync();

        /// <summary>
        /// Gets the name of this LLM provider
        /// </summary>
        string ProviderName { get; }
    }
}