using ProcessZero.Domain.Entities;

namespace ProcessZero.Application.Interfaces
{
    /// <summary>
    /// Service for managing relay email accounts and their sending operations.
    /// 
    /// Relay accounts are authenticated Gmail accounts that campaigns use to send emails.
    /// This service handles:
    /// - Account validation and health checks
    /// - Daily send limit enforcement
    /// - Token refresh and validity checks
    /// - Account selection for campaigns
    /// - Sending stats tracking
    /// 
    /// Usage:
    /// - Campaign sends emails through a relay account
    /// - Before sending: GetHealthyAccountAsync() to validate
    /// - After sending: RecordSendAsync() to update stats
    /// - Check limits: CanSendAsync() to enforce daily limits
    /// </summary>
    public interface IRelayService
    {
        /// <summary>
        /// Gets a specific relay account by ID with user authorization check.
        /// </summary>
        /// <param name="accountId">ID of the relay account</param>
        /// <param name="userId">User ID requesting the account (for authorization)</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>RelayEmailAccount if found and user is authorized</returns>
        /// <exception cref="KeyNotFoundException">If account not found</exception>
        /// <exception cref="UnauthorizedAccessException">If user doesn't own the account</exception>
        Task<RelayEmailAccount> GetAccountAsync(int accountId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all relay accounts owned by a specific user.
        /// </summary>
        /// <param name="userId">User ID to filter accounts</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>List of user's relay accounts</returns>
        Task<List<RelayEmailAccount>> GetUserAccountsAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active (healthy) relay accounts owned by a user, sorted by send capacity.
        /// 
        /// Used when selecting which account to use for sending.
        /// Returns only accounts with:
        /// - HealthStatus = Healthy
        /// - IsActive = true
        /// - Haven't exceeded daily send limit
        /// </summary>
        /// <param name="userId">User ID to filter accounts</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>List of healthy accounts sorted by available capacity</returns>
        Task<List<RelayEmailAccount>> GetHealthyAccountsAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a single healthy relay account suitable for sending.
        /// 
        /// Selection strategy:
        /// 1. Filter to healthy accounts with available capacity
        /// 2. Sort by least recently used (to distribute load evenly)
        /// 3. Return the first available account
        /// 
        /// Used by campaigns when they need to pick one account for sending.
        /// </summary>
        /// <param name="userId">User ID to filter accounts</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>A healthy account ready to send, or null if none available</returns>
        Task<RelayEmailAccount?> GetHealthyAccountForSendingAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a relay account can send more emails today.
        /// 
        /// Returns false if:
        /// - Account has reached daily send limit
        /// - Account is not healthy/active
        /// - Account needs token refresh and refresh fails
        /// </summary>
        /// <param name="account">The relay account to check</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>True if account can send, False otherwise</returns>
        Task<bool> CanSendAsync(RelayEmailAccount account, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records that an email was successfully sent from this account.
        /// 
        /// Updates:
        /// - SentToday count (incremented)
        /// - LastUsedAt timestamp (set to now)
        /// - UpdatedAt timestamp (set to now)
        /// 
        /// Persists changes to database.
        /// </summary>
        /// <param name="account">The relay account that sent the email</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Updated account with new stats</returns>
        Task<RelayEmailAccount> RecordSendAsync(RelayEmailAccount account, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records that an email send failed from this account.
        /// 
        /// Updates health status to Critical or Warning based on error type.
        /// Persists changes to database.
        /// </summary>
        /// <param name="account">The relay account that failed</param>
        /// <param name="errorMessage">Description of the failure</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Updated account with health status set to Critical</returns>
        Task<RelayEmailAccount> RecordSendFailureAsync(RelayEmailAccount account, string errorMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets the daily send counter for all accounts.
        /// 
        /// Called once per day (via scheduled job) to reset SentToday counters.
        /// Allows accounts to send their full daily limit again.
        /// 
        /// Only resets accounts owned by the specified user, or all if userId is null.
        /// </summary>
        /// <param name="userId">User ID to filter, or null to reset all accounts</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Number of accounts reset</returns>
        Task<int> ResetDailyCountersAsync(string? userId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs comprehensive health check on a relay account.
        /// 
        /// Validates:
        /// - Token is valid and not expired
        /// - Refresh token works (if needed)
        /// - Account is still accessible
        /// 
        /// Updates HealthStatus and HealthCheckError based on results.
        /// Deactivates account if health check fails completely.
        /// </summary>
        /// <param name="accountId">ID of account to check</param>
        /// <param name="userId">User ID requesting check (for authorization)</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Updated account with new health status</returns>
        Task<RelayEmailAccount> CheckAccountHealthAsync(int accountId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates (disables) a relay account.
        /// 
        /// Used when an account should stop being used (e.g., user revoked access, token invalid).
        /// Campaigns will skip this account when selecting which one to use.
        /// </summary>
        /// <param name="accountId">ID of account to deactivate</param>
        /// <param name="userId">User ID requesting deactivation (for authorization)</param>
        /// <param name="reason">Optional reason for deactivation (stored in HealthCheckError)</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Updated deactivated account</returns>
        Task<RelayEmailAccount> DeactivateAccountAsync(int accountId, string userId, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reactivates a previously deactivated relay account.
        /// 
        /// Only works if account has a valid refresh token.
        /// Performs health check and updates status before reactivating.
        /// </summary>
        /// <param name="accountId">ID of account to reactivate</param>
        /// <param name="userId">User ID requesting reactivation (for authorization)</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Reactivated account if successful</returns>
        /// <exception cref="InvalidOperationException">If account cannot be reactivated</exception>
        Task<RelayEmailAccount> ReactivateAccountAsync(int accountId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets relay account statistics for dashboard/reporting.
        /// </summary>
        /// <param name="userId">User ID to filter accounts</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Statistics object with total accounts, healthy count, etc.</returns>
        Task<RelayAccountStatistics> GetAccountStatisticsAsync(string userId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Statistics about a user's relay accounts.
    /// </summary>
    public class RelayAccountStatistics
    {
        /// <summary>Total number of connected relay accounts</summary>
        public int TotalAccounts { get; set; }

        /// <summary>Number of accounts currently healthy and active</summary>
        public int HealthyAccounts { get; set; }

        /// <summary>Number of accounts in warning state</summary>
        public int WarningAccounts { get; set; }

        /// <summary>Number of accounts in critical state</summary>
        public int CriticalAccounts { get; set; }

        /// <summary>Total emails sent today across all accounts</summary>
        public int TotalSentToday { get; set; }

        /// <summary>Total daily capacity across all accounts</summary>
        public int TotalDailyCapacity { get; set; }

        /// <summary>Percentage of daily capacity used (0-100)</summary>
        public double CapacityUsedPercentage => TotalDailyCapacity > 0 ? (TotalSentToday / (double)TotalDailyCapacity) * 100 : 0;
    }
}
