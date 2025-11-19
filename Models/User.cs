namespace VSSAuthPrototype.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string? ClerkUserId { get; set; }
        public string? Username { get; set; }
        public required string Email { get; set; }
        public string? PasswordHash { get; set; }
        public required string Role { get; set; } // e.g., "admin", "sponsor", "viewer"
        public required string SubscriptionPlan { get; set; } // e.g., "basic", "premium"
        public DateTime? SubscriptionExpiry { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
