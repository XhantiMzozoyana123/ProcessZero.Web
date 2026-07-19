using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;

namespace ProcessZero.Infrastructure.Services
{
    public class RelayCampaignBackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public RelayCampaignBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        // ─────────────────────────────────────────────
        // 🚀 MASTER CAMPAIGN RUNNER (MAIN LOOP)
        // ─────────────────────────────────────────────
        public void Start()
        {
            RecurringJob.AddOrUpdate<RelayCampaignBackgroundService>(
                "relay-campaign-runner",
                svc => svc.ProcessCampaigns(),
                Cron.Minutely);

            RecurringJob.AddOrUpdate<RelayCampaignBackgroundService>(
                "relay-reply-sync",
                svc => svc.SyncReplies(),
                Cron.Minutely);

            RecurringJob.AddOrUpdate<RelayCampaignBackgroundService>(
                "relay-reset-inboxes",
                svc => svc.ResetInboxLimits(),
                Cron.Daily);
        }

        // ─────────────────────────────────────────────
        // 🔁 PROCESS CAMPAIGNS
        // ─────────────────────────────────────────────
        public async Task ProcessCampaigns()
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var sequenceService = scope.ServiceProvider.GetRequiredService<IRelaySequenceService>();

            var activeCampaigns = await db.RelayCampaigns
                .Where(x => x.IsActive)
                .ToListAsync();

            foreach (var campaign in activeCampaigns)
            {
                await sequenceService.ProcessSequenceAsync(campaign.Id);
            }
        }

        // ─────────────────────────────────────────────
        // 📊 SYNC REPLIES
        // ─────────────────────────────────────────────
        public async Task SyncReplies()
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var trackingService = scope.ServiceProvider.GetRequiredService<IRelayEmailTrackingService>();

            // Only sync replies for active inboxes
            var inboxes = await db.RelayEmailAccounts
                .Where(x => x.IsActive)
                .ToListAsync();

            foreach (var inbox in inboxes)
            {
                await trackingService.SyncRepliesAsync(inbox.Id);
            }
        }

        // ─────────────────────────────────────────────
        // 📦 RESET INBOX LIMITS DAILY
        // ─────────────────────────────────────────────
        public async Task ResetInboxLimits()
        {
            using var scope = _scopeFactory.CreateScope();

            var inboxService = scope.ServiceProvider.GetRequiredService<IRelayInboxRotationService>();

            await inboxService.ResetDailyLimitsAsync();
        }
    }
}
