using Microsoft.AspNetCore.Mvc;
using VSSAuthPrototype.Middleware;
using VSSAuthPrototype.Repositories;
using VSSAuthPrototype.Services;

namespace VSSAuthPrototype.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsAccessController : ControllerBase
    {
        private readonly IStreamRepository _streamRepo;
        private readonly IStorageService _storageService;

        public EventsAccessController(IStreamRepository streamRepo, IStorageService storageService)
        {
            _streamRepo = streamRepo;
            _storageService = storageService;
        }

        /// <summary>
        /// GET /api/events/{id}/access
        /// THE MAIN ENDPOINT — checks auth + entitlement, returns actual video URL.
        /// Frontend calls this when user clicks "Watch".
        /// </summary>
        [RequireAuth]
        [HttpGet("{id:guid}/access")]
        public async Task<IActionResult> GetEventAccess([FromRoute] Guid id)
        {
            // 1. Find the stream
            var stream = await _streamRepo.GetByIdAsync(id);
            if (stream == null)
                return NotFound(new { error = "Event not found" });

            // 2. Check permissions from JWT claims
            var user = HttpContext.User;
            var role = user.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            var permValues = user.Claims.Where(c => c.Type == "permissions").Select(c => c.Value).ToList();
            var perms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var val in permValues)
                foreach (var p in val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    perms.Add(p);

            var isAdmin = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);

            var allowed =
                isAdmin ||
                string.Equals(role, "subscriber", StringComparison.OrdinalIgnoreCase) ||
                perms.Contains("content:watch") ||
                perms.Contains("stream:view:all") ||
                (stream.RequiredSubscription.ToLower() == "basic" && perms.Contains("stream:view:free")) ||
                (stream.RequiredSubscription.ToLower() == "express" && perms.Contains("stream:view:specific")) ||
                (stream.RequiredSubscription.ToLower() == "premium" && perms.Contains("stream:view:premium"));

            if (!allowed)
            {
                return StatusCode(402, new
                {
                    error = "Payment Required",
                    message = "This content requires an active subscription.",
                    requiredSubscription = stream.RequiredSubscription,
                    upgradeUrl = "/subscribe"
                });
            }

            // 3. Resolve video URL based on live vs VOD
            string? streamUrl = null;
            string? vodUrl = null;
            string? thumbnailUrl = null;
            string accessType;

            if (stream.IsLive && !string.IsNullOrEmpty(stream.DacastStreamId))
            {
                // ── LIVE: Dacast embed ──
                accessType = "live";
                streamUrl = $"https://iframe.dacast.com/live/{stream.DacastStreamId}";
            }
            else
            {
                // ── VOD: Signed B2 URL ──
                accessType = "vod";
                var vodKey = $"livestreams/{stream.Slug}.mp4";
                vodUrl = await _storageService.GetSignedUrlAsync(vodKey);

                // Fallback: scan /list-videos if /signed-url isn't deployed yet
                if (string.IsNullOrEmpty(vodUrl))
                {
                    vodUrl = await _storageService.GetVideoUrlByFilenameAsync($"{stream.Slug}.mp4");
                }
            }

            // Thumbnail
            var thumbKey = $"thumbnails/{stream.Slug}.png";
            thumbnailUrl = await _storageService.GetSignedUrlAsync(thumbKey);
            if (string.IsNullOrEmpty(thumbnailUrl))
            {
                thumbnailUrl = await _storageService.GetThumbnailUrlByFilenameAsync($"{stream.Slug}.mp4");
            }

            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);

            return Ok(new
            {
                allowed = true,
                eventId = id,
                title = stream.Title,
                accessType,
                streamUrl,
                vodUrl,
                thumbnailUrl,
                expiresAt = expiresAt.ToString("o")
            });
        }
    }
}