using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VSSAuthPrototype.Models
{
    [Table("Streams")]
    public class Stream
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
        
        [Required]
        [MaxLength(50)]
        public required string RequiredSubscription { get; set; } // "basic", "premium", "express"
        
        public bool IsLive { get; set; } = false;
        
        public DateTime? ScheduledStart { get; set; }
        
        public DateTime? ActualStart { get; set; }
        
        public DateTime? ActualEnd { get; set; }
        
        [MaxLength(500)]
        public string? DacastStreamId { get; set; }
        
        [MaxLength(500)]
        public string? RtmpUrl { get; set; }
        
        public Guid CreatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsDeleted { get; set; } = false;
    }
}
