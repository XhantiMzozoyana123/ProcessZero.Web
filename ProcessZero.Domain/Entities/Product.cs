using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProfilePictureBase64 { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string NegotiableAmounts { get; set; } = string.Empty;
        public decimal ActualAmount { get; set; }

        /// <summary>
        /// The cal.com event type ID associated with this product.
        /// Used when creating bookings for this product via the cal.com integration.
        /// </summary>
        public int? CalEventTypeId { get; set; }
    }
}
