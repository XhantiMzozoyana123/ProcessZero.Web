using System;
using System.ComponentModel.DataAnnotations;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Represents a user's credit wallet in Process Zero 2.0.
    /// Credits translate to active usage time on the platform.
    /// </summary>
    public class UserWallet : BaseEntity
    {
        /// <summary>
        /// The user this wallet belongs to (links to ApplicationUser.Id)
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Current credit balance (in credits, where each credit = specific time unit)
        /// </summary>
        public decimal CreditBalance { get; set; } = 0;

        /// <summary>
        /// Total credits purchased (lifetime purchases)
        /// </summary>
        public decimal TotalCreditsPurchased { get; set; } = 0;

        /// <summary>
        /// Total credits consumed (lifetime usage)
        /// </summary>
        public decimal TotalCreditsConsumed { get; set; } = 0;

        /// <summary>
        /// Last time credits were updated
        /// </summary>
        public DateTime? LastUpdatedAt { get; set; }

        /// <summary>
        /// Optional subscription reference (if user has recurring credits)
        /// </summary>
        public string? SubscriptionId { get; set; }

        /// <summary>
        /// Subscription status (active, cancelled, expired)
        /// </summary>
        [StringLength(50)]
        public string? SubscriptionStatus { get; set; }

        /// <summary>
        /// Navigation property to user
        /// </summary>
        public virtual ApplicationUser? User { get; set; }
    }
}