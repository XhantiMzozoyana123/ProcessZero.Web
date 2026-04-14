using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    public class Invoice : BaseEntity
    {
        public int ProductId { get; set; }
        public int ClientId { get; set; }

        public string InvoiceCode { get; set; } // Unique code for the invoice
        public string CustomerCode { get; set; } // Code to identify the customer (could be email or internal code)

        public decimal Amount { get; set; }

        public bool IsPaid { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        // Optional but powerful
        public string ExternalInvoiceId { get; set; } // Stripe, PayPal, etc.
    }
}
