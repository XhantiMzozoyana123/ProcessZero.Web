using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Interfaces
{
    public interface IContactService
    {
        Task<List<Contact>> GetAllContactsByUserIdAsync(string userId);

        Task<List<Contact>> GetAllContactTypesAsync(string type);

        Task<Contact> GetContactByIdAsync(string id);

        Task AddContactAsync(LeadLake leadLake);

        Task AddBatchContactAsync(List<LeadLake> leadLakes);

        Task DeleteContactAsync(string id);

        Task UpdateContactAsync(Contact contact);
    }
}
