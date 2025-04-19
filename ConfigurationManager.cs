using System;
using System.IO;
using System.Text.Json;

namespace HoveringBallApp
{
    /// <summary>
    /// Manages application configuration and environment variables
    /// </summary>
    public class ConfigurationManager
    {
        private JsonElement _config;

        // API Keys
        public string GroqApiKey { get; private set; }
        public string GLHFApiKey { get; private set; }
        public string OpenRouterApiKey { get; private set; }
        public string CohereApiKey { get; private set; }

        // Application settings
        public bool UseMemorySystem { get; internal set; } = true;
        public int MemoryLimit { get; internal set; } = 10;

        /// <summary>
        /// Initializes a new instance of ConfigurationManager
        /// </summary>
        public ConfigurationManager()
        {
            LoadConfiguration();
        }

        /// <summary>
        /// Loads configuration from appsettings.json, .env file, or environment variables
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                // Try to load from appsettings.json first
                if (File.Exists("appsettings.json"))
                {
                    string json = File.ReadAllText("appsettings.json");
                    _config = JsonSerializer.Deserialize<JsonElement>(json);
                }

                // API Keys - try JSON first, then environment variables
                GroqApiKey = GetConfigValue("NEXT_PUBLIC_GROQ_API_KEY", "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");
                GLHFApiKey = GetConfigValue("NEXT_PUBLIC_GLHF_API_KEY", "glhf_c76b963772fb03bde9947474dd0c17ed");
                OpenRouterApiKey = GetConfigValue("NEXT_PUBLIC_OPENROUTER_API_KEY", "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");
                CohereApiKey = GetConfigValue("NEXT_PUBLIC_COHERE_API_KEY", "SYUCPBj4nkrHSf1tccBZ433OCocMmDQbxgouek7G");

                // Application settings
                string useMemory = GetConfigValue("USE_MEMORY_SYSTEM", "true");
                UseMemorySystem = bool.TryParse(useMemory, out bool memorySystem) && memorySystem;

                string memoryLimit = GetConfigValue("MEMORY_LIMIT", "10");
                MemoryLimit = int.TryParse(memoryLimit, out int limit) ? limit : 10;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");

                // Set default values if configuration loading fails
                GroqApiKey = "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";
                GLHFApiKey = "glhf_c76b963772fb03bde9947474dd0c17ed";
                OpenRouterApiKey = "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";
                CohereApiKey = "SYUCPBj4nkrHSf1tccBZ433OCocMmDQbxgouek7G";
            }
        }

        /// <summary>
        /// Gets a configuration value from JSON config, environment variable, or default value
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>The configuration value</returns>
        private string GetConfigValue(string key, string defaultValue)
        {
            // Try to get from JSON config
            if (_config.ValueKind == JsonValueKind.Object &&
                _config.TryGetProperty(key, out JsonElement value) &&
                value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            // Try to get from environment variable
            string envValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(envValue))
            {
                return envValue;
            }

            // Fall back to default
            return defaultValue;
        }

        /// <summary>
        /// Saves the current configuration to appsettings.json
        /// </summary>
        public void SaveConfiguration()
        {
            var config = new
            {
                NEXT_PUBLIC_GROQ_API_KEY = GroqApiKey,
                NEXT_PUBLIC_GLHF_API_KEY = GLHFApiKey,
                NEXT_PUBLIC_OPENROUTER_API_KEY = OpenRouterApiKey,
                NEXT_PUBLIC_COHERE_API_KEY = CohereApiKey,
                USE_MEMORY_SYSTEM = UseMemorySystem.ToString(),
                MEMORY_LIMIT = MemoryLimit.ToString()
            };

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("appsettings.json", json);
        }

        /// <summary>
        /// Updates an API key value
        /// </summary>
        /// <param name="provider">The API provider name</param>
        /// <param name="apiKey">The new API key</param>
        public void UpdateApiKey(string provider, string apiKey)
        {
            switch (provider.ToLower())
            {
                case "groq":
                    GroqApiKey = apiKey;
                    break;
                case "glhf":
                    GLHFApiKey = apiKey;
                    break;
                case "openrouter":
                    OpenRouterApiKey = apiKey;
                    break;
                case "cohere":
                    CohereApiKey = apiKey;
                    break;
            }

            SaveConfiguration();
        }
    }
}