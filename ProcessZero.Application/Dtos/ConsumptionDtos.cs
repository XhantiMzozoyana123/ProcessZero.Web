using System;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// DTO for consumption configuration (admin manage this)
    /// </summary>
    public class ConsumptionConfigDto
    {
        public int Id { get; set; }
        public decimal CreditsPerHour { get; set; }
        public int CheckIntervalMinutes { get; set; }
        public int MaxSessionMinutes { get; set; }
        public bool IsEnabled { get; set; }
        public int GracePeriodMinutes { get; set; }
        public decimal InitialFreeHours { get; set; }
        public bool EnforceAccessBlock { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for updating consumption configuration (admin only)
    /// </summary>
    public class UpdateConsumptionConfigDto
    {
        public decimal CreditsPerHour { get; set; } = 0.2m;
        public int CheckIntervalMinutes { get; set; } = 1;
        public int MaxSessionMinutes { get; set; } = 480;
        public bool IsEnabled { get; set; } = true;
        public int GracePeriodMinutes { get; set; } = 0;
        public decimal InitialFreeHours { get; set; } = 5;
        public bool EnforceAccessBlock { get; set; } = true;
    }

    /// <summary>
    /// DTO for a user's active session
    /// </summary>
    public class UserSessionDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime SessionStartUtc { get; set; }
        public DateTime? SessionEndUtc { get; set; }
        public decimal MinutesConsumed { get; set; }
        public decimal CreditsConsumed { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastHeartbeatUtc { get; set; }
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Computed fields for live display
        /// </summary>
        public double ElapsedMinutes { get; set; }
        public decimal EstimatedCreditsConsumed { get; set; }
        public string? TimeRemainingDisplay { get; set; }
    }

    /// <summary>
    /// DTO for starting a new usage session
    /// </summary>
    public class StartSessionDto
    {
        public string? DeviceInfo { get; set; }
    }

    /// <summary>
    /// DTO for session heartbeat (keeps session alive)
    /// </summary>
    public class SessionHeartbeatResponseDto
    {
        public bool Success { get; set; }
        public bool IsConsuming { get; set; }
        public bool IsBlocked { get; set; }
        public decimal CreditsConsumed { get; set; }
        public double MinutesElapsed { get; set; }
        public decimal? RemainingCreditBalance { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// DTO for consumption statistics (admin dashboard)
    /// </summary>
    public class ConsumptionStatsDto
    {
        public int ActiveSessionsCount { get; set; }
        public int TotalSessionsToday { get; set; }
        public int TotalSessionsThisMonth { get; set; }
        public decimal TotalCreditsConsumedToday { get; set; }
        public decimal TotalCreditsConsumedThisMonth { get; set; }
        public decimal TotalMinutesLoggedToday { get; set; }
        public decimal TotalMinutesLoggedThisMonth { get; set; }
        public decimal Rate { get; set; }
        public bool IsEnabled { get; set; }
    }
}