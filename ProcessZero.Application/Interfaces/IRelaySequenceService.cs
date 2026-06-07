using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface IRelaySequenceService
    {
        Task ProcessSequenceAsync(int campaignId);

        Task ScheduleNextStepAsync(int campaignLeadId);

        Task MoveLeadToNextStepAsync(int campaignLeadId);

        Task<bool> IsSequenceCompletedAsync(int campaignLeadId);
    }
}
