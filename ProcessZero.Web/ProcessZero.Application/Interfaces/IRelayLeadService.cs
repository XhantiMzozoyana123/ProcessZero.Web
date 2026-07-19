using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface IRelayLeadService
    {
        Task AddLeadToCampaignAsync(int campaignId, int leadId);

        Task AddLeadsToCampaignAsync(int campaignId, IEnumerable<int> leadIds);

        Task RemoveLeadFromCampaignAsync(int campaignId, int leadId);

        Task RemoveLeadsFromCampaignAsync(int campaignId, IEnumerable<int> leadIds);

        Task<RelayCampaignLead?> GetLeadStateAsync(int campaignId, int leadId);

        Task UpdateLeadStatusAsync(int campaignLeadId, CampaignLeadStatus status);

        Task UpdateLeadInCampaignAsync(int campaignId, int leadId, int? currentSequenceStepId = null, CampaignLeadStatus? status = null, bool? replied = null, bool? unsubscribed = null, bool? completed = null);

        Task UpdateLeadsInCampaignAsync(int campaignId, IEnumerable<RelayLeadUpdate> updates);

        Task<BatchResultDto> ProcessBatchAsync(int campaignId, BatchLeadModificationDto request);
    }

public record RelayLeadUpdate(int LeadId, int? CurrentSequenceStepId = null, CampaignLeadStatus? Status = null, bool? Replied = null, bool? Unsubscribed = null, bool? Completed = null);
}
