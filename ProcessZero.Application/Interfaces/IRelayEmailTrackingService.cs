using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface IRelayEmailTrackingService
    {
        Task SyncRepliesAsync(int inboxId);

        Task<bool> MarkAsRepliedAsync(string gmailThreadId);

        Task<List<RelayEmailActivity>> GetActivityByLeadAsync(int leadId);

        Task<List<RelayEmailActivity>> GetActivityByCampaignAsync(int campaignId);
    }
}
