using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VSSAuthPrototype.Models
{
    [Table("streams")]
    public class VssStream
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [MaxLength(200)]
        public required string Title { get; set; }
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(100)]
        public required string Slug { get; set; }

        // ── Sport / League ──
        [MaxLength(100)]
        public string? League { get; set; }

        // ── Teams ──
        [MaxLength(200)]
        public string? HomeTeamName { get; set; }

        [MaxLength(20)]
        public string? HomeTeamShort { get; set; }

        [MaxLength(500)]
        public string? HomeTeamLogo { get; set; }

        [MaxLength(200)]
        public string? AwayTeamName { get; set; }

        [MaxLength(20)]
        public string? AwayTeamShort { get; set; }

        [MaxLength(500)]
        public string? AwayTeamLogo { get; set; }

        // ── Location ──
        [MaxLength(200)]
        public string? Location { get; set; }

        // ── Access / Pricing ──
        [Required]
        [MaxLength(50)]
        public string Access { get; set; } = "free"; // "free", "ppv", "subscriber"

        [Column(TypeName = "decimal(10,2)")]
        public decimal? PriceUSD { get; set; }

        [Required]
        [MaxLength(50)]
        public string RequiredSubscription { get; set; } = "basic"; // "basic", "express", "premium"

        // ── Stream Status ──
        public bool IsLive { get; set; } = false;
        
        public DateTime? ScheduledStart { get; set; }
        
        public DateTime? ActualStart { get; set; }
        
        public DateTime? ActualEnd { get; set; }

        // ── Dacast ──
        [MaxLength(500)]
        public string? DacastStreamId { get; set; }

        [MaxLength(1000)]
        public string? DacastIframeSrc { get; set; }

        [MaxLength(500)]
        public string? RtmpUrl { get; set; }

        // ── Thumbnail ──
        [MaxLength(1000)]
        public string? ThumbnailUrl { get; set; }

        // ── Metadata ──
        public Guid CreatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsDeleted { get; set; } = false;
    }
}