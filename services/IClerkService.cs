using VSSAuthPrototype.Models;

namespace VSSAuthPrototype.Services
{
    public interface IClerkService
    {
        Task<User?> VerifyClerkTokenAsync(string clerkToken);
        Task<User?> GetUserFromClerkAsync(string clerkUserId);
    }
}
