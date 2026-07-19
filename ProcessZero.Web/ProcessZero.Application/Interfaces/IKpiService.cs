using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Interfaces
{
    public interface IKpiService
    {
        // ── Outreach layer ──────────────────────────────────────────
        Task AddCallOutreachAsync(string userId, int productId, int count = 1);
        Task AddEmailOutreachAsync(string userId, int productId, int count = 1);

        // ── Conversion layer ────────────────────────────────────────
        Task AddCallsMadeAsync(string userId, int productId, int count = 1);
        Task AddRepliesReceivedAsync(string userId, int productId, int count = 1);

        // ── Meetings & deals layer ──────────────────────────────────
        Task AddMeetingBookedAsync(string userId, int productId); // Handled by MeetingService when a meeting is created
        Task AddDealClosedAsync(string userId, int productId, decimal amount);

        // ── Calculations ────────────────────────────────────────────
        // Recalculate MRR from active client contacts and their closed invoice amounts.
        Task RecalculateMrrAsync(string userId, int productId);

        // ── Queries ─────────────────────────────────────────────────
        Task<KPI> GetLatestKPIAsync(string userId, int productId);
        Task<decimal> GetTotalDealsClosedAsync(string userId, int productId);
    }
}
