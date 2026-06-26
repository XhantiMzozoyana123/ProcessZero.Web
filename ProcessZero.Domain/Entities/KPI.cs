using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Daily performance snapshot for a sales rep on a given product.
    /// Tracks the complete sales pipeline: outreach → replies → meetings → deals.
    /// </summary>
    public class KPI : BaseEntity
    {
        [Required]
        public int ProductId { get; set; }

        // ── Outreach Activity ──────────────────────────────────────
        /// <summary>Total emails sent from campaigns for the day.</summary>
        public int EmailsSent { get; set; } = 0;

        /// <summary>Call outreach attempts made for the day.</summary>
        public int CallsAttempted { get; set; } = 0;

        /// <summary>Calls actually connected for the day.</summary>
        public int CallsCompleted { get; set; } = 0;

        // ── Response Tracking ──────────────────────────────────────
        /// <summary>Number of email replies received for the day.</summary>
        public int RepliesReceived { get; set; } = 0;

        // ── Conversion Metrics ─────────────────────────────────────
        /// <summary>Number of new meetings booked for the day.</summary>
        public int MeetingsBooked { get; set; } = 0;

        /// <summary>Number of deals closed for the day.</summary>
        public int DealsClosed { get; set; } = 0;

        /// <summary>Total dollar amount closed from deals for the day.</summary>
        public decimal RevenueClosed { get; set; } = 0;

        // ── Pipeline Health ────────────────────────────────────────
        /// <summary>Number of active clients used for MRR calculation.</summary>
        public int ActiveClients { get; set; } = 0;

        /// <summary>Monthly Recurring Revenue from active clients.</summary>
        public decimal MonthlyRecurringRevenue { get; set; } = 0;
    }
}
