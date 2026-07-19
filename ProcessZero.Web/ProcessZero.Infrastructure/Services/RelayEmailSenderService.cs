using Google.Apis.Gmail.v1.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    public class RelayEmailSenderService : IRelayEmailSenderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGmailService _gmailService;
        private readonly IGoogleOAuthService _oauthService;
        private readonly IRelayInboxRotationService _inboxRotation;
        private readonly IRelayA_BTestingService _abTesting;
        private readonly string _publicBaseUrl;

        public RelayEmailSenderService(
            ApplicationDbContext context,
            IGmailService gmailService,
            IGoogleOAuthService oauthService,
            IRelayInboxRotationService inboxRotation,
            IRelayA_BTestingService abTesting,
            IConfiguration configuration)
        {
            _context = context;
            _gmailService = gmailService;
            _oauthService = oauthService;
            _inboxRotation = inboxRotation;
            _abTesting = abTesting;

            // Public base URL used to build the unsubscribe link in outgoing emails.
            _publicBaseUrl = configuration["Relay:PublicBaseUrl"]
                ?? "https://app.processzero.xyz";
        }



        // ─────────────────────────────────────────────
        // SEND INITIAL EMAIL
        // ─────────────────────────────────────────────
        public async Task<RelayEmailActivity> SendInitialEmailAsync(
            int inboxId,
            int campaignId,
            int leadId,
            RelayEmailVariant variant)
        {
            var inbox = await _context.RelayEmailAccounts
                .FirstOrDefaultAsync(x => x.Id == inboxId);

            var lead = await _context.RelayLeads
                .FirstOrDefaultAsync(x => x.Id == leadId);

            if (inbox == null || lead == null)
                throw new Exception("Inbox or Lead not found");

            // Respect explicit disable flag: do not send from inactive inboxes
            if (!inbox.IsActive)
                throw new Exception("Inbox is disabled and cannot be used for sending");

            // 1. Ensure token is valid (uses YOUR OAuth layer)
            await _oauthService.EnsureValidAccessTokenAsync(inbox);

            // 2. Personalize content (merge tags) and append the unsubscribe footer
            var subject = RelayEmailContentHelper.Personalize(variant.Subject, lead);
            var htmlBody = RelayEmailContentHelper.Personalize(variant.HtmlBody, lead);
            htmlBody = RelayEmailContentHelper.AppendUnsubscribeFooter(
                htmlBody, _publicBaseUrl, campaignId, leadId);

            // 3. Send email using EXISTING Gmail service
            await _gmailService.SendAsync(
                inbox,
                lead.Email,
                subject,
                htmlBody
            );


            // 3. Retrieve latest sent message (optional tracking step)
            var messages = await _gmailService.ReceiveAsync(inbox, 5);

            var lastMessage = messages.FirstOrDefault();

            // 4. Save activity
            var activity = new RelayEmailActivity
            {
                RelayCampaignId = campaignId,
                RelayLeadId = leadId,
                RelayInboxId = inboxId,
                EmailVariantId = variant.Id,
                GmailMessageId = lastMessage?.MessageId ?? string.Empty,
                GmailThreadId = lastMessage?.ThreadId ?? string.Empty,
                SentAt = DateTime.UtcNow,
                Status = EmailStatus.Sent,
                Replied = false
            };

            _context.RelayEmailActivities.Add(activity);

            await _context.SaveChangesAsync();

            return activity;
        }

        // ─────────────────────────────────────────────
        // SEND FOLLOW-UP EMAIL
        // ─────────────────────────────────────────────
        public async Task<RelayEmailActivity> SendFollowUpAsync(
            int inboxId,
            string threadId,
            RelayEmailVariant variant,
            int campaignId,
            int leadId)
        {
            var inbox = await _context.RelayEmailAccounts
                .FirstOrDefaultAsync(x => x.Id == inboxId);

            var lead = await _context.RelayLeads
                .FirstOrDefaultAsync(x => x.Id == leadId);

            if (inbox == null || lead == null)
                throw new Exception("Inbox or Lead not found");

            // Respect explicit disable flag: do not send from inactive inboxes
            if (!inbox.IsActive)
                throw new Exception("Inbox is disabled and cannot be used for sending");

            // 1. Ensure valid token
            await _oauthService.EnsureValidAccessTokenAsync(inbox);

            // 2. Personalize content (merge tags) and append the unsubscribe footer
            var subject = RelayEmailContentHelper.Personalize(variant.Subject, lead);
            var htmlBody = RelayEmailContentHelper.Personalize(variant.HtmlBody, lead);
            htmlBody = RelayEmailContentHelper.AppendUnsubscribeFooter(
                htmlBody, _publicBaseUrl, campaignId, leadId);

            // 3. Send follow-up inside thread (your Gmail service handles it)
            await _gmailService.SendAsync(
                inbox,
                lead.Email,
                subject,
                htmlBody
            );


            // NOTE:
            // Your current IGmailService does not yet support thread injection.
            // That will be upgraded later in tracking layer.

            var activity = new RelayEmailActivity
            {
                RelayCampaignId = campaignId,
                RelayLeadId = leadId,
                RelayInboxId = inboxId,
                EmailVariantId = variant.Id,
                GmailThreadId = threadId,
                SentAt = DateTime.UtcNow,
                Status = EmailStatus.Sent,
                Replied = false
            };

            _context.RelayEmailActivities.Add(activity);

            await _context.SaveChangesAsync();

            return activity;
        }

        // ─────────────────────────────────────────────
        // SEND SEQUENCE EMAIL (resolves step, inbox, variant)
        // ─────────────────────────────────────────────
        public async Task<RelayEmailActivity> SendSequenceEmailAsync(int campaignLeadId)
        {
            var campaignLead = await _context.RelayCampaignLeads
                .FirstOrDefaultAsync(x => x.Id == campaignLeadId);

            if (campaignLead == null)
                throw new Exception("Campaign lead not found");

            if (campaignLead.Replied || campaignLead.Unsubscribed || campaignLead.Completed)
                throw new Exception("Lead is no longer active in the sequence");

            // Load campaign with its sequence steps + variants
            var campaign = await _context.RelayCampaigns
                .Include(x => x.Sequences)
                    .ThenInclude(s => s.Steps)
                        .ThenInclude(st => st.Variants)
                .FirstOrDefaultAsync(x => x.Id == campaignLead.RelayCampaignId);

            if (campaign == null)
                throw new Exception("Campaign not found");

            var sequence = campaign.Sequences.FirstOrDefault();

            if (sequence == null)
                throw new Exception("Campaign has no sequence configured");

            var orderedSteps = sequence.Steps
                .Where(x => x.IsActive)
                .OrderBy(x => x.StepOrder)
                .ToList();

            if (!orderedSteps.Any())
                throw new Exception("Sequence has no active steps");

            // Determine the step to execute:
            // - if no current step, start at the first step
            // - otherwise advance to the step after the current one
            RelaySequenceStep? stepToExecute;

            if (campaignLead.CurrentSequenceStepId == null)
            {
                stepToExecute = orderedSteps.First();
            }
            else
            {
                var currentIndex = orderedSteps
                    .FindIndex(x => x.Id == campaignLead.CurrentSequenceStepId);

                var nextIndex = currentIndex + 1;

                if (currentIndex < 0 || nextIndex >= orderedSteps.Count)
                {
                    campaignLead.Status = CampaignLeadStatus.Completed;
                    campaignLead.Completed = true;
                    await _context.SaveChangesAsync();
                    throw new Exception("Lead has already completed the sequence");
                }

                stepToExecute = orderedSteps[nextIndex];
            }

            // Select an available inbox via rotation
            var inbox = await _inboxRotation.GetNextAvailableInboxAsync(campaign.Id);

            if (inbox == null)
                throw new Exception("No available inbox to send from");

            // Select the email variant via A/B testing (fallback to step variant)
            var variant = await _abTesting.SelectVariantAsync(stepToExecute.Id)
                ?? stepToExecute.Variants
                    .OrderBy(v => Guid.NewGuid())
                    .FirstOrDefault();

            if (variant == null)
                throw new Exception("Step has no email variant configured");

            // Send the email and log activity
            var activity = await SendInitialEmailAsync(
                inbox.Id,
                campaign.Id,
                campaignLead.RelayLeadId,
                variant);

            // Advance the lead's state machine
            campaignLead.CurrentSequenceStepId = stepToExecute.Id;
            campaignLead.Status = CampaignLeadStatus.Active;

            if (orderedSteps.Last().Id == stepToExecute.Id)
            {
                campaignLead.Status = CampaignLeadStatus.Completed;
                campaignLead.Completed = true;
            }

            await _inboxRotation.IncrementSentCountAsync(inbox.Id);
            await _context.SaveChangesAsync();

            return activity;
        }

        // ─────────────────────────────────────────────
        // SEND TEST EMAIL
        // ─────────────────────────────────────────────
        public async Task<RelayEmailActivity> SendTestEmailAsync(int inboxId, string to)
        {
            if (string.IsNullOrWhiteSpace(to))
                throw new Exception("Recipient address is required");

            var inbox = await _context.RelayEmailAccounts
                .FirstOrDefaultAsync(x => x.Id == inboxId);

            if (inbox == null)
                throw new Exception("Inbox not found");

            // Respect explicit disable flag: do not send from inactive inboxes
            if (!inbox.IsActive)
                throw new Exception("Inbox is disabled and cannot be used for sending");

            // 1. Ensure token is valid
            await _oauthService.EnsureValidAccessTokenAsync(inbox);

            // 2. Send a simple test email
            const string subject = "ProcessZero Relay — Test Email";
            const string body =
                "<p>This is a test email from your ProcessZero Relay inbox. " +
                "If you received this, your inbox is connected and able to send.</p>";

            await _gmailService.SendAsync(inbox, to, subject, body);

            // 3. Try to capture the sent message id/thread for tracking
            var messages = await _gmailService.ReceiveAsync(inbox, 5);
            var lastMessage = messages.FirstOrDefault();

            // 4. Log activity (no campaign/lead/variant for a raw test send)
            var activity = new RelayEmailActivity
            {
                RelayInboxId = inboxId,
                GmailMessageId = lastMessage?.MessageId ?? string.Empty,
                GmailThreadId = lastMessage?.ThreadId ?? string.Empty,
                SentAt = DateTime.UtcNow,
                Status = EmailStatus.Sent,
                Replied = false
            };

            _context.RelayEmailActivities.Add(activity);
            await _context.SaveChangesAsync();

            return activity;
        }
    }
}


