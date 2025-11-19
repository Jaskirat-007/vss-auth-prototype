using VSSAuthPrototype.Models;
using VSSAuthPrototype.Models.DTOs;

namespace VSSAuthPrototype.Services
{
    public interface IAuthService
    {
        // We will add the ClerkLogin method later
        // Task<LoginResponse> ClerkLogin(string clerkToken);

        string GenerateVssToken(User user);
        List<string> GetPermissionsBySubscription(string subscriptionPlan, string role);
    }
}
