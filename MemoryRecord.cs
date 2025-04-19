using System;

namespace HoveringBallApp.Memory
{
    /// <summary>
    /// Represents a single memory record in the shared memory system
    /// This structure matches the format described in the system prompt
    /// </summary>
    public class MemoryRecord
    {
        /// <summary>
        /// Unique identifier for the memory record
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Session identifier to group conversations
        /// </summary>
        public Guid SessionId { get; set; }

        /// <summary>
        /// When the interaction occurred
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Topic categorization tag to group related memories
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// The user's original message
        /// </summary>
        public string UserInput { get; set; }

        /// <summary>
        /// The assistant's response to the user
        /// </summary>
        public string AssistantResponse { get; set; }

        /// <summary>
        /// Returns a JSON-formatted string representation as described in the system prompt
        /// </summary>
        public string ToPromptFormat()
        {
            return $@"{{
  ""session_id"": ""{SessionId}"",
  ""timestamp"": ""{Timestamp:yyyy-MM-dd HH:mm:ss}"",
  ""topic"": ""{Topic}"",
  ""user_input"": ""{UserInput.Replace("\"", "\\\"")}"",
  ""assistant_response"": ""{AssistantResponse.Replace("\"", "\\\"")}""
}}";
        }
    }
}