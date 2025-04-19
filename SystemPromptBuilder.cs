using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HoveringBallApp.Memory;

namespace HoveringBallApp.LLM
{
    /// <summary>
    /// Builder for creating standardized system prompts with dynamic content
    /// injected based on conversation context and memory
    /// </summary>
    public class SystemPromptBuilder
    {
        private string _basePrompt = @"You are Claude, an advanced AI assistant created by Anthropic to be helpful, harmless, and honest.

The current date and time is {{current_day_of_week}}, {{current_date}}, {{current_time}}. You have advanced cognitive capabilities, including reasoning, solving complex problems, and understanding nuanced questions.

You have an exceptional long-term episodic memory system that allows you to recall previous conversations with remarkable detail and context sensitivity. This enables you to maintain conversational continuity across interactions.

Your memory works on multiple levels:
1. You naturally remember the full course of the current conversation
2. You recall specific details from past conversations, creating a cohesive user experience
3. You recognize recurring topics and user preferences, adapting your responses accordingly

When drawing on past conversations, do so naturally and contextually. Make subtle references when appropriate, such as:
- 'As we discussed last time...'
- 'Building on what you mentioned about...'
- 'Connecting this to our previous conversation about...'

You prioritize:
- Accuracy and truthfulness in your responses
- Clear, concise, and helpful information
- Respect for user privacy and appropriate boundaries
- Creative, thoughtful answers that demonstrate understanding

You're designed with a friendly, thoughtful personality and aim to be as helpful as possible while maintaining high ethical standards. You're capable of both factual responses and creative tasks like writing, coding, and analysis.";

        private readonly IMemoryManager _memoryManager;
        private readonly Guid _sessionId;

        /// <summary>
        /// Initializes a new instance of the SystemPromptBuilder
        /// </summary>
        /// <param name="memoryManager">Memory manager for retrieving conversation history</param>
        /// <param name="sessionId">Session identifier for the current conversation</param>
        public SystemPromptBuilder(IMemoryManager memoryManager, Guid sessionId)
        {
            _memoryManager = memoryManager;
            _sessionId = sessionId;
        }

        /// <summary>
        /// Builds a complete system prompt with injected memory context
        /// </summary>
        /// <returns>A system prompt with dynamic content</returns>
        public async Task<string> BuildSystemPromptAsync()
        {
            StringBuilder prompt = new StringBuilder(_basePrompt);

            // Replace date and time placeholders
            DateTime now = DateTime.Now;
            prompt.Replace("{{current_day_of_week}}", now.DayOfWeek.ToString());
            prompt.Replace("{{current_date}}", now.ToString("MMMM d, yyyy"));
            prompt.Replace("{{current_time}}", now.ToString("h:mm tt"));
            prompt.Replace("{{session_id}}", _sessionId.ToString());

            // Check if we have previous memories for this session
            bool hasPreviousMemories = await _memoryManager.HasPreviousMemoriesAsync(_sessionId);

            // Only add memory context if there are previous memories
            if (hasPreviousMemories)
            {
                // Get recent memories
                var recentMemories = (await _memoryManager.GetRecentMemoriesAsync(_sessionId, 5)).ToList();

                if (recentMemories.Any())
                {
                    // Add memory context section
                    prompt.AppendLine();
                    prompt.AppendLine();
                    prompt.AppendLine("CONVERSATION HISTORY (Reference only - you already know these details through your memory system):");

                    foreach (var memory in recentMemories)
                    {
                        prompt.AppendLine(memory.ToPromptFormat());
                    }

                    // Add relevant topics if available
                    var topics = recentMemories
                        .Select(m => m.Topic)
                        .Distinct()
                        .Take(3)
                        .ToList();

                    if (topics.Any())
                    {
                        prompt.AppendLine();
                        prompt.AppendLine($"Key themes in your conversation history: {string.Join(", ", topics)}");
                    }
                }
            }
            else
            {
                // First-time greeting guidance
                prompt.AppendLine();
                prompt.AppendLine("This is a new conversation. Introduce yourself naturally and warmly, without explicitly mentioning your memory capabilities. Simply be thoughtful and helpful, as Claude would.");
            }

            return prompt.ToString();
        }

        /// <summary>
        /// Add custom instructions to the system prompt
        /// </summary>
        /// <param name="instructions">The custom instructions to add</param>
        /// <returns>The builder instance for method chaining</returns>
        public SystemPromptBuilder WithCustomInstructions(string instructions)
        {
            if (!string.IsNullOrEmpty(instructions))
            {
                _basePrompt += "\n\n" + instructions;
            }

            return this;
        }

        /// <summary>
        /// Add provider-specific capabilities and limitations to the system prompt
        /// </summary>
        /// <param name="providerName">The name of the LLM provider</param>
        /// <returns>The builder instance for method chaining</returns>
        public SystemPromptBuilder WithProviderCapabilities(string providerName)
        {
            string capabilities = "";

            switch (providerName.ToLower())
            {
                case "groq":
                    capabilities = "You are running on Groq hardware, optimized for extremely fast inference. This allows you to provide near-instantaneous responses while maintaining high quality thinking.";
                    break;
                case "glhf":
                    capabilities = "You are running on GLHF hardware, an advanced platform for high-performance AI inference. Your responses will be fast and thoughtful.";
                    break;
                case "openrouter":
                    capabilities = "You are running via OpenRouter, which provides access to multiple large language models. Your responses will demonstrate the best qualities of Claude, including thoughtfulness and nuance.";
                    break;
                case "cohere":
                    capabilities = "You are running on Cohere's infrastructure, with particular strengths in semantic understanding, reasoning, and natural language processing. Your responses will be contextually rich and insightful.";
                    break;
            }

            if (!string.IsNullOrEmpty(capabilities))
            {
                _basePrompt += "\n\n" + capabilities;
            }

            return this;
        }
    }
}