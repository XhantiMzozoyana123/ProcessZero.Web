using ProcessZero.Application.Dtos;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    /// <summary>
    /// Service for managing credit consumption tracking (pay-to-use model).
    /// Consumption rate: 0.2 credits per hour of active app usage.
    /// </summary>
    public interface IConsumptionService
    {
        // ── Session Management ──

        /// <summary>
        /// Start a new usage session for a user
        /// </summary>
        Task<UserSessionDto> StartSessionAsync(string userId, string? deviceInfo = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// End an active usage session and finalize credit consumption
        /// </summary>
        Task<SessionHeartbeatResponseDto> EndSessionAsync(int sessionId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a heartbeat to keep the session alive and track elapsed time.
        /// Returns current consumption info.
        /// </summary>
        Task<SessionHeartbeatResponseDto> HeartbeatAsync(int sessionId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the user's current active session (if any)
        /// </summary>
        Task<UserSessionDto?> GetActiveSessionAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get session history for a user
        /// </summary>
        Task<List<UserSessionDto>> GetSessionHistoryAsync(string userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        // ── Admin Management ──

        /// <summary>
        /// Get the current consumption configuration
        /// </summary>
        Task<ConsumptionConfigDto> GetConfigAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Update the consumption configuration (admin only)
        /// </summary>
        Task<ConsumptionConfigDto> UpdateConfigAsync(UpdateConsumptionConfigDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all active sessions across all users (admin view)
        /// </summary>
        Task<List<UserSessionDto>> GetAllActiveSessionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Force-end a user's session (admin only)
        /// </summary>
        Task<bool> ForceEndSessionAsync(int sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get consumption statistics (admin dashboard)
        /// </summary>
        Task<ConsumptionStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Process all active sessions, consuming credits based on actual elapsed time.
        /// Called periodically by the Hangfire background job so that credit consumption
        /// continues even when the browser is closed or the user logs out.
        /// Returns the number of sessions that were processed.
        /// </summary>
        Task<int> ProcessActiveSessionsAsync(CancellationToken cancellationToken = default);
    }
}
