using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Admin-only controller for sending bulk outbound messages (Email, SMS, WhatsApp, Facebook).
    ///
    /// MODELS / ENTITIES USED:
    ///
    /// 1. EmailDto (ProcessZero.Application.Dtos)
    ///    - Subject (string): email subject line
    ///    - Body (string): HTML/text message body
    ///    - RecipientEmail (string): target recipient email address
    ///    - RecipientName (string): recipient display name
    ///
    /// 2. TwilioSmsDto (ProcessZero.Application.Dtos)
    ///    - PhoneNumber (string): recipient phone number in E.164 format
    ///    - Message (string): SMS message content
    ///
    /// 3. TwilioWhatsAppDto (ProcessZero.Application.Dtos)
    ///    - PhoneNumber (string): recipient phone number
    ///    - Message (string): WhatsApp message content
    ///
    /// 4. TwilioFacebookDto (ProcessZero.Application.Dtos)
    ///    - RecipientId (string): Facebook recipient ID
    ///    - Message (string): Facebook message content
    ///
    /// 5. Inbox (ProcessZero.Domain.Entities) - used indirectly by EmailService
    ///    - Id (int): inbox primary key
    ///    - Username (string): sender email/login
    ///    - Password (string): SMTP password/app password
    ///    - SmtpHost (string): SMTP server host
    ///    - SmtpPort (int): SMTP server port
    ///    - SmtpUseSsl (bool): whether SSL/TLS is enabled
    ///    - IsPrimary (bool): marks preferred inbox for sending
    ///
    /// FLOW:
    /// - Admin posts a list of message DTOs.
    /// - Controller validates payload and delegates sending to IBlasterService.
    /// - Service sends each message through the configured pipeline (Email, SMS, WhatsApp, or Facebook).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    public class BlasterController : ControllerBase
    {
        private readonly IBlasterService _blasterService;

        public BlasterController(IBlasterService blasterService)
        {
            _blasterService = blasterService;
        }

        /// <summary>
        /// Sends bulk emails to multiple recipients
        /// </summary>
        /// <param name="emails">List of EmailDto objects containing recipient and message details</param>
        /// <returns>Success or error message</returns>
        [HttpPost("send-bulk-emails")]
        public async Task<IActionResult> SendBulkEmails([FromBody] List<EmailDto> emails)
        {
            if (emails == null || emails.Count == 0)
                return BadRequest(new { error = "At least one email is required." });

            try
            {
                await _blasterService.SendBulkEmailToUsersAsync(emails);
                return Ok(new { message = $"Bulk emails ({emails.Count}) queued successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to send bulk emails: {ex.Message}" });
            }
        }

        /// <summary>
        /// Sends bulk SMS messages to multiple recipients
        /// </summary>
        /// <param name="messages">List of TwilioSmsDto objects containing phone numbers and message content</param>
        /// <returns>Success or error message</returns>
        [HttpPost("send-bulk-sms")]
        public async Task<IActionResult> SendBulkSms([FromBody] List<TwilioSmsDto> messages)
        {
            if (messages == null || messages.Count == 0)
                return BadRequest(new { error = "At least one SMS message is required." });

            if (messages.Any(m => string.IsNullOrWhiteSpace(m.PhoneNumber) || string.IsNullOrWhiteSpace(m.Message)))
                return BadRequest(new { error = "All messages must have a phone number and message content." });

            try
            {
                await _blasterService.SendBulkSmsAsync(messages);
                return Ok(new { message = $"Bulk SMS messages ({messages.Count}) queued successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to send bulk SMS: {ex.Message}" });
            }
        }

        /// <summary>
        /// Sends bulk WhatsApp messages to multiple recipients
        /// </summary>
        /// <param name="messages">List of TwilioWhatsAppDto objects containing phone numbers and message content</param>
        /// <returns>Success or error message</returns>
        [HttpPost("send-bulk-whatsapp")]
        public async Task<IActionResult> SendBulkWhatsApp([FromBody] List<TwilioWhatsAppDto> messages)
        {
            if (messages == null || messages.Count == 0)
                return BadRequest(new { error = "At least one WhatsApp message is required." });

            if (messages.Any(m => string.IsNullOrWhiteSpace(m.PhoneNumber) || string.IsNullOrWhiteSpace(m.Message)))
                return BadRequest(new { error = "All messages must have a phone number and message content." });

            try
            {
                await _blasterService.SendBulkWhatsAppAsync(messages);
                return Ok(new { message = $"Bulk WhatsApp messages ({messages.Count}) queued successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to send bulk WhatsApp messages: {ex.Message}" });
            }
        }

        /// <summary>
        /// Sends bulk Facebook messages to multiple recipients
        /// </summary>
        /// <param name="messages">List of TwilioFacebookDto objects containing recipient IDs and message content</param>
        /// <returns>Success or error message</returns>
        [HttpPost("send-bulk-facebook")]
        public async Task<IActionResult> SendBulkFacebook([FromBody] List<TwilioFacebookDto> messages)
        {
            if (messages == null || messages.Count == 0)
                return BadRequest(new { error = "At least one Facebook message is required." });

            if (messages.Any(m => string.IsNullOrWhiteSpace(m.RecipientId) || string.IsNullOrWhiteSpace(m.Message)))
                return BadRequest(new { error = "All messages must have a recipient ID and message content." });

            try
            {
                await _blasterService.SendBulkFacebookAsync(messages);
                return Ok(new { message = $"Bulk Facebook messages ({messages.Count}) queued successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to send bulk Facebook messages: {ex.Message}" });
            }
        }

        /// <summary>
        /// Legacy endpoint - redirects to send-bulk-emails for backward compatibility
        /// </summary>
        [HttpPost("send-users")]
        public async Task<IActionResult> SendBulkToUsers([FromBody] List<EmailDto> emails)
        {
            return await SendBulkEmails(emails);
        }
    }
}

