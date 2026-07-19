using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;

namespace ProcessZero.Infrastructure.Services
{
    public class RelaySequenceService : IRelaySequenceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRelayInboxRotationService _inboxRotation;
        private readonly IRelayEmailSenderService _emailSender;
        private readonly IRelayA_BTestingService _abTesting;

        // ── Deliverability controls (configurable, sensible defaults) ──
        // Sending window is expressed in UTC hours [start, end). Defaults to 0–24
        // (always allowed) so it never silently blocks sends out of the box; tighten
        // these in appsettings for real campaigns (e.g. 8–18, weekdays only).
        private readonly int _sendWindowStartHour;
        private readonly int _sendWindowEndHour;
        private readonly bool _sendOnWeekends;
        // % chance to defer a due lead to a later tick, spreading sends out instead of
        // blasting every due lead in the same minute (0 = off).
        private readonly int _jitterSkipPercent;
        private static readonly Random _random = new Random();

        public RelaySequenceService(
            ApplicationDbContext context,
            IRelayInboxRotationService inboxRotation,
            IRelayEmailSenderService emailSender,
            IRelayA_BTestingService abTesting,
            IConfiguration configuration)
        {
            _context = context;
            _inboxRotation = inboxRotation;
            _emailSender = emailSender;
            _abTesting = abTesting;

            _sendWindowStartHour = configuration.GetValue<int?>("Relay:SendWindowStartHour") ?? 0;
            _sendWindowEndHour = configuration.GetValue<int?>("Relay:SendWindowEndHour") ?? 24;
            _sendOnWeekends = configuration.GetValue<bool?>("Relay:SendOnWeekends") ?? true;
            _jitterSkipPercent = configuration.GetValue<int?>("Relay:SendJitterSkipPercent") ?? 0;
        }

        // Returns true if the current UTC time is inside the configured sending window.
        private bool IsWithinSendingWindow(DateTime nowUtc)
        {
            if (!_sendOnWeekends &&
                (nowUtc.DayOfWeek == DayOfWeek.Saturday || nowUtc.DayOfWeek == DayOfWeek.Sunday))
                return false;

            var hour = nowUtc.Hour;
            return hour >= _sendWindowStartHour && hour < _sendWindowEndHour;
        }



        // ─────────────────────────────────────────────
        // MAIN SEQUENCE PROCESSOR
        // ─────────────────────────────────────────────
        public async Task ProcessSequenceAsync(int campaignId)
        {
            // Include both Pending (newly enrolled) and Active leads so that
            // leads added at campaign creation actually get processed.
            var campaignLeads = await _context.RelayCampaignLeads
                .Where(x => x.RelayCampaignId == campaignId &&
                            (x.Status == CampaignLeadStatus.Pending ||
                             x.Status == CampaignLeadStatus.Active) &&
                            !x.Replied &&
                            !x.Unsubscribed &&
                            !x.Completed)
                .ToListAsync();


            foreach (var lead in campaignLeads)
            {
                if (await IsSequenceCompletedAsync(lead.Id))
                    continue;

                await ScheduleNextStepAsync(lead.Id);
            }
        }

