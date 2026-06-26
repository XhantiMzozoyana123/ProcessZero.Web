using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Defines performance targets and consequences for sales reps.
    /// Policies enforce accountability across the full sales pipeline:
    /// outreach, replies, meetings, and revenue.
    /// </summary>
    public class KpiPolicy : BaseEntity
    {
        /// <summary>Which product this policy applies to (null = all products).</summary>
        public int? ProductId { get; set; }

        /// <summary>Human-readable name (e.g., "Standard Rep Policy").</summary>
        [Required]
        [MaxLength(100)]
        public string PolicyName { get; set; } = string.Empty;

        // ── Policy Lifecycle ───────────────────────────────────────
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;

        // ── Activity Targets (Daily) ───────────────────────────────
        /// <summary>Minimum emails a rep must send per day.</summary>
        public int DailyEmailsTarget { get; set; } = 50;

        /// <summary>Minimum calls a rep must attempt per day.</summary>
        public int DailyCallsTarget { get; set; } = 25;

        // ── Conversion Targets ─────────────────────────────────────
        /// <summary>Minimum reply rate expected (as % of emails sent).</summary>
        public decimal MinimumReplyRate { get; set; } = 5;

        /// <summary>Minimum meetings rep must book per week.</summary>
        public int WeeklyMeetingsTarget { get; set; } = 5;

        // ── Revenue Targets (Monthly) ──────────────────────────────
        /// <summary>Minimum monthly revenue target.</summary>
        public decimal MonthlyRevenueTarget { get; set; } = 10000;

        /// <summary>Minimum Monthly Recurring Revenue target.</summary>
        public decimal MonthlyRecurringRevenueTarget { get; set; } = 5000;

        // ── Tolerance & Grace Periods ──────────────────────────────
        /// <summary>Days a rep can underperform before consequences trigger.</summary>
        public int GracePeriodDays { get; set; } = 3;

        /// <summary>Percentage buffer before warning (e.g., 10 = 10% below target).</summary>
        public decimal PerformanceTolerance { get; set; } = 10;

        // ── Consequences ───────────────────────────────────────────
        /// <summary>What happens when policy is breached after grace period.</summary>
        public ConsequenceLevel ConsequenceOnBreach { get; set; } = ConsequenceLevel.Freeze;

        /// <summary>Legacy flag: if true, overrides ConsequenceOnBreach to Freeze.</summary>
        public bool AutoFreezeOnBreach { get; set; } = false;

        /// <summary>Require manager approval before unfreezing.</summary>
        public bool ManagerApprovalRequiredToUnfreeze { get; set; } = true;
    }

    /// <summary>
    /// Defines the consequence level when a sales rep breaches KPI policy.
    /// </summary>
    public enum ConsequenceLevel
    {
        /// <summary>No consequence, just track and report.</summary>
        None = 0,

        /// <summary>Send warning notification to rep and manager.</summary>
        Warning = 1,

        /// <summary>Temporarily freeze account until manager review.</summary>
        Freeze = 2,

        /// <summary>Suspend account pending performance improvement plan.</summary>
        Suspend = 3
    }
}
