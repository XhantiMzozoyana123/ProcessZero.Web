using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Infrastructure.Services
{
    public class BankAccountService : IBankAccountService
    {
        private readonly ApplicationDbContext _context;

        public BankAccountService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateAsync(BankAccount bankAccount, CancellationToken ct = default)
        {
            await _context.BankAccounts.AddAsync(bankAccount, ct);
            await _context.SaveChangesAsync(ct);

            return bankAccount.Id;
        }

        public async Task UpdateAsync(BankAccount bankAccount, CancellationToken ct = default)
        {
            _context.BankAccounts.Update(bankAccount);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<BankAccount?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        {
            return await _context.BankAccounts
                .FirstOrDefaultAsync(x => x.UserId == userId, ct);
        }
    }
}
