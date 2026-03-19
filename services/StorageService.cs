using System.Text.Json;
using System.Text.Json.Serialization;

namespace VSSAuthPrototype.Services
{
    public class StorageService : IStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _storageBaseUrl;

        public StorageService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            // Points to teammate's Flask B2 backend (his server.py)
            _storageBaseUrl = configuration["Storage:BaseUrl"] ?? "http://localhost:5000";
        }

        /// <summary>
        /// PRIMARY: Calls GET /signed-url?key=livestreams/game.mp4
        /// Returns a time-limited download URL from B2.
        /// </summary>
        public async Task<string?> GetSignedUrlAsync(string key)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_storageBaseUrl}/signed-url?key={Uri.EscapeDataString(key)}");

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SignedUrlResponse>(json);

                return result?.Url;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// FALLBACK: Scans /list-videos to find a video match by filename.
        /// Works with the storage teammate's existing endpoint.
        /// </summary>
        public async Task<string?> GetVideoUrlByFilenameAsync(string filename)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_storageBaseUrl}/list-videos");

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var videos = JsonSerializer.Deserialize<List<B2VideoEntry>>(json);

                var match = videos?.FirstOrDefault(v =>
                    v.Title != null && v.Title.Equals(filename, StringComparison.OrdinalIgnoreCase));

                return match?.VideoUrl;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// FALLBACK: Scans /list-videos to find a thumbnail match by filename.
        /// </summary>
        public async Task<string?> GetThumbnailUrlByFilenameAsync(string filename)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_storageBaseUrl}/list-videos");

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var videos = JsonSerializer.Deserialize<List<B2VideoEntry>>(json);

                var match = videos?.FirstOrDefault(v =>
                    v.Title != null && v.Title.Equals(filename, StringComparison.OrdinalIgnoreCase));

                return match?.ThumbnailUrl;
            }
            catch
            {
                return null;
            }
        }

        // ── DTOs for B2 backend responses ──

        private class SignedUrlResponse
        {
            [JsonPropertyName("url")]
            public string? Url { get; set; }
        }

        private class B2VideoEntry
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("videoUrl")]
            public string? VideoUrl { get; set; }

            [JsonPropertyName("thumbnailUrl")]
            public string? ThumbnailUrl { get; set; }
        }
    }
}