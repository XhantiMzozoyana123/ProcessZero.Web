using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Interfaces
{
    public interface IKpiPolicyService
    {
        Task AddPolicyAsync(KpiPolicy policy);

        Task<KpiPolicy> GetPolicyByIdAsync(int id);

        Task<List<KpiPolicy>> GetAllPoliciesAsync();

        Task UpdatePolicyAsync(KpiPolicy policy);

        Task DeletePolicyAsync(int id);
    }
}
