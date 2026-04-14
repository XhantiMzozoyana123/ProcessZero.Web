using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    public class KpiPolicy : BaseEntity
    {
        // Simplified KPI policy used for platform-wide thresholds.
        // Keep only the most important fields for clarity and maintenance.

        // Which product this policy applies to (null = all products)
        public int? ProductId { get; set; }

        // Policy lifecycle
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;

        // Key thresholds
        public decimal MinMonthlyRevenue { get; set; }
        // Minimum required outreach attempts in the period (e.g. day/week/month)
        public int MinOutreachAttempts { get; set; } = 0;

        // Minimum calls booked in the period
        public int MinCallsBooked { get; set; } = 0;

        // Consequence flags
        public int GracePeriodDays { get; set; }
        public bool AutoFreezeOnBreach { get; set; }
    }
}
