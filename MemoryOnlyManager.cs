using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HoveringBallApp.Memory
{
    /// <summary>
    /// In-memory implementation of IMemoryManager that doesn't rely on external databases.
    /// This is used as a fallback when PostgreSQL and Redis connections fail.
    /// </summary>
    public class MemoryOnlyManager : IMemoryManager
    {
        private readonly List<MemoryRecord> _memories = new List<MemoryRecord>();
        private int _nextId = 1;

        /// <summary>
        /// Gets recent memory records for a specific session
        /// </summary>
        public Task<IEnumerable<MemoryRecord>> GetRecentMemoriesAsync(Guid sessionId, int limit = 10)
        {
            var memories = _memories
                .Where(m => m.SessionId == sessionId)
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .ToList();

            return Task.FromResult<IEnumerable<MemoryRecord>>(memories);
        }

        /// <summary>
        /// Gets memories related to a specific topic
        /// </summary>
        public Task<IEnumerable<MemoryRecord>> GetMemoriesByTopicAsync(Guid sessionId, string topic, int limit = 5)
        {
            var memories = _memories
                .Where(m => m.SessionId == sessionId &&
                            m.Topic.IndexOf(topic, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .ToList();

            return Task.FromResult<IEnumerable<MemoryRecord>>(memories);
        }

        /// <summary>
        /// Saves a new memory record
        /// </summary>
        public Task SaveMemoryAsync(Guid sessionId, string topic, string userInput, string response)
        {
            _memories.Add(new MemoryRecord
            {
                Id = _nextId++,
                SessionId = sessionId,
                Timestamp = DateTime.Now,
                Topic = topic,
                UserInput = userInput,
                AssistantResponse = response
            });

            // Limit memory size to avoid excessive memory usage
            const int maxMemories = 100;
            if (_memories.Count > maxMemories)
            {
                _memories.RemoveAt(0); // Remove oldest memory
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Determines if there are any previous memories for this session
        /// </summary>
        public Task<bool> HasPreviousMemoriesAsync(Guid sessionId)
        {
            bool hasMemories = _memories.Any(m => m.SessionId == sessionId);
            return Task.FromResult(hasMemories);
        }

        /// <summary>
        /// Extracts likely topics from user input using basic NLP techniques
        /// </summary>
        public string ExtractTopicFromInput(string userInput)
        {
            // Simplified topic extraction using keyword analysis
            // In a production system, this should use a proper NLP library

            // Remove special characters and convert to lowercase
            string normalizedInput = Regex.Replace(userInput.ToLower(), @"[^\w\s]", " ");

            // Split into words and remove common stop words
            var words = normalizedInput.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3) // Only keep words with 4+ characters
                .ToList();

            // Get the most frequent substantive words (simplified approach)
            var wordFrequency = words
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(2)
                .Select(g => g.Key)
                .ToList();

            // Use the most frequent words as the topic
            return string.Join("_", wordFrequency);
        }
    }
}