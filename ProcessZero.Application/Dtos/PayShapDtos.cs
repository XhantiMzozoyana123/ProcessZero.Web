using System;
using System.ComponentModel.DataAnnotations;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// Request DTO for creating a PayShap payment order
    /// </summary>
    public class CreatePayShapOrderRequest
    {
        public int PackageId { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    /// <summary>
    /// Response DTO for creating a PayShap payment order
    /// </summary>
    public class CreatePayShapOrderResponse
    {
        public string OrderId { get; set; } = string.Empty;
        public int PackageId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public string PayShapAccountNumber { get; set; } = string.Empty;
        public string PayShapAccountHolder { get; set; } = string.Empty;
        public string PayShapReference { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// Request DTO for submitting payment proof (screenshot)
    /// </summary>
    public class SubmitPaymentProofRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string PaymentProofScreenshot { get; set; } = string.Empty; // Base64 encoded image
        public string? BankTransactionReference { get; set; }
    }

    /// <summary>
    /// Response DTO for payment proof submission
    /// </summary>
    public class SubmitPaymentProofResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request DTO for admin to verify payment
    /// </summary>
    public class VerifyPaymentRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public bool Approved { get; set; }
        public string? AdminNotes { get; set; }
    }

    /// <summary>
    /// Response DTO for payment verification
    /// </summary>
    public class VerifyPaymentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for payment order details
    /// </summary>
    public class PaymentOrderDto
    {
        public string OrderId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int CreditPackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public string PayShapAccountNumber { get; set; } = string.Empty;
        public string PayShapAccountHolder { get; set; } = string.Empty;
        public string PayShapReference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? PaymentProofScreenshot { get; set; }
        public string? BankTransactionReference { get; set; }
        public string? AdminNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}