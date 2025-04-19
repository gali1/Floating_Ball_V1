using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HoveringBallApp.Memory
{
    /// <summary>
    /// Interface for the shared memory management system
    /// Allows all LLM providers to access a common memory store
    /// </summary>
    public interface IMemoryManager
    {
        /// <summary>
        /// Gets recent memory records for a specific session
        /// </summary>
        /// <param name="sessionId">The session identifier</param>
        /// <param name="limit">Maximum number of records to retrieve (optional)</param>
        /// <returns>Collection of memory records</returns>
        Task<IEnumerable<MemoryRecord>> GetRecentMemoriesAsync(Guid sessionId, int limit = 10);

        /// <summary>
        /// Gets memories related to a specific topic
        /// </summary>
        /// <param name="sessionId">The session identifier</param>
        /// <param name="topic">The topic tag to search for</param>
        /// <param name="limit">Maximum number of records to retrieve (optional)</param>
        /// <returns>Collection of memory records related to the topic</returns>
        Task<IEnumerable<MemoryRecord>> GetMemoriesByTopicAsync(Guid sessionId, string topic, int limit = 5);

        /// <summary>
        /// Saves a new memory record
        /// </summary>
        /// <param name="sessionId">The session identifier</param>
        /// <param name="topic">The topic tag for categorization</param>
        /// <param name="userInput">The user's message</param>
        /// <param name="response">The assistant's response</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task SaveMemoryAsync(Guid sessionId, string topic, string userInput, string response);

        /// <summary>
        /// Determines if there are any previous memories for this session
        /// </summary>
        /// <param name="sessionId">The session identifier</param>
        /// <returns>True if previous memories exist, otherwise false</returns>
        Task<bool> HasPreviousMemoriesAsync(Guid sessionId);

        /// <summary>
        /// Extracts likely topics from user input using basic NLP techniques
        /// </summary>
        /// <param name="userInput">The user's message</param>
        /// <returns>A string representing the detected topic</returns>
        string ExtractTopicFromInput(string userInput);
    }
}