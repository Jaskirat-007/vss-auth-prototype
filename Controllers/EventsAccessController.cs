using Microsoft.AspNetCore.Mvc;
using VSSAuthPrototype.Middleware;

namespace VSSAuthPrototype.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsAccessController : ControllerBase
    {
        // Stubbed “event type” for now. Later DB/streaming will decide live/vod.
        private static string GetAccessTypeStub(int eventId)
            => (eventId % 2 == 0) ? "live" : "vod";

        [RequireAuth]
        [HttpGet("{id:int}/access")]
        public IActionResult GetEventAccess([FromRoute] int id)
        {
            var user = HttpContext.User;
            var role = user.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            var permValues = user.Claims.Where(c => c.Type == "permissions").Select(c => c.Value).ToList();
            var perms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var val in permValues)
            {
                foreach (var p in val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    perms.Add(p);
            }

            var isAdmin = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);

            var allowed =
                isAdmin ||
                string.Equals(role, "subscriber", StringComparison.OrdinalIgnoreCase) ||
                perms.Contains("content:watch");

            if (!allowed)
            {
                return StatusCode(402, new
                {
                    error = "Payment Required",
                    message = "This content requires an active subscription.",
                    requiredPermission = "content:watch",
                    upgradeUrl = "/subscribe"
                });
            }

            var accessType = GetAccessTypeStub(id);
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);

            return Ok(new
            {
                allowed = true,
                eventId = id,
                accessType,    // "live" or "vod"
                expiresAt = expiresAt.ToString("o")
            });
        }
    }
}
