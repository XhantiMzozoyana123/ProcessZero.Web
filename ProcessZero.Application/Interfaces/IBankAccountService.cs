using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Interfaces
{
    public interface IBankAccountService
    {
        Task<int> CreateAsync(BankAccount bankAccount, CancellationToken ct = default);

        Task UpdateAsync(BankAccount bankAccount, CancellationToken ct = default);

        Task<BankAccount?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    }
}
