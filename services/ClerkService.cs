using System.Net.Http.Headers;
using System.Text.Json;
using VSSAuthPrototype.Models;

namespace VSSAuthPrototype.Services
{
    public class ClerkService : IClerkService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ClerkService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            
            var apiKey = _configuration["Clerk:ApiKey"] ?? throw new InvalidOperationException("Clerk API Key not configured");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.BaseAddress = new Uri(_configuration["Clerk:Domain"] ?? throw new InvalidOperationException("Clerk Domain not configured"));
        }

        public async Task<User?> VerifyClerkTokenAsync(string clerkToken)
        {
            try
            {
                // Verify the Clerk session token
                var response = await _httpClient.GetAsync($"/v1/sessions/{clerkToken}");
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var sessionJson = await response.Content.ReadAsStringAsync();
                var session = JsonSerializer.Deserialize<ClerkSession>(sessionJson);
                
                if (session?.UserId == null)
                {
                    return null;
                }

                // Get full user details from Clerk
                return await GetUserFromClerkAsync(session.UserId);
            }
            catch
            {
                return null;
            }
        }

        public async Task<User?> GetUserFromClerkAsync(string clerkUserId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v1/users/{clerkUserId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var userJson = await response.Content.ReadAsStringAsync();
                var clerkUser = JsonSerializer.Deserialize<ClerkUser>(userJson);
                
                if (clerkUser == null)
                {
                    return null;
                }

                // Map Clerk user to VSS user model
                var user = new User
                {
                    Id = Guid.NewGuid(), // Generate new ID or use existing from database
                    Email = clerkUser.EmailAddresses?.FirstOrDefault()?.EmailAddress ?? "",
                    UserName = clerkUser.Username ?? clerkUser.FirstName ?? "user",
                    Role = clerkUser.PublicMetadata?.Role ?? "viewer",
                    SubscriptionPlan = clerkUser.PublicMetadata?.SubscriptionPlan ?? "basic",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                return user;
            }
            catch
            {
                return null;
            }
        }

        // DTOs for Clerk API responses
        private class ClerkSession
        {
            public string? UserId { get; set; }
        }

        private class ClerkUser
        {
            public string? Id { get; set; }
            public string? Username { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public List<EmailAddress>? EmailAddresses { get; set; }
            public ClerkMetadata? PublicMetadata { get; set; }
        }

        private class EmailAddress
        {
            public string? EmailAddress { get; set; }
        }

        private class ClerkMetadata
        {
            public string? Role { get; set; }
            public string? SubscriptionPlan { get; set; }
        }
    }
}
