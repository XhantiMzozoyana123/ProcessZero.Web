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
    public class RelayCampaignService : IRelayCampaignService
    {
        private readonly ApplicationDbContext _context;

        public RelayCampaignService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────
        // CREATE CAMPAIGN
        // ─────────────────────────────────────────────
        public async Task<int> CreateCampaignAsync(RelayCampaign campaign)
        {
            if (string.IsNullOrWhiteSpace(campaign.Name))
                throw new ArgumentException("Campaign name is required");

            campaign.IsActive = false; // safety default

            _context.RelayCampaigns.Add(campaign);
            await _context.SaveChangesAsync();

            return campaign.Id;
        }

        // ─────────────────────────────────────────────
        // CREATE FULL CAMPAIGN (SmartLead-style, one shot)
        // Builds: campaign -> sequences -> steps -> A/B variants,
        // links inboxes, and adds leads — all in one transaction.
        // ─────────────────────────────────────────────
        public async Task<int> CreateFullCampaignAsync(CreateCampaignRequest request)
        {
            if (request == null)
                throw new ArgumentException("Request is required");

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Campaign name is required");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1) Build the campaign aggregate root
                var campaign = new RelayCampaign
                {
                    Name = request.Name,
                    Description = request.Description ?? string.Empty,
                    DailySendLimit = request.DailySendLimit > 0 ? request.DailySendLimit : 50,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    IsActive = false // safety default; activate explicitly
                };

                // 2) Build sequences -> steps -> A/B variants
                if (request.Sequences != null)
                {
                    foreach (var seqReq in request.Sequences)
                    {
                        var sequence = new RelaySequence
                        {
                            Name = string.IsNullOrWhiteSpace(seqReq.Name) ? "Sequence" : seqReq.Name,
                            MessageRotationEnabled = seqReq.MessageRotationEnabled,
                            InboxRotationEnabled = seqReq.InboxRotationEnabled
                        };

                        if (seqReq.Steps != null)
                        {
                            foreach (var stepReq in seqReq.Steps)
                            {
                                var step = new RelaySequenceStep
                                {
                                    Name = string.IsNullOrWhiteSpace(stepReq.Name) ? "Step" : stepReq.Name,
                                    StepOrder = stepReq.StepOrder,
                                    DelayDays = stepReq.DelayDays,
                                    IsActive = stepReq.IsActive
                                };

                                if (stepReq.Variants != null)
                                {
                                    foreach (var variantReq in stepReq.Variants)
                                    {
                                        step.Variants.Add(new RelayEmailVariant
                                        {
                                            VariantName = string.IsNullOrWhiteSpace(variantReq.VariantName) ? "A" : variantReq.VariantName,
                                            Subject = variantReq.Subject ?? string.Empty,
                                            HtmlBody = variantReq.HtmlBody ?? string.Empty,
                                            Weight = variantReq.Weight > 0 ? variantReq.Weight : 50
                                        });
                                    }
                                }

                                sequence.Steps.Add(step);
                            }
                        }

                        campaign.Sequences.Add(sequence);
                    }
                }

                // 3) Link inboxes (RelayEmailAccount ids)
                if (request.InboxAccountIds != null && request.InboxAccountIds.Any())
                {
                    var validInboxIds = await _context.RelayEmailAccounts
                        .Where(x => request.InboxAccountIds.Contains(x.Id))
                        .Select(x => x.Id)
                        .ToListAsync();

                    foreach (var inboxId in validInboxIds.Distinct())
                    {
                        campaign.Inboxes.Add(new RelayCampaignInbox
                        {
                            RelayInboxId = inboxId
                        });
                    }
                }

                // EF inserts the whole graph (campaign + sequences + steps + variants + inboxes)
                _context.RelayCampaigns.Add(campaign);
                await _context.SaveChangesAsync();

                // 4) Add leads (junction records) — only leads that actually exist
                if (request.LeadIds != null && request.LeadIds.Any())
                {
                    var validLeadIds = await _context.RelayLeads
                        .Where(x => request.LeadIds.Contains(x.Id))
                        .Select(x => x.Id)
                        .ToListAsync();

                    foreach (var leadId in validLeadIds.Distinct())
                    {
                        _context.RelayCampaignLeads.Add(new RelayCampaignLead
                        {
                            RelayCampaignId = campaign.Id,
                            RelayLeadId = leadId,
                            Status = CampaignLeadStatus.Pending,
                            CurrentSequenceStepId = null,
                            Replied = false,
                            Unsubscribed = false,
                            Completed = false
                        });
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return campaign.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // ─────────────────────────────────────────────
        // GET SINGLE CAMPAIGN
        // ─────────────────────────────────────────────
        public async Task<RelayCampaign?> GetCampaignAsync(int campaignId)
        {
            return await _context.RelayCampaigns
                .Include(x => x.Sequences)
                .Include(x => x.Inboxes)
                .Include(x => x.Leads)
                .FirstOrDefaultAsync(x => x.Id == campaignId);
        }

        // ─────────────────────────────────────────────
        // GET ACTIVE CAMPAIGNS
        // ─────────────────────────────────────────────
        public async Task<List<RelayCampaign>> GetActiveCampaignsAsync()
        {
            return await _context.RelayCampaigns
                .Where(x => x.IsActive)
                .Include(x => x.Sequences)
                .Include(x => x.Inboxes)
                .ToListAsync();
        }

        // ─────────────────────────────────────────────
        // ACTIVATE CAMPAIGN
        // ─────────────────────────────────────────────
        public async Task ActivateCampaignAsync(int campaignId)
        {
            var campaign = await _context.RelayCampaigns
                .FirstOrDefaultAsync(x => x.Id == campaignId);

            if (campaign == null)
                throw new Exception("Campaign not found");

            if (!campaign.Sequences.Any())
                throw new Exception("Cannot activate campaign without sequences");

            if (!campaign.Inboxes.Any())
                throw new Exception("Cannot activate campaign without inboxes");

            campaign.IsActive = true;

            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────
        // UPDATE CAMPAIGN
        // ─────────────────────────────────────────────
        public async Task UpdateCampaignAsync(RelayCampaign campaign)
        {
            if (campaign == null)
                throw new ArgumentException("Campaign is required");

            var existing = await _context.RelayCampaigns
                .FirstOrDefaultAsync(x => x.Id == campaign.Id);

            if (existing == null)
                throw new Exception("Campaign not found");

            existing.Name = campaign.Name;
            existing.Description = campaign.Description;
            existing.UpdatedAt = DateTime.UtcNow;

            _context.Update(existing);
            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────
        // PAUSE CAMPAIGN
        // ─────────────────────────────────────────────
        public async Task PauseCampaignAsync(int campaignId)
        {
            var campaign = await _context.RelayCampaigns
                .FirstOrDefaultAsync(x => x.Id == campaignId);

            if (campaign == null)
                throw new Exception("Campaign not found");

            campaign.IsActive = false;

            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────
        // DELETE CAMPAIGN
        // ─────────────────────────────────────────────
        public async Task DeleteCampaignAsync(int campaignId)
        {
            var campaign = await _context.RelayCampaigns
                .FirstOrDefaultAsync(x => x.Id == campaignId);

            if (campaign == null)
                throw new Exception("Campaign not found");

            _context.RelayCampaigns.Remove(campaign);
            await _context.SaveChangesAsync();
        }
    }
}
