using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Interfaces
{
    public interface IInboxService
    {
        Task<int> CreateInboxAsync(Inbox inbox, CancellationToken cancellationToken = default);

        Task UpdateInboxAsync(Inbox inbox, CancellationToken cancellationToken = default);

        Task DeleteInboxAsync(int id, CancellationToken cancellationToken = default);

        Task<Inbox?> GetInboxByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<List<Inbox>> GetInboxesByUserIdAsync(string userId, CancellationToken cancellationToken = default);

        Task<List<Inbox>> GetAllInboxesAsync(CancellationToken cancellationToken = default);
    }
}
