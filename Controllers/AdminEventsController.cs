using Microsoft.AspNetCore.Mvc;
using VSSAuthPrototype.Middleware;
using VSSAuthPrototype.Models;
using VSSAuthPrototype.Models.DTOs;
using VSSAuthPrototype.Repositories;

namespace VSSAuthPrototype.Controllers
{
    [ApiController]
    [Route("api/admin/events")]
    public class AdminEventsController : ControllerBase
    {
        private readonly IStreamRepository _streamRepo;

        public AdminEventsController(IStreamRepository streamRepo)
        {
            _streamRepo = streamRepo;
        }

        public record CreateEventRequest(
            string Title,
            string Slug,
            string League,
            string HomeTeamName,
            string HomeTeamShort,
            string AwayTeamName,
            string AwayTeamShort,
            string Access = "free",                 // "free", "ppv", "subscriber"
            decimal? PriceUSD = null,
            string RequiredSubscription = "basic",  // "basic", "express", "premium"
            string? Description = null,
            string? Location = null,
            string? HomeTeamLogo = null,
            string? AwayTeamLogo = null,
            string? ThumbnailUrl = null,
            string? DacastIframeSrc = null,
            string? DacastStreamId = null,
            DateTime? ScheduledStart = null
        );

        /// <summary>
        /// POST /api/admin/events
        /// Admin creates a new event. Returns the created stream in frontend Stream format.
        /// </summary>
        [RequireAuth]
        [RequirePermission("events:write")]
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest req)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var createdBy = Guid.TryParse(userIdClaim, out var uid) ? uid : Guid.Empty;

            var stream = new VssStream
            {
                Title = req.Title,
                Slug = req.Slug,
                Description = req.Description,
                League = req.League,
                HomeTeamName = req.HomeTeamName,
                HomeTeamShort = req.HomeTeamShort,
                HomeTeamLogo = req.HomeTeamLogo,
                AwayTeamName = req.AwayTeamName,
                AwayTeamShort = req.AwayTeamShort,
                AwayTeamLogo = req.AwayTeamLogo,
                Location = req.Location,
                Access = req.Access,
                PriceUSD = req.PriceUSD,
                RequiredSubscription = req.RequiredSubscription,
                ThumbnailUrl = req.ThumbnailUrl,
                DacastIframeSrc = req.DacastIframeSrc,
                DacastStreamId = req.DacastStreamId,
                ScheduledStart = req.ScheduledStart,
                CreatedBy = createdBy
            };

            var created = await _streamRepo.CreateAsync(stream);

            // Return in frontend Stream format
            var dto = new StreamDto
            {
                Id = created.Id.ToString(),
                Slug = created.Slug,
                Title = created.Title,
                League = created.League ?? "Sports",
                Home = new StreamTeamDto
                {
                    Name = created.HomeTeamName ?? "Home",
                    ShortName = created.HomeTeamShort ?? "HME",
                    Logo = created.HomeTeamLogo
                },
                Away = new StreamTeamDto
                {
                    Name = created.AwayTeamName ?? "Away",
                    ShortName = created.AwayTeamShort ?? "AWY",
                    Logo = created.AwayTeamLogo
                },
                Location = created.Location,
                StartAt = (created.ScheduledStart ?? created.CreatedAt).ToUniversalTime().ToString("o"),
                Status = created.ScheduledStart.HasValue && created.ScheduledStart > DateTime.UtcNow ? "upcoming" : "live",
                Access = created.Access,
                PriceUSD = created.PriceUSD,
                ThumbnailUrl = created.ThumbnailUrl,
                DacastIframeSrc = created.DacastIframeSrc,
                DacastChannelId = created.DacastStreamId,
                CreatedAt = created.CreatedAt.ToUniversalTime().ToString("o"),
                UpdatedAt = (created.UpdatedAt ?? created.CreatedAt).ToUniversalTime().ToString("o")
            };

            return Ok(new
            {
                message = "Event created successfully.",
                stream = dto
            });
        }

        /// <summary>
        /// GET /api/admin/events
        /// Admin lists all events in frontend Stream format.
        /// </summary>
        [RequireAuth]
        [RequireAdmin]
        [HttpGet]
        public async Task<IActionResult> ListEvents()
        {
            var streams = await _streamRepo.GetAllAsync();

            var dtos = streams.Select(s => new StreamDto
            {
                Id = s.Id.ToString(),
                Slug = s.Slug,
                Title = s.Title,
                League = s.League ?? "Sports",
                Home = new StreamTeamDto
                {
                    Name = s.HomeTeamName ?? "Home",
                    ShortName = s.HomeTeamShort ?? "HME",
                    Logo = s.HomeTeamLogo
                },
                Away = new StreamTeamDto
                {
                    Name = s.AwayTeamName ?? "Away",
                    ShortName = s.AwayTeamShort ?? "AWY",
                    Logo = s.AwayTeamLogo
                },
                Location = s.Location,
                StartAt = (s.ScheduledStart ?? s.CreatedAt).ToUniversalTime().ToString("o"),
                Status = s.IsLive ? "live" : (s.ScheduledStart.HasValue && s.ScheduledStart > DateTime.UtcNow ? "upcoming" : "past"),
                Access = s.Access,
                PriceUSD = s.PriceUSD,
                ThumbnailUrl = s.ThumbnailUrl,
                DacastIframeSrc = s.DacastIframeSrc,
                DacastChannelId = s.DacastStreamId,
                CreatedAt = s.CreatedAt.ToUniversalTime().ToString("o"),
                UpdatedAt = (s.UpdatedAt ?? s.CreatedAt).ToUniversalTime().ToString("o")
            }).ToList();

            return Ok(new
            {
                total = dtos.Count,
                streams = dtos
            });
        }

        /// <summary>
        /// PUT /api/admin/events/{id}/go-live
        /// </summary>
        [RequireAuth]
        [RequireAdmin]
        [HttpPut("{id:guid}/go-live")]
        public async Task<IActionResult> GoLive([FromRoute] Guid id)
        {
            var stream = await _streamRepo.GetByIdAsync(id);
            if (stream == null)
                return NotFound(new { error = "Event not found" });

            stream.IsLive = true;
            stream.ActualStart = DateTime.UtcNow;
            await _streamRepo.UpdateAsync(stream);

            return Ok(new { message = "Event is now LIVE.", eventId = id, isLive = true, actualStart = stream.ActualStart });
        }

        /// <summary>
        /// PUT /api/admin/events/{id}/end
        /// </summary>
        [RequireAuth]
        [RequireAdmin]
        [HttpPut("{id:guid}/end")]
        public async Task<IActionResult> EndEvent([FromRoute] Guid id)
        {
            var stream = await _streamRepo.GetByIdAsync(id);
            if (stream == null)
                return NotFound(new { error = "Event not found" });

            stream.IsLive = false;
            stream.ActualEnd = DateTime.UtcNow;
            await _streamRepo.UpdateAsync(stream);

            return Ok(new { message = "Event ended. Available as VOD.", eventId = id, isLive = false, actualEnd = stream.ActualEnd });
        }

        /// <summary>
        /// DELETE /api/admin/events/{id}
        /// </summary>
        [RequireAuth]
        [RequireAdmin]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteEvent([FromRoute] Guid id)
        {
            var deleted = await _streamRepo.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { error = "Event not found" });

            return Ok(new { message = "Event deleted.", eventId = id });
        }
    }
}