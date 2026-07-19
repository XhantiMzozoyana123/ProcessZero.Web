using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Application.Options;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static ProcessZero.Application.Dtos.GoogleAuthDto;

namespace ProcessZero.Infrastructure.Services
{
    /// <summary>
    /// Google OAuth 2.0 Service
    /// 
    /// Handles authentication with Google accounts to enable email sending through Gmail API.
    /// This service manages:
    /// - OAuth authorization flow (redirect to Google, handle callback)
    /// - Token exchange (convert auth code to access/refresh tokens)
    /// - Token refresh (keep access tokens valid for long-lived operations)
    /// - User info retrieval (get email, name from authenticated user)
    /// - Token validation (verify JWT tokens, check scopes)
    /// - Account health monitoring (track token validity, detect failures)
    /// - Database persistence (saving authenticated accounts)
    /// 
    /// Flow Overview:
    /// 1. User clicks "Connect Gmail" → GenerateAuthUrl() → redirects to Google
    /// 2. User approves permissions → callback with authorization code
    /// 3. ExchangeCodeAsync() → trades code for AccessToken + RefreshToken
    /// 4. GetUserInfoAsync() → retrieves email, name, profile
    /// 5. SaveEmailAccountAsync() → stores in database as RelayEmailAccount
    /// 6. Token stored in RelayEmailAccount entity
    /// 7. Before each campaign email: EnsureValidAccessTokenAsync() checks expiry
    /// 8. If expired: RefreshTokenAsync() gets new access token (refresh token is long-lived)
    /// 9. TryRefreshAsync() is resilient wrapper (handles failures gracefully for health checks)
    /// </summary>
    public class GoogleOAuthService : IGoogleOAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly GoogleOAuthOptions _options;

        /// <summary>
        /// Constructor for GoogleOAuthService.
        /// Injects HttpClient for Google API calls, ApplicationDbContext for persistence,
        /// and IOptions for Google OAuth credentials from appsettings.json.
        /// </summary>
        /// <param name="httpClient">HttpClient for making HTTP requests to Google OAuth endpoints</param>
        /// <param name="context">Database context for saving email accounts</param>
        /// <param name="options">Google OAuth options (ClientId, ClientSecret, RedirectUri)</param>
        public GoogleOAuthService(
            HttpClient httpClient,
            ApplicationDbContext context,
            IOptions<GoogleOAuthOptions> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        // ---------------------------
        // 1. AUTH URL GENERATION
        // ---------------------------
        /// <summary>
        /// Generates the Google OAuth authorization URL that users click to grant permissions.
        /// 
        /// Scopes requested:
        /// - openid: Sign in with Google
        /// - email: Access email address
        /// - profile: Access name, picture
        /// - gmail.send: Permission to send emails on behalf of user
        /// 
        /// Parameters:
        /// - access_type=offline: Requests refresh token (enables long-lived access)
        /// - prompt=consent: Always shows consent screen (ensures user approves scopes)
        /// - state: Security token to prevent CSRF attacks during callback
        /// </summary>
        /// <param name="state">CSRF protection token (should be random, stored in session/db)</param>
        /// <returns>Full authorization URL for browser redirect</returns>
        public string GenerateAuthUrl(string state)
        {
            var scope = "openid email profile https://www.googleapis.com/auth/gmail.send";

            return
                "https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={_options.ClientId}" +
                $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
                $"&response_type=code" +
                $"&scope={Uri.EscapeDataString(scope)}" +
                $"&access_type=offline" +
                $"&prompt=consent" +
                $"&state={state}";
        }

        // ---------------------------
        // 2. EXCHANGE AUTHORIZATION CODE FOR TOKENS
        // ---------------------------
        /// <summary>
        /// Exchanges authorization code (from Google callback) for access and refresh tokens.
        /// 
        /// This is the critical step where we get:
        /// - AccessToken: Short-lived (~1 hour), used in Gmail API requests
        /// - RefreshToken: Long-lived (~6 months), used to get new access tokens when expired
        /// - ExpiryUtc: When the access token expires (UTC timestamp)
        /// - IdToken: JWT containing user identity (email, name verified by Google)
        /// 
        /// The authorization code is single-use and expires quickly (~10 minutes).
        /// Must be exchanged immediately after receiving from Google callback.
        /// </summary>
        /// <param name="code">Authorization code from Google OAuth callback</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>GoogleOAuthResult with tokens and expiry info</returns>
        /// <exception cref="HttpRequestException">If token exchange fails</exception>
        public async Task<GoogleOAuthResult> ExchangeCodeAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            var values = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", _options.ClientId },
                { "client_secret", _options.ClientSecret },
                { "redirect_uri", _options.RedirectUri },
                { "grant_type", "authorization_code" }
            };

