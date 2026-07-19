using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    public class Meeting : BaseEntity
    {
        // Authenticated user who owns this meeting record
        public string UserId { get; set; } = string.Empty;

        // Client/attendee reference
        public int ClientId { get; set; }

        // Product reference
        public int ProductId { get; set; }

        // cal.com booking references
        public int? CalBookingId { get; set; }
        public string? CalBookingUid { get; set; }

        // Meeting date/time (start)
        public DateTime MeetingDate { get; set; }

        // Meeting end time (optional, for duration tracking)
        public DateTime? EndTime { get; set; }

        // Optional notes
        public string? Notes { get; set; }

        // cal.com booking metadata
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? Location { get; set; }
        public string? MeetingUrl { get; set; }
        public string? CancellationReason { get; set; }
        public string? CancelledByEmail { get; set; }

        // JSON store for webhook payload / raw cal.com data
        public string? RawPayload { get; set; }
    }
}
