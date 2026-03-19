using Microsoft.AspNetCore.Mvc;
using VSSAuthPrototype.Middleware;
using VSSAuthPrototype.Models;
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
            string Sport,
            string Slug,
            string RequiredSubscription = "basic",
            string? Description = null,
            string? DacastStreamId = null,
            DateTime? ScheduledStart = null
        );

        /// <summary>
        /// POST /api/admin/events
        /// Admin creates a new event/stream. Persists to the Streams table.
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
                Description = req.Description ?? $"{req.Sport} event",
                RequiredSubscription = req.RequiredSubscription,
                DacastStreamId = req.DacastStreamId,
                ScheduledStart = req.ScheduledStart,
                CreatedBy = createdBy
            };

            var created = await _streamRepo.CreateAsync(stream);

            return Ok(new
            {
                message = "Event created successfully.",
                eventData = new
                {
                    id = created.Id,
                    title = created.Title,
                    slug = created.Slug,
                    requiredSubscription = created.RequiredSubscription,
                    dacastStreamId = created.DacastStreamId,
                    scheduledStart = created.ScheduledStart,
                    status = "scheduled"
                }
            });
        }

        /// <summary>
        /// GET /api/admin/events
        /// Admin lists all events.
        /// </summary>
        [RequireAuth]
        [RequireAdmin]
        [HttpGet]
        public async Task<IActionResult> ListEvents()
        {
            var streams = await _streamRepo.GetAllAsync();

            return Ok(new
            {
                total = streams.Count,
                events = streams.Select(s => new
                {
                    id = s.Id,
                    title = s.Title,
                    slug = s.Slug,
                    requiredSubscription = s.RequiredSubscription,
                    isLive = s.IsLive,
                    scheduledStart = s.ScheduledStart,
                    createdAt = s.CreatedAt
                })
            });
        }

        /// <summary>
        /// PUT /api/admin/events/{id}/go-live
        /// Flips a scheduled event to live status.
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

            return Ok(new
            {
                message = "Event is now LIVE.",
                eventId = id,
                isLive = true,
                actualStart = stream.ActualStart
            });
        }

        /// <summary>
        /// PUT /api/admin/events/{id}/end
        /// Ends a live event (marks it as VOD).
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

            return Ok(new
            {
                message = "Event ended. Available as VOD.",
                eventId = id,
                isLive = false,
                actualEnd = stream.ActualEnd
            });
        }

        /// <summary>
        /// DELETE /api/admin/events/{id}
        /// Soft-deletes an event.
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