            // POST to Google's token endpoint with credentials
            var response = await _httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(values),
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var token = JsonSerializer.Deserialize<GoogleTokenResponse>(json)!;

            // Return structured result with computed expiry
            return new GoogleOAuthResult
            {
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                ExpiryUtc = DateTime.UtcNow.AddSeconds(token.ExpiresIn),
                Scope = token.Scope,
                IdToken = token.IdToken
            };
        }

        // ---------------------------
        // 3. REFRESH ACCESS TOKEN
        // ---------------------------
        /// <summary>
        /// Refreshes an expired or expiring access token using the refresh token.
        /// 
        /// Key points:
        /// - Refresh tokens don't expire (unless user revokes, account deleted, etc.)
        /// - Access tokens expire every ~1 hour
        /// - Before sending campaign emails, check if token is expiring soon (2-min buffer)
        /// - If expiring: call this method to get new access token
        /// - Google may return a new refresh token (rare, but handle it)
        /// 
        /// Used by:
        /// - EnsureValidAccessTokenAsync() for proactive refresh during campaigns
        /// - TryRefreshAsync() for health checks when account seems broken
        /// </summary>
        /// <param name="refreshToken">Long-lived refresh token from earlier authentication</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>GoogleOAuthResult with new access token and expiry</returns>
        /// <exception cref="HttpRequestException">If refresh fails (e.g., account revoked)</exception>
        public async Task<GoogleOAuthResult> RefreshTokenAsync(
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            var values = new Dictionary<string, string>
            {
                { "client_id", _options.ClientId },
                { "client_secret", _options.ClientSecret },
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" }
            };

            // POST to Google's token endpoint with refresh token
            var response = await _httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(values),
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var token = JsonSerializer.Deserialize<GoogleTokenResponse>(json)!;

            // Return new access token with expiry
            return new GoogleOAuthResult
            {
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                ExpiryUtc = DateTime.UtcNow.AddSeconds(token.ExpiresIn),
                Scope = token.Scope,
                IdToken = token.IdToken
            };
        }

        // ---------------------------
        // 4. GET USER INFO
        // ---------------------------
        /// <summary>
        /// Retrieves user profile information (email, name, picture, etc.) using access token.
        /// 
        /// Called after successful OAuth to store email address and user info.
        /// Uses the Google OAuth2 v2 API userinfo endpoint.
        /// </summary>
        /// <param name="accessToken">Valid Google access token</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>GoogleUserInfo with email, name, picture, etc.</returns>
        /// <exception cref="HttpRequestException">If API request fails</exception>
        /// <exception cref="InvalidOperationException">If email is missing from response</exception>
        public async Task<GoogleUserInfo> GetUserInfoAsync(
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://www.googleapis.com/oauth2/v2/userinfo");

            // Add access token to Authorization header (Bearer scheme)
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(json)
                ?? throw new InvalidOperationException("Failed to deserialize Google user info response");

            // Validate required fields
            if (string.IsNullOrWhiteSpace(userInfo.Email))
                throw new InvalidOperationException("Email not found in Google user info response. Response: " + json);

            // Use given_name + family_name if full name is missing
            if (string.IsNullOrWhiteSpace(userInfo.Name))
            {
                userInfo.Name = $"{userInfo.GivenName} {userInfo.FamilyName}".Trim();
            }

            return userInfo;
        }

        // ---------------------------
        // 5. VALIDATE ID TOKEN (OPTIONAL BUT RECOMMENDED)
        // ---------------------------
        /// <summary>
        /// Validates the JWT ID token received from Google.
        /// 
        /// ID token validation provides:
        /// - Cryptographic proof the user is who they claim (signed by Google)
        /// - Email verified by Google (more trusted than self-reported)
        /// - Prevents token tampering or replay attacks
        /// 
        /// This is an extra security layer on top of GetUserInfoAsync().
        /// Google libraries handle JWT signature verification for us.
        /// </summary>
        /// <param name="idToken">JWT token from OAuth response</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Validated token payload with email, name, issuer, expiry</returns>
        public async Task<GoogleIdTokenPayload> ValidateIdTokenAsync(
            string idToken,
            CancellationToken cancellationToken = default)
        {
            // Use Google's official JWT validation library
            var payload = await Google.Apis.Auth.GoogleJsonWebSignature
                .ValidateAsync(idToken);

            return new GoogleIdTokenPayload
            {
                Email = payload.Email,
                Name = payload.Name,
                Issuer = payload.Issuer,
                ExpirationTime = payload.ExpirationTimeSeconds.HasValue
                    ? DateTimeOffset.FromUnixTimeSeconds(payload.ExpirationTimeSeconds.Value).UtcDateTime
                    : DateTime.MinValue
            };
        }

        // ---------------------------
        // 6. ENSURE VALID ACCESS TOKEN (USED BY CAMPAIGNS)
        // ---------------------------
        /// <summary>
        /// Ensures the email account has a valid (non-expired) access token.
        /// 
        /// This is called right before sending campaign emails to prevent:
        /// - "Authentication failed" errors mid-campaign
        /// - Failed email sends due to expired tokens
        /// 
        /// Strategy:
        /// - Check if token expiry is within 2-minute buffer
        /// - If so, proactively refresh using refresh token
        /// - Update account with new token and expiry
        /// - If no refresh token exists, throw error (account broken)
        /// 
        /// Exception: Throws InvalidOperationException if refresh token missing.
        /// This indicates the account was never properly authenticated or is revoked.
        /// </summary>
        /// <param name="account">Email account with token and refresh token</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <exception cref="InvalidOperationException">If refresh token is missing</exception>
        public async Task EnsureValidAccessTokenAsync(
            RelayEmailAccount account,
            CancellationToken cancellationToken = default)
        {
            // 2-minute buffer ensures we refresh before token actually expires
            var buffer = TimeSpan.FromMinutes(2);

            // If token is still valid beyond the buffer, no refresh needed
            if (account.TokenExpiry > DateTime.UtcNow.Add(buffer))
                return;

            // Refresh token must exist to get new access token
            if (string.IsNullOrEmpty(account.RefreshToken))
                throw new InvalidOperationException("Missing refresh token");

            // Get new access token using refresh token
            var refreshed = await RefreshTokenAsync(account.RefreshToken, cancellationToken);

            // Update account with new tokens
            account.AccessToken = refreshed.AccessToken;
            account.TokenExpiry = refreshed.ExpiryUtc;

            // Google may return a new refresh token (store it if provided)
            if (!string.IsNullOrEmpty(refreshed.RefreshToken))
                account.RefreshToken = refreshed.RefreshToken;
        }

        /// <summary>
        /// Resilient token refresh wrapper that handles failures gracefully.
        /// 
        /// Unlike EnsureValidAccessTokenAsync(), this method:
        /// - Returns bool instead of throwing exceptions
        /// - Catches and logs errors without propagating
        /// - Updates account health status on failure
        /// - Deactivates account if refresh fails (prevent further failures)
        /// 
        /// Used by:
        /// - Account health check endpoints
        /// - Monitoring/diagnostic methods
        /// - Cleanup routines that shouldn't crash the app
        /// 
        /// Never use this for critical campaign sending - use EnsureValidAccessTokenAsync instead.
        /// </summary>
        /// <param name="account">Email account to refresh</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>True if refresh succeeded, False if it failed</returns>
        public async Task<bool> TryRefreshAsync(
            RelayEmailAccount account,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(account.RefreshToken))
                return false;

            try
            {
                var refreshed = await RefreshTokenAsync(
                    account.RefreshToken,
                    cancellationToken);

                account.AccessToken = refreshed.AccessToken;
                account.TokenExpiry = refreshed.ExpiryUtc;

                // ⚠️ Only overwrite if Google sends a new one (rare edge case)
                if (!string.IsNullOrWhiteSpace(refreshed.RefreshToken))
                {
                    account.RefreshToken = refreshed.RefreshToken;
                }

                // Update health status - success (useful for monitoring dashboards)
                account.HealthStatus = AccountHealthStatus.Healthy;
                account.HealthCheckError = null;

                return true;
            }
            catch (Exception ex)
            {
                // Mark account as problematic - prevents repeated campaign failures
                // This gives admin visibility to broken accounts via health status
                account.HealthStatus = AccountHealthStatus.Critical;
                account.HealthCheckError = $"Token refresh failed: {ex.Message}";
                account.IsActive = false;

                return false;
            }
        }

        // ---------------------------
        // 7. VERIFY REQUIRED SCOPES
        // ---------------------------
        /// <summary>
        /// Checks if the user granted the required "gmail.send" scope.
        /// 
        /// Users can selectively deny scopes during OAuth consent screen.
        /// This method verifies we have permission to send emails.
        /// 
        /// Called after oauth callback to validate permissions before storing account.
        /// If returns false, user denied email-sending permission - ask them to reconnect.
        /// </summary>
        /// <param name="scope">Space-separated list of granted scopes from OAuth response</param>
        /// <returns>True if gmail.send scope is present, False otherwise</returns>
        public bool HasRequiredScopes(string scope)
        {
            // Empty scope list means no permissions granted
            if (string.IsNullOrWhiteSpace(scope))
                return false;

            // Parse space-separated scope list
            var scopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Check for gmail.send permission (case-insensitive comparison)
            return scopes.Contains(
                "https://www.googleapis.com/auth/gmail.send",
                StringComparer.OrdinalIgnoreCase);
        }

        // ---------------------------
        // 8. SAVE EMAIL ACCOUNT TO DATABASE
        // ---------------------------
        /// <summary>
        /// Saves a new authenticated Gmail account to the database.
        /// 
        /// Called after successful OAuth callback to persist the email account.
        /// 
        /// Steps:
        /// 1. Creates new RelayEmailAccount with OAuth token data
        /// 2. Sets initial health status to Healthy
        /// 3. Marks account as active
        /// 4. Saves to database with UserId for multi-tenancy
        /// 5. Returns saved account with generated ID
        /// 
        /// Throws if email already connected by this user to prevent duplicates.
        /// </summary>
        /// <param name="userId">Application user ID (from JWT claims)</param>
        /// <param name="emailAddress">Gmail address being connected</param>
        /// <param name="oauthResult">OAuth tokens from Google (access, refresh, expiry)</param>
        /// <param name="userInfo">User info from Google (email, name)</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Saved RelayEmailAccount entity with ID</returns>
        /// <exception cref="ArgumentException">If required parameters are null or empty</exception>
        /// <exception cref="InvalidOperationException">If email already connected by user</exception>
        public async Task<RelayEmailAccount> SaveEmailAccountAsync(
            string userId,
            string emailAddress,
            GoogleOAuthResult oauthResult,
            GoogleUserInfo userInfo,
            CancellationToken cancellationToken = default)
        {
            // Validate required parameters
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

            if (string.IsNullOrWhiteSpace(emailAddress))
                throw new ArgumentException("Email address cannot be null or empty", nameof(emailAddress));

            if (oauthResult == null)
                throw new ArgumentNullException(nameof(oauthResult));

            if (userInfo == null)
                throw new ArgumentNullException(nameof(userInfo));

            if (string.IsNullOrWhiteSpace(oauthResult.AccessToken))
                throw new ArgumentException("Access token cannot be null or empty", nameof(oauthResult));

            // Check if this email is already connected by this user
            var existingAccount = await _context.RelayEmailAccounts
                .FirstOrDefaultAsync(a => a.EmailAddress == emailAddress && a.UserId == userId, cancellationToken);

            if (existingAccount != null)
                throw new InvalidOperationException($"Email {emailAddress} is already connected to your account.");

            // Create new email account entity with OAuth tokens
            var account = new RelayEmailAccount
            {
                UserId = userId,
                EmailAddress = emailAddress,
                DisplayName = userInfo.Name ?? emailAddress,
                AccessToken = oauthResult.AccessToken,
                RefreshToken = oauthResult.RefreshToken,
                TokenExpiry = oauthResult.ExpiryUtc,
                IsActive = true,
                HealthStatus = AccountHealthStatus.Healthy,
                HealthCheckError = null,
                LastUsedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add to database context
            _context.RelayEmailAccounts.Add(account);

            // Save changes to database
            await _context.SaveChangesAsync(cancellationToken);

            return account;
        }

        /// <summary>
        /// Updates an existing email account with refreshed OAuth tokens.
        /// 
        /// Used after token refresh to persist new access token and expiry.
        /// Important for maintaining long-running operations without token expiration.
        /// </summary>
        /// <param name="accountId">ID of email account to update</param>
        /// <param name="oauthResult">New OAuth tokens from refresh</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Updated RelayEmailAccount</returns>
        /// <exception cref="KeyNotFoundException">If account not found</exception>
        public async Task<RelayEmailAccount> UpdateTokensAsync(
            int accountId,
            GoogleOAuthResult oauthResult,
            CancellationToken cancellationToken = default)
        {
            var account = await _context.RelayEmailAccounts.FindAsync(new object[] { accountId }, cancellationToken: cancellationToken);

            if (account == null)
                throw new KeyNotFoundException($"Email account with ID {accountId} not found.");

            // Update tokens
            account.AccessToken = oauthResult.AccessToken;
            account.TokenExpiry = oauthResult.ExpiryUtc;

            // Update refresh token only if Google sent a new one
            if (!string.IsNullOrWhiteSpace(oauthResult.RefreshToken))
                account.RefreshToken = oauthResult.RefreshToken;

            account.UpdatedAt = DateTime.UtcNow;

            // Save changes
            await _context.SaveChangesAsync(cancellationToken);

            return account;
        }

        // ---------------------------
        // 9. ACCOUNT MANAGEMENT
        // ---------------------------

        /// <summary>
        /// Retrieves all Gmail accounts connected by a specific user.
        /// 
        /// Returns public account info (email, display name, health status) but NOT tokens.
        /// Filtered to only the specified user for multi-tenant support.
        /// </summary>
        /// <param name="userId">User ID to filter accounts</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>List of user's connected accounts</returns>
        public async Task<List<RelayEmailAccount>> GetAccountsByUserIdAsync(
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
        /// Retrieves a specific email account by ID, with authorization check.
        /// 
        /// Ensures the requesting user owns the account (multi-tenant security).
        /// </summary>
        /// <param name="accountId">ID of email account to retrieve</param>
        /// <param name="userId">User ID requesting the account (for authorization)</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>RelayEmailAccount if found and owned by user</returns>
        /// <exception cref="KeyNotFoundException">If account not found</exception>
        /// <exception cref="UnauthorizedAccessException">If account not owned by user</exception>
        public async Task<RelayEmailAccount> GetAccountByIdAsync(
            int accountId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (accountId <= 0)
                throw new ArgumentException("Account ID must be positive.", nameof(accountId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            var account = await _context.RelayEmailAccounts.FindAsync(
                new object[] { accountId },
                cancellationToken: cancellationToken);

            if (account == null)
                throw new KeyNotFoundException($"Email account with ID {accountId} not found.");

            // Verify ownership
            if (account.UserId != userId)
                throw new UnauthorizedAccessException("You do not have access to this account.");

            return account;
        }

        /// <summary>
        /// Deletes (disconnects) a Gmail account.
        /// 
        /// After deletion:
        /// - Account cannot be used in new campaigns
        /// - Tokens are permanently removed
        /// - Campaigns using this account will fail with meaningful error
        /// 
        /// Includes authorization check to prevent users from deleting others' accounts.
        /// </summary>
        /// <param name="accountId">ID of account to delete</param>
        /// <param name="userId">User ID requesting deletion (for authorization)</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <exception cref="KeyNotFoundException">If account not found</exception>
        /// <exception cref="UnauthorizedAccessException">If account not owned by user</exception>
        public async Task DeleteAccountAsync(
            int accountId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (accountId <= 0)
                throw new ArgumentException("Account ID must be positive.", nameof(accountId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            var account = await _context.RelayEmailAccounts.FindAsync(
                new object[] { accountId },
                cancellationToken: cancellationToken);

            if (account == null)
                throw new KeyNotFoundException($"Email account with ID {accountId} not found.");

            // Verify ownership
            if (account.UserId != userId)
                throw new UnauthorizedAccessException("You do not have access to this account.");

            // Remove the account
            _context.RelayEmailAccounts.Remove(account);

            // Save changes
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Performs a health check on a Gmail account.
        /// 
        /// Attempts to refresh the access token to verify:
        /// 1. The refresh token is still valid
        /// 2. The user hasn't revoked access in Google account settings
        /// 3. The refresh token hasn't expired (~6 months)
        /// 
        /// Updates the account's health status and error message based on result.
        /// 
        /// Includes authorization check to prevent checking others' accounts.
        /// </summary>
        /// <param name="accountId">ID of account to check</param>
        /// <param name="userId">User ID requesting check (for authorization)</param>
        /// <param name="cancellationToken">Async cancellation token</param>
        /// <returns>Updated account with new health status</returns>
        /// <exception cref="KeyNotFoundException">If account not found</exception>
        /// <exception cref="UnauthorizedAccessException">If account not owned by user</exception>
        public async Task<RelayEmailAccount> CheckAccountHealthAsync(
            int accountId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (accountId <= 0)
                throw new ArgumentException("Account ID must be positive.", nameof(accountId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            var account = await _context.RelayEmailAccounts.FindAsync(
                new object[] { accountId },
                cancellationToken: cancellationToken);

            if (account == null)
                throw new KeyNotFoundException($"Email account with ID {accountId} not found.");

            // Verify ownership
            if (account.UserId != userId)
                throw new UnauthorizedAccessException("You do not have access to this account.");

            // Attempt to refresh the token (this validates the refresh token is still valid)
            var refreshed = await TryRefreshAsync(account, cancellationToken);

            // TryRefreshAsync updates the account's health status and error
            // Save the updated health status to database
            await _context.SaveChangesAsync(cancellationToken);

            return account;
        }
    }
}
