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

        /// <summary>
        /// GET /api/stream/my-streams
        /// Returns all streams in the exact format the frontend Stream type expects.
        /// </summary>
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

        /// <summary>
        /// GET /api/stream/{slug}
        /// Returns a single stream in the frontend Stream format.
        /// If user doesn't have access, still returns stream data but without dacastIframeSrc.
        /// </summary>
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

            // If no access, strip the streaming URL but still return the stream info
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

            // If VOD (not live), try to get signed B2 URL
            if (!stream.IsLive && string.IsNullOrEmpty(dto.DacastIframeSrc))
            {
                var vodKey = $"livestreams/{stream.Slug}.mp4";
                var vodUrl = await _storageService.GetSignedUrlAsync(vodKey);
                if (string.IsNullOrEmpty(vodUrl))
                    vodUrl = await _storageService.GetVideoUrlByFilenameAsync($"{stream.Slug}.mp4");
                
                // Store VOD URL in the iframe field so frontend can use it
                if (!string.IsNullOrEmpty(vodUrl))
                    dto.DacastIframeSrc = vodUrl;
            }

            return Ok(dto);
        }

        /// <summary>
        /// GET /api/stream/{slug}/access-check
        /// Quick boolean check — does the user have access?
        /// </summary>
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

        // ── Helper: Map VssStream DB model → frontend StreamDto ──
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
                Home = new StreamTeamDto
                {
                    Name = s.HomeTeamName ?? "Home",
                    ShortName = s.HomeTeamShort ?? (s.HomeTeamName?.Length >= 3 ? s.HomeTeamName[..3].ToUpper() : "HME"),
                    Logo = s.HomeTeamLogo
                },
                Away = new StreamTeamDto
                {
                    Name = s.AwayTeamName ?? "Away",
                    ShortName = s.AwayTeamShort ?? (s.AwayTeamName?.Length >= 3 ? s.AwayTeamName[..3].ToUpper() : "AWY"),
                    Logo = s.AwayTeamLogo
                },
                Location = s.Location,
                StartAt = (s.ScheduledStart ?? s.CreatedAt).ToUniversalTime().ToString("o"),
                Status = status,
                Access = hasAccess ? s.Access : s.Access, // always return the access type
                PriceUSD = s.PriceUSD,
                ThumbnailUrl = s.ThumbnailUrl,
                DacastIframeSrc = hasAccess ? s.DacastIframeSrc : null, // only return if user has access
                DacastChannelId = hasAccess ? s.DacastStreamId : null,
                CreatedAt = s.CreatedAt.ToUniversalTime().ToString("o"),
                UpdatedAt = (s.UpdatedAt ?? s.CreatedAt).ToUniversalTime().ToString("o")
            };
        }

        // ── Helper: Compute status from stream data (matches frontend logic) ──
        private string ComputeStatus(VssStream s)
        {
            if (s.IsLive) return "live";

            var now = DateTime.UtcNow;
            var start = s.ScheduledStart ?? s.CreatedAt;

            if (now < start) return "upcoming";
            if ((now - start).TotalHours > 4) return "past";
            return "live"; // started within last 4 hours = live
        }

        // ── Helper: Check access based on permissions ──
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