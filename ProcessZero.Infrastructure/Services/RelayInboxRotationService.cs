using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    public class RelayInboxRotationService : IRelayInboxRotationService
    {
        private readonly ApplicationDbContext _context;

        public RelayInboxRotationService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────
        // GET NEXT AVAILABLE INBOX (CORE LOGIC)
        // ─────────────────────────────────────────────
        public async Task<RelayEmailAccount?> GetNextAvailableInboxAsync(int campaignId)
        {
            var campaignInboxes = await _context.RelayCampaignInboxes
                .Where(x => x.RelayCampaignId == campaignId)
                .Include(x => x.RelayInbox)
                .Select(x => x.RelayInbox)
                .ToListAsync();

            if (!campaignInboxes.Any())
                return null;

            // pick inbox with lowest usage today (round-robin + safety)
            var inbox = campaignInboxes
                .Where(x => x.IsActive && x.HealthStatus == AccountHealthStatus.Healthy)
                .OrderBy(x => x.SentToday)
                .ThenBy(x => x.LastUsedAt)
                .FirstOrDefault();

            if (inbox == null)
                return null;

            var canSend = await CanSendAsync(inbox.Id);

            if (!canSend)
                return null;

            return inbox;
        }

        // ─────────────────────────────────────────────
        // CHECK IF INBOX CAN SEND
        // ─────────────────────────────────────────────
        public async Task<bool> CanSendAsync(int inboxId)
        {
            var inbox = await _context.RelayEmailAccounts
                .FirstOrDefaultAsync(x => x.Id == inboxId);

            if (inbox == null)
                return false;

            if (!inbox.IsActive)
                return false;

            if (inbox.HealthStatus != AccountHealthStatus.Healthy)
                return false;

            // daily limit protection
            if (inbox.SentToday >= inbox.DailySendLimit)
                return false;

            // optional safety buffer (prevents Gmail spam flags)
            if (inbox.SentToday >= inbox.DailySendLimit * 0.9)
                return false;

            return true;
        }

        // ─────────────────────────────────────────────
        // INCREMENT USAGE AFTER SENDING
        // ─────────────────────────────────────────────
        public async Task IncrementSentCountAsync(int inboxId)
        {
            var inbox = await _context.RelayEmailAccounts
                .FirstOrDefaultAsync(x => x.Id == inboxId);

            if (inbox == null)
                return;

            inbox.SentToday += 1;
            inbox.LastUsedAt = DateTime.UtcNow;

            // health degradation logic (important for deliverability)
            if (inbox.SentToday > inbox.DailySendLimit)
            {
                inbox.HealthStatus = AccountHealthStatus.Warning;
            }

            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────
        // RESET DAILY LIMITS (CRON JOB)
        // ─────────────────────────────────────────────
        public async Task ResetDailyLimitsAsync()
        {
            var inboxes = await _context.RelayEmailAccounts.ToListAsync();

            foreach (var inbox in inboxes)
            {
                inbox.SentToday = 0;

                // recover health if previously warned
                if (inbox.HealthStatus == AccountHealthStatus.Warning)
                {
                    inbox.HealthStatus = AccountHealthStatus.Healthy;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
