using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VSSAuthPrototype.Models;
using VSSAuthPrototype.Models.DTOs;

namespace VSSAuthPrototype.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        // private readonly IUserRepository _userRepository; // We will add this later

        public AuthService(IConfiguration configuration /*, IUserRepository userRepository */)
        {
            _configuration = configuration;
            // _userRepository = userRepository; // Add this later
        }

        public string GenerateVssToken(User user)
        {
            var secret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var permissions = GetPermissionsBySubscription(user.SubscriptionPlan, user.Role);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("username", user.Username ?? ""),
                new Claim("role", user.Role),
                new Claim("subscriptionPlan", user.SubscriptionPlan),
                new Claim("permissions", string.Join(",", permissions)),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:AccessTokenExpirationMinutes"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public List<string> GetPermissionsBySubscription(string subscriptionPlan, string role)
        {
            var permissions = new List<string>();

            // Assign permissions based on role first
            if (role.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                permissions.AddRange(new[] { "stream:create", "stream:manage", "stream:delete", "user:manage" });
            }
            else if (role.Equals("sponsor", StringComparison.OrdinalIgnoreCase))
            {
                permissions.AddRange(new[] { "stream:create", "stream:manage" });
            }

            // Add viewing permissions based on subscription plan
            switch (subscriptionPlan?.ToLower())
            {
                case "premium":
                    permissions.AddRange(new[] { "stream:view:all", "stream:view:premium", "stream:view:free" });
                    break;
                case "giveaway":
                case "express":
                    permissions.AddRange(new[] { "stream:view:specific", "stream:view:free" });
                    break;
                default: // "basic" or null
                    permissions.Add("stream:view:free");
                    break;
            }
            
            return permissions.Distinct().ToList();
        }
    }
}
