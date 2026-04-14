using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Interfaces
{
    public interface IKpiService
    {
        Task AddOutreachAttemptsAsync(string userId, int productId, int attempts);
        Task AddCallBookedAsync(string userId, int productId); // Handled by MeetingService when a meeting is created
        Task AddCallAttendanceAsync(string userId, int productId, int attended);
        Task AddDealsInfluencedAsync(string userId, int productId, int deals = 1);
        Task AddRevenueGeneratedAsync(string userId, int productId, decimal revenue); // Handled by InvoiceService when an invoice is marked as paid
        Task AddDealsClosedAsync(string userId, int productId, int dealsClosed, int dealsAttempted); // Handled by InvoiceService when an invoice is marked as paid
        Task AddAverageDealSizeAsync(string userId, int productId, decimal dealAmount); // Handled by InvoiceService when an invoice is marked as paid
        Task AddRevenueInfluencedAsync(string userId, int productId, decimal revenue);
        Task AddBasicClientRetentionAsync(string userId, int productId, double retentionPercentage); // Handled by PayrollService when paying commissions
        Task AddActivityConsistencyAsync(string userId, int productId, int targetMet);
        Task UpdateActiveTeamSizeAsync(string userId, int productId);
        Task UpdateTeamRevenueAsync(string userId, int productId);
        Task UpdateTeamCloseRateAsync(string userId, int productId);
        Task UpdateTeamChurnRateAsync(string userId, int productId, double churnRate = 0);
        Task UpdateLeaderActivityLevelAsync(string userId, int productId, double level = 1);
        Task UpdateMonthlyRecurringRevenueAsync(string userId, int productId);
        Task UpdateGrowthRateAsync(string userId, int productId, double growthRate = 0.1);
        Task UpdateClientRetentionAsync(string userId, int productId, double retention = 1.0);
        Task UpdateTeamPerformanceHealthAsync(string userId, int productId, double health = 1.0);
        Task UpdateBrandComplianceAsync(string userId, int productId, double complianceScore);
        Task UpdateLongTermRevenueGrowthAsync(string userId, int productId, double growth = 0.2);
        Task UpdateStrategicInitiativesDeliveredAsync(string userId, int productId, int initiatives);
        Task UpdateBrandRiskManagementAsync(string userId, int productId, double riskScore);
        Task UpdateInnovationContributionAsync(string userId, int productId, double contributionScore);
        Task UpdateLeadershipStabilityAsync(string userId, int productId, double stability = 1.0);
        Task<KPI> GetLatestKPIAsync(string userId, int productId);
        Task<decimal> GetTotalRevenueAsync(string userId, int productId);
        Task<double> GetCloseRateAsync(string userId, int productId);
    }
}
