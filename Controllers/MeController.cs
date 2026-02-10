using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VSSAuthPrototype.Controllers
{
    [ApiController]
    public class MeController : ControllerBase
    {
        [Authorize]
        [HttpGet("api/me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                isAuthenticated = User.Identity?.IsAuthenticated,
                clerkUserId = User.FindFirst("sub")?.Value,
                claims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }
    }
}
