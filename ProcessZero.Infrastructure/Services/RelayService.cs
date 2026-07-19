using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    /// <summary>
    /// Service for managing relay email accounts and their sending operations.
    /// 
    /// Relay accounts are authenticated Gmail accounts that campaigns use to send emails.
    /// This service provides high-level operations for:
    /// - Finding healthy accounts for sending
    /// - Tracking send statistics per account
    /// - Enforcing daily send limits
    /// - Health monitoring and token validation
    /// - Account activation/deactivation
    /// </summary>
    public class RelayService : IRelayService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGoogleOAuthService _googleOAuth;

        /// <summary>
        /// Constructor for RelayService.
        /// Injects database context for account persistence and OAuth service for token management.
        /// </summary>
        /// <param name="context">Database context for relay account persistence</param>
        /// <param name="googleOAuth">Google OAuth service for token validation and refresh</param>
        public RelayService(
            ApplicationDbContext context,
            IGoogleOAuthService googleOAuth)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _googleOAuth = googleOAuth ?? throw new ArgumentNullException(nameof(googleOAuth));
        }

        /// <summary>
        /// Gets a specific relay account by ID with user authorization check.
        /// </summary>
        public async Task<RelayEmailAccount> GetAccountAsync(
            int accountId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (accountId <= 0)
                throw new ArgumentException("Account ID must be positive.", nameof(accountId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            var account = await _context.RelayEmailAccounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, cancellationToken);

            if (account == null)
                throw new KeyNotFoundException($"Relay account {accountId} not found or not owned by user.");

            return account;
        }

        /// <summary>
        /// Gets all relay accounts owned by a specific user.
        /// </summary>
        public async Task<List<RelayEmailAccount>> GetUserAccountsAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            return await _context.RelayEmailAccounts
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets all active (healthy) relay accounts owned by a user.
        /// </summary>
        public async Task<List<RelayEmailAccount>> GetHealthyAccountsAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            return await _context.RelayEmailAccounts
                .Where(a => a.UserId == userId &&
                            a.IsActive &&
                            a.HealthStatus == AccountHealthStatus.Healthy &&
                            a.SentToday < a.DailySendLimit)
                .OrderBy(a => a.LastUsedAt)  // Least recently used first (for load distribution)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets a single healthy relay account suitable for sending.
        /// </summary>
        public async Task<RelayEmailAccount?> GetHealthyAccountForSendingAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            return await _context.RelayEmailAccounts
                .Where(a => a.UserId == userId &&
                            a.IsActive &&
                            a.HealthStatus == AccountHealthStatus.Healthy &&
                            a.SentToday < a.DailySendLimit)
                .OrderBy(a => a.LastUsedAt)  // Least recently used first
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Checks if a relay account can send more emails today.
        /// </summary>
        public async Task<bool> CanSendAsync(
            RelayEmailAccount account,
            CancellationToken cancellationToken = default)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));

            // Check basic status
            if (!account.IsActive || account.HealthStatus != AccountHealthStatus.Healthy)
                return false;

            // Check daily limit
            if (account.SentToday >= account.DailySendLimit)
                return false;

            // Check token validity (this will auto-refresh if needed)
            try
            {
                await _googleOAuth.EnsureValidAccessTokenAsync(account, cancellationToken);
                return true;
            }
            catch
            {
                // Token refresh failed - account can't send
                return false;
            }
        }

        /// <summary>
        /// Records that an email was successfully sent from this account.
        /// </summary>
        public async Task<RelayEmailAccount> RecordSendAsync(
            RelayEmailAccount account,
            CancellationToken cancellationToken = default)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));

            // Update sending stats
            account.SentToday++;
            account.LastUsedAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;

            // Persist changes
            _context.RelayEmailAccounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            return account;
        }

        /// <summary>
        /// Records that an email send failed from this account.
        /// </summary>
        public async Task<RelayEmailAccount> RecordSendFailureAsync(
            RelayEmailAccount account,
            string errorMessage,
            CancellationToken cancellationToken = default)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));

            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message cannot be empty.", nameof(errorMessage));

            // Mark as critical and deactivate
            account.HealthStatus = AccountHealthStatus.Critical;
            account.HealthCheckError = errorMessage;
            account.IsActive = false;
            account.UpdatedAt = DateTime.UtcNow;

            // Persist changes
            _context.RelayEmailAccounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            return account;
        }

        /// <summary>
        /// Resets the daily send counter for all accounts.
        /// </summary>
        public async Task<int> ResetDailyCountersAsync(
            string? userId = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<RelayEmailAccount> query = _context.RelayEmailAccounts;

            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(a => a.UserId == userId);

            var accounts = await query.ToListAsync(cancellationToken);

            foreach (var account in accounts)
            {
                account.SentToday = 0;
                account.UpdatedAt = DateTime.UtcNow;
            }

            _context.RelayEmailAccounts.UpdateRange(accounts);
            await _context.SaveChangesAsync(cancellationToken);

            return accounts.Count;
        }

        /// <summary>
        /// Performs comprehensive health check on a relay account.
        /// </summary>
        public async Task<RelayEmailAccount> CheckAccountHealthAsync(
            int accountId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (accountId <= 0)
                throw new ArgumentException("Account ID must be positive.", nameof(accountId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            var account = await _context.RelayEmailAccounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, cancellationToken);

            if (account == null)
                throw new KeyNotFoundException($"Relay account {accountId} not found.");

            // Delegate to Google OAuth service for comprehensive check
            return await _googleOAuth.CheckAccountHealthAsync(accountId, userId, cancellationToken);
        }

        /// <summary>
        /// Deactivates (disables) a relay account.
        /// </summary>
        public async Task<RelayEmailAccount> DeactivateAccountAsync(
            int accountId,
            string userId,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            if (accountId <= 0)
                throw new ArgumentException("Account ID must be positive.", nameof(accountId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            var account = await _context.RelayEmailAccounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, cancellationToken);

            if (account == null)
                throw new KeyNotFoundException($"Relay account {accountId} not found.");

            // Deactivate
            account.IsActive = false;
            account.HealthStatus = AccountHealthStatus.Disabled;
            account.HealthCheckError = reason ?? "Account deactivated by user";
            account.UpdatedAt = DateTime.UtcNow;

            // Persist changes
            _context.RelayEmailAccounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            return account;
        }

        /// <summary>
        /// Reactivates a previously deactivated relay account.
        /// </summary>
        public async Task<RelayEmailAccount> ReactivateAccountAsync(
            int accountId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (accountId <= 0)
                throw new ArgumentException("Account ID must be positive.", nameof(accountId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            var account = await _context.RelayEmailAccounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, cancellationToken);

            if (account == null)
                throw new KeyNotFoundException($"Relay account {accountId} not found.");

            if (string.IsNullOrWhiteSpace(account.RefreshToken))
                throw new InvalidOperationException("Cannot reactivate account without refresh token. Please reconnect the account.");

            // Perform health check to validate token
            var refreshed = await _googleOAuth.TryRefreshAsync(account, cancellationToken);

            if (!refreshed)
                throw new InvalidOperationException("Cannot reactivate account. Token refresh failed. Please reconnect the account.");

            // Reactivate
            account.IsActive = true;
            account.HealthStatus = AccountHealthStatus.Healthy;
            account.HealthCheckError = null;
            account.UpdatedAt = DateTime.UtcNow;

            // Persist changes
            _context.RelayEmailAccounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            return account;
        }

        /// <summary>
        /// Gets relay account statistics for dashboard/reporting.
        /// </summary>
        public async Task<RelayAccountStatistics> GetAccountStatisticsAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            var accounts = await _context.RelayEmailAccounts
                .Where(a => a.UserId == userId)
                .ToListAsync(cancellationToken);

            return new RelayAccountStatistics
            {
                TotalAccounts = accounts.Count,
                HealthyAccounts = accounts.Count(a => a.IsActive && a.HealthStatus == AccountHealthStatus.Healthy),
                WarningAccounts = accounts.Count(a => a.HealthStatus == AccountHealthStatus.Warning),
                CriticalAccounts = accounts.Count(a => a.HealthStatus == AccountHealthStatus.Critical),
                TotalSentToday = accounts.Sum(a => a.SentToday),
                TotalDailyCapacity = accounts.Sum(a => a.DailySendLimit)
            };
        }
    }
}
