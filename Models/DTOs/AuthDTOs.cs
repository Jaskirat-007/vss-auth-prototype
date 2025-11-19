namespace VSSAuthPrototype.Models.DTOs
{
    public class LoginResponse
    {
        public required string VssToken { get; set; }
        public int ExpiresIn { get; set; }
        public required UserInfo User { get; set; }
    }

    public class UserInfo
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public required string SubscriptionPlan { get; set; }
        public required List<string> Permissions { get; set; }
    }
}
