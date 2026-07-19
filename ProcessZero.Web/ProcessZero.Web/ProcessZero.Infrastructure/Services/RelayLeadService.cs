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
    public class RelayLeadService : IRelayLeadService
    {
        private readonly ApplicationDbContext _context;

        public RelayLeadService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────
        // BATCH PROCESS (Add/Remove/Update in single transaction)
        // ─────────────────────────────────────────────
        public async Task<ProcessZero.Application.Interfaces.BatchResultDto> ProcessBatchAsync(int campaignId, ProcessZero.Application.Interfaces.BatchLeadModificationDto request)
        {
            if (request == null)
                return new ProcessZero.Application.Interfaces.BatchResultDto(0, 0, 0);

            using var txn = await _context.Database.BeginTransactionAsync();
            try
            {
                var added = 0;
                var removed = 0;
                var updated = 0;

                if (request.Add != null && request.Add.Any())
                {
                    await AddLeadsToCampaignAsync(campaignId, request.Add);
                    added = request.Add.Distinct().Count();
                }

                if (request.Remove != null && request.Remove.Any())
                {
                    await RemoveLeadsFromCampaignAsync(campaignId, request.Remove);
                    removed = request.Remove.Distinct().Count();
                }

                if (request.Edit != null && request.Edit.Any())
                {
                    var updates = request.Edit.Select(e => new ProcessZero.Application.Interfaces.RelayLeadUpdate(e.LeadId, e.CurrentSequenceStepId, e.Status, e.Replied, e.Unsubscribed, e.Completed));
                    await UpdateLeadsInCampaignAsync(campaignId, updates);
                    updated = request.Edit.Count;
                }

                await txn.CommitAsync();
                return new ProcessZero.Application.Interfaces.BatchResultDto(added, removed, updated);
            }
            catch
            {
                await txn.RollbackAsync();
                throw;
            }
        }

        // Update lead fields in campaign (partial update semantics)
        public async Task UpdateLeadInCampaignAsync(int campaignId, int leadId, int? currentSequenceStepId = null, CampaignLeadStatus? status = null, bool? replied = null, bool? unsubscribed = null, bool? completed = null)
        {
            var lead = await _context.Set<RelayCampaignLead>()
                .FirstOrDefaultAsync(x => x.RelayCampaignId == campaignId && x.RelayLeadId == leadId);

            if (lead == null)
                throw new Exception("Campaign lead not found");

            if (currentSequenceStepId.HasValue)
                lead.CurrentSequenceStepId = currentSequenceStepId;

            if (status.HasValue)
                lead.Status = status.Value;

            if (replied.HasValue)
                lead.Replied = replied.Value;

            if (unsubscribed.HasValue)
                lead.Unsubscribed = unsubscribed.Value;

            if (completed.HasValue)
                lead.Completed = completed.Value;

            lead.UpdatedAt = DateTime.UtcNow;

            _context.Update(lead);
            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────
        // ADD LEAD TO CAMPAIGN
        // ─────────────────────────────────────────────
        public async Task AddLeadToCampaignAsync(int campaignId, int leadId)
        {
            var existing = await _context.Set<RelayCampaignLead>()
                .FirstOrDefaultAsync(x =>
                    x.RelayCampaignId == campaignId &&
                    x.RelayLeadId == leadId);

            if (existing != null)
                return; // already exists, avoid duplicates

            var campaignLead = new RelayCampaignLead
            {
                RelayCampaignId = campaignId,
                RelayLeadId = leadId,

                CurrentSequenceStepId = null, // start before sequence begins

                Status = CampaignLeadStatus.Pending,

                Replied = false,
                Unsubscribed = false,
                Completed = false,

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Add(campaignLead);
            await _context.SaveChangesAsync();
        }

        public async Task AddLeadsToCampaignAsync(int campaignId, IEnumerable<int> leadIds)
        {
            var distinct = leadIds?.Distinct().ToList() ?? new List<int>();
            if (!distinct.Any())
                return;

            var existing = await _context.Set<RelayCampaignLead>()
                .Where(x => x.RelayCampaignId == campaignId && distinct.Contains(x.RelayLeadId))
                .Select(x => x.RelayLeadId)
                .ToListAsync();

            var toAdd = distinct.Except(existing).ToList();
            if (!toAdd.Any())
                return;

            var now = DateTime.UtcNow;
            var entities = toAdd.Select(id => new RelayCampaignLead
            {
                RelayCampaignId = campaignId,
                RelayLeadId = id,
                CurrentSequenceStepId = null,
                Status = CampaignLeadStatus.Pending,
                Replied = false,
                Unsubscribed = false,
                Completed = false,
                CreatedAt = now,
                UpdatedAt = now
            });

            await _context.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────
        // REMOVE LEAD FROM CAMPAIGN
        // ─────────────────────────────────────────────
        public async Task RemoveLeadFromCampaignAsync(int campaignId, int leadId)
        {
            var campaignLead = await _context.Set<RelayCampaignLead>()
                .FirstOrDefaultAsync(x =>
                    x.RelayCampaignId == campaignId &&
                    x.RelayLeadId == leadId);

            if (campaignLead == null)
                return;

            _context.Remove(campaignLead);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveLeadsFromCampaignAsync(int campaignId, IEnumerable<int> leadIds)
        {
            var distinct = leadIds?.Distinct().ToList() ?? new List<int>();
            if (!distinct.Any())
                return;

            var items = await _context.Set<RelayCampaignLead>()
                .Where(x => x.RelayCampaignId == campaignId && distinct.Contains(x.RelayLeadId))
                .ToListAsync();

            if (!items.Any())
                return;

            _context.RemoveRange(items);
            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────
        // GET LEAD STATE IN CAMPAIGN
        // ─────────────────────────────────────────────
        public async Task<RelayCampaignLead?> GetLeadStateAsync(int campaignId, int leadId)
        {
            return await _context.Set<RelayCampaignLead>()
                .FirstOrDefaultAsync(x =>
                    x.RelayCampaignId == campaignId &&
                    x.RelayLeadId == leadId);
        }

        // ─────────────────────────────────────────────
        // UPDATE LEAD STATUS
        // ─────────────────────────────────────────────
        public async Task UpdateLeadStatusAsync(int campaignLeadId, CampaignLeadStatus status)
        {
            var lead = await _context.Set<RelayCampaignLead>()
                .FirstOrDefaultAsync(x => x.Id == campaignLeadId);

            if (lead == null)
                throw new Exception("Campaign lead not found");

            switch (status)
            {
                case CampaignLeadStatus.Replied:
                    lead.Replied = true;
                    break;

                case CampaignLeadStatus.Unsubscribed:
                    lead.Unsubscribed = true;
                    break;

                case CampaignLeadStatus.Completed:
                    lead.Completed = true;
                    break;
            }

            lead.UpdatedAt = DateTime.UtcNow;

            _context.Update(lead);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateLeadsInCampaignAsync(int campaignId, IEnumerable<RelayLeadUpdate> updates)
        {
            var list = updates?.ToList() ?? new List<RelayLeadUpdate>();
            if (!list.Any())
                return;

            var leadIds = list.Select(x => x.LeadId).Distinct().ToList();

            var existing = await _context.Set<RelayCampaignLead>()
                .Where(x => x.RelayCampaignId == campaignId && leadIds.Contains(x.RelayLeadId))
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var upd in list)
            {
                var lead = existing.FirstOrDefault(x => x.RelayLeadId == upd.LeadId);
                if (lead == null)
                    continue; // skip updates for non-existing leads

                if (upd.CurrentSequenceStepId.HasValue)
                    lead.CurrentSequenceStepId = upd.CurrentSequenceStepId;

                if (upd.Status.HasValue)
                    lead.Status = upd.Status.Value;

                if (upd.Replied.HasValue)
                    lead.Replied = upd.Replied.Value;

                if (upd.Unsubscribed.HasValue)
                    lead.Unsubscribed = upd.Unsubscribed.Value;

                if (upd.Completed.HasValue)
                    lead.Completed = upd.Completed.Value;

                lead.UpdatedAt = now;
            }

            _context.UpdateRange(existing);
            await _context.SaveChangesAsync();
        }
            }
        }
