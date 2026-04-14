using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Infrastructure.Services
{
    public class KpiService : IKpiService
    {
        private readonly ApplicationDbContext _context;

        public KpiService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddOutreachAttemptsAsync(string userId, int productId, int attempts)
        {
            await AddKPIIncrementAsync(userId, productId, outreachAttempts: attempts);
        }

        public async Task AddCallBookedAsync(string userId, int productId)
        {
            await AddKPIIncrementAsync(userId, productId, callsBooked: 1);
        }

        public async Task AddCallAttendanceAsync(string userId, int productId, int attended)
        {
            await AddKPIIncrementAsync(userId, productId, callsAttended: attended);
        }

        public async Task AddDealsInfluencedAsync(string userId, int productId, int deals = 1)
        {
            await AddKPIIncrementAsync(userId, productId, dealsInfluenced: deals);
        }

        public async Task AddRevenueGeneratedAsync(string userId, int productId, decimal revenue)
        {
            await AddKPIIncrementAsync(userId, productId, revenueGenerated: revenue);
        }

        public async Task AddDealsClosedAsync(string userId, int productId, int dealsClosed, int dealsAttempted)
        {
            await AddKPIIncrementAsync(userId, productId, dealsClosed: dealsClosed, dealsAttempted: dealsAttempted);
        }

        public async Task AddAverageDealSizeAsync(string userId, int productId, decimal dealAmount)
        {
            await AddKPIIncrementAsync(userId, productId, averageDealSize: dealAmount);
        }

        public async Task AddRevenueInfluencedAsync(string userId, int productId, decimal revenue)
        {
            await AddKPIIncrementAsync(userId, productId, revenueInfluenced: revenue);
        }

        public async Task AddBasicClientRetentionAsync(string userId, int productId, double retentionPercentage)
        {
            await AddKPIIncrementAsync(userId, productId, basicClientRetention: retentionPercentage);
        }

        public async Task AddActivityConsistencyAsync(string userId, int productId, int targetMet)
        {
            await AddKPIIncrementAsync(userId, productId, activityConsistency: targetMet);
        }

        public async Task UpdateActiveTeamSizeAsync(string userId, int productId)
        {
            var teamSize = await _context.Users.CountAsync(p => p.Id != userId);
            await AddKPIIncrementAsync(userId, productId, activeTeamSize: teamSize);
        }

        public async Task UpdateTeamRevenueAsync(string userId, int productId)
        {
            var revenue = await _context.KPIs
                .Where(k => k.ProductId == productId && k.UserId != userId)
                .SumAsync(k => k.RevenueGenerated);

            await AddKPIIncrementAsync(userId, productId, teamRevenue: revenue);
        }

        public async Task UpdateTeamCloseRateAsync(string userId, int productId)
        {
            var teamKPIs = await _context.KPIs
                .Where(k => k.ProductId == productId && k.UserId != userId)
                .ToListAsync();

            if (!teamKPIs.Any()) return;

            var avgCloseRate = teamKPIs.Average(k => k.TeamCloseRate);
            await AddKPIIncrementAsync(userId, productId, teamCloseRate: avgCloseRate);
        }

        public async Task UpdateTeamChurnRateAsync(string userId, int productId, double churnRate = 0)
        {
            await AddKPIIncrementAsync(userId, productId, teamChurnRate: churnRate);
        }

        public async Task UpdateLeaderActivityLevelAsync(string userId, int productId, double level = 1)
        {
            await AddKPIIncrementAsync(userId, productId, leaderActivityLevel: level);
        }

        public async Task UpdateMonthlyRecurringRevenueAsync(string userId, int productId)
        {
            var totalRevenue = await _context.KPIs
                .Where(k => k.ProductId == productId)
                .SumAsync(k => k.RevenueGenerated);

            await AddKPIIncrementAsync(userId, productId, monthlyRecurringRevenue: totalRevenue);
        }

        public async Task UpdateGrowthRateAsync(string userId, int productId, double growthRate = 0.1)
        {
            await AddKPIIncrementAsync(userId, productId, growthRate: growthRate);
        }

        public async Task UpdateClientRetentionAsync(string userId, int productId, double retention = 1.0)
        {
            await AddKPIIncrementAsync(userId, productId, clientRetention: retention);
        }

        public async Task UpdateTeamPerformanceHealthAsync(string userId, int productId, double health = 1.0)
        {
            await AddKPIIncrementAsync(userId, productId, teamPerformanceHealth: health);
        }

        public async Task UpdateBrandComplianceAsync(string userId, int productId, double complianceScore)
        {
            await AddKPIIncrementAsync(userId, productId, brandCompliance: complianceScore);
        }

        public async Task UpdateLongTermRevenueGrowthAsync(string userId, int productId, double growth = 0.2)
        {
            await AddKPIIncrementAsync(userId, productId, longTermRevenueGrowth: growth);
        }

        public async Task UpdateStrategicInitiativesDeliveredAsync(string userId, int productId, int initiatives)
        {
            await AddKPIIncrementAsync(userId, productId, strategicInitiativesDelivered: initiatives);
        }

        public async Task UpdateBrandRiskManagementAsync(string userId, int productId, double riskScore)
        {
            await AddKPIIncrementAsync(userId, productId, brandRiskManagement: riskScore);
        }

        public async Task UpdateInnovationContributionAsync(string userId, int productId, double contributionScore)
        {
            await AddKPIIncrementAsync(userId, productId, innovationContribution: contributionScore);
        }

        public async Task UpdateLeadershipStabilityAsync(string userId, int productId, double stability = 1.0)
        {
            await AddKPIIncrementAsync(userId, productId, leadershipStability: stability);
        }
        private async Task AddKPIIncrementAsync(
            string userId,
            int productId,
            int outreachAttempts = 0,
            int callsBooked = 0,
            int callsAttended = 0,
            int dealsInfluenced = 0,
            decimal revenueGenerated = 0,
            int dealsClosed = 0,
            int dealsAttempted = 0,
            decimal averageDealSize = 0,
            decimal revenueInfluenced = 0,
            double basicClientRetention = 0,
            double activityConsistency = 0,
            int activeTeamSize = 0,
            decimal teamRevenue = 0,
            double teamCloseRate = 0,
            double teamChurnRate = 0,
            double leaderActivityLevel = 0,
            decimal monthlyRecurringRevenue = 0,
            double growthRate = 0,
            double clientRetention = 0,
            double teamPerformanceHealth = 0,
            double brandCompliance = 0,
            double longTermRevenueGrowth = 0,
            int strategicInitiativesDelivered = 0,
            double brandRiskManagement = 0,
            double innovationContribution = 0,
            double leadershipStability = 0
        )
        {
            var kpi = new KPI
            {
                UserId = userId,
                ProductId = productId,
                OutreachAttempts = outreachAttempts,
                CallsBooked = callsBooked,
                CallsAttended = callsAttended,
                DealsInfluenced = dealsInfluenced,
                RevenueGenerated = revenueGenerated,
                DealsClosed = dealsClosed,
                DealsAttempted = dealsAttempted,
                AverageDealSize = averageDealSize,
                RevenueInfluenced = revenueInfluenced,
                BasicClientRetention = basicClientRetention,
                ActivityConsistency = activityConsistency,
                ActiveTeamSize = activeTeamSize,
                TeamRevenue = teamRevenue,
                TeamCloseRate = teamCloseRate,
                TeamChurnRate = teamChurnRate,
                LeaderActivityLevel = leaderActivityLevel,
                MonthlyRecurringRevenue = monthlyRecurringRevenue,
                GrowthRate = growthRate,
                ClientRetention = clientRetention,
                TeamPerformanceHealth = teamPerformanceHealth,
                BrandCompliance = brandCompliance,
                LongTermRevenueGrowth = longTermRevenueGrowth,
                StrategicInitiativesDelivered = strategicInitiativesDelivered,
                BrandRiskManagement = brandRiskManagement,
                InnovationContribution = innovationContribution,
                LeadershipStability = leadershipStability,
                CreatedAt = DateTime.UtcNow
            };

            // Try to update the latest KPI row for this user+product. If none exists, insert new.
            var existing = await _context.KPIs
                .Where(k => k.UserId == userId && k.ProductId == productId)
                .OrderByDescending(k => k.CreatedAt)
                .FirstOrDefaultAsync();

            if (existing is null)
            {
                await _context.KPIs.AddAsync(kpi);
            }
            else
            {
                // Increment counters
                existing.OutreachAttempts += outreachAttempts;
                existing.CallsBooked += callsBooked;
                existing.CallsAttended += callsAttended;
                existing.DealsInfluenced += dealsInfluenced;
                existing.RevenueGenerated += revenueGenerated;
                existing.DealsClosed += dealsClosed;
                existing.DealsAttempted += dealsAttempted;
                existing.StrategicInitiativesDelivered += strategicInitiativesDelivered;

                // Overwrite / set values only when a non-default value is provided
                if (averageDealSize != 0) existing.AverageDealSize = averageDealSize;
                if (revenueInfluenced != 0) existing.RevenueInfluenced = revenueInfluenced;
                if (basicClientRetention != 0) existing.BasicClientRetention = basicClientRetention;
                if (activityConsistency != 0) existing.ActivityConsistency = activityConsistency;
                if (activeTeamSize != 0) existing.ActiveTeamSize = activeTeamSize;
                if (teamRevenue != 0) existing.TeamRevenue = teamRevenue;
                if (teamCloseRate != 0) existing.TeamCloseRate = teamCloseRate;
                if (teamChurnRate != 0) existing.TeamChurnRate = teamChurnRate;
                if (leaderActivityLevel != 0) existing.LeaderActivityLevel = leaderActivityLevel;
                if (monthlyRecurringRevenue != 0) existing.MonthlyRecurringRevenue = monthlyRecurringRevenue;
                if (growthRate != 0) existing.GrowthRate = growthRate;
                if (clientRetention != 0) existing.ClientRetention = clientRetention;
                if (teamPerformanceHealth != 0) existing.TeamPerformanceHealth = teamPerformanceHealth;
                if (brandCompliance != 0) existing.BrandCompliance = brandCompliance;
                if (longTermRevenueGrowth != 0) existing.LongTermRevenueGrowth = longTermRevenueGrowth;
                if (brandRiskManagement != 0) existing.BrandRiskManagement = brandRiskManagement;
                if (innovationContribution != 0) existing.InnovationContribution = innovationContribution;
                if (leadershipStability != 0) existing.LeadershipStability = leadershipStability;

                existing.UpdatedAt = DateTime.UtcNow;

                _context.KPIs.Update(existing);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<KPI> GetLatestKPIAsync(string userId, int productId)
        {
            return await _context.KPIs
                .Where(k => k.UserId == userId && k.ProductId == productId)
                .OrderByDescending(k => k.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<decimal> GetTotalRevenueAsync(string userId, int productId)
        {
            return await _context.KPIs
                .Where(k => k.UserId == userId && k.ProductId == productId)
                .SumAsync(k => k.RevenueGenerated);
        }

        public async Task<double> GetCloseRateAsync(string userId, int productId)
        {
            var totalClosed = await _context.KPIs
                .Where(k => k.UserId == userId && k.ProductId == productId)
                .SumAsync(k => k.DealsClosed);

            var totalAttempted = await _context.KPIs
                .Where(k => k.UserId == userId && k.ProductId == productId)
                .SumAsync(k => k.DealsAttempted);

            if (totalAttempted == 0) return 0;
            return (double)totalClosed / totalAttempted;
        }
    }
}
