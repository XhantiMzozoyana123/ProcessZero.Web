using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface IRelayEmailSenderService
    {
        /// <summary>
        /// Sends the first email in a campaign sequence to a lead.
        /// </summary>
        Task<RelayEmailActivity> SendInitialEmailAsync(
            int inboxId,
            int campaignId,
            int leadId,
            RelayEmailVariant variant);

        /// <summary>
        /// Sends a follow-up email inside an existing Gmail thread.
        /// </summary>
        Task<RelayEmailActivity> SendFollowUpAsync(
            int inboxId,
            string threadId,
            RelayEmailVariant variant,
            int campaignId,
            int leadId);

        /// <summary>
        /// Resolves the current/next sequence step for a campaign lead,
        /// selects an inbox (rotation) and email variant (A/B), then sends.
        /// </summary>
        Task<RelayEmailActivity> SendSequenceEmailAsync(int campaignLeadId);

        /// <summary>
        /// Sends a simple test email from the specified inbox to a recipient.
        /// </summary>
        Task<RelayEmailActivity> SendTestEmailAsync(int inboxId, string to);
    }

}
