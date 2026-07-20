using System;
using System.ComponentModel.DataAnnotations;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Represents a purchaseable credit package in Process Zero 2.0.
    /// Users can purchase these packages to add credits to their wallet.
    /// </summary>
    public class CreditPackage : BaseEntity
    {
        /// <summary>
        /// Name of the credit package (e.g., "Starter Pack", "Pro Pack", "Enterprise")
        /// </summary>
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of what the package provides
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Number of credits included in this package
        /// </summary>
        public decimal CreditAmount { get; set; }

        /// <summary>
        /// Price in USD (or configured currency)
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Currency code (USD, EUR, etc.)
        /// </summary>
        [StringLength(3)]
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Time duration in minutes that credits provide (e.g., 60 credits = 60 minutes of access)
        /// </summary>
        public int? DurationMinutes { get; set; }

        /// <summary>
        /// Whether this package is active and available for purchase
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Display order for UI sorting
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Optional discount percentage (0-100)
        /// </summary>
        public decimal? DiscountPercentage { get; set; }

        /// <summary>
        /// Whether this is a subscription package (recurring)
        /// </summary>
        public bool IsSubscription { get; set; }
    }
}