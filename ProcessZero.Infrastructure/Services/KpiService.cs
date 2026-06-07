using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public Task AddCallOutreachAsync(string userId, int productId, int count = 1)
            => AddKpiIncrementAsync(userId, productId, callOutreach: count);

        public Task AddEmailOutreachAsync(string userId, int productId, int count = 1)
            => AddKpiIncrementAsync(userId, productId, emailOutreach: count);

        public Task AddCallsMadeAsync(string userId, int productId, int count = 1)
            => AddKpiIncrementAsync(userId, productId, callsMade: count);

        public Task AddMeetingBookedAsync(string userId, int productId)
            => AddKpiIncrementAsync(userId, productId, meetingsBooked: 1);

        public Task AddDealClosedAsync(string userId, int productId, decimal amount)
            => AddKpiIncrementAsync(userId, productId, dealSizeClosed: amount);

        /// <summary>
        /// Finds (or creates) the daily KPI row for the rep + product, applies the
        /// activity increments, then recalculates MRR from Contacts + Invoices.
        /// </summary>
        private async Task AddKpiIncrementAsync(
            string userId,
            int productId,
            int callOutreach = 0,
            int emailOutreach = 0,
            int callsMade = 0,
            int meetingsBooked = 0,
            decimal dealSizeClosed = 0)
        {
            var kpi = await GetOrCreateTodayKpiAsync(userId, productId);

            kpi.CallOutreach += callOutreach;
            kpi.EmailOutreach += emailOutreach;
            kpi.CallsMade += callsMade;
            kpi.MeetingsBooked += meetingsBooked;
            kpi.DealSizeClosed += dealSizeClosed;
            kpi.UpdatedAt = DateTime.UtcNow;

            await ApplyMrrAsync(userId, productId, kpi);

            await _context.SaveChangesAsync();
        }

        public async Task RecalculateMrrAsync(string userId, int productId)
        {
            var kpi = await GetOrCreateTodayKpiAsync(userId, productId);
            await ApplyMrrAsync(userId, productId, kpi);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Calculates MRR for the rep + product from active client contacts that
        /// have at least one invoice for the product, using the amount they closed on.
        /// </summary>
        private async Task ApplyMrrAsync(string userId, int productId, KPI kpi)
        {
            var activeClients = await _context.Contacts
                .Where(c => c.UserId == userId
                            && c.Status == ContactStatus.Active
                            && _context.Invoices.Any(i => i.ClientId == c.Id && i.ProductId == productId))
                .Select(c => new { c.Id, c.ClosedAmount })
                .ToListAsync();

            kpi.ActiveClients = activeClients.Count;
            kpi.MonthlyRecurringRevenue = activeClients.Sum(c => c.ClosedAmount);
            kpi.UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns the KPI row for the current UTC day for this rep + product,
        /// creating and tracking a new one if none exists yet.
        /// </summary>
        private async Task<KPI> GetOrCreateTodayKpiAsync(string userId, int productId)
        {
            var todayUtc = DateTime.UtcNow.Date;

            var existing = await _context.KPIs
                .Where(k => k.UserId == userId && k.ProductId == productId && k.CreatedAt >= todayUtc)
                .OrderByDescending(k => k.CreatedAt)
                .FirstOrDefaultAsync();

            if (existing is not null) return existing;

            var kpi = new KPI
            {
                UserId = userId,
                ProductId = productId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.KPIs.AddAsync(kpi);
            return kpi;
        }

        public async Task<KPI> GetLatestKPIAsync(string userId, int productId)
        {
            return await _context.KPIs
                .Where(k => k.UserId == userId && k.ProductId == productId)
                .OrderByDescending(k => k.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<decimal> GetTotalDealsClosedAsync(string userId, int productId)
        {
            return await _context.KPIs
                .Where(k => k.UserId == userId && k.ProductId == productId)
                .SumAsync(k => k.DealSizeClosed);
        }
    }
}
