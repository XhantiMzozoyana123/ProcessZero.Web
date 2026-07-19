using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface IRelayA_BTestingService
    {
        Task<RelayEmailVariant> SelectVariantAsync(int sequenceStepId);

        Task RecordVariantResultAsync(int variantId, bool wasSuccessful);

        Task<RelayEmailVariant> GetBestPerformingVariantAsync(int stepId);
    }
}
