using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Represents an invoice issued to a client for a product.
    /// Tracks payment status, amounts, and external payment provider references.
    /// </summary>
    public class Invoice : BaseEntity
    {
        /// <summary>The product this invoice is for.</summary>
        public int ProductId { get; set; }

        /// <summary>The client/contact this invoice is issued to.</summary>
        public int ClientId { get; set; }

        /// <summary>Unique code for the invoice (e.g., INV-1001).</summary>
        public string InvoiceCode { get; set; } = string.Empty;

        /// <summary>Customer code to identify the customer (could be email or internal code).</summary>
        public string CustomerCode { get; set; } = string.Empty;

        /// <summary>Invoice amount in the transaction currency.</summary>
        public decimal Amount { get; set; }

        /// <summary>Indicates whether the invoice has been paid.</summary>
        public bool IsPaid { get; set; }

        /// <summary>Date and time when the invoice was issued.</summary>
        public DateTime IssuedAt { get; set; }

        /// <summary>Date and time when the invoice was paid (null if not paid).</summary>
        public DateTime? PaidAt { get; set; }

        /// <summary>External invoice ID from payment providers (Stripe, PayPal, etc.).</summary>
        public string ExternalInvoiceId { get; set; } = string.Empty;
    }
}
