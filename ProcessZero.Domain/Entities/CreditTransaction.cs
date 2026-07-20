using System;
using System.ComponentModel.DataAnnotations;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Represents a credit transaction in Process Zero 2.0.
    /// Tracks all credit purchases, consumption, and adjustments.
    /// </summary>
    public enum CreditTransactionType
    {
        Purchase,      // Credits purchased
        Consumption,   // Credits consumed for platform access
        Refund,        // Credits refunded
        Adjustment,    // Manual credit adjustment by admin
        Bonus,         // Bonus credits awarded
        Subscription   // Subscription-based credit allocation
    }

    /// <summary>
    /// Represents a credit transaction in Process Zero 2.0.
    /// </summary>
    public class CreditTransaction : BaseEntity
    {
        /// <summary>
        /// The wallet this transaction belongs to
        /// </summary>
        public int UserWalletId { get; set; }

        /// <summary>
        /// Type of transaction (purchase, consumption, etc.)
        /// </summary>
        public CreditTransactionType TransactionType { get; set; }

        /// <summary>
        /// Amount of credits (positive for purchases, negative for consumption)
        /// </summary>
        public decimal CreditAmount { get; set; }

        /// <summary>
        /// Balance after this transaction
        /// </summary>
        public decimal BalanceAfterTransaction { get; set; }

        /// <summary>
        /// Description of the transaction
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Reference to external payment (invoice, payment provider, etc.)
        /// </summary>
        public string? ReferenceId { get; set; }

        /// <summary>
        /// Related entity type (e.g., "Meeting", "Product", "Invoice")
        /// </summary>
        [StringLength(100)]
        public string? RelatedEntityType { get; set; }

        /// <summary>
        /// Related entity ID
        /// </summary>
        public int? RelatedEntityId { get; set; }

        /// <summary>
        /// When the transaction occurred
        /// </summary>
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property to wallet
        /// </summary>
        public virtual UserWallet? UserWallet { get; set; }
    }
}