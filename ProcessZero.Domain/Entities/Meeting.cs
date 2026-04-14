using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    public class Meeting : BaseEntity
    {
        // Client/attendee reference
        public int ClientId { get; set; }

        // Product reference
        public int ProductId { get; set; }

        // Meeting date/time
        public DateTime MeetingDate { get; set; }

        // Optional notes
        public string? Notes { get; set; }
    }
}