        // ─────────────────────────────────────────────
        // SCHEDULE NEXT STEP (CORE ENGINE)
        // ─────────────────────────────────────────────
        public async Task ScheduleNextStepAsync(int campaignLeadId)
        {
            var campaignLead = await _context.RelayCampaignLeads
                .Include(x => x.RelayCampaign)
                .FirstOrDefaultAsync(x => x.Id == campaignLeadId);

            if (campaignLead == null)
                return;

            var campaign = await _context.RelayCampaigns
                .Include(x => x.Sequences)
                    .ThenInclude(s => s.Steps)
                        .ThenInclude(st => st.Variants)
                .FirstOrDefaultAsync(x => x.Id == campaignLead.RelayCampaignId);

            if (campaign == null)
                return;

            // ─────────────────────────────────────────────
            // CAMPAIGN GATE: only send while the campaign is switched on
            // and inside its scheduled [StartDate, EndDate] window.
            // ─────────────────────────────────────────────
            var now = DateTime.UtcNow;

            if (!campaign.IsActive)
                return;

            if (campaign.StartDate.HasValue && now < campaign.StartDate.Value)
                return; // campaign hasn't started yet

            if (campaign.EndDate.HasValue && now > campaign.EndDate.Value)
                return; // campaign window has closed

            // Respect the configured sending window (UTC hours / weekends).
            if (!IsWithinSendingWindow(now))
            {
                campaignLead.Status = campaignLead.CurrentSequenceStepId == null
                    ? CampaignLeadStatus.Pending
                    : CampaignLeadStatus.Active;
                await _context.SaveChangesAsync();
                return;
            }

            var sequence = campaign.Sequences.FirstOrDefault();


            if (sequence == null)
                return;

            // ─────────────────────────────────────────────
            // FIND STEP TO EXECUTE
            //  - First run (CurrentSequenceStepId == null): execute the FIRST step.
            //  - Otherwise: advance to the step AFTER the current one.
            //  Only ACTIVE steps participate in the cadence.
            // ─────────────────────────────────────────────
            var orderedSteps = sequence.Steps
                .Where(x => x.IsActive)
                .OrderBy(x => x.StepOrder)
                .ToList();


            if (!orderedSteps.Any())
            {
                campaignLead.Status = CampaignLeadStatus.Completed;
                campaignLead.Completed = true;
                await _context.SaveChangesAsync();
                return;
            }

            RelaySequenceStep stepToExecute;
            bool isFirstTouch;

            if (campaignLead.CurrentSequenceStepId == null)
            {
                // First touch — send the first step.
                stepToExecute = orderedSteps.First();
                isFirstTouch = true;
            }
            else
            {
                var currentIndex = orderedSteps.FindIndex(x => x.Id == campaignLead.CurrentSequenceStepId);
                var nextIndex = currentIndex + 1;

                if (currentIndex < 0 || nextIndex >= orderedSteps.Count)
                {
                    // No more steps — sequence finished for this lead.
                    campaignLead.Status = CampaignLeadStatus.Completed;
                    campaignLead.Completed = true;
                    await _context.SaveChangesAsync();
                    return;
                }

                stepToExecute = orderedSteps[nextIndex];
                isFirstTouch = false;
            }

            // ─────────────────────────────────────────────
            // CADENCE / DELAY ENFORCEMENT (DelayDays)
            //  The reference point we measure the wait from is:
            //   - First touch : when the lead was enrolled (CreatedAt)
            //   - Follow-ups  : the SentAt of the most recent email for this lead
            //  We only send once now >= reference + DelayDays. Otherwise we leave
            //  the lead Active and let a later scheduler tick pick it up — this is
            //  what makes the sequence respect its day-by-day cadence instead of
            //  firing every step on every minute the runner executes.
            // ─────────────────────────────────────────────
            DateTime referenceTime;

            if (isFirstTouch)
            {
                referenceTime = campaignLead.CreatedAt;
            }
            else
            {
                var lastActivity = await _context.RelayEmailActivities
                    .Where(a => a.RelayCampaignId == campaign.Id &&
                                a.RelayLeadId == campaignLead.RelayLeadId)
                    .OrderByDescending(a => a.SentAt)
                    .FirstOrDefaultAsync();

                referenceTime = lastActivity?.SentAt ?? campaignLead.CreatedAt;
            }

            var dueAt = referenceTime.AddDays(stepToExecute.DelayDays);

            if (now < dueAt)
            {
                // Not yet due — keep the lead Active and retry on a later tick.
                campaignLead.Status = CampaignLeadStatus.Active;
                await _context.SaveChangesAsync();
                return;
            }

            // ─────────────────────────────────────────────
            // CAMPAIGN DAILY SEND LIMIT
            //  Cap total sends per UTC day for the whole campaign (0 = unlimited).
            //  This complements the per-inbox daily limit enforced in rotation.
            // ─────────────────────────────────────────────
            if (campaign.DailySendLimit > 0)
            {
                var startOfDayUtc = now.Date;

                var sentTodayForCampaign = await _context.RelayEmailActivities
                    .CountAsync(a => a.RelayCampaignId == campaign.Id &&
                                     a.SentAt >= startOfDayUtc);

                if (sentTodayForCampaign >= campaign.DailySendLimit)
                {
                    // Campaign hit its daily cap — defer to a later tick/day.
                    campaignLead.Status = CampaignLeadStatus.Active;
                    await _context.SaveChangesAsync();
                    return;
                }
            }


            // ─────────────────────────────────────────────
            // INBOX ROTATION
            // ─────────────────────────────────────────────
            var inbox = await _inboxRotation.GetNextAvailableInboxAsync(campaign.Id);


            if (inbox == null)
            {
                // No available inbox (all disabled or at limit) — leave lead as pending
                campaignLead.Status = CampaignLeadStatus.Pending;
                await _context.SaveChangesAsync();
                return;
            }

            // ─────────────────────────────────────────────
            // JITTER: optionally defer a due lead to a later tick so we don't
            // blast every due lead in the same minute (better deliverability).
            // ─────────────────────────────────────────────
            if (_jitterSkipPercent > 0 && _random.Next(100) < _jitterSkipPercent)
            {
                campaignLead.Status = CampaignLeadStatus.Active;
                await _context.SaveChangesAsync();
                return;
            }

            // ─────────────────────────────────────────────
            // A/B VARIANT SELECTION (weighted, via the A/B testing service)
            //  Falls back to a random variant if the service can't resolve one.
            // ─────────────────────────────────────────────
            RelayEmailVariant variant;
            try
            {
                variant = await _abTesting.SelectVariantAsync(stepToExecute.Id);
            }
            catch
            {
                variant = stepToExecute.Variants
                    .OrderBy(v => Guid.NewGuid())
                    .FirstOrDefault();
            }

            if (variant == null)
                return;


            // ─────────────────────────────────────────────
            // SEND THE EMAIL (real Gmail send + activity logging)
            //  - Delegates to the sender service which performs the actual
            //    Gmail send, OAuth token refresh and writes the
            //    RelayEmailActivity record (with the real message/thread ids).
            //  - If the send fails (token/Gmail error), the lead is left
            //    Pending so it is retried on the next scheduler tick instead
            //    of being incorrectly marked as Active/Sent.
            // ─────────────────────────────────────────────
            try
            {
                await _emailSender.SendInitialEmailAsync(
                    inbox.Id,
                    campaign.Id,
                    campaignLead.RelayLeadId,
                    variant);
            }
            catch
            {
                campaignLead.Status = CampaignLeadStatus.Pending;
                await _context.SaveChangesAsync();
                return;
            }

            // ─────────────────────────────────────────────
            // UPDATE LEAD STATE (only after a successful send)
            // ─────────────────────────────────────────────
            campaignLead.CurrentSequenceStepId = stepToExecute.Id;
            campaignLead.Status = CampaignLeadStatus.Active;

            // If this was the final step, mark the lead as completed.
            if (orderedSteps.Last().Id == stepToExecute.Id)
            {
                campaignLead.Status = CampaignLeadStatus.Completed;
                campaignLead.Completed = true;
            }

            await _inboxRotation.IncrementSentCountAsync(inbox.Id);

            await _context.SaveChangesAsync();
        }


