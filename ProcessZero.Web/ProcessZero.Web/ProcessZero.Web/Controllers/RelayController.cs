using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Interfaces;
using System.Security.Claims;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Controller for managing relay email accounts and sending operations.
    /// 
    /// Relay accounts are authenticated Gmail accounts that campaigns use to send emails.
    /// This controller provides operations for:
    /// - Listing and retrieving relay accounts
    /// - Selecting accounts for campaign sending
    /// - Checking account health and status
    /// - Managing account activation/deactivation
    /// - Viewing statistics and capacity
    /// 
    /// All endpoints require [Authorize] - users can only manage their own accounts.
    /// The GoogleAuthController handles account connection (OAuth flow).
    /// </summary>
    [Route("api/relay")]
    [ApiController]
    [Authorize]
    public class RelayController : ControllerBase
    {
        private readonly IRelayService _relayService;

        public RelayController(IRelayService relayService)
        {
            _relayService = relayService ?? throw new ArgumentNullException(nameof(relayService));
        }

        /// <summary>
        /// Helper to extract the current authenticated user's ID from JWT claims.
        /// </summary>
        private string GetUserId()
            => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        // =========================================================
        // REQUEST/RESPONSE MODELS
        // =========================================================

        /// <summary>
        /// Response model for a relay account (safe to return to client).
        /// Excludes tokens and sensitive information.
        /// </summary>
        public record RelayAccountResponse(
            int Id,
            string EmailAddress,
            string DisplayName,
            bool IsActive,
            int HealthStatus,
            int SentToday,
            int DailySendLimit,
            DateTime? LastUsedAt,
            DateTime CreatedAt,
            DateTime UpdatedAt);

        /// <summary>
        /// Request to reactivate a disabled account.
        /// </summary>
        public record ReactivateAccountRequest(
            int AccountId);

        /// <summary>
        /// Request to deactivate an account.
        /// </summary>
        public record DeactivateAccountRequest(
            int AccountId,
            string? Reason);

        // =========================================================
        // 1. LIST ALL ACCOUNTS
        // =========================================================
        /// <summary>
        /// Gets all relay accounts owned by the current user.
        /// 
        /// Returns:
        /// - Active and inactive accounts
        /// - Account health status
        /// - Send statistics (sent today, daily limit)
        /// - Timestamps for creation and last use
        /// </summary>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>List of relay accounts owned by user</returns>
        [HttpGet("accounts")]
        public async Task<IActionResult> GetAllAccounts(CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { error = "User not authenticated" });

            try
            {
                var accounts = await _relayService.GetUserAccountsAsync(userId, cancellationToken);

                var response = accounts.Select(a => new RelayAccountResponse(
                    Id: a.Id,
                    EmailAddress: a.EmailAddress,
                    DisplayName: a.DisplayName,
                    IsActive: a.IsActive,
                    HealthStatus: (int)a.HealthStatus,
                    SentToday: a.SentToday,
                    DailySendLimit: a.DailySendLimit,
                    LastUsedAt: a.LastUsedAt,
                    CreatedAt: a.CreatedAt,
                    UpdatedAt: a.UpdatedAt
                ));

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = $"Failed to retrieve accounts: {ex.Message}" });
            }
        }

        // =========================================================
        // 2. LIST HEALTHY ACCOUNTS
        // =========================================================
        /// <summary>
        /// Gets only healthy (active) relay accounts available for sending.
        /// 
        /// Filters to accounts that:
        /// - Are marked as active (IsActive = true)
        /// - Have healthy status (not in warning/critical state)
        /// - Haven't exceeded daily send limit
        /// 
        /// Sorted by least recently used to enable load balancing.
        /// </summary>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>List of healthy accounts ready for sending</returns>
        [HttpGet("accounts/healthy")]
        public async Task<IActionResult> GetHealthyAccounts(CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { error = "User not authenticated" });

            try
            {
                var accounts = await _relayService.GetHealthyAccountsAsync(userId, cancellationToken);

                var response = accounts.Select(a => new RelayAccountResponse(
                    Id: a.Id,
                    EmailAddress: a.EmailAddress,
                    DisplayName: a.DisplayName,
                    IsActive: a.IsActive,
                    HealthStatus: (int)a.HealthStatus,
                    SentToday: a.SentToday,
                    DailySendLimit: a.DailySendLimit,
                    LastUsedAt: a.LastUsedAt,
                    CreatedAt: a.CreatedAt,
                    UpdatedAt: a.UpdatedAt
                ));

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = $"Failed to retrieve healthy accounts: {ex.Message}" });
            }
        }

        // =========================================================
        // 3. GET SINGLE ACCOUNT
        // =========================================================
        /// <summary>
        /// Gets a specific relay account by ID.
        /// 
        /// Includes:
        /// - Account details (email, display name)
        /// - Health status and error messages
        /// - Sending statistics
        /// - Token expiry information (if available)
        /// 
        /// Only returns account if user owns it.
        /// </summary>
        /// <param name="id">ID of the relay account</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Relay account details</returns>
        [HttpGet("accounts/{id:int}")]
        public async Task<IActionResult> GetAccount(
            int id,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { error = "User not authenticated" });

            try
            {
                var account = await _relayService.GetAccountAsync(id, userId, cancellationToken);

                var response = new RelayAccountResponse(
                    Id: account.Id,
                    EmailAddress: account.EmailAddress,
                    DisplayName: account.DisplayName,
                    IsActive: account.IsActive,
                    HealthStatus: (int)account.HealthStatus,
                    SentToday: account.SentToday,
                    DailySendLimit: account.DailySendLimit,
                    LastUsedAt: account.LastUsedAt,
                    CreatedAt: account.CreatedAt,
                    UpdatedAt: account.UpdatedAt
                );

                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = $"Failed to retrieve account: {ex.Message}" });
            }
        }

        // =========================================================
        // 4. CHECK ACCOUNT HEALTH
        // =========================================================
        /// <summary>
        /// Performs a comprehensive health check on a relay account.
        /// 
        /// Validates:
        /// - Refresh token is still valid
        /// - Access token can be refreshed
        /// - Account hasn't been revoked by user in Google settings
        /// 
        /// Updates HealthStatus and HealthCheckError if issues detected.
        /// </summary>
        /// <param name="id">ID of the relay account to check</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Updated account with new health status</returns>
        [HttpPost("accounts/{id:int}/health-check")]
        public async Task<IActionResult> CheckAccountHealth(
            int id,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { error = "User not authenticated" });

            try
            {
                var account = await _relayService.CheckAccountHealthAsync(id, userId, cancellationToken);

                var response = new RelayAccountResponse(
                    Id: account.Id,
                    EmailAddress: account.EmailAddress,
                    DisplayName: account.DisplayName,
                    IsActive: account.IsActive,
                    HealthStatus: (int)account.HealthStatus,
                    SentToday: account.SentToday,
                    DailySendLimit: account.DailySendLimit,
                    LastUsedAt: account.LastUsedAt,
                    CreatedAt: account.CreatedAt,
                    UpdatedAt: account.UpdatedAt
                );

                return Ok(new
                {
                    account = response,
                    healthCheckMessage = account.HealthStatus.ToString(),
                    errorDetails = account.HealthCheckError
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = $"Health check failed: {ex.Message}" });
            }
        }

        // =========================================================
        // 5. DEACTIVATE ACCOUNT
        // =========================================================
        /// <summary>
        /// Deactivates (disables) a relay account.
        /// 
        /// After deactivation:
        /// - Account will not be selected for campaign sending
        /// - Account can be reactivated later
        /// - Tokens are retained for possible reactivation
        /// 
        /// Use cases:
        /// - User suspects account compromise
        /// - Temporary pause of sending through this account
        /// - Account shows errors but may be recoverable
        /// </summary>
        /// <param name="request">Contains account ID and optional reason</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Deactivated account details</returns>
        [HttpPost("accounts/deactivate")]
        public async Task<IActionResult> DeactivateAccount(
            [FromBody] DeactivateAccountRequest request,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { error = "User not authenticated" });

            if (request == null || request.AccountId <= 0)
                return BadRequest(new { error = "Valid account ID is required" });

            try
            {
                var account = await _relayService.DeactivateAccountAsync(
                    request.AccountId,
                    userId,
                    request.Reason,
                    cancellationToken);

                var response = new RelayAccountResponse(
                    Id: account.Id,
                    EmailAddress: account.EmailAddress,
                    DisplayName: account.DisplayName,
                    IsActive: account.IsActive,
                    HealthStatus: (int)account.HealthStatus,
                    SentToday: account.SentToday,
                    DailySendLimit: account.DailySendLimit,
                    LastUsedAt: account.LastUsedAt,
                    CreatedAt: account.CreatedAt,
                    UpdatedAt: account.UpdatedAt
                );

                return Ok(new { message = "Account deactivated successfully", account = response });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = $"Failed to deactivate account: {ex.Message}" });
            }
        }

        // =========================================================
        // 6. REACTIVATE ACCOUNT
        // =========================================================
        /// <summary>
        /// Reactivates a previously deactivated relay account.
        /// 
        /// Before reactivation:
        /// - Performs token refresh to validate access
        /// - Checks if refresh token is still valid
        /// - If validation fails, returns error asking to reconnect
        /// 
        /// After successful reactivation:
        /// - Account is available for campaign sending again
        /// - Health status is set to Healthy
        /// </summary>
        /// <param name="request">Contains account ID to reactivate</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Reactivated account details</returns>
        [HttpPost("accounts/reactivate")]
        public async Task<IActionResult> ReactivateAccount(
            [FromBody] ReactivateAccountRequest request,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { error = "User not authenticated" });

            if (request == null || request.AccountId <= 0)
                return BadRequest(new { error = "Valid account ID is required" });

            try
            {
                var account = await _relayService.ReactivateAccountAsync(
                    request.AccountId,
                    userId,
                    cancellationToken);

                var response = new RelayAccountResponse(
                    Id: account.Id,
                    EmailAddress: account.EmailAddress,
                    DisplayName: account.DisplayName,
                    IsActive: account.IsActive,
                    HealthStatus: (int)account.HealthStatus,
                    SentToday: account.SentToday,
                    DailySendLimit: account.DailySendLimit,
                    LastUsedAt: account.LastUsedAt,
                    CreatedAt: account.CreatedAt,
                    UpdatedAt: account.UpdatedAt
                );

                return Ok(new { message = "Account reactivated successfully", account = response });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = $"Failed to reactivate account: {ex.Message}" });
            }
        }

        // =========================================================
        // 7. GET ACCOUNT STATISTICS
        // =========================================================
        /// <summary>
        /// Gets aggregated statistics about all relay accounts.
        /// 
        /// Returns:
        /// - Total number of connected accounts
        /// - Count of healthy, warning, and critical accounts
        /// - Total emails sent today
        /// - Total daily capacity
        /// - Usage percentage
        /// 
        /// Useful for dashboards and capacity planning.
        /// </summary>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Relay account statistics</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { error = "User not authenticated" });

            try
            {
                var stats = await _relayService.GetAccountStatisticsAsync(userId, cancellationToken);

                return Ok(new
                {
                    totalAccounts = stats.TotalAccounts,
                    healthyAccounts = stats.HealthyAccounts,
                    warningAccounts = stats.WarningAccounts,
                    criticalAccounts = stats.CriticalAccounts,
                    totalSentToday = stats.TotalSentToday,
                    totalDailyCapacity = stats.TotalDailyCapacity,
                    capacityUsedPercentage = Math.Round(stats.CapacityUsedPercentage, 2),
                    message = stats.TotalAccounts == 0 
                        ? "No relay accounts connected. Use Google Auth to connect accounts." 
                        : $"{stats.HealthyAccounts} of {stats.TotalAccounts} accounts are healthy"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = $"Failed to retrieve statistics: {ex.Message}" });
            }
        }

        // =========================================================
        // 8. GET RECOMMENDED ACCOUNT FOR SENDING
        // =========================================================
        /// <summary>
        /// Gets the recommended account to use for the next campaign send.
        /// 
        /// Selection criteria:
        /// - Account must be active and healthy
        /// - Account must have sending capacity (not reached daily limit)
        /// - Accounts are rotated based on last usage (least recently used first)
        /// 
        /// Returns null if no suitable account exists.
        /// </summary>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Recommended account or null if none available</returns>
        [HttpGet("recommended-account")]
        public async Task<IActionResult> GetRecommendedAccount(CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { error = "User not authenticated" });

            try
            {
                var account = await _relayService.GetHealthyAccountForSendingAsync(userId, cancellationToken);

                if (account == null)
                {
                    return Ok(new
                    {
                        account = (RelayAccountResponse?)null,
                        message = "No healthy accounts available for sending. Please connect a Gmail account or check account health."
                    });
                }

                var response = new RelayAccountResponse(
                    Id: account.Id,
                    EmailAddress: account.EmailAddress,
                    DisplayName: account.DisplayName,
                    IsActive: account.IsActive,
                    HealthStatus: (int)account.HealthStatus,
                    SentToday: account.SentToday,
                    DailySendLimit: account.DailySendLimit,
                    LastUsedAt: account.LastUsedAt,
                    CreatedAt: account.CreatedAt,
                    UpdatedAt: account.UpdatedAt
                );

                return Ok(new
                {
                    account = response,
                    remainingCapacity = account.DailySendLimit - account.SentToday,
                    message = $"Account {account.EmailAddress} has {account.DailySendLimit - account.SentToday} sends remaining today"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = $"Failed to get recommended account: {ex.Message}" });
            }
        }
    }
}
