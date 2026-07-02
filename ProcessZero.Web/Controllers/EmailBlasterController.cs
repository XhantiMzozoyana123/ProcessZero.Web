using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Admin-only controller for sending bulk outbound emails.
    ///
    /// MODELS / ENTITIES USED:
    ///
    /// 1. EmailDto (ProcessZero.Application.Dtos)
    ///    - Subject (string): email subject line
    ///    - Body (string): HTML/text message body
    ///    - RecipientEmail (string): target recipient email address
    ///    - RecipientName (string): recipient display name
    ///
    /// 2. Inbox (ProcessZero.Domain.Entities) - used indirectly by EmailService
    ///    - Id (int): inbox primary key
    ///    - Username (string): sender email/login
    ///    - Password (string): SMTP password/app password
    ///    - SmtpHost (string): SMTP server host
    ///    - SmtpPort (int): SMTP server port
    ///    - SmtpUseSsl (bool): whether SSL/TLS is enabled
    ///    - IsPrimary (bool): marks preferred inbox for sending
    ///
    /// FLOW:
    /// - Admin posts a list of EmailDto items.
    /// - Controller validates payload and delegates sending to IEmailBlasterService.
    /// - Service sends each message through the configured email pipeline.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    public class EmailBlasterController : ControllerBase
    {
        private readonly IEmailBlasterService _emailBlasterService;

        public EmailBlasterController(IEmailBlasterService emailBlasterService)
        {
            _emailBlasterService = emailBlasterService;
        }

        [HttpPost("send-users")]
        public async Task<IActionResult> SendBulkToUsers([FromBody] List<EmailDto> emails)
        {
            if (emails == null || emails.Count == 0)
                return BadRequest(new { error = "At least one email is required." });

            await _emailBlasterService.SendBulkEmailToUsersAsync(emails);
            return Ok(new { message = "Bulk user emails queued successfully." });
        }

    }
}
