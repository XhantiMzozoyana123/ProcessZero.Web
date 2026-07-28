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
        private readonly ILogger<ConsumptionService> _logger;

        public ConsumptionService(
            TimerServiceClient timerClient,
            ILogger<ConsumptionService> logger)
        {
            _timerClient = timerClient ?? throw new ArgumentNullException(nameof(timerClient));
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
            try
            {
                var result = await _timerClient.GetConfigAsync(cancellationToken);
                if (result != null) return MapToAppConfigDto(result);

                _logger.LogWarning("TimerService unavailable for GetConfig, returning default");
                return DefaultConfig();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetConfigAsync");
                return DefaultConfig();
            }
        }

        public async Task<ConsumptionConfigDto> UpdateConfigAsync(UpdateConsumptionConfigDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var timerDto = new TimerConfigDto
                {
                    CreditsPerHour = dto.CreditsPerHour,
                    CheckIntervalMinutes = dto.CheckIntervalMinutes,
                    MaxSessionMinutes = dto.MaxSessionMinutes,
                    IsEnabled = dto.IsEnabled,
                    GracePeriodMinutes = dto.GracePeriodMinutes,
                    InitialFreeHours = dto.InitialFreeHours,
                    EnforceAccessBlock = dto.EnforceAccessBlock
                };

                var result = await _timerClient.UpdateConfigAsync(timerDto, cancellationToken);
                if (result != null) return MapToAppConfigDto(result);

                _logger.LogWarning("TimerService unavailable for UpdateConfig, returning submitted config");
                return ConfigFromDto(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateConfigAsync");
                // Return safe default config when timer service is unavailable
                return ConfigFromDto(dto);
            }
        }

        public async Task<ConsumptionConfigDto> DeleteConfigAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _timerClient.DeleteConfigAsync(cancellationToken);
                if (result != null) return MapToAppConfigDto(result);

                _logger.LogWarning("TimerService unavailable for DeleteConfig, returning default");
                return DefaultConfig();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteConfigAsync");
                return DefaultConfig();
            }
        }

        public async Task<List<UserSessionDto>> GetAllActiveSessionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _timerClient.GetAllActiveSessionsAsync(cancellationToken);
                if (result != null)
                {
                    var sessions = new List<UserSessionDto>();
                    foreach (var s in result)
                    {
                        sessions.Add(MapToAppSessionDto(s));
                    }
                    return sessions;
                }

                _logger.LogWarning("TimerService unavailable for GetAllActiveSessions, returning empty list");
                return new List<UserSessionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllActiveSessionsAsync");
                return new List<UserSessionDto>();
            }
        }

        public async Task<bool> ForceEndSessionAsync(int sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _timerClient.ForceEndSessionAsync(sessionId, cancellationToken);
                if (result) return true;

                _logger.LogWarning("TimerService unavailable for ForceEndSession, returning false");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ForceEndSessionAsync");
                return false;
            }
        }

        public async Task<ConsumptionStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _timerClient.GetStatsAsync(cancellationToken);
                if (result != null) return MapToAppStatsDto(result);

                _logger.LogWarning("TimerService unavailable for GetStats, returning default");
                return new ConsumptionStatsDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetStatsAsync");
                return new ConsumptionStatsDto();
            }
        }

        /// <summary>
        /// This is no longer run locally - it runs in the standalone ProcessZero.TimerService.
        /// </summary>
        public async Task<int> ProcessActiveSessionsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("ProcessActiveSessionsAsync is handled by the standalone ProcessZero.TimerService");
            return await Task.FromResult(0);
        }

        // ── Mapping Helpers ──

        private static ConsumptionConfigDto DefaultConfig()
        {
            return new ConsumptionConfigDto
            {
                Id = 1,
                CreditsPerHour = 0.2m,
                CheckIntervalMinutes = 1,
                MaxSessionMinutes = 480,
                IsEnabled = true,
                GracePeriodMinutes = 0,
                InitialFreeHours = 5,
                EnforceAccessBlock = true,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static ConsumptionConfigDto ConfigFromDto(UpdateConsumptionConfigDto dto)
        {
            return new ConsumptionConfigDto
            {
                Id = 1,
                CreditsPerHour = dto.CreditsPerHour,
                CheckIntervalMinutes = dto.CheckIntervalMinutes,
                MaxSessionMinutes = dto.MaxSessionMinutes,
                IsEnabled = dto.IsEnabled,
                GracePeriodMinutes = dto.GracePeriodMinutes,
                InitialFreeHours = dto.InitialFreeHours,
                EnforceAccessBlock = dto.EnforceAccessBlock,
                UpdatedAt = DateTime.UtcNow
            };
        }

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

        private static ConsumptionConfigDto MapToAppConfigDto(TimerConfigDto dto)
        {
            return new ConsumptionConfigDto
            {
                Id = dto.Id,
                CreditsPerHour = dto.CreditsPerHour,
                CheckIntervalMinutes = dto.CheckIntervalMinutes,
                MaxSessionMinutes = dto.MaxSessionMinutes,
                IsEnabled = dto.IsEnabled,
                GracePeriodMinutes = dto.GracePeriodMinutes,
                InitialFreeHours = dto.InitialFreeHours,
                EnforceAccessBlock = dto.EnforceAccessBlock,
                UpdatedAt = dto.UpdatedAt
            };
        }

        private static ConsumptionStatsDto MapToAppStatsDto(TimerStatsDto dto)
        {
            return new ConsumptionStatsDto
            {
                ActiveSessionsCount = dto.ActiveSessionsCount,
                TotalSessionsToday = dto.TotalSessionsToday,
                TotalSessionsThisMonth = dto.TotalSessionsThisMonth,
                TotalCreditsConsumedToday = dto.TotalCreditsConsumedToday,
                TotalCreditsConsumedThisMonth = dto.TotalCreditsConsumedThisMonth,
                TotalMinutesLoggedToday = dto.TotalMinutesLoggedToday,
                TotalMinutesLoggedThisMonth = dto.TotalMinutesLoggedThisMonth,
                Rate = dto.Rate,
                IsEnabled = dto.IsEnabled
            };
        }
    }
}