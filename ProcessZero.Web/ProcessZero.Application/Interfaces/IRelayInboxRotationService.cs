using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface IRelayInboxRotationService
    {
        Task<RelayEmailAccount?> GetNextAvailableInboxAsync(int campaignId);

        Task<bool> CanSendAsync(int inboxId);

        Task IncrementSentCountAsync(int inboxId);

        Task ResetDailyLimitsAsync();
    }
}
