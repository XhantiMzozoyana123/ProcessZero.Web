using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain.Entities;
using System.Security.Claims;

namespace ProcessZero.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    /// <summary>
    /// Controller responsible for managing inbox configurations (email accounts) in the system.
    /// Only users with the "Admin" policy may call these endpoints.
    ///
    /// Models and columns involved:
    ///
    /// Inbox (inherits BaseEntity):
    /// - Id (int)                      : Primary key (from BaseEntity)
    /// - UserId (string)               : Owner (AspNetUsers.Id) — which user created/owns this inbox
    /// - CreatedAt (DateTime)          : Creation timestamp (UTC)
    /// - UpdatedAt (DateTime)          : Last update timestamp (UTC)
    /// - Username (string)             : Login name/email used for SMTP/IMAP authentication
    /// - Password (string)             : Login password (sensitive — consider encrypting or storing in a secrets store)
    /// - SmtpHost (string)             : SMTP server host for sending
    /// - SmtpPort (int)                : SMTP server port
    /// - SmtpUseSsl (bool)             : Whether to use SSL/TLS for SMTP
    /// - ImapHost (string)             : IMAP server host for receiving
    /// - ImapPort (int)                : IMAP server port
    /// - ImapUseSsl (bool)             : Whether to use SSL/TLS for IMAP
    /// - IsPrimary (bool)              : Marks the inbox as the primary outbound inbox for the user
    ///
    /// Service used:
    /// - IInboxService: provides CRUD methods (CreateInboxAsync, UpdateInboxAsync, DeleteInboxAsync,
    ///   GetInboxByIdAsync, GetInboxesByUserIdAsync, GetAllInboxesAsync).
    ///
    /// Security notes:
    /// - Passwords are currently stored on the entity; do not return or log passwords in production.
    /// - Consider field-level encryption or a secure key/value store for credentials.
    /// </summary>
    public class InboxController : ControllerBase
    {
        private readonly IInboxService _inboxService;

        public InboxController(IInboxService inboxService)
        {
            _inboxService = inboxService;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Inbox model)
        {
            if (model == null) return BadRequest("Inbox model is required.");

            model.UserId = GetUserId();
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            var id = await _inboxService.CreateInboxAsync(model);

            return CreatedAtAction(nameof(GetById), new { id }, model);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Inbox model)
        {
            if (model == null) return BadRequest("Inbox model is required.");
            if (id != model.Id) return BadRequest("Id mismatch.");

            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            model.UserId = userId;

            try
            {
                await _inboxService.UpdateInboxAsync(model);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            var list = await _inboxService.GetInboxesByUserIdAsync(userId);
            return Ok(list);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetByUserId()
        {
            var userId = GetUserId();
            var list = await _inboxService.GetInboxesByUserIdAsync(userId);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var inbox = await _inboxService.GetInboxByIdAsync(id);
            if (inbox == null) return NotFound();

            // Ensure owner
            var userId = GetUserId();
            if (!string.Equals(inbox.UserId, userId, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            return Ok(inbox);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var inbox = await _inboxService.GetInboxByIdAsync(id);
            if (inbox == null) return NotFound();

            var userId = GetUserId();
            if (!string.Equals(inbox.UserId, userId, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            await _inboxService.DeleteInboxAsync(id);
            return NoContent();
        }
    }
}
