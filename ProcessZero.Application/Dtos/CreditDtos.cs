using System;
using System.Collections.Generic;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// Response DTO for user wallet information
    /// </summary>
    public class UserWalletDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal CreditBalance { get; set; }
        public decimal TotalCreditsPurchased { get; set; }
        public decimal TotalCreditsConsumed { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public string? SubscriptionId { get; set; }
        public string? SubscriptionStatus { get; set; }
    }

    /// <summary>
    /// DTO for credit transaction
    /// </summary>
    public class CreditTransactionDto
    {
        public int Id { get; set; }
        public int UserWalletId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal CreditAmount { get; set; }
        public decimal BalanceAfterTransaction { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ReferenceId { get; set; }
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
        public DateTime TransactionDate { get; set; }
    }

    /// <summary>
    /// DTO for credit package
    /// </summary>
    public class CreditPackageDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal CreditAmount { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public int? DurationMinutes { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public bool IsSubscription { get; set; }
        public decimal? DiscountedPrice { get; set; }
    }

    /// <summary>
    /// Request DTO for purchasing credits
    /// </summary>
    public class PurchaseCreditsRequestDto
    {
        public int CreditPackageId { get; set; }
        public string? PaymentMethod { get; set; } // "card", "paypal", etc.
        public string? PaymentReference { get; set; } // Payment provider reference
    }

    /// <summary>
    /// Response DTO for credit purchase
    /// </summary>
    public class CreditPurchaseResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal NewBalance { get; set; }
        public int TransactionId { get; set; }
    }

    /// <summary>
    /// Request DTO for consuming credits
    /// </summary>
    public class ConsumeCreditsRequestDto
    {
        public decimal CreditAmount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
    }

    /// <summary>
    /// Response DTO for credit consumption
    /// </summary>
    public class CreditConsumeResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal NewBalance { get; set; }
        public decimal CreditsConsumed { get; set; }
    }

    /// <summary>
    /// DTO for creating a credit package (admin)
    /// </summary>
    public class CreateCreditPackageDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal CreditAmount { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public int? DurationMinutes { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public decimal? DiscountPercentage { get; set; }
        public bool IsSubscription { get; set; }
    }

    public class CreatePayPalOrderRequest
    {
        public int PackageId { get; set; }
    }

    public class CapturePayPalOrderRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public int PackageId { get; set; }
    }

    /// <summary>
    /// DTO for credit balance check response
    /// </summary>
    public class CreditBalanceResponseDto
    {
        public decimal CreditBalance { get; set; }
        public bool HasSufficientCredits { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}