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
                Id = Guid.NewGuid(),
                Email = "test@varsitysportsshow.com",
                UserName = "test_user",
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
                UserId = testUser.Id.GetHashCode(), 
                Username = testUser.UserName,
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

        [HttpPost("login-mock")]
        public IActionResult MockLogin([FromBody] MockLoginRequest request)
        {
            // This is for TESTING ONLY - simulates a login
            var testUser = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                UserName = request.Email.Split('@')[0],
                Role = request.Role ?? "viewer",
                SubscriptionPlan = request.SubscriptionPlan ?? "basic"
            };

            var token = _authService.GenerateVssToken(testUser);
            var permissions = _authService.GetPermissionsBySubscription(testUser.SubscriptionPlan, testUser.Role);

            var userInfo = new UserInfo
            {
                UserId = testUser.Id.GetHashCode(),
                Username = testUser.UserName,
                Email = testUser.Email,
                Role = testUser.Role,
                SubscriptionPlan = testUser.SubscriptionPlan,
                Permissions = permissions
            };

            return Ok(new LoginResponse
            {
                VssToken = token,
                ExpiresIn = 900,
                User = userInfo
            });
        }

        [HttpPost("clerk-login")]
        public async Task<IActionResult> ClerkLogin([FromBody] ClerkLoginRequest request, [FromServices] IClerkService clerkService)
        {
            // Verify Clerk token and get user data
            var clerkUser = await clerkService.VerifyClerkTokenAsync(request.ClerkToken);
            
            if (clerkUser == null)
            {
                return Unauthorized(new { error = "Invalid Clerk token" });
            }

            // Generate VSS JWT token
            var vssToken = _authService.GenerateVssToken(clerkUser);
            var permissions = _authService.GetPermissionsBySubscription(clerkUser.SubscriptionPlan, clerkUser.Role);

            var userInfo = new UserInfo
            {
                UserId = clerkUser.Id.GetHashCode(),
                Username = clerkUser.UserName ?? "user",
                Email = clerkUser.Email,
                Role = clerkUser.Role,
                SubscriptionPlan = clerkUser.SubscriptionPlan,
                Permissions = permissions
            };

            return Ok(new LoginResponse
            {
                VssToken = vssToken,
                ExpiresIn = 900,
                User = userInfo
            });
        }

        public class MockLoginRequest
        {
            public required string Email { get; set; }
            public string? Role { get; set; }
            public string? SubscriptionPlan { get; set; }
        }

        public class ClerkLoginRequest
        {
            public required string ClerkToken { get; set; }
        }
    }
}
