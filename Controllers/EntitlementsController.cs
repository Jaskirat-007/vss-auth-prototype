using Microsoft.AspNetCore.Mvc;
using VSSAuthPrototype.Middleware;

namespace VSSAuthPrototype.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class EntitlementsController : ControllerBase
    {
        [RequireAuth]
        [HttpGet("entitlements")]
        public IActionResult GetEntitlements()
        {
            var user = HttpContext.User;

            var role = user.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            // permissions claim can come as:
            // - one claim with CSV: "events:write,streams:manage"
            // - OR multiple claims: permissions=events:write, permissions=streams:manage
            var permValues = user.Claims.Where(c => c.Type == "permissions").Select(c => c.Value).ToList();
            var perms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var val in permValues)
            {
                foreach (var p in val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    perms.Add(p);
            }

            var isAdmin = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);

            // Decide "can watch" from either role or permission
            var canWatchContent =
                isAdmin ||
                string.Equals(role, "subscriber", StringComparison.OrdinalIgnoreCase) ||
                perms.Contains("content:watch");

            var canUploadStorage =
                isAdmin ||
                perms.Contains("storage:upload");

            return Ok(new
            {
                role = role ?? "none",
                permissions = perms.ToArray(),
                isAdmin,
                canWatchContent,
                canUploadStorage
            });
        }
    }
}
