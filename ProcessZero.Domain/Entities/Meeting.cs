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

        // ── Opportunity fields ──────────────────────────────────
        
        /// <summary>
        /// Prospect's budget for the project/product
        /// </summary>
        public decimal? Budget { get; set; }

        /// <summary>
        /// Commission amount the rep earns for closing the deal
        /// </summary>
        public decimal? Commission { get; set; }

        /// <summary>
        /// Opportunity status: Available, Claimed, InProgress, Closed, Cancelled
        /// </summary>
        public OpportunityStatus OpportunityStatus { get; set; } = OpportunityStatus.Available;

        /// <summary>
        /// The contact/attendee name (denormalized for display)
        /// </summary>
        public string? ContactName { get; set; }

        /// <summary>
        /// The contact/attendee email (denormalized for display)
        /// </summary>
        public string? ContactEmail { get; set; }

        /// <summary>
        /// The contact/attendee company (denormalized for display)
        /// </summary>
        public string? ContactCompany { get; set; }
    }

    public enum OpportunityStatus
    {
        Available = 0,      // Open for claiming
        Claimed = 1,        // Claimed by a rep, meeting scheduled
        InProgress = 2,     // Rep is working on the deal
        Closed = 3,         // Deal successfully closed
        Cancelled = 4       // Opportunity cancelled by admin
    }
}
