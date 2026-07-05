using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    public class SchedulerService : ISchedulerService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITwilioService _twilioService;
        private readonly IEmailService _emailService;
        private readonly ILogger<SchedulerService> _logger;

        public SchedulerService(
            ApplicationDbContext context,
            ITwilioService twilioService,
            IEmailService emailService,
            ILogger<SchedulerService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _twilioService = twilioService ?? throw new ArgumentNullException(nameof(twilioService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Schedule Methods

        public async Task<int> ScheduleSmsAsync(ScheduleSmsDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.PhoneNumber)) throw new ArgumentException("Phone number is required", nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Message)) throw new ArgumentException("Message is required", nameof(dto));
            if (dto.ScheduledAt <= DateTime.UtcNow) throw new ArgumentException("Scheduled time must be in the future", nameof(dto));

            var scheduledMessage = new ScheduledSmsMessage
            {
                PhoneNumber = dto.PhoneNumber,
                Message = dto.Message,
                ScheduledAt = dto.ScheduledAt,
                Status = MessageStatus.Scheduled,
                UserId = string.Empty // Will be set by controller
            };

            _context.Set<ScheduledSmsMessage>().Add(scheduledMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"SMS scheduled with ID {scheduledMessage.Id} for {dto.ScheduledAt}");
            return scheduledMessage.Id;
        }

        public async Task<int> ScheduleWhatsAppAsync(ScheduleWhatsAppDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.PhoneNumber)) throw new ArgumentException("Phone number is required", nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Message)) throw new ArgumentException("Message is required", nameof(dto));
            if (dto.ScheduledAt <= DateTime.UtcNow) throw new ArgumentException("Scheduled time must be in the future", nameof(dto));

            var scheduledMessage = new ScheduledWhatsAppMessage
            {
                PhoneNumber = dto.PhoneNumber,
                Message = dto.Message,
                ScheduledAt = dto.ScheduledAt,
                Status = MessageStatus.Scheduled,
                UserId = string.Empty // Will be set by controller
            };

            _context.Set<ScheduledWhatsAppMessage>().Add(scheduledMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"WhatsApp message scheduled with ID {scheduledMessage.Id} for {dto.ScheduledAt}");
            return scheduledMessage.Id;
        }

        public async Task<int> ScheduleFacebookAsync(ScheduleFacebookDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.RecipientId)) throw new ArgumentException("Recipient ID is required", nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Message)) throw new ArgumentException("Message is required", nameof(dto));
            if (dto.ScheduledAt <= DateTime.UtcNow) throw new ArgumentException("Scheduled time must be in the future", nameof(dto));

            var scheduledMessage = new ScheduledFacebookMessage
            {
                RecipientId = dto.RecipientId,
                Message = dto.Message,
                ScheduledAt = dto.ScheduledAt,
                Status = MessageStatus.Scheduled,
                UserId = string.Empty // Will be set by controller
            };

            _context.Set<ScheduledFacebookMessage>().Add(scheduledMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Facebook message scheduled with ID {scheduledMessage.Id} for {dto.ScheduledAt}");
            return scheduledMessage.Id;
        }

        public async Task<int> ScheduleEmailAsync(ScheduleEmailDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.RecipientEmail)) throw new ArgumentException("Recipient email is required", nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Subject)) throw new ArgumentException("Subject is required", nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Body)) throw new ArgumentException("Body is required", nameof(dto));
            if (dto.ScheduledAt <= DateTime.UtcNow) throw new ArgumentException("Scheduled time must be in the future", nameof(dto));

            var scheduledMessage = new ScheduledEmailMessage
            {
                RecipientEmail = dto.RecipientEmail,
                RecipientName = dto.RecipientName,
                Subject = dto.Subject,
                Body = dto.Body,
                ScheduledAt = dto.ScheduledAt,
                Status = MessageStatus.Scheduled,
                UserId = string.Empty // Will be set by controller
            };

            _context.Set<ScheduledEmailMessage>().Add(scheduledMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Email scheduled with ID {scheduledMessage.Id} for {dto.ScheduledAt}");
            return scheduledMessage.Id;
        }

        #endregion

        #region Reschedule Methods

        public async Task<bool> RescheduleSmsAsync(int id, DateTime newScheduledTime)
        {
            if (newScheduledTime <= DateTime.UtcNow)
                throw new ArgumentException("Scheduled time must be in the future", nameof(newScheduledTime));

            var message = await _context.Set<ScheduledSmsMessage>().FindAsync(id);
            if (message == null) return false;

            if (message.Status != MessageStatus.Scheduled && message.Status != MessageStatus.Pending)
                throw new InvalidOperationException("Cannot reschedule a message that has already been sent or cancelled");

            message.ScheduledAt = newScheduledTime;
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"SMS with ID {id} rescheduled to {newScheduledTime}");
            return true;
        }

        public async Task<bool> RescheduleWhatsAppAsync(int id, DateTime newScheduledTime)
        {
            if (newScheduledTime <= DateTime.UtcNow)
                throw new ArgumentException("Scheduled time must be in the future", nameof(newScheduledTime));

            var message = await _context.Set<ScheduledWhatsAppMessage>().FindAsync(id);
            if (message == null) return false;

            if (message.Status != MessageStatus.Scheduled && message.Status != MessageStatus.Pending)
                throw new InvalidOperationException("Cannot reschedule a message that has already been sent or cancelled");

            message.ScheduledAt = newScheduledTime;
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"WhatsApp message with ID {id} rescheduled to {newScheduledTime}");
            return true;
        }

        public async Task<bool> RescheduleFacebookAsync(int id, DateTime newScheduledTime)
        {
            if (newScheduledTime <= DateTime.UtcNow)
                throw new ArgumentException("Scheduled time must be in the future", nameof(newScheduledTime));

            var message = await _context.Set<ScheduledFacebookMessage>().FindAsync(id);
            if (message == null) return false;

            if (message.Status != MessageStatus.Scheduled && message.Status != MessageStatus.Pending)
                throw new InvalidOperationException("Cannot reschedule a message that has already been sent or cancelled");

            message.ScheduledAt = newScheduledTime;
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Facebook message with ID {id} rescheduled to {newScheduledTime}");
            return true;
        }

        public async Task<bool> RescheduleEmailAsync(int id, DateTime newScheduledTime)
        {
            if (newScheduledTime <= DateTime.UtcNow)
                throw new ArgumentException("Scheduled time must be in the future", nameof(newScheduledTime));

            var message = await _context.Set<ScheduledEmailMessage>().FindAsync(id);
            if (message == null) return false;

            if (message.Status != MessageStatus.Scheduled && message.Status != MessageStatus.Pending)
                throw new InvalidOperationException("Cannot reschedule a message that has already been sent or cancelled");

            message.ScheduledAt = newScheduledTime;
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Email with ID {id} rescheduled to {newScheduledTime}");
            return true;
        }

        #endregion

        #region Cancel Methods

        public async Task<bool> CancelScheduledSmsAsync(int id)
        {
            var message = await _context.Set<ScheduledSmsMessage>().FindAsync(id);
            if (message == null) return false;

            if (message.Status == MessageStatus.Sent)
                throw new InvalidOperationException("Cannot cancel a message that has already been sent");

            message.Status = MessageStatus.Cancelled;
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"SMS with ID {id} cancelled");
            return true;
        }

        public async Task<bool> CancelScheduledWhatsAppAsync(int id)
        {
            var message = await _context.Set<ScheduledWhatsAppMessage>().FindAsync(id);
            if (message == null) return false;

            if (message.Status == MessageStatus.Sent)
                throw new InvalidOperationException("Cannot cancel a message that has already been sent");

            message.Status = MessageStatus.Cancelled;
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"WhatsApp message with ID {id} cancelled");
            return true;
        }

        public async Task<bool> CancelScheduledFacebookAsync(int id)
        {
            var message = await _context.Set<ScheduledFacebookMessage>().FindAsync(id);
            if (message == null) return false;

            if (message.Status == MessageStatus.Sent)
                throw new InvalidOperationException("Cannot cancel a message that has already been sent");

            message.Status = MessageStatus.Cancelled;
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Facebook message with ID {id} cancelled");
            return true;
        }

        public async Task<bool> CancelScheduledEmailAsync(int id)
        {
            var message = await _context.Set<ScheduledEmailMessage>().FindAsync(id);
            if (message == null) return false;

            if (message.Status == MessageStatus.Sent)
                throw new InvalidOperationException("Cannot cancel a message that has already been sent");

            message.Status = MessageStatus.Cancelled;
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Email with ID {id} cancelled");
            return true;
        }

        #endregion

        #region Get Methods

        public async Task<List<ScheduledMessageDetailsDto>> GetPendingSmsByUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required", nameof(userId));

            var messages = await _context.Set<ScheduledSmsMessage>()
                .Where(m => m.UserId == userId && (m.Status == MessageStatus.Scheduled || m.Status == MessageStatus.Pending))
                .ToListAsync();

            return messages.Select(MapToDetailsDto).ToList();
        }

        public async Task<List<ScheduledMessageDetailsDto>> GetPendingWhatsAppByUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required", nameof(userId));

            var messages = await _context.Set<ScheduledWhatsAppMessage>()
                .Where(m => m.UserId == userId && (m.Status == MessageStatus.Scheduled || m.Status == MessageStatus.Pending))
                .ToListAsync();

            return messages.Select(MapToDetailsDto).ToList();
        }

        public async Task<List<ScheduledMessageDetailsDto>> GetPendingFacebookByUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required", nameof(userId));

            var messages = await _context.Set<ScheduledFacebookMessage>()
                .Where(m => m.UserId == userId && (m.Status == MessageStatus.Scheduled || m.Status == MessageStatus.Pending))
                .ToListAsync();

            return messages.Select(MapToDetailsDto).ToList();
        }

        public async Task<List<ScheduledMessageDetailsDto>> GetPendingEmailsByUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required", nameof(userId));

            var messages = await _context.Set<ScheduledEmailMessage>()
                .Where(m => m.UserId == userId && (m.Status == MessageStatus.Scheduled || m.Status == MessageStatus.Pending))
                .ToListAsync();

            return messages.Select(MapToDetailsDto).ToList();
        }

        public async Task<ScheduledMessageDetailsDto?> GetScheduledSmsAsync(int id)
        {
            var message = await _context.Set<ScheduledSmsMessage>().FindAsync(id);
            return message != null ? MapToDetailsDto(message) : null;
        }

        public async Task<ScheduledMessageDetailsDto?> GetScheduledWhatsAppAsync(int id)
        {
            var message = await _context.Set<ScheduledWhatsAppMessage>().FindAsync(id);
            return message != null ? MapToDetailsDto(message) : null;
        }

        public async Task<ScheduledMessageDetailsDto?> GetScheduledFacebookAsync(int id)
        {
            var message = await _context.Set<ScheduledFacebookMessage>().FindAsync(id);
            return message != null ? MapToDetailsDto(message) : null;
        }

        public async Task<ScheduledMessageDetailsDto?> GetScheduledEmailAsync(int id)
        {
            var message = await _context.Set<ScheduledEmailMessage>().FindAsync(id);
            return message != null ? MapToDetailsDto(message) : null;
        }

        #endregion

        #region Background Processing

        public async Task ProcessPendingMessagesAsync()
        {
            try
            {
                var now = DateTime.UtcNow;

                // Process pending SMS messages
                await ProcessPendingSmsMessagesAsync(now);

                // Process pending WhatsApp messages
                await ProcessPendingWhatsAppMessagesAsync(now);

                // Process pending Facebook messages
                await ProcessPendingFacebookMessagesAsync(now);

                // Process pending emails
                await ProcessPendingEmailMessagesAsync(now);

                _logger.LogInformation("Scheduled message processing completed");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing scheduled messages: {ex.Message}");
            }
        }

        private async Task ProcessPendingSmsMessagesAsync(DateTime now)
        {
            var messages = await _context.Set<ScheduledSmsMessage>()
                .Where(m => m.ScheduledAt <= now && (m.Status == MessageStatus.Scheduled || m.Status == MessageStatus.Pending))
                .ToListAsync();

            foreach (var message in messages)
            {
                try
                {
                    var smsDto = new TwilioSmsDto
                    {
                        PhoneNumber = message.PhoneNumber,
                        Message = message.Message
                    };

                    await _twilioService.SendSmsAsync(smsDto);

                    message.Status = MessageStatus.Sent;
                    message.SentAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    message.Status = MessageStatus.Failed;
                    message.ErrorMessage = ex.Message;
                    _logger.LogError($"Failed to send SMS {message.Id}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task ProcessPendingWhatsAppMessagesAsync(DateTime now)
        {
            var messages = await _context.Set<ScheduledWhatsAppMessage>()
                .Where(m => m.ScheduledAt <= now && (m.Status == MessageStatus.Scheduled || m.Status == MessageStatus.Pending))
                .ToListAsync();

            foreach (var message in messages)
            {
                try
                {
                    var whatsAppDto = new TwilioWhatsAppDto
                    {
                        PhoneNumber = message.PhoneNumber,
                        Message = message.Message
                    };

                    await _twilioService.SendWhatsAppAsync(whatsAppDto);

                    message.Status = MessageStatus.Sent;
                    message.SentAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    message.Status = MessageStatus.Failed;
                    message.ErrorMessage = ex.Message;
                    _logger.LogError($"Failed to send WhatsApp {message.Id}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task ProcessPendingFacebookMessagesAsync(DateTime now)
        {
            var messages = await _context.Set<ScheduledFacebookMessage>()
                .Where(m => m.ScheduledAt <= now && (m.Status == MessageStatus.Scheduled || m.Status == MessageStatus.Pending))
                .ToListAsync();

            foreach (var message in messages)
            {
                try
                {
                    var facebookDto = new TwilioFacebookDto
                    {
                        RecipientId = message.RecipientId,
                        Message = message.Message
                    };

                    await _twilioService.SendFacebookMessageAsync(facebookDto);

                    message.Status = MessageStatus.Sent;
                    message.SentAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    message.Status = MessageStatus.Failed;
                    message.ErrorMessage = ex.Message;
                    _logger.LogError($"Failed to send Facebook message {message.Id}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task ProcessPendingEmailMessagesAsync(DateTime now)
        {
            var messages = await _context.Set<ScheduledEmailMessage>()
                .Where(m => m.ScheduledAt <= now && (m.Status == MessageStatus.Scheduled || m.Status == MessageStatus.Pending))
                .ToListAsync();

            foreach (var message in messages)
            {
                try
                {
                    var emailDto = new EmailDto
                    {
                        RecipientEmail = message.RecipientEmail,
                        RecipientName = message.RecipientName ?? string.Empty,
                        Subject = message.Subject,
                        Body = message.Body
                    };

                    await _emailService.SendEmailAsync(emailDto);

                    message.Status = MessageStatus.Sent;
                    message.SentAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    message.Status = MessageStatus.Failed;
                    message.ErrorMessage = ex.Message;
                    _logger.LogError($"Failed to send email {message.Id}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
        }

        #endregion

        #region Helper Methods

        private ScheduledMessageDetailsDto MapToDetailsDto<T>(T message) where T : BaseEntity
        {
            var dto = new ScheduledMessageDetailsDto
            {
                Id = message.Id,
                CreatedAt = message.CreatedAt,
                UpdatedAt = message.UpdatedAt
            };

            if (message is ScheduledSmsMessage sms)
            {
                dto.Content = sms.Message;
                dto.ScheduledAt = sms.ScheduledAt;
                dto.SentAt = sms.SentAt;
                dto.Status = sms.Status;
                dto.ErrorMessage = sms.ErrorMessage;
            }
            else if (message is ScheduledWhatsAppMessage whatsapp)
            {
                dto.Content = whatsapp.Message;
                dto.ScheduledAt = whatsapp.ScheduledAt;
                dto.SentAt = whatsapp.SentAt;
                dto.Status = whatsapp.Status;
                dto.ErrorMessage = whatsapp.ErrorMessage;
            }
            else if (message is ScheduledFacebookMessage facebook)
            {
                dto.Content = facebook.Message;
                dto.ScheduledAt = facebook.ScheduledAt;
                dto.SentAt = facebook.SentAt;
                dto.Status = facebook.Status;
                dto.ErrorMessage = facebook.ErrorMessage;
            }
            else if (message is ScheduledEmailMessage email)
            {
                dto.Content = $"{email.Subject}: {email.Body}";
                dto.ScheduledAt = email.ScheduledAt;
                dto.SentAt = email.SentAt;
                dto.Status = email.Status;
                dto.ErrorMessage = email.ErrorMessage;
            }

            return dto;
        }

        #endregion
    }
}
