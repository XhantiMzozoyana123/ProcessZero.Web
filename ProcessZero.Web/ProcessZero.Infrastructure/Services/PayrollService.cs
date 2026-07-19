using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Infrastructure.Services
{
    public class PayrollService : IPayrollService
    {
        private class ClientProduct
        {
            public Contact Lead { get; set; }
            public Invoice Invoice { get; set; }
            public Product Product { get; set; }
        }

        private readonly ApplicationDbContext _context;
        private readonly IKpiService _kpiService;
        private readonly Microsoft.Extensions.Logging.ILogger<PayrollService> _logger;
        private readonly decimal _commissionRate;

        public PayrollService(
            IKpiService kPIService,
            ApplicationDbContext context,
            Microsoft.Extensions.Logging.ILogger<PayrollService> logger,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _kpiService = kPIService ?? throw new ArgumentNullException(nameof(kPIService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Read commission rate from configuration (Payroll:CommissionRate). Fallback to 0.30m.
            decimal rate = 0.30m;
            try
            {
                var raw = configuration["Payroll:CommissionRate"];
                if (!string.IsNullOrWhiteSpace(raw) && decimal.TryParse(raw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                {
                    rate = parsed;
                }
            }
            catch
            {
                // use default
            }

            // Clamp to sensible range 0..1
            if (rate < 0) rate = 0;
            if (rate > 1) rate = 1;
            _commissionRate = rate;
        }

        public async Task<PayrollReportResult> GenerateMonthlyCommissionsReportAsync()
        {
            // Single-pass approach:
            // 1) Find all contacts (leads) that are Active/Converted and have at least one invoice.
            // 2) Aggregate distinct lead closed amounts per user (sum of unique contact.ClosedAmount per user).
            // 3) Fetch bank accounts and user records for those users in bulk.
            // 4) Create payouts in a single AddRange + SaveChanges.

            var result = new PayrollReportResult();
            var csvRows = new List<string[]>();
            csvRows.Add(new[] { "UserId", "UserName", "AccountNumber", "BankName", "Amount" });

            // 1) Contacts that are Active or Converted and have at least one invoice
            var leadsWithInvoices = await _context.Contacts
                .AsNoTracking()
                .Where(c => (c.Status == ContactStatus.Active || c.Status == ContactStatus.Converted)
                            && _context.Invoices.Any(i => i.ClientId == c.Id))
                .Select(c => new { c.UserId, ContactId = c.Id, c.ClosedAmount })
                .ToListAsync();

            if (!leadsWithInvoices.Any())
            {
                result.Rows = csvRows;
                result.AdminEmails = new List<string>();
                return result;
            }

            // Group distinct leads per user and sum ClosedAmount
            var perUserTotals = leadsWithInvoices
                .GroupBy(x => x.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalClosed = g
                        .GroupBy(x => x.ContactId)
                        .Select(sg => sg.First().ClosedAmount)
                        .Sum(),
                    ContactIds = g.Select(x => x.ContactId).Distinct().ToList()
                })
                .ToList();

            var userIds = perUserTotals.Select(p => p.UserId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();

            // Bulk fetch related data
            var users = await _context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var bankAccounts = await _context.BankAccounts
                .AsNoTracking()
                .Where(b => userIds.Contains(b.UserId))
                .ToDictionaryAsync(b => b.UserId);

            // Fetch invoices for contacts to pick a product for KPI (pick first available product per user)
            var allContactIds = perUserTotals.SelectMany(p => p.ContactIds).Distinct().ToList();
            var invoicesForContacts = await _context.Invoices
                .AsNoTracking()
                .Where(i => allContactIds.Contains(i.ClientId))
                .Select(i => new { i.ClientId, i.ProductId })
                .ToListAsync();

            var contactToProduct = invoicesForContacts
                .GroupBy(i => i.ClientId)
                .ToDictionary(g => g.Key, g => g.First().ProductId);

            var payouts = new List<Payout>();

            foreach (var pu in perUserTotals)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(pu.UserId)) continue;

                    if (!users.TryGetValue(pu.UserId, out var user))
                    {
                        _logger.LogDebug("Payroll: skipping missing user {UserId}", pu.UserId);
                        continue;
                    }

                    if (!bankAccounts.TryGetValue(pu.UserId, out var bankAccount))
                    {
                        _logger.LogDebug("Payroll: user {UserId} ({UserName}) - no bank account, skipping payout.", user.Id, user.UserName);
                        continue;
                    }

                    var totalCommission = pu.TotalClosed * _commissionRate;
                    if (totalCommission <= 0) continue;

                    var payout = new Payout
                    {
                        UserId = user.Id,
                        BankAccountId = bankAccount.Id,
                        Amount = totalCommission,
                        Month = DateTime.UtcNow.Month,
                        Year = DateTime.UtcNow.Year,
                        Notes = $"Process Zero commission for {DateTime.UtcNow:MMMM yyyy}",
                        IsPaid = false
                    };

                    payouts.Add(payout);

                    csvRows.Add(new string[]
                    {
                        user.Id,
                        user.UserName ?? string.Empty,
                        bankAccount.AccountNumber ?? string.Empty,
                        bankAccount.BankName ?? string.Empty,
                        totalCommission.ToString("F2")
                    });

                    result.UserNotifications.Add(new PayrollUserNotificationDto
                    {
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        Amount = totalCommission
                    });

                    // compute first product id for KPI
                    int? firstProductId = null;
                    foreach (var cid in pu.ContactIds)
                    {
                        if (contactToProduct.TryGetValue(cid, out var pid))
                        {
                            firstProductId = pid;
                            break;
                        }
                    }

                    // compute retention using pre-aggregated counts later if needed
                    double clientRetention = 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Payroll: error while preparing payout for user {UserId}", pu.UserId);
                    // swallow and continue
                }
            }

            if (payouts.Any())
            {
                await _context.Payouts.AddRangeAsync(payouts);
                await _context.SaveChangesAsync();

                foreach (var p in payouts)
                {
                    _logger.LogInformation("Payroll: created payout for user {UserId} amount {Amount}", p.UserId, p.Amount);
                }
            }

            result.Rows = csvRows;
            result.AdminEmails = new List<string>();
            return result;
        }

        private async Task<double> CalculateClientRetentionAsync(string userId)
        {
            var totalClients = await _context.Contacts
                .Where(c => c.UserId == userId && c.Status == ContactStatus.Converted)
                .CountAsync();

            if (totalClients == 0) return 0;

            var activeClients = await _context.Contacts
                .Where(c => c.UserId == userId && c.Status == ContactStatus.Active)
                .CountAsync();

            return (double)activeClients / totalClients;
        }
    }
}