        // ─────────────────────────────────────────────
        // MOVE LEAD FORWARD (SAFE VERSION)
        // ─────────────────────────────────────────────
        public async Task MoveLeadToNextStepAsync(int campaignLeadId)
        {
            var lead = await _context.RelayCampaignLeads
                .FirstOrDefaultAsync(x => x.Id == campaignLeadId);

            if (lead == null)
                return;

            // progression handled in scheduler already
            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────
        // CHECK IF COMPLETED
        // ─────────────────────────────────────────────
        public async Task<bool> IsSequenceCompletedAsync(int campaignLeadId)
        {
            var lead = await _context.RelayCampaignLeads
                .FirstOrDefaultAsync(x => x.Id == campaignLeadId);

            if (lead == null)
                return true;

            if (lead.Status == CampaignLeadStatus.Completed)
                return true;

            var campaign = await _context.RelayCampaigns
                .Include(x => x.Sequences)
                    .ThenInclude(s => s.Steps)
                .FirstOrDefaultAsync(x => x.Id == lead.RelayCampaignId);

            if (campaign == null)
                return true;

            var sequence = campaign.Sequences.FirstOrDefault();

            if (sequence == null)
                return true;

            var orderedSteps = sequence.Steps
                .OrderBy(x => x.StepOrder)
                .ToList();

            if (!orderedSteps.Any())
                return true;

            var lastStep = orderedSteps.Last();

            return lead.CurrentSequenceStepId == lastStep.Id;
        }
    }
}