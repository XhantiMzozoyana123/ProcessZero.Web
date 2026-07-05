using ProcessZero.Domain.Entities;
using System;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// DTO for scheduling an SMS message
    /// </summary>
    public class ScheduleSmsDto
    {
        public string PhoneNumber { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime ScheduledAt { get; set; }
    }

    /// <summary>
    /// DTO for scheduling a WhatsApp message
    /// </summary>
    public class ScheduleWhatsAppDto
    {
        public string PhoneNumber { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime ScheduledAt { get; set; }
    }

    /// <summary>
    /// DTO for scheduling a Facebook message
    /// </summary>
    public class ScheduleFacebookDto
    {
        public string RecipientId { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime ScheduledAt { get; set; }
    }

    /// <summary>
    /// DTO for scheduling an Email message
    /// </summary>
    public class ScheduleEmailDto
    {
        public string RecipientEmail { get; set; } = string.Empty;

        public string RecipientName { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public DateTime ScheduledAt { get; set; }
    }

    /// <summary>
    /// DTO for retrieving scheduled message details
    /// </summary>
    public class ScheduledMessageDetailsDto
    {
        public int Id { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime ScheduledAt { get; set; }

        public DateTime? SentAt { get; set; }

        public MessageStatus Status { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
