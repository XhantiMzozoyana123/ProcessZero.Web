using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface IRelayCampaignService
    {
        Task<int> CreateCampaignAsync(RelayCampaign campaign);

        /// <summary>
        /// Creates a complete SmartLead-style campaign in a single transaction:
        /// the campaign itself plus its sequences, steps, A/B email variants,
        /// linked inboxes, and (optionally) leads.
        /// </summary>
        Task<int> CreateFullCampaignAsync(CreateCampaignRequest request);

        Task<RelayCampaign?> GetCampaignAsync(int campaignId);
        Task<List<RelayCampaign>> GetActiveCampaignsAsync();
        Task UpdateCampaignAsync(RelayCampaign campaign);
        Task ActivateCampaignAsync(int campaignId);
        Task PauseCampaignAsync(int campaignId);
        Task DeleteCampaignAsync(int campaignId);
    }
}
