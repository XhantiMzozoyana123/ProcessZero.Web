using ProcessZero.Application.Dtos;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ProcessZero.Application.Dtos.GoogleAuthDto;

namespace ProcessZero.Application.Interfaces
{
    public interface IGoogleOAuthService
    {
        /// <summary>
        /// Generates OAuth URL for user to click and grant permissions to Relay AI
        /// </summary>
        string GenerateAuthUrl(string state);

        /// <summary>
        /// Exchanges authorization code (from Google callback) for access/refresh tokens
        /// </summary>
        Task<GoogleOAuthResult> ExchangeCodeAsync(
            string code,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves user profile info (email, name, picture) using access token
        /// </summary>
        Task<GoogleUserInfo> GetUserInfoAsync(
            string accessToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets new access token using refresh token (when old one expires)
        /// </summary>
        Task<GoogleOAuthResult> RefreshTokenAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Ensures the account has a valid access token (refreshes if needed)
        /// Throws exception if refresh fails - use for critical operations
        /// </summary>
        Task EnsureValidAccessTokenAsync(
            RelayEmailAccount account,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts a refresh and returns success/failure (no throw)
        /// Returns false gracefully for health checks and monitoring
        /// </summary>
        Task<bool> TryRefreshAsync(
            RelayEmailAccount account,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if user granted required "gmail.send" scope
        /// </summary>
        bool HasRequiredScopes(string scope);

        /// <summary>
        /// Saves authenticated Gmail account to database
        /// Called after successful OAuth callback
        /// </summary>
        Task<RelayEmailAccount> SaveEmailAccountAsync(
            string userId,
            string emailAddress,
            GoogleOAuthResult oauthResult,
            GoogleUserInfo userInfo,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates existing account with refreshed tokens
        /// Called after token refresh to persist new tokens
        /// </summary>
        Task<RelayEmailAccount> UpdateTokensAsync(
            int accountId,
            GoogleOAuthResult oauthResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all Gmail accounts connected by a user
        /// </summary>
        Task<List<RelayEmailAccount>> GetAccountsByUserIdAsync(
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a specific account by ID with authorization check
        /// Throws UnauthorizedAccessException if account not owned by user
        /// </summary>
        Task<RelayEmailAccount> GetAccountByIdAsync(
            int accountId,
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes (disconnects) a Gmail account
        /// Throws UnauthorizedAccessException if account not owned by user
        /// </summary>
        Task DeleteAccountAsync(
            int accountId,
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs health check on account by attempting token refresh
        /// Updates account health status and error message
        /// Throws UnauthorizedAccessException if account not owned by user
        /// </summary>
        Task<RelayEmailAccount> CheckAccountHealthAsync(
            int accountId,
            string userId,
            CancellationToken cancellationToken = default);
    }
}
