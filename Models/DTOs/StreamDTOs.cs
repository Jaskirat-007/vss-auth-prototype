namespace VSSAuthPrototype.Models.DTOs
{
    // ── Matches frontend Stream interface exactly ──
    public class StreamDto
    {
        public required string Id { get; set; }
        public required string Slug { get; set; }
        public required string Title { get; set; }
        public required string League { get; set; }
        public required string SchoolA { get; set; }
        public required string SchoolB { get; set; }
        public required string StartAt { get; set; }          // ISO string
        public required string Status { get; set; }           // "live", "upcoming", "past"
        public required string Access { get; set; }           // "free", "ppv", "subscriber"
        public decimal? PriceUSD { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? DacastIframeSrc { get; set; }
        public string? DacastChannelId { get; set; }
        public required string CreatedAt { get; set; }
        public required string UpdatedAt { get; set; }
    }

    // ── List response wrapper ──
    public class StreamListResponse
    {
        public required List<StreamDto> Streams { get; set; }
        public required string UserSubscription { get; set; }
    }
}