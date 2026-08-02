using System;
using System.ComponentModel.DataAnnotations;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Represents a PayShap payment order for manual bank transfer with screenshot verification.
    /// </summary>
    public class PaymentOrder : BaseEntity
    {
        /// <summary>
        /// The user this payment order belongs to
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Unique order identifier
        /// </summary>
        [Required]
        [StringLength(256)]
        public string OrderId { get; set; } = string.Empty;

        /// <summary>
        /// The credit package being purchased
        /// </summary>
        public int CreditPackageId { get; set; }

        /// <summary>
        /// Amount to be paid
        /// </summary>
        [Required]
        public decimal Amount { get; set; }

        /// <summary>
        /// Currency code (e.g., ZAR, USD)
        /// </summary>
        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "ZAR";

        /// <summary>
        /// PayShap account details displayed to user
        /// </summary>
        [Required]
        [StringLength(256)]
        public string PayShapAccountNumber { get; set; } = string.Empty;

        /// <summary>
        /// PayShap account holder name
        /// </summary>
        [Required]
        [StringLength(256)]
        public string PayShapAccountHolder { get; set; } = string.Empty;

        /// <summary>
        /// PayShap reference/ID for this order
        /// </summary>
        [Required]
        [StringLength(256)]
        public string PayShapReference { get; set; } = string.Empty;

        /// <summary>
        /// Order status: Pending, PaymentReceived, Verified, Completed, Failed, Expired
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Screenshot URL or base64 of payment proof uploaded by user
        /// </summary>
        public string? PaymentProofScreenshot { get; set; }

        /// <summary>
        /// User's email for notifications
        /// </summary>
        [StringLength(256)]
        public string? UserEmail { get; set; }

        /// <summary>
        /// User's phone number for notifications
        /// </summary>
        [StringLength(20)]
        public string? UserPhone { get; set; }

        /// <summary>
        /// Transaction reference from the user's bank (entered after payment)
        /// </summary>
        [StringLength(256)]
        public string? BankTransactionReference { get; set; }

        /// <summary>
        /// Admin notes for verification
        /// </summary>
        public string? AdminNotes { get; set; }

        /// <summary>
        /// When the payment was verified by admin
        /// </summary>
        public DateTime? VerifiedAt { get; set; }

        /// <summary>
        /// When the order expires (typically 24 hours from creation)
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// When the payment was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        public virtual ApplicationUser? User { get; set; }
        public virtual CreditPackage CreditPackage { get; set; } = null!;
    }
}
