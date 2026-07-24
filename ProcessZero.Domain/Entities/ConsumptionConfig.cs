using System;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Global consumption configuration (singleton row).
    /// Admins can modify the consumption rate and other settings for the pay-to-use model.
    /// Consumption rate: 0.2 credits per hour of active app usage (1 credit = 5 hours).
    /// </summary>
    public class ConsumptionConfig
    {
        public int Id { get; set; }

        /// <summary>
        /// Credits consumed per hour of active usage (default: 0.2)
        /// </summary>
        public decimal CreditsPerHour { get; set; } = 0.2m;

        /// <summary>
        /// How often the background job checks active sessions (in minutes)
        /// </summary>
        public int CheckIntervalMinutes { get; set; } = 1;

        /// <summary>
        /// Maximum session duration before auto-termination (in minutes, 0 = no limit)
        /// </summary>
        public int MaxSessionMinutes { get; set; } = 480; // 8 hours default

        /// <summary>
        /// Whether consumption is enabled globally
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Grace period in minutes before a session starts consuming credits
        /// (allows users to briefly check the platform without being charged)
        /// </summary>
        public int GracePeriodMinutes { get; set; } = 0;

        /// <summary>
        /// Initial free hours granted to new/returning users before consumption begins.
        /// When these hours run out, the user is blocked and must top up.
        /// </summary>
        public decimal InitialFreeHours { get; set; } = 5; // Default: 5 hours free

        /// <summary>
        /// Whether to enforce access blocking when credits/hours are exhausted
        /// </summary>
        public bool EnforceAccessBlock { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}