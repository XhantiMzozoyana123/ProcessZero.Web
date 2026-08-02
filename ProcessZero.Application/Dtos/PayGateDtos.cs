using System;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// Request DTO for creating a PayGate order
    /// </summary>
    public class CreatePayGateOrderRequest
    {
        public int PackageId { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>
    /// Response DTO for creating a PayGate order
    /// </summary>
    public class CreatePayGateOrderResponse
    {
        public string PaymentUrl { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public int PackageId { get; set; }
        public string AddressIn { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request DTO for PayGate callback (IPN)
    /// </summary>
    public class PayGateCallbackRequest
    {
        public string UserId { get; set; } = string.Empty;
        public int PackageId { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string? IpnToken { get; set; }
        public string? Status { get; set; }
        public string? TxidOut { get; set; }
        public string? ValueCoin { get; set; }
    }
}