using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Interfaces;

namespace ProcessZero.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/user
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        // GET: api/user/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("id is required");

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();

            return Ok(user);
        }

        /// <summary>
        /// Returns users that are currently banned.
        /// </summary>
        [HttpGet("banned")]
        public async Task<IActionResult> GetBannedUsers(CancellationToken cancellationToken)
        {
            var users = await _userService.GetBannedUsersAsync(cancellationToken);
            return Ok(users);
        }

        /// <summary>
        /// Ban a user account. Accepts an optional reason in the request body.
        /// </summary>
        [HttpPost("{id}/ban")]
        public async Task<IActionResult> BanUser(string id, [FromBody] BanDto? dto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("id is required");

            await _userService.BanUserAsync(id, dto?.Reason, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Unban a user account.
        /// </summary>
        [HttpPost("{id}/unban")]
        public async Task<IActionResult> UnbanUser(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("id is required");

            await _userService.UnbanUserAsync(id, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Check whether a user is banned.
        /// </summary>
        [HttpGet("{id}/is-banned")]
        public async Task<IActionResult> IsUserBanned(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("id is required");

            var banned = await _userService.IsUserBannedAsync(id, cancellationToken);
            return Ok(new { id, isBanned = banned });
        }
    }

    public sealed record BanDto(string? Reason);
}
