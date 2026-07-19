using ProcessZero.Domain;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Represents a scheduled SMS message to be sent at a future time
    /// </summary>
    public class ScheduledSmsMessage : BaseEntity
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        public DateTime ScheduledAt { get; set; }

        public DateTime? SentAt { get; set; }

        [Required]
        public MessageStatus Status { get; set; } = MessageStatus.Pending;

        public string? ErrorMessage { get; set; }

        public string? TwilioSid { get; set; } // Twilio message ID
    }

    /// <summary>
    /// Represents a scheduled WhatsApp message to be sent at a future time
    /// </summary>
    public class ScheduledWhatsAppMessage : BaseEntity
    {
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        public DateTime ScheduledAt { get; set; }

        public DateTime? SentAt { get; set; }

        [Required]
        public MessageStatus Status { get; set; } = MessageStatus.Pending;

        public string? ErrorMessage { get; set; }

        public string? TwilioSid { get; set; } // Twilio message ID
    }

    /// <summary>
    /// Represents a scheduled Facebook message to be sent at a future time
    /// </summary>
    public class ScheduledFacebookMessage : BaseEntity
    {
        [Required]
        public string RecipientId { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        public DateTime ScheduledAt { get; set; }

        public DateTime? SentAt { get; set; }

        [Required]
        public MessageStatus Status { get; set; } = MessageStatus.Pending;

        public string? ErrorMessage { get; set; }

        public string? TwilioSid { get; set; } // Twilio message ID
    }

    /// <summary>
    /// Represents a scheduled Email message to be sent at a future time
    /// </summary>
    public class ScheduledEmailMessage : BaseEntity
    {
        [Required]
        [EmailAddress]
        public string RecipientEmail { get; set; } = string.Empty;

        public string? RecipientName { get; set; }

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        [Required]
        public DateTime ScheduledAt { get; set; }

        public DateTime? SentAt { get; set; }

        [Required]
        public MessageStatus Status { get; set; } = MessageStatus.Pending;

        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Status enumeration for scheduled messages
    /// </summary>
    public enum MessageStatus
    {
        Pending = 0,
        Sent = 1,
        Failed = 2,
        Cancelled = 3,
        Scheduled = 4
    }
}
