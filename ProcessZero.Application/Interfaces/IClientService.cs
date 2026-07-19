using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Interfaces
{
    public interface IClientService
    {
        Task<int> CreateContactAsync(Contact contact, CancellationToken cancellationToken = default);

        Task UpdateContactAsync(Contact contact, CancellationToken cancellationToken = default);

        Task DeleteContactAsync(int id, CancellationToken cancellationToken = default);

        Task<Contact?> GetContactByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<List<Contact>> GetAllContactsAsync(CancellationToken cancellationToken = default);

        // Paginated variant
        Task<List<Contact>> GetAllContactsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        Task<List<Contact>> GetContactsByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    }
}
