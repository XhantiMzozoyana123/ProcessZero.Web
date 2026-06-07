using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Interfaces
{
    public interface IKpiService
    {
        // Daily sales rep activity
        Task AddCallOutreachAsync(string userId, int productId, int count = 1);
        Task AddEmailOutreachAsync(string userId, int productId, int count = 1);
        Task AddCallsMadeAsync(string userId, int productId, int count = 1);
        Task AddMeetingBookedAsync(string userId, int productId); // Handled by MeetingService when a meeting is created
        Task AddDealClosedAsync(string userId, int productId, decimal amount);

        // Recalculate MRR from active client contacts and their closed invoice amounts.
        Task RecalculateMrrAsync(string userId, int productId);

        Task<KPI> GetLatestKPIAsync(string userId, int productId);
        Task<decimal> GetTotalDealsClosedAsync(string userId, int productId);
    }
}
