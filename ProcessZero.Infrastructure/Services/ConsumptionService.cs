using Microsoft.Extensions.Logging;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    /// <summary>
    /// Proxies consumption/session management to the standalone ProcessZero.TimerService.
    /// This ensures timers continue running even during main API deployments.
    /// Falls back to local wallet service for remaining hours if timer service is unavailable.
    /// </summary>
    public class ConsumptionService : IConsumptionService
    {
        private readonly TimerServiceClient _timerClient;
        private readonly IUserWalletService _walletService;
        private readonly ILogger<ConsumptionService> _logger;

        public ConsumptionService(
            TimerServiceClient timerClient,
            IUserWalletService walletService,
            ILogger<ConsumptionService> logger)
        {
            _timerClient = timerClient ?? throw new ArgumentNullException(nameof(timerClient));
            _walletService = walletService ?? throw new ArgumentNullException(nameof(walletService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ── Session Management ──

        public async Task<UserSessionDto> StartSessionAsync(string userId, string? deviceInfo = null, CancellationToken cancellationToken = default)
        {
            var result = await _timerClient.StartSessionAsync(userId, deviceInfo, cancellationToken);
            if (result != null) return MapToAppSessionDto(result);
            
            _logger.LogWarning("TimerService unavailable for StartSession, returning default");
            return new UserSessionDto { UserId = userId, IsActive = false };
        }

        public async Task<SessionHeartbeatResponseDto> EndSessionAsync(int sessionId, string userId, CancellationToken cancellationToken = default)
        {
            var result = await _timerClient.EndSessionAsync(sessionId, userId, cancellationToken);
            if (result != null) return MapToAppHeartbeatDto(result);
            
            return new SessionHeartbeatResponseDto { Success = false, Message = "Timer service unavailable" };
        }

        public async Task<SessionHeartbeatResponseDto> HeartbeatAsync(int sessionId, string userId, CancellationToken cancellationToken = default)
        {
            var result = await _timerClient.HeartbeatAsync(sessionId, userId, cancellationToken);
            if (result != null) return MapToAppHeartbeatDto(result);
            
            return new SessionHeartbeatResponseDto { Success = false, Message = "Timer service unavailable" };
        }

        public async Task<UserSessionDto?> GetActiveSessionAsync(string userId, CancellationToken cancellationToken = default)
        {
            var result = await _timerClient.GetActiveSessionAsync(userId, cancellationToken);
            if (result?.Session != null) return MapToAppSessionDto(result.Session);
            return null;
        }

        public async Task<List<UserSessionDto>> GetSessionHistoryAsync(string userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            return new List<UserSessionDto>();
        }

        // ── Admin Management ──

        public async Task<ConsumptionConfigDto> GetConfigAsync(CancellationToken cancellationToken = default)
        {
            return new ConsumptionConfigDto { IsEnabled = true, CreditsPerHour = 0.2m, GracePeriodMinutes = 0, InitialFreeHours = 5 };
        }

        public async Task<ConsumptionConfigDto> UpdateConfigAsync(UpdateConsumptionConfigDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Config updates are managed on the ProcessZero.TimerService dashboard");
            return await GetConfigAsync(cancellationToken);
        }

        public async Task<List<UserSessionDto>> GetAllActiveSessionsAsync(CancellationToken cancellationToken = default)
        {
            return new List<UserSessionDto>();
        }

        public async Task<bool> ForceEndSessionAsync(int sessionId, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Session management is handled by ProcessZero.TimerService");
            return false;
        }

        public async Task<ConsumptionStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            return new ConsumptionStatsDto();
        }

        /// <summary>
        /// This is no longer run locally - it runs in the standalone timer service.
        /// </summary>
        public async Task<int> ProcessActiveSessionsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("ProcessActiveSessionsAsync is handled by the standalone ProcessZero.TimerService");
            return await Task.FromResult(0);
        }

        // ── Mapping Helpers ──

        private static UserSessionDto MapToAppSessionDto(TimerUserSessionDto dto)
        {
            return new UserSessionDto
            {
                Id = dto.Id,
                UserId = dto.UserId,
                SessionStartUtc = dto.SessionStartUtc,
                SessionEndUtc = dto.SessionEndUtc,
                MinutesConsumed = (decimal)dto.MinutesConsumed,
                CreditsConsumed = dto.CreditsConsumed,
                IsActive = dto.IsActive,
                LastHeartbeatUtc = dto.LastHeartbeatUtc,
                DeviceInfo = dto.DeviceInfo,
                ElapsedMinutes = dto.ElapsedMinutes,
                EstimatedCreditsConsumed = dto.EstimatedCreditsConsumed,
                TimeRemainingDisplay = dto.TimeRemainingDisplay
            };
        }

        private static SessionHeartbeatResponseDto MapToAppHeartbeatDto(TimerSessionHeartbeatResponseDto dto)
        {
            return new SessionHeartbeatResponseDto
            {
                Success = dto.Success,
                IsConsuming = dto.IsConsuming,
                IsBlocked = dto.IsBlocked,
                CreditsConsumed = dto.CreditsConsumed,
                MinutesElapsed = dto.MinutesElapsed,
                RemainingCreditBalance = dto.RemainingCreditBalance,
                Message = dto.Message
            };
        }
    }
}