using System;
using System.ComponentModel.DataAnnotations;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Tracks a user's active app usage session for credit consumption.
    /// The consumption engine deducts credits at 0.2 credits/hour while the session is active.
    /// </summary>
    public class UserSession : BaseEntity
    {
        /// <summary>
        /// When this usage session started (UTC)
        /// </summary>
        [Required]
        public DateTime SessionStartUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this usage session ended (UTC), null if still active
        /// </summary>
        public DateTime? SessionEndUtc { get; set; }

        /// <summary>
        /// Total minutes consumed in this session (updated when session ends)
        /// </summary>
        public decimal MinutesConsumed { get; set; } = 0;

        /// <summary>
        /// Total credits consumed in this session
        /// </summary>
        public decimal CreditsConsumed { get; set; } = 0;

        /// <summary>
        /// Whether this session is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Last heartbeat received time (UTC). Used to detect stale sessions.
        /// </summary>
        public DateTime? LastHeartbeatUtc { get; set; }

        /// <summary>
        /// Last time credits were processed for this session (UTC).
        /// Used by the Hangfire consumption job to calculate incremental credit deduction.
        /// </summary>
        public DateTime? LastConsumptionProcessedUtc { get; set; }

        /// <summary>
        /// Whether the user has been blocked from this session due to insufficient credits.
        /// </summary>
        public bool IsBlocked { get; set; } = false;

        /// <summary>
        /// IP address or device identifier where the session started
        /// </summary>
        [MaxLength(100)]
        public string? DeviceInfo { get; set; }
    }
}
