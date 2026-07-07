using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Interfaces;
using System.Security.Claims;

namespace ProcessZero.Web.Controllers
{
    [Route("api/googleauth")]
    [ApiController]
    public class GoogleAuthController : ControllerBase
    {
        private readonly IGoogleOAuthService _googleOAuth;

        public GoogleAuthController(IGoogleOAuthService googleOAuth)
        {
            _googleOAuth = googleOAuth ?? throw new ArgumentNullException(nameof(googleOAuth));
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
        /// Request body for POST /callback-exchange
        /// Contains authorization code and state from Google redirect
        /// </summary>
        public record CallbackExchangeRequest(
            string Code,
            string State);

        /// <summary>
        /// Response for successful account connection
        /// </summary>
        public record AccountConnectedResponse(
            int Id,
            string EmailAddress,
            string DisplayName,
            DateTime ConnectedAt);

        // =========================================================
        // 1. GET GOOGLE AUTH URL
        // =========================================================
        /// <summary>
        /// Generates the Google OAuth authorization URL.
        /// Frontend calls this, then redirects user to the returned authUrl.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("auth-url")]
        public IActionResult GetAuthUrl()
        {
            try
            {
                var state = Guid.NewGuid().ToString();
                var url = _googleOAuth.GenerateAuthUrl(state);

                return Ok(new
                {
                    authUrl = url,
                    state,
                    message = "Redirect user to this authUrl. After approval, Google redirects to /auth-callback route."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // =========================================================
        // 2. GOOGLE CALLBACK - RECEIVE CODE FROM GOOGLE
        // =========================================================
        /// <summary>
        /// This endpoint receives the redirect from Google after user approves.
        /// Google sends: ?code=AUTH_CODE&state=STATE
        /// 
        /// IMPORTANT: This is redirected to a FRONTEND route, not an API.
        /// Frontend reads code and state from URL query parameters,
        /// then calls POST /callback-exchange with JWT token.
        /// 
        /// FLOW:
        /// 1. Frontend gets authUrl from GET /auth-url
        /// 2. Frontend redirects user to authUrl
        /// 3. User approves on Google consent screen
        /// 4. Google redirects back to GET /callback with code and state
        /// 5. We redirect to frontend route http://localhost:8100/auth-callback
        /// 6. Frontend extracts code from URL
        /// 7. Frontend calls POST /callback-exchange with code + JWT token
        /// </summary>
        [AllowAnonymous]
        [HttpGet("callback")]
        public async Task<IActionResult> GoogleCallback(
            [FromQuery] string code,
            [FromQuery] string state,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                var error = Uri.EscapeDataString("Missing authorization code from Google");
                return Redirect($"https://processzero.xyz/auth-callback?error={error}");
            }

            if (string.IsNullOrWhiteSpace(state))
            {
                var error = Uri.EscapeDataString("Missing state token");
                return Redirect($"https://processzero.xyz/auth-callback?error={error}");
            }

            try
            {
                // Redirect to frontend with code and state
                // Frontend will read these from URL and call POST /callback-exchange
                var callbackUrl = $"https://processzero.xyz/auth-callback?" +
                    $"code={Uri.EscapeDataString(code)}&" +
                    $"state={Uri.EscapeDataString(state)}";

                return Redirect(callbackUrl);
            }
            catch (Exception ex)
            {
                var error = Uri.EscapeDataString(ex.Message);
                return Redirect($"https://processzero.xyz/auth-callback?error={error}");
            }
        }

        // =========================================================
        // 2B. EXCHANGE CODE - FRONTEND CALLS WITH JWT TOKEN
        // =========================================================
        /// <summary>
        /// Frontend calls this endpoint (after receiving code from Google) with JWT token.
        /// This is where we exchange the code for OAuth tokens and save the account.
        /// 
        /// IMPORTANT: This requires [Authorize] because we need JWT to get userId.
        /// 
        /// FLOW:
        /// 1. GET /callback receives code from Google (redirects frontend)
        /// 2. Frontend reads code from URL query parameters
        /// 3. Frontend calls this endpoint (POST /callback-exchange) with:
        ///    - JSON body: { code, state }
        ///    - Authorization header: Bearer <jwt-token>
        /// 4. We extract userId from JWT (now we know which user this is!)
        /// 5. We exchange code for access/refresh tokens
        /// 6. We fetch user's Gmail profile
        /// 7. We save the Gmail account to database with this userId
        /// </summary>
        [Authorize]  // REQUIRED to get userId from JWT
        [HttpPost("callback-exchange")]
        public async Task<IActionResult> CallbackExchange(
            [FromBody] CallbackExchangeRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Code))
                return BadRequest(new { error = "Authorization code is required" });

            if (string.IsNullOrWhiteSpace(request.State))
                return BadRequest(new { error = "State token is required for CSRF protection" });

            // Get userId from JWT (this is the magic part!)
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { error = "User not authenticated" });

            try
            {
                // 1. Exchange code for tokens
                var oauthResult = await _googleOAuth.ExchangeCodeAsync(
                    request.Code,
                    cancellationToken);

                // 2. Verify required scopes
                if (!_googleOAuth.HasRequiredScopes(oauthResult.Scope))
                    return BadRequest(new
                    {
                        error = "Missing gmail.send scope. Please reconnect and approve all permissions."
                    });

                // 3. Get user profile
                var userInfo = await _googleOAuth.GetUserInfoAsync(
                    oauthResult.AccessToken,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(userInfo.Email))
                    return BadRequest(new { error = "Failed to retrieve email from Google profile" });

                // 4. Save account to database (now we have userId from JWT!)
                var account = await _googleOAuth.SaveEmailAccountAsync(
                    userId,
                    userInfo.Email,
                    oauthResult,
                    userInfo,
                    cancellationToken);

                // 5. Return success response
                return Ok(new AccountConnectedResponse(
                    Id: account.Id,
                    EmailAddress: account.EmailAddress,
                    DisplayName: account.DisplayName,
                    ConnectedAt: account.CreatedAt));
            }
            catch (InvalidOperationException ex)
            {
                // Email already connected by this user
                return BadRequest(new { error = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(new { error = $"OAuth failed: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Unexpected error: {ex.Message}" }); 
            }
        }

        // =========================================================
        // 3. LIST ACCOUNTS
        // =========================================================
        [Authorize]
        [HttpGet("accounts")]
        public async Task<IActionResult> GetAccounts(CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { error = "User not authenticated" });

            try
            {
                var accounts = await _googleOAuth.GetAccountsByUserIdAsync(userId, cancellationToken);

                return Ok(accounts.Select(a => new
                {
                    a.Id,
                    a.EmailAddress,
                    a.DisplayName,
                    a.IsActive,
                    a.HealthStatus,
                    a.LastUsedAt,
                    a.CreatedAt
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = $"Failed to retrieve accounts: {ex.Message}" });
            }
        }

        // =========================================================
        // 4. GET SINGLE ACCOUNT
        // =========================================================
        [Authorize]
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
                var account = await _googleOAuth.GetAccountByIdAsync(id, userId, cancellationToken);

                return Ok(new
                {
                    account.Id,
                    account.EmailAddress,
                    account.DisplayName,
                    account.IsActive,
                    account.HealthStatus,
                    account.LastUsedAt,
                    account.CreatedAt
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
                    new { error = $"Failed to retrieve account: {ex.Message}" });
            }
        }

        // =========================================================
        // 5. DELETE ACCOUNT
        // =========================================================
        [Authorize]
        [HttpDelete("accounts/{id:int}")]
        public async Task<IActionResult> DeleteAccount(
            int id,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { error = "User not authenticated" });

            try
            {
                await _googleOAuth.DeleteAccountAsync(id, userId, cancellationToken);
                return NoContent();
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
                    new { error = $"Failed to delete account: {ex.Message}" });
            }
        }

        // =========================================================
        // 6. CHECK ACCOUNT HEALTH
        // =========================================================
        [Authorize]
        [HttpPost("accounts/{id:int}/health-check")]
        public async Task<IActionResult> CheckHealth(
            int id,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { error = "User not authenticated" });

            try
            {
                var account = await _googleOAuth.CheckAccountHealthAsync(id, userId, cancellationToken);

                return Ok(new
                {
                    account.Id,
                    account.EmailAddress,
                    account.HealthStatus,
                    account.HealthCheckError,
                    lastChecked = DateTime.UtcNow
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
    }
}
