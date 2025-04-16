using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace HoveringBallApp
{
    public class SearXNGClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public SearXNGClient(string baseUrl = "http://localhost:8080")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
        }

        public async Task<List<SearchResult>> Search(string query)
        {
            try
            {
                var searchUrl = $"{_baseUrl}/search?q={Uri.EscapeDataString(query)}&format=json";
                var response = await _httpClient.GetAsync(searchUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var searchResults = JsonSerializer.Deserialize<SearchResponse>(jsonResponse);
                    return searchResults?.Results ?? new List<SearchResult>();
                }

                return new List<SearchResult>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search error: {ex.Message}");
                return new List<SearchResult>();
            }
        }
    }

    public class SearchResponse
    {
        public List<SearchResult> Results { get; set; } = new List<SearchResult>();
    }

    public class SearchResult
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Content { get; set; }
    }
}