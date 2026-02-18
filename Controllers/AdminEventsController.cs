using Microsoft.AspNetCore.Mvc;
using VSSAuthPrototype.Middleware;

namespace VSSAuthPrototype.Controllers
{
    [ApiController]
    [Route("api/admin/events")]
    public class AdminEventsController : ControllerBase
    {
        public record CreateEventRequest(string Title, DateTimeOffset StartTime, string Sport);

        [RequireAuth]
        [RequirePermission("events:write")]
        [HttpPost]
        public IActionResult CreateEvent([FromBody] CreateEventRequest req)
        {
            // Stub: later will write to DB.
            var created = new
            {
                id = new Random().Next(1000, 9999),
                title = req.Title,
                startTime = req.StartTime,
                sport = req.Sport,
                status = "scheduled"
            };

            return Ok(new
            {
                message = "Event created (stub).",
                eventData = created
            });
        }
    }
}
