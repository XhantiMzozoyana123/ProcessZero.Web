using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Controller for credit consumption tracking (pay-to-use model).
    /// Users start/end sessions, admin configures consumption rate and monitors usage.
    /// Consumption rate: 0.2 credits per hour of active app usage.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ConsumptionController : ControllerBase
    {
        private readonly IConsumptionService _consumptionService;
        private readonly ILogger<ConsumptionController> _logger;

        public ConsumptionController(
            IConsumptionService consumptionService,
            ILogger<ConsumptionController> logger)
        {
            _consumptionService = consumptionService ?? throw new ArgumentNullException(nameof(consumptionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string GetUserId() =>
            User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        /// <summary>
        /// POST: api/Consumption/start
        /// Start a new usage session. Credits begin counting after grace period.
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartSession([FromBody] StartSessionDto? dto, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var session = await _consumptionService.StartSessionAsync(userId, dto?.DeviceInfo, cancellationToken);
            return Ok(session);
        }

        /// <summary>
        /// POST: api/Consumption/{sessionId}/end
        /// End an active usage session and finalize credit consumption.
        /// </summary>
        [HttpPost("{sessionId:int}/end")]
        public async Task<IActionResult> EndSession(int sessionId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var result = await _consumptionService.EndSessionAsync(sessionId, userId, cancellationToken);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// POST: api/Consumption/{sessionId}/heartbeat
        /// Send heartbeat to keep session alive. Returns current consumption info.
        /// Call this periodically (every 30-60 seconds) from the client.
        /// </summary>
        [HttpPost("{sessionId:int}/heartbeat")]
        public async Task<IActionResult> Heartbeat(int sessionId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var result = await _consumptionService.HeartbeatAsync(sessionId, userId, cancellationToken);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// GET: api/Consumption/active
        /// Get the current user's active session (if any).
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveSession(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var session = await _consumptionService.GetActiveSessionAsync(userId, cancellationToken);

            if (session == null)
                return Ok(new { hasActiveSession = false });

            return Ok(new { hasActiveSession = true, session });
        }

        /// <summary>
        /// GET: api/Consumption/history
        /// Get session history for the current user.
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var history = await _consumptionService.GetSessionHistoryAsync(userId, page, pageSize, cancellationToken);
            return Ok(history);
        }

        // ── Admin Endpoints ──

        /// <summary>
        /// GET: api/Consumption/config
        /// Get the current consumption configuration (admin only).
        /// </summary>
        [HttpGet("config")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
        {
            var config = await _consumptionService.GetConfigAsync(cancellationToken);
            return Ok(config);
        }

        /// <summary>
        /// PUT: api/Consumption/config
        /// Update the consumption configuration (admin only).
        /// </summary>
        [HttpPut("config")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateConfig([FromBody] UpdateConsumptionConfigDto dto, CancellationToken cancellationToken)
        {
            if (dto == null)
                return BadRequest(new { error = "Configuration data is required." });

            var config = await _consumptionService.UpdateConfigAsync(dto, cancellationToken);
            return Ok(config);
        }

        /// <summary>
        /// GET: api/Consumption/admin/sessions
        /// Get all active sessions across all users (admin only).
        /// </summary>
        [HttpGet("admin/sessions")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllActiveSessions(CancellationToken cancellationToken)
        {
            var sessions = await _consumptionService.GetAllActiveSessionsAsync(cancellationToken);
            return Ok(sessions);
        }

        /// <summary>
        /// POST: api/Consumption/admin/sessions/{sessionId}/force-end
        /// Force-end a user's session (admin only).
        /// </summary>
        [HttpPost("admin/sessions/{sessionId:int}/force-end")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ForceEndSession(int sessionId, CancellationToken cancellationToken)
        {
            var result = await _consumptionService.ForceEndSessionAsync(sessionId, cancellationToken);
            if (!result)
                return NotFound(new { error = $"Active session with ID {sessionId} not found." });

            return Ok(new { message = "Session force-ended." });
        }

        /// <summary>
        /// GET: api/Consumption/admin/stats
        /// Get consumption statistics for the admin dashboard.
        /// </summary>
        [HttpGet("admin/stats")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
        {
            var stats = await _consumptionService.GetStatsAsync(cancellationToken);
            return Ok(stats);
        }
    }
}