namespace VSSAuthPrototype.Models.DTOs
{
    public class StreamAccessResponse
    {
        public Guid StreamId { get; set; }
        public required string Title { get; set; }
        public required string Slug { get; set; }
        public bool HasAccess { get; set; }
        public bool IsLocked { get; set; }
        public string? RequiredSubscription { get; set; }
        public string? LockedReason { get; set; }
    }

    public class StreamListResponse
    {
        public required List<StreamAccessResponse> Streams { get; set; }
        public required string UserSubscription { get; set; }
    }

    public class StreamDetailResponse
    {
        public Guid StreamId { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public required string Slug { get; set; }
        public bool IsLive { get; set; }
        public DateTime? ScheduledStart { get; set; }
        public bool HasAccess { get; set; }
        public string? StreamUrl { get; set; }
        public string? RequiredSubscription { get; set; }
    }
}
