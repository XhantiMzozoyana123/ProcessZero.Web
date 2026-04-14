using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Dtos
{
    // Invoice helper class
    public class InvoiceItemDto
    {
        public string Name { get; set; }
        public decimal Amount { get; set; } // Amount in NGN
        public int Quantity { get; set; }
    }

    public class InvoiceDto
    {
        public string InvoiceCode { get; set; }
        public string Status { get; set; }
        public decimal Total { get; set; }
        public string CustomerCode { get; set; }
    }

    public class InvoiceSettlementDto
    {
        public string UserId { get; set; } = string.Empty;

        public Product Product { get; set; } = new Product();

        public Contact Contact { get; set; } = new Contact();
    }
}
