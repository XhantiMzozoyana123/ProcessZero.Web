using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Defines the MRR target a sales rep (or product) is expected to reach.
    /// MRR is calculated from active client contacts and their closed invoice
    /// amounts, and is compared against <see cref="TargetMRR"/>.
    /// </summary>
    public class KpiPolicy : BaseEntity
    {
        // Which product this policy applies to (null = all products)
        public int? ProductId { get; set; }

        // Policy lifecycle
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;

        // The Monthly Recurring Revenue target for the period.
        public decimal TargetMRR { get; set; }

        // Consequence flags
        public int GracePeriodDays { get; set; }
        public bool AutoFreezeOnBreach { get; set; }
    }
}
