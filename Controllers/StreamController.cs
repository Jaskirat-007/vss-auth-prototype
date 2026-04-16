using Microsoft.AspNetCore.Mvc;
using VSSAuthPrototype.Middleware;
using VSSAuthPrototype.Models;
using VSSAuthPrototype.Models.DTOs;
using VSSAuthPrototype.Repositories;
using VSSAuthPrototype.Services;

namespace VSSAuthPrototype.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StreamController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IStreamRepository _streamRepo;
        private readonly IStorageService _storageService;

        public StreamController(
            IAuthService authService,
            IStreamRepository streamRepo,
            IStorageService storageService)
        {
            _authService = authService;
            _streamRepo = streamRepo;
            _storageService = storageService;
        }

        [HttpGet("my-streams")]
        [RequireAuth]
        public async Task<IActionResult> GetMyStreams()
        {
            var userSubscription = User.Claims.FirstOrDefault(c => c.Type == "subscriptionPlan")?.Value ?? "basic";
            var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value ?? "viewer";
            var permissions = _authService.GetPermissionsBySubscription(userSubscription, role);

            var streams = await _streamRepo.GetAllAsync();

            var streamList = streams.Select(s => MapToStreamDto(s, permissions, role)).ToList();

            return Ok(new StreamListResponse
            {
                Streams = streamList,
                UserSubscription = userSubscription
            });
        }

        [HttpGet("{slug}")]
        [RequireAuth]
        public async Task<IActionResult> GetStreamBySlug(string slug)
        {
            var stream = await _streamRepo.GetBySlugAsync(slug);

            if (stream == null)
                return NotFound(new { error = "Stream not found" });

            var userSubscription = User.Claims.FirstOrDefault(c => c.Type == "subscriptionPlan")?.Value ?? "basic";
            var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value ?? "viewer";
            var permissions = _authService.GetPermissionsBySubscription(userSubscription, role);

            bool hasAccess = CanAccessStream(permissions, stream.RequiredSubscription, role);

            var dto = MapToStreamDto(stream, permissions, role);

            if (!hasAccess)
            {
                dto.DacastIframeSrc = null;
                dto.DacastChannelId = null;

                return StatusCode(402, new
                {
                    error = "Payment Required",
                    message = stream.Access == "ppv"
                        ? $"This game is pay-per-view · ${stream.PriceUSD?.ToString("F2") ?? "–"}"
                        : $"This content requires a {stream.RequiredSubscription} subscription",
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

            return Ok(dto);
        }

        [HttpGet("{slug}/access-check")]
        [RequireAuth]
        public async Task<IActionResult> CheckStreamAccess(string slug)
        {
            var stream = await _streamRepo.GetBySlugAsync(slug);

            if (stream == null)
                return NotFound(new { error = "Stream not found" });

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

        private StreamDto MapToStreamDto(VssStream s, List<string> permissions, string role)
        {
            var status = ComputeStatus(s);
            bool hasAccess = CanAccessStream(permissions, s.RequiredSubscription, role);

            return new StreamDto
            {
                Id = s.Id.ToString(),
                Slug = s.Slug,
                Title = s.Title,
                League = s.League ?? "Sports",
                SchoolA = s.HomeTeamName ?? "Home",
                SchoolB = s.AwayTeamName ?? "Away",
                StartAt = (s.ScheduledStart ?? s.CreatedAt).ToUniversalTime().ToString("o"),
                Status = status,
                Access = s.Access,
                PriceUSD = s.PriceUSD,
                ThumbnailUrl = s.ThumbnailUrl,
                DacastIframeSrc = hasAccess ? s.DacastIframeSrc : null,
                DacastChannelId = hasAccess ? s.DacastStreamId : null,
                CreatedAt = s.CreatedAt.ToUniversalTime().ToString("o"),
                UpdatedAt = (s.UpdatedAt ?? s.CreatedAt).ToUniversalTime().ToString("o")
            };
        }

        private string ComputeStatus(VssStream s)
        {
            if (s.IsLive) return "live";
            var now = DateTime.UtcNow;
            var start = s.ScheduledStart ?? s.CreatedAt;
            if (now < start) return "upcoming";
            if ((now - start).TotalHours > 4) return "past";
            return "live";
        }

        private bool CanAccessStream(List<string> permissions, string requiredSubscription, string role)
        {
            if (role.Equals("admin", StringComparison.OrdinalIgnoreCase)) return true;
            if (permissions.Contains("stream:view:all")) return true;

            return requiredSubscription.ToLower() switch
            {
                "basic" => permissions.Contains("stream:view:free"),
                "express" => permissions.Contains("stream:view:specific") || permissions.Contains("stream:view:all"),
                "premium" => permissions.Contains("stream:view:premium") || permissions.Contains("stream:view:all"),
                _ => false
            };
        }
    }
}