using Microsoft.AspNetCore.Mvc;
using VSSAuthPrototype.Middleware;
using VSSAuthPrototype.Models.DTOs;
using VSSAuthPrototype.Services;

namespace VSSAuthPrototype.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StreamController : ControllerBase
    {
        private readonly IAuthService _authService;

        public StreamController(IAuthService authService)
        {
            _authService = authService;
        }

        // Mock stream data (replace with database later)
        private List<MockStream> GetMockStreams()
        {
            return new List<MockStream>
            {
                new MockStream 
                { 
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), 
                    Title = "Free Preview Game", 
                    Slug = "free-preview", 
                    RequiredSubscription = "basic",
                    IsLive = true,
                    Description = "Free for all users"
                },
                new MockStream 
                { 
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), 
                    Title = "Championship Finals", 
                    Slug = "championship-finals", 
                    RequiredSubscription = "premium",
                    IsLive = true,
                    Description = "Premium subscribers only"
                },
                new MockStream 
                { 
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), 
                    Title = "Express Pass Exclusive", 
                    Slug = "express-exclusive", 
                    RequiredSubscription = "express",
                    IsLive = false,
                    Description = "For Express Pass holders",
                    ScheduledStart = DateTime.UtcNow.AddHours(2)
                }
            };
        }

        [HttpGet("my-streams")]
        [RequireAuth]
        public IActionResult GetMyStreams()
        {
            var userSubscription = User.Claims.FirstOrDefault(c => c.Type == "subscriptionPlan")?.Value ?? "basic";
            var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value ?? "viewer";
            var permissions = _authService.GetPermissionsBySubscription(userSubscription, role);

            var streams = GetMockStreams();
            var streamList = streams.Select(s => new StreamAccessResponse
            {
                StreamId = s.Id,
                Title = s.Title,
                Slug = s.Slug,
                HasAccess = CanAccessStream(permissions, s.RequiredSubscription, role),
                IsLocked = !CanAccessStream(permissions, s.RequiredSubscription, role),
                RequiredSubscription = s.RequiredSubscription,
                LockedReason = !CanAccessStream(permissions, s.RequiredSubscription, role) 
                    ? $"Requires {s.RequiredSubscription} subscription" 
                    : null
            }).ToList();

            return Ok(new StreamListResponse
            {
                Streams = streamList,
                UserSubscription = userSubscription
            });
        }

        [HttpGet("{slug}")]
        [RequireAuth]
        public IActionResult GetStreamBySlug(string slug)
        {
            var stream = GetMockStreams().FirstOrDefault(s => s.Slug == slug);
            
            if (stream == null)
            {
                return NotFound(new { error = "Stream not found" });
            }

            var userSubscription = User.Claims.FirstOrDefault(c => c.Type == "subscriptionPlan")?.Value ?? "basic";
            var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value ?? "viewer";
            var permissions = _authService.GetPermissionsBySubscription(userSubscription, role);
            
            bool hasAccess = CanAccessStream(permissions, stream.RequiredSubscription, role);

            var response = new StreamDetailResponse
            {
                StreamId = stream.Id,
                Title = stream.Title,
                Description = stream.Description,
                Slug = stream.Slug,
                IsLive = stream.IsLive,
                ScheduledStart = stream.ScheduledStart,
                HasAccess = hasAccess,
                StreamUrl = hasAccess && stream.IsLive ? $"https://dacast.com/embed/{stream.Id}" : null,
                RequiredSubscription = stream.RequiredSubscription
            };

            if (!hasAccess)
            {
                return StatusCode(402, new 
                { 
                    error = "Payment Required",
                    message = $"This stream requires a {stream.RequiredSubscription} subscription",
                    stream = response,
                    upgradeUrl = "/subscribe"
                });
            }

            return Ok(response);
        }

        [HttpGet("{slug}/access-check")]
        [RequireAuth]
        public IActionResult CheckStreamAccess(string slug)
        {
            var stream = GetMockStreams().FirstOrDefault(s => s.Slug == slug);
            
            if (stream == null)
            {
                return NotFound(new { error = "Stream not found" });
            }

            var userSubscription = User.Claims.FirstOrDefault(c => c.Type == "subscriptionPlan")?.Value ?? "basic";
            var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value ?? "viewer";
            var permissions = _authService.GetPermissionsBySubscription(userSubscription, role);
            
            bool hasAccess = CanAccessStream(permissions, stream.RequiredSubscription, role);

            return Ok(new 
            { 
                hasAccess,
                streamTitle = stream.Title,
                requiredSubscription = stream.RequiredSubscription,
                userSubscription,
                isAdmin = role == "admin"
            });
        }

        private bool CanAccessStream(List<string> permissions, string requiredSubscription, string role)
        {
            // Admins can access everything
            if (role.ToLower() == "admin") return true;

            // Check for all-access permission
            if (permissions.Contains("stream:view:all")) return true;

            // Map subscription to permission
            return requiredSubscription.ToLower() switch
            {
                "basic" => permissions.Contains("stream:view:free"),
                "express" => permissions.Contains("stream:view:specific") || permissions.Contains("stream:view:all"),
                "premium" => permissions.Contains("stream:view:premium") || permissions.Contains("stream:view:all"),
                _ => false
            };
        }

        private class MockStream
        {
            public Guid Id { get; set; }
            public required string Title { get; set; }
            public required string Slug { get; set; }
            public required string RequiredSubscription { get; set; }
            public bool IsLive { get; set; }
            public string? Description { get; set; }
            public DateTime? ScheduledStart { get; set; }
        }
    }
}
