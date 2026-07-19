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
    public class RelayEmailTrackingService : IRelayEmailTrackingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGmailService _gmailService;

        public RelayEmailTrackingService(
            ApplicationDbContext context,
            IGmailService gmailService)
        {
            _context = context;
            _gmailService = gmailService;
        }

        // ─────────────────────────────────────────────
        // SYNC REPLIES FROM GMAIL
        // ─────────────────────────────────────────────
        public async Task SyncRepliesAsync(int inboxId)
        {
            var inbox = await _context.RelayEmailAccounts
                .FirstOrDefaultAsync(x => x.Id == inboxId);

            if (inbox == null)
                throw new Exception("Inbox not found");

            // Respect explicit disable flag: do not sync from inactive inboxes
            if (!inbox.IsActive)
                return;

            // 1. Pull latest emails from Gmail
            var messages = await _gmailService.ReceiveAsync(inbox, 50);

            // 2. Filter only replies (UNREAD or inbound messages)
            var replies = messages
                .Where(m => m.From != inbox.EmailAddress)
                .ToList();

            foreach (var message in replies)
            {
                var existing = await _context.RelayEmailActivities
                    .FirstOrDefaultAsync(x => x.GmailMessageId == message.MessageId);

                if (existing != null)
                    continue;

                // 3. Find related activity by thread
                var activity = await _context.RelayEmailActivities
                    .FirstOrDefaultAsync(x => x.GmailThreadId == message.ThreadId);

                if (activity != null)
                {
                    activity.Replied = true;
                    activity.RepliedAt = DateTime.UtcNow;
                    activity.Status = EmailStatus.Replied;

                    // CRITICAL: stop the sequence for this lead. The sequence engine
                    // filters on RelayCampaignLead.Replied, so without this update the
                    // lead would keep receiving follow-ups after they have replied.
                    var campaignLead = await _context.RelayCampaignLeads
                        .FirstOrDefaultAsync(cl =>
                            cl.RelayCampaignId == activity.RelayCampaignId &&
                            cl.RelayLeadId == activity.RelayLeadId);

                    if (campaignLead != null && !campaignLead.Replied)
                    {
                        campaignLead.Replied = true;
                        campaignLead.Status = CampaignLeadStatus.Replied;
                    }
                }
                else

                {
                    // orphan reply (not in campaign)
                    _context.RelayEmailActivities.Add(new RelayEmailActivity
                    {
                        GmailMessageId = message.MessageId,
                        GmailThreadId = message.ThreadId,
                        SentAt = message.ReceivedDate,
                        Replied = true,
                        RepliedAt = DateTime.UtcNow,
                        Status = EmailStatus.Replied
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────
        // MARK THREAD AS REPLIED
        // ─────────────────────────────────────────────
        public async Task<bool> MarkAsRepliedAsync(string gmailThreadId)
        {
            var activities = await _context.RelayEmailActivities
                .Where(x => x.GmailThreadId == gmailThreadId)
                .ToListAsync();

            if (!activities.Any())
                return false;

            foreach (var activity in activities)
            {
                activity.Replied = true;
                activity.RepliedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // ─────────────────────────────────────────────
        // GET ACTIVITY BY LEAD
        // ─────────────────────────────────────────────
        public async Task<List<RelayEmailActivity>> GetActivityByLeadAsync(int leadId)
        {
            return await _context.RelayEmailActivities
                .Where(x => x.RelayLeadId == leadId)
                .OrderByDescending(x => x.SentAt)
                .ToListAsync();
        }

        // ─────────────────────────────────────────────
        // GET ACTIVITY BY CAMPAIGN
        // ─────────────────────────────────────────────
        public async Task<List<RelayEmailActivity>> GetActivityByCampaignAsync(int campaignId)
        {
            return await _context.RelayEmailActivities
                .Where(x => x.RelayCampaignId == campaignId)
                .OrderByDescending(x => x.SentAt)
                .ToListAsync();
        }
    }
}
