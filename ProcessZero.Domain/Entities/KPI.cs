using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Daily performance snapshot for a sales rep on a given product.
    /// One row is maintained per rep + product + day and updated as the rep
    /// records their activity throughout the day.
    /// </summary>
    public class KPI : BaseEntity
    {
        [Required]
        public int ProductId { get; set; }

        // ── Daily sales rep activity ───────────────────────────────
        // Number of call outreach attempts made for the day
        public int CallOutreach { get; set; } = 0;

        // Number of email outreach attempts made for the day
        public int EmailOutreach { get; set; } = 0;

        // Number of calls actually made for the day
        public int CallsMade { get; set; } = 0;

        // Number of meetings booked for the day
        public int MeetingsBooked { get; set; } = 0;

        // Total deal size (amount) closed for the day
        public decimal DealSizeClosed { get; set; } = 0;

        // ── MRR tracking (derived from Contacts + Invoices) ────────
        // Number of active clients used to calculate MRR.
        public int ActiveClients { get; set; } = 0;

        // Monthly Recurring Revenue, recalculated from active clients and
        // the amounts they have closed on every time KPIs are updated.
        public decimal MonthlyRecurringRevenue { get; set; } = 0;
    }
}
