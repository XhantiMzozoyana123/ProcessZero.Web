using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Interfaces
{
    public interface ILeadLakeService
    {
        Task<List<LeadLake>> GetLeadLakesAsync();

        Task<LeadLake> GetLeadLakeByIdAsync(int id);

        // Adds multiple LeadLake entries in a single batch
        Task AddBatchLeadLakesAsync(List<LeadLake> leadLakes);

        // Admin CRUD operations
        Task AddLeadLakeAsync(LeadLake leadLake);

        Task UpdateLeadLakeAsync(LeadLake leadLake);

        Task DeleteLeadLakeAsync(int id);
    }

}
