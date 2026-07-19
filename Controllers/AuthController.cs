using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System.Security.Claims;

namespace ProcessZero.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    /// <summary>
    /// Controller responsible for authentication and user account related endpoints.
    /// This controller delegates the actual work to an injected <see cref="IAuthService"/> implementation.
    /// References:
    /// - DTOs: <c>RegisterDto</c>, <c>LoginDto</c>, <c>ResetPasswordDto</c>, <c>UserDto</c>, and the small request-body records <c>EmailDto</c> and <c>TwoFactorDto</c>.
    /// - The controller issues/validates JWT tokens, handles two-factor verification, and provides endpoints
    ///   for registering users, password reset flow and updating the authenticated user's profile.
    ///
    /// Model columns:
    /// - RegisterDto: <c>UserName</c> (string), <c>Email</c> (string), <c>Password</c> (string)
    /// - LoginDto: <c>Email</c> (string), <c>Password</c> (string)
    /// - ResetPasswordDto: <c>Id</c> (string), <c>Token</c> (string), <c>NewPassword</c> (string)
    /// - UserDto: <c>Id</c> (string?), <c>UserName</c> (string), <c>NewEmail</c> (string), <c>CurrentPassword</c> (string), <c>NewPassword</c> (string)
    /// - ApplicationUser (inherits <c>IdentityUser</c>): typical Identity fields.
    ///   Common columns include:
    ///   <c>Id</c> (string), <c>UserName</c> (string), <c>NormalizedUserName</c> (string),
    ///   <c>Email</c> (string), <c>NormalizedEmail</c> (string), <c>EmailConfirmed</c> (bool),
    ///   <c>PasswordHash</c> (string), <c>SecurityStamp</c> (string), <c>ConcurrencyStamp</c> (string),
    ///   <c>PhoneNumber</c> (string), <c>PhoneNumberConfirmed</c> (bool), <c>TwoFactorEnabled</c> (bool),
    ///   <c>LockoutEnd</c> (DateTimeOffset?), <c>LockoutEnabled</c> (bool), <c>AccessFailedCount</c> (int).
    /// </summary>
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        /// <summary>
        /// Constructor. The controller depends on an <see cref="IAuthService"/> which is injected by DI.
        /// The <see cref="IAuthService"/> contains the business logic for authentication, token generation,
        /// email sending and user updates. A null <paramref name="authService"/> will cause an exception.
        /// </summary>
        /// <param name="authService">Service implementing authentication operations.</param>
        public AuthController(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        /// <summary>
        /// Helper to extract the current authenticated user's id from the JWT claims.
        /// Uses <see cref="ClaimTypes.NameIdentifier"/> which should be populated by the authentication middleware.
        /// Returns an empty string if the user is not authenticated or the claim is missing.
        /// </summary>
        /// <returns>User id as string or empty string when not available.</returns>
        private string GetUserId() => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        // Small, local DTO-like records used for compact request bodies specific to controller endpoints.
        // These are not the main application DTOs, but convenient types for single-purpose requests.
        // - EmailDto: used for the forgot-password endpoint where only an email string is required.
        // - TwoFactorDto: used to pass a user id and a 2FA token for verification.
        public sealed record EmailDto(string Email);
        public sealed record TwoFactorDto(string UserId, string Token);
        /// <summary>
        /// Registers a new user.
        /// Accepts a <see cref="RegisterDto"/> which should contain a username, email and password.
        /// On success the underlying service returns a human-readable message. On failure an error is returned.
        /// This endpoint is anonymous (no authentication required).
        /// </summary>
        /// <param name="model">Registration details (<see cref="RegisterDto"/>).</param>
        /// <returns>200 OK with message or 400 Bad Request with error information.</returns>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (model is null) return BadRequest(new { error = "Model is required." });

            try
            {
                // Delegates to IAuthService.RegisterAsync which handles user creation and any validation.
                var message = await _authService.RegisterAsync(model);
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                // Return the exception message in a 400 response. The service should throw meaningful exceptions.
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token on success. Accepts <see cref="LoginDto"/> which
        /// typically contains email and password. The underlying service returns a tuple:
        /// (token, userId, requires2FA). If two-factor authentication is required the endpoint responds
        /// with <c>requires2FA = true</c> and the <c>userId</c> needed to verify the second factor.
        /// </summary>
        /// <param name="model">Login request (<see cref="LoginDto"/>).</param>
        /// <returns>200 OK with either token and userId, or requires2FA and userId; 400 on error.</returns>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (model is null) return BadRequest(new { error = "Model is required." });

            try
            {
                // The service handles credential validation and token creation.
                var (token, userId, requires2FA) = await _authService.LoginAsync(model);

                if (requires2FA)
                    return Ok(new { requires2FA = true, userId });

                return Ok(new { token, userId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Verifies a two-factor authentication token for the specified user id and returns a JWT.
        /// The request body uses the local <see cref="TwoFactorDto"/> record which includes the user id
        /// and the 2FA token (code). On success a JWT string is returned.
        /// </summary>
        /// <param name="model">Two-factor payload (<see cref="TwoFactorDto"/>).</param>
        /// <returns>200 OK with token or 400 on failure.</returns>
        [AllowAnonymous]
        [HttpPost("verify-2fa")]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorDto model)
        {
            if (model is null) return BadRequest(new { error = "Model is required." });

            try
            {
                var jwt = await _authService.VerifyTwoFactorCodeAsync(model.UserId, model.Token);
                return Ok(new { token = jwt });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Initiates a password reset flow by sending a reset email to the provided address.
        /// Uses the local <see cref="EmailDto"/> which contains a single <c>Email</c> property.
        /// The underlying service should generate a reset token and send an email with a link
        /// or instructions to the user.
        /// </summary>
        /// <param name="model">Email payload (<see cref="EmailDto"/>).</param>
        /// <returns>200 OK with message or 400 on error.</returns>
        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] EmailDto model)
        {
            if (model is null || string.IsNullOrWhiteSpace(model.Email))
                return BadRequest(new { error = "Email is required." });

            try
            {
                var message = await _authService.ForgotPasswordAsync(model.Email);
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Resets the user's password using the token previously issued via email. Expects a
        /// <see cref="ResetPasswordDto"/> which contains the user id, token and new password.
        /// </summary>
        /// <param name="model">Reset payload (<see cref="ResetPasswordDto"/>).</param>
        /// <returns>200 OK with message or 400 on failure.</returns>
        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            if (model is null) return BadRequest(new { error = "Model is required." });

            try
            {
                var message = await _authService.ResetPasswordAsync(model);
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Returns details about the currently authenticated user. This endpoint requires
        /// authentication and will use the claim extracted by <see cref="GetUserId"/>.
        /// The returned object is a <see cref="UserDto"/> provided by the service.
        /// </summary>
        /// <returns>200 OK with <see cref="UserDto"/> or 401/404 as appropriate.</returns>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var user = await _authService.GetUserByIdAsync(userId);
            if (user is null) return NotFound();
            return Ok(user);
        }

        /// <summary>
        /// Updates the currently authenticated user's email and/or password. Accepts a <see cref="UserDto"/>
        /// which must include the current password for verification and the new password and/or new email.
        /// The method ensures the operation targets the authenticated user by checking the id claim.
        /// </summary>
        /// <param name="model">User update payload (<see cref="UserDto"/>).</param>
        /// <returns>200 OK on success with message from service, 400 on validation failure or 403 if id mismatch.</returns>
        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateCurrentUser([FromBody] UserDto model)
        {
            if (model is null) return BadRequest(new { error = "Model is required." });

            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            // Ensure the operation targets the authenticated user. If no id provided on the DTO,
            // set it to the id from the JWT claim. If a different id is provided, forbid the operation.
            if (string.IsNullOrWhiteSpace(model.Id))
                model.Id = userId;
            else if (!string.Equals(model.Id, userId, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            try
            {
                var result = await _authService.UpdateUserAsync(model);
                // The service returns textual messages. We look for the word "success" to treat it as successful.
                if (!string.IsNullOrWhiteSpace(result) && result.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Ok(new { message = result });

                return BadRequest(new { error = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
