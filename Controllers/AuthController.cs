using Microsoft.AspNetCore.Mvc;
using VSSAuthPrototype.Models;
using VSSAuthPrototype.Models.DTOs;
using VSSAuthPrototype.Services;

namespace VSSAuthPrototype.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("test-token")]
        public IActionResult GenerateTestToken(string role = "viewer", string subscription = "basic")
        {
            // This is a TEST endpoint - it creates a fake user to demonstrate JWT generation
            var testUser = new User
            {
                UserId = 99,
                Email = "test@varsitysportsshow.com",
                Username = "test_user",
                Role = role,
                SubscriptionPlan = subscription
            };

            // Generate the JWT token
            var token = _authService.GenerateVssToken(testUser);

            // Get the permissions for this user
            var permissions = _authService.GetPermissionsBySubscription(subscription, role);

            // Build the response
            var userInfo = new UserInfo
            {
                UserId = testUser.UserId,
                Username = testUser.Username,
                Email = testUser.Email,
                Role = testUser.Role,
                SubscriptionPlan = testUser.SubscriptionPlan,
                Permissions = permissions
            };

            return Ok(new
            {
                Message = "✅ This is a TEST token. It is NOT connected to a real user in the database.",
                UserInfo = userInfo,
                VssToken = token,
                ExpiresIn = 900 // 15 minutes in seconds
            });
        }
    }
}
