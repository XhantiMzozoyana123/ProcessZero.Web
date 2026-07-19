using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Constants;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Infrastructure.Services
{
    public class KpiPolicyService : IKpiPolicyService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IBackgroundEmailService _backgroundEmailService;

        public KpiPolicyService(ApplicationDbContext context, IEmailService emailService, IBackgroundEmailService backgroundEmailService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _backgroundEmailService = backgroundEmailService ?? throw new ArgumentNullException(nameof(backgroundEmailService));
        }

        public async Task AddPolicyAsync(KpiPolicy policy)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            await NotifyStakeholdersOfPolicyCreationAsync(policy);

            policy.CreatedAt = DateTime.UtcNow;
            policy.UpdatedAt = DateTime.UtcNow;

            await _context.KpiPolicies.AddAsync(policy);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePolicyAsync(int id)
        {
            var existing = await _context.KpiPolicies.FindAsync(id);
            if (existing == null) return;

            await NotifyStakeholdersOfPolicyDeletionAsync(existing);

            _context.KpiPolicies.Remove(existing);
            await _context.SaveChangesAsync();
        }

        public async Task<List<KpiPolicy>> GetAllPoliciesAsync()
        {
            return await _context.KpiPolicies
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<KpiPolicy> GetPolicyByIdAsync(int id)
        {
            return await _context.KpiPolicies.FindAsync(id);
        }

        public async Task UpdatePolicyAsync(KpiPolicy policy)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            await NotifyStakeholdersOfPolicyChangeAsync(policy);

            var existing = await _context.KpiPolicies.FindAsync(policy.Id);
            if (existing == null) throw new InvalidOperationException($"KpiPolicy with id {policy.Id} not found");

            _context.Entry(existing).CurrentValues.SetValues(policy);
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private async Task NotifyStakeholdersOfPolicyChangeAsync(KpiPolicy policy)
        {
            // Load only the fields we need to avoid pulling large tracked entities (prevents N+1 elsewhere)
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserName, u.Email })
                .ToListAsync();

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == policy.ProductId);

            foreach (var user in users)
            {
                var notice = NoticeConstant.NotifyKpiPolicyUpdated(
                    user.UserName,
                    user.Email,
                    policy,
                    product
                );
                _backgroundEmailService.EnqueueEmail(notice);
            }
        }

        private async Task NotifyStakeholdersOfPolicyCreationAsync(KpiPolicy policy)
        {
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserName, u.Email })
                .ToListAsync();

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == policy.ProductId);

            foreach (var user in users)
            {
                var notice = NoticeConstant.NotifyKpiPolicyCreated(
                    user.UserName,
                    user.Email,
                    policy,
                    product
                );
                _backgroundEmailService.EnqueueEmail(notice);
            }
        }

        private async Task NotifyStakeholdersOfPolicyDeletionAsync(KpiPolicy policy)
        {
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserName, u.Email })
                .ToListAsync();

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == policy.ProductId);

            foreach (var user in users)
            {
                var notice = NoticeConstant.NotifyKpiPolicyDeleted(
                    user.UserName,
                    user.Email,
                    policy,
                    product
                );
                _backgroundEmailService.EnqueueEmail(notice);
            }
        }
    }
}
