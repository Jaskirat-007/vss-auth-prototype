using Microsoft.AspNetCore.Mvc;
using VSSAuthPrototype.Middleware;
using VSSAuthPrototype.Models.DTOs;
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

        [RequireAuth]
        [HttpGet("{id:guid}/access")]
        public async Task<IActionResult> GetEventAccess([FromRoute] Guid id)
        {
            var stream = await _streamRepo.GetByIdAsync(id);
            if (stream == null)
                return NotFound(new { error = "Event not found" });

            var user = HttpContext.User;
            var role = user.Claims.FirstOrDefault(c => c.Type == "role")?.Value ?? "viewer";
            var permValues = user.Claims.Where(c => c.Type == "permissions").Select(c => c.Value).ToList();
            var perms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var val in permValues)
                foreach (var p in val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    perms.Add(p);

            var isAdmin = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);

            var allowed =
                isAdmin ||
                perms.Contains("stream:view:all") ||
                (stream.RequiredSubscription.ToLower() == "basic" && perms.Contains("stream:view:free")) ||
                (stream.RequiredSubscription.ToLower() == "express" && perms.Contains("stream:view:specific")) ||
                (stream.RequiredSubscription.ToLower() == "premium" && perms.Contains("stream:view:premium"));

            var status = stream.IsLive ? "live"
                : (stream.ScheduledStart.HasValue && stream.ScheduledStart > DateTime.UtcNow ? "upcoming" : "past");

            var dto = new StreamDto
            {
                Id = stream.Id.ToString(),
                Slug = stream.Slug,
                Title = stream.Title,
                League = stream.League ?? "Sports",
                SchoolA = stream.HomeTeamName ?? "Home",
                SchoolB = stream.AwayTeamName ?? "Away",
                StartAt = (stream.ScheduledStart ?? stream.CreatedAt).ToUniversalTime().ToString("o"),
                Status = status,
                Access = stream.Access,
                PriceUSD = stream.PriceUSD,
                ThumbnailUrl = stream.ThumbnailUrl,
                DacastIframeSrc = allowed ? stream.DacastIframeSrc : null,
                DacastChannelId = allowed ? stream.DacastStreamId : null,
                CreatedAt = stream.CreatedAt.ToUniversalTime().ToString("o"),
                UpdatedAt = (stream.UpdatedAt ?? stream.CreatedAt).ToUniversalTime().ToString("o")
            };

            if (!allowed)
            {
                return StatusCode(402, new
                {
                    error = "Payment Required",
                    message = "This content requires an active subscription.",
                    stream = dto,
                    upgradeUrl = "/test"
                });
            }

            if (!stream.IsLive && string.IsNullOrEmpty(dto.DacastIframeSrc))
            {
                var vodKey = $"livestreams/{stream.Slug}.mp4";
                var vodUrl = await _storageService.GetSignedUrlAsync(vodKey);
                if (string.IsNullOrEmpty(vodUrl))
                    vodUrl = await _storageService.GetVideoUrlByFilenameAsync($"{stream.Slug}.mp4");
                if (!string.IsNullOrEmpty(vodUrl))
                    dto.DacastIframeSrc = vodUrl;
            }

            return Ok(new { allowed = true, stream = dto });
        }
    }
}