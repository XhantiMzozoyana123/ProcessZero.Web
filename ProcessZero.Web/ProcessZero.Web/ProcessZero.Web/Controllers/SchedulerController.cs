using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Admin-only controller for scheduling outbound messages (Email, SMS, WhatsApp, Facebook).
    /// Allows users to schedule messages for future delivery and manage scheduled messages.
    ///
    /// MODELS / ENTITIES USED:
    ///
    /// 1. ScheduleSmsDto - Schedule SMS
    ///    - PhoneNumber (string): recipient phone number
    ///    - Message (string): SMS content
    ///    - ScheduledAt (DateTime): when to send
    ///
    /// 2. ScheduleWhatsAppDto - Schedule WhatsApp
    ///    - PhoneNumber (string): recipient phone number
    ///    - Message (string): WhatsApp content
    ///    - ScheduledAt (DateTime): when to send
    ///
    /// 3. ScheduleFacebookDto - Schedule Facebook
    ///    - RecipientId (string): Facebook recipient ID
    ///    - Message (string): Facebook content
    ///    - ScheduledAt (DateTime): when to send
    ///
    /// 4. ScheduleEmailDto - Schedule Email
    ///    - RecipientEmail (string): recipient email address
    ///    - RecipientName (string): recipient display name
    ///    - Subject (string): email subject
    ///    - Body (string): email body
    ///    - ScheduledAt (DateTime): when to send
    ///
    /// FLOW:
    /// - Admin posts scheduled message details.
    /// - Controller validates payload and delegates to ISchedulerService.
    /// - Service stores message in database with scheduled status.
    /// - Background job processes messages when their scheduled time arrives.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    public class SchedulerController : ControllerBase
    {
        private readonly ISchedulerService _schedulerService;

        public SchedulerController(ISchedulerService schedulerService)
        {
            _schedulerService = schedulerService;
        }

        #region Schedule Endpoints

        /// <summary>
        /// Schedules an SMS message to be sent at a future time
        /// </summary>
        [HttpPost("schedule-sms")]
        public async Task<IActionResult> ScheduleSms([FromBody] ScheduleSmsDto dto)
        {
            if (dto == null)
                return BadRequest(new { error = "Request body is required." });

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber) || string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest(new { error = "Phone number and message are required." });

            if (dto.ScheduledAt <= DateTime.UtcNow)
                return BadRequest(new { error = "Scheduled time must be in the future." });

            try
            {
                var id = await _schedulerService.ScheduleSmsAsync(dto);
                return Ok(new { message = "SMS scheduled successfully.", id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to schedule SMS: {ex.Message}" });
            }
        }

        /// <summary>
        /// Schedules a WhatsApp message to be sent at a future time
        /// </summary>
        [HttpPost("schedule-whatsapp")]
        public async Task<IActionResult> ScheduleWhatsApp([FromBody] ScheduleWhatsAppDto dto)
        {
            if (dto == null)
                return BadRequest(new { error = "Request body is required." });

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber) || string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest(new { error = "Phone number and message are required." });

            if (dto.ScheduledAt <= DateTime.UtcNow)
                return BadRequest(new { error = "Scheduled time must be in the future." });

            try
            {
                var id = await _schedulerService.ScheduleWhatsAppAsync(dto);
                return Ok(new { message = "WhatsApp message scheduled successfully.", id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to schedule WhatsApp message: {ex.Message}" });
            }
        }

        /// <summary>
        /// Schedules a Facebook message to be sent at a future time
        /// </summary>
        [HttpPost("schedule-facebook")]
        public async Task<IActionResult> ScheduleFacebook([FromBody] ScheduleFacebookDto dto)
        {
            if (dto == null)
                return BadRequest(new { error = "Request body is required." });

            if (string.IsNullOrWhiteSpace(dto.RecipientId) || string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest(new { error = "Recipient ID and message are required." });

            if (dto.ScheduledAt <= DateTime.UtcNow)
                return BadRequest(new { error = "Scheduled time must be in the future." });

            try
            {
                var id = await _schedulerService.ScheduleFacebookAsync(dto);
                return Ok(new { message = "Facebook message scheduled successfully.", id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to schedule Facebook message: {ex.Message}" });
            }
        }

        /// <summary>
        /// Schedules an email message to be sent at a future time
        /// </summary>
        [HttpPost("schedule-email")]
        public async Task<IActionResult> ScheduleEmail([FromBody] ScheduleEmailDto dto)
        {
            if (dto == null)
                return BadRequest(new { error = "Request body is required." });

            if (string.IsNullOrWhiteSpace(dto.RecipientEmail) || string.IsNullOrWhiteSpace(dto.Subject) || string.IsNullOrWhiteSpace(dto.Body))
                return BadRequest(new { error = "Email, subject, and body are required." });

            if (dto.ScheduledAt <= DateTime.UtcNow)
                return BadRequest(new { error = "Scheduled time must be in the future." });

            try
            {
                var id = await _schedulerService.ScheduleEmailAsync(dto);
                return Ok(new { message = "Email scheduled successfully.", id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to schedule email: {ex.Message}" });
            }
        }

        #endregion

        #region Reschedule Endpoints

        /// <summary>
        /// Reschedules a previously scheduled SMS message to a new time
        /// </summary>
        [HttpPut("reschedule-sms/{id}")]
        public async Task<IActionResult> RescheduleSms(int id, [FromBody] RescheduleMessageDto dto)
        {
            if (dto == null || dto.NewScheduledAt == null)
                return BadRequest(new { error = "New scheduled time is required." });

            if (dto.NewScheduledAt <= DateTime.UtcNow)
                return BadRequest(new { error = "New scheduled time must be in the future." });

            try
            {
                var success = await _schedulerService.RescheduleSmsAsync(id, dto.NewScheduledAt.Value);
                return success ? Ok(new { message = "SMS rescheduled successfully." }) : NotFound(new { error = "SMS not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to reschedule SMS: {ex.Message}" });
            }
        }

        /// <summary>
        /// Reschedules a previously scheduled WhatsApp message to a new time
        /// </summary>
        [HttpPut("reschedule-whatsapp/{id}")]
        public async Task<IActionResult> RescheduleWhatsApp(int id, [FromBody] RescheduleMessageDto dto)
        {
            if (dto == null || dto.NewScheduledAt == null)
                return BadRequest(new { error = "New scheduled time is required." });

            if (dto.NewScheduledAt <= DateTime.UtcNow)
                return BadRequest(new { error = "New scheduled time must be in the future." });

            try
            {
                var success = await _schedulerService.RescheduleWhatsAppAsync(id, dto.NewScheduledAt.Value);
                return success ? Ok(new { message = "WhatsApp message rescheduled successfully." }) : NotFound(new { error = "WhatsApp message not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to reschedule WhatsApp message: {ex.Message}" });
            }
        }

        /// <summary>
        /// Reschedules a previously scheduled Facebook message to a new time
        /// </summary>
        [HttpPut("reschedule-facebook/{id}")]
        public async Task<IActionResult> RescheduleFacebook(int id, [FromBody] RescheduleMessageDto dto)
        {
            if (dto == null || dto.NewScheduledAt == null)
                return BadRequest(new { error = "New scheduled time is required." });

            if (dto.NewScheduledAt <= DateTime.UtcNow)
                return BadRequest(new { error = "New scheduled time must be in the future." });

            try
            {
                var success = await _schedulerService.RescheduleFacebookAsync(id, dto.NewScheduledAt.Value);
                return success ? Ok(new { message = "Facebook message rescheduled successfully." }) : NotFound(new { error = "Facebook message not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to reschedule Facebook message: {ex.Message}" });
            }
        }

        /// <summary>
        /// Reschedules a previously scheduled email to a new time
        /// </summary>
        [HttpPut("reschedule-email/{id}")]
        public async Task<IActionResult> RescheduleEmail(int id, [FromBody] RescheduleMessageDto dto)
        {
            if (dto == null || dto.NewScheduledAt == null)
                return BadRequest(new { error = "New scheduled time is required." });

            if (dto.NewScheduledAt <= DateTime.UtcNow)
                return BadRequest(new { error = "New scheduled time must be in the future." });

            try
            {
                var success = await _schedulerService.RescheduleEmailAsync(id, dto.NewScheduledAt.Value);
                return success ? Ok(new { message = "Email rescheduled successfully." }) : NotFound(new { error = "Email not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to reschedule email: {ex.Message}" });
            }
        }

        #endregion

        #region Cancel Endpoints

        /// <summary>
        /// Cancels a previously scheduled SMS message
        /// </summary>
        [HttpDelete("cancel-sms/{id}")]
        public async Task<IActionResult> CancelScheduledSms(int id)
        {
            try
            {
                var success = await _schedulerService.CancelScheduledSmsAsync(id);
                return success ? Ok(new { message = "SMS cancelled successfully." }) : NotFound(new { error = "SMS not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to cancel SMS: {ex.Message}" });
            }
        }

        /// <summary>
        /// Cancels a previously scheduled WhatsApp message
        /// </summary>
        [HttpDelete("cancel-whatsapp/{id}")]
        public async Task<IActionResult> CancelScheduledWhatsApp(int id)
        {
            try
            {
                var success = await _schedulerService.CancelScheduledWhatsAppAsync(id);
                return success ? Ok(new { message = "WhatsApp message cancelled successfully." }) : NotFound(new { error = "WhatsApp message not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to cancel WhatsApp message: {ex.Message}" });
            }
        }

        /// <summary>
        /// Cancels a previously scheduled Facebook message
        /// </summary>
        [HttpDelete("cancel-facebook/{id}")]
        public async Task<IActionResult> CancelScheduledFacebook(int id)
        {
            try
            {
                var success = await _schedulerService.CancelScheduledFacebookAsync(id);
                return success ? Ok(new { message = "Facebook message cancelled successfully." }) : NotFound(new { error = "Facebook message not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to cancel Facebook message: {ex.Message}" });
            }
        }

        /// <summary>
        /// Cancels a previously scheduled email
        /// </summary>
        [HttpDelete("cancel-email/{id}")]
        public async Task<IActionResult> CancelScheduledEmail(int id)
        {
            try
            {
                var success = await _schedulerService.CancelScheduledEmailAsync(id);
                return success ? Ok(new { message = "Email cancelled successfully." }) : NotFound(new { error = "Email not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to cancel email: {ex.Message}" });
            }
        }

        #endregion

        #region Get Endpoints

        /// <summary>
        /// Gets all pending SMS messages for the current user
        /// </summary>
        [HttpGet("pending-sms")]
        public async Task<IActionResult> GetPendingSms()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User ID not found in token." });

            try
            {
                var messages = await _schedulerService.GetPendingSmsByUserAsync(userId);
                return Ok(new { count = messages.Count, messages });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to retrieve SMS: {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets all pending WhatsApp messages for the current user
        /// </summary>
        [HttpGet("pending-whatsapp")]
        public async Task<IActionResult> GetPendingWhatsApp()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User ID not found in token." });

            try
            {
                var messages = await _schedulerService.GetPendingWhatsAppByUserAsync(userId);
                return Ok(new { count = messages.Count, messages });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to retrieve WhatsApp messages: {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets all pending Facebook messages for the current user
        /// </summary>
        [HttpGet("pending-facebook")]
        public async Task<IActionResult> GetPendingFacebook()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User ID not found in token." });

            try
            {
                var messages = await _schedulerService.GetPendingFacebookByUserAsync(userId);
                return Ok(new { count = messages.Count, messages });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to retrieve Facebook messages: {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets all pending emails for the current user
        /// </summary>
        [HttpGet("pending-emails")]
        public async Task<IActionResult> GetPendingEmails()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User ID not found in token." });

            try
            {
                var messages = await _schedulerService.GetPendingEmailsByUserAsync(userId);
                return Ok(new { count = messages.Count, messages });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to retrieve emails: {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets details of a specific scheduled SMS message
        /// </summary>
        [HttpGet("sms/{id}")]
        public async Task<IActionResult> GetScheduledSms(int id)
        {
            try
            {
                var message = await _schedulerService.GetScheduledSmsAsync(id);
                return message != null ? Ok(message) : NotFound(new { error = "SMS not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to retrieve SMS: {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets details of a specific scheduled WhatsApp message
        /// </summary>
        [HttpGet("whatsapp/{id}")]
        public async Task<IActionResult> GetScheduledWhatsApp(int id)
        {
            try
            {
                var message = await _schedulerService.GetScheduledWhatsAppAsync(id);
                return message != null ? Ok(message) : NotFound(new { error = "WhatsApp message not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to retrieve WhatsApp message: {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets details of a specific scheduled Facebook message
        /// </summary>
        [HttpGet("facebook/{id}")]
        public async Task<IActionResult> GetScheduledFacebook(int id)
        {
            try
            {
                var message = await _schedulerService.GetScheduledFacebookAsync(id);
                return message != null ? Ok(message) : NotFound(new { error = "Facebook message not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to retrieve Facebook message: {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets details of a specific scheduled email
        /// </summary>
        [HttpGet("email/{id}")]
        public async Task<IActionResult> GetScheduledEmail(int id)
        {
            try
            {
                var message = await _schedulerService.GetScheduledEmailAsync(id);
                return message != null ? Ok(message) : NotFound(new { error = "Email not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to retrieve email: {ex.Message}" });
            }
        }

        #endregion
    }

    /// <summary>
    /// DTO for rescheduling messages
    /// </summary>
    public class RescheduleMessageDto
    {
        public DateTime? NewScheduledAt { get; set; }
    }
}
