using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VSSAuthPrototype.Models
{
    [Table("AbpUsers")]
    public class User
    {
        [Key]
        public Guid Id { get; set; }
        
        public Guid? TenantId { get; set; }
        
        [Required]
        [MaxLength(256)]
        public required string UserName { get; set; }
        
        [MaxLength(256)]
        public string? NormalizedUserName { get; set; }
        
        [MaxLength(256)]
        public string? Name { get; set; }
        
        [MaxLength(256)]
        public string? Surname { get; set; }
        
        [Required]
        [MaxLength(256)]
        public required string Email { get; set; }
        
        [MaxLength(256)]
        public string? NormalizedEmail { get; set; }
        
        public bool EmailConfirmed { get; set; }
        
        [MaxLength(256)]
        public string? PasswordHash { get; set; }
        
        [MaxLength(128)]
        public string? SecurityStamp { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [MaxLength(16)]
        public string? PhoneNumber { get; set; }
        
        public bool PhoneNumberConfirmed { get; set; }
        
        public bool TwoFactorEnabled { get; set; }
        
        public DateTimeOffset? LockoutEnd { get; set; }
        
        public bool LockoutEnabled { get; set; }
        
        public int AccessFailedCount { get; set; }
        
        public bool ShouldChangePasswordOnNextLogin { get; set; }
        
        public Guid? EntityVersion { get; set; }
        
        // Custom field for storing subscription data as JSON
        public string? ExtraProperties { get; set; }
        
        public DateTimeOffset? LastPasswordChangeTime { get; set; }
        
        // Not mapped - derived from ExtraProperties
        [NotMapped]
        public string Role { get; set; } = "viewer";
        
        [NotMapped]
        public string SubscriptionPlan { get; set; } = "basic";
        
        [NotMapped]
        public DateTime? SubscriptionExpiry { get; set; }
    }
}
