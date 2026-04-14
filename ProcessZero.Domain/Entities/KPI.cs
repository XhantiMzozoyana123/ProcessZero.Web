using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    public class KPI : BaseEntity
    {
        [Required]
        public int ProductId { get; set; }

        // LEVEL 1 — Sales Partner
        public int OutreachAttempts { get; set; } = 0;
        public int CallsBooked { get; set; } = 0;
        public int CallsAttended { get; set; } = 0;
        public int DealsInfluenced { get; set; } = 0;
        public decimal RevenueGenerated { get; set; } = 0;

        // LEVEL 2 — Senior Partner
        public int DealsClosed { get; set; } = 0;
        public int DealsAttempted { get; set; } = 0;
        public decimal AverageDealSize { get; set; } = 0;
        public decimal RevenueInfluenced { get; set; } = 0;
        public double BasicClientRetention { get; set; } = 0;
        public double ActivityConsistency { get; set; } = 0;

        // LEVEL 3 — Network Leader
        public int ActiveTeamSize { get; set; } = 0;
        public decimal TeamRevenue { get; set; } = 0;
        public double TeamCloseRate { get; set; } = 0;
        public double TeamChurnRate { get; set; } = 0;
        public double LeaderActivityLevel { get; set; } = 0;

        // LEVEL 4 & 5
        public decimal MonthlyRecurringRevenue { get; set; } = 0;
        public double GrowthRate { get; set; } = 0;
        public double ClientRetention { get; set; } = 0;
        public double TeamPerformanceHealth { get; set; } = 0;
        public double BrandCompliance { get; set; } = 0;
        public double LongTermRevenueGrowth { get; set; } = 0;
        public int StrategicInitiativesDelivered { get; set; } = 0;
        public double BrandRiskManagement { get; set; } = 0;
        public double InnovationContribution { get; set; } = 0;
        public double LeadershipStability { get; set; } = 0;
    }

}
