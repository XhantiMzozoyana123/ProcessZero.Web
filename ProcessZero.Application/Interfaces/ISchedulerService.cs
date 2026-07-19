using ProcessZero.Application.Dtos;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface ISchedulerService
    {
        /// <summary>
        /// Schedules an SMS message to be sent at a specified time
        /// </summary>
        Task<int> ScheduleSmsAsync(ScheduleSmsDto dto);

        /// <summary>
        /// Schedules a WhatsApp message to be sent at a specified time
        /// </summary>
        Task<int> ScheduleWhatsAppAsync(ScheduleWhatsAppDto dto);

        /// <summary>
        /// Schedules a Facebook message to be sent at a specified time
        /// </summary>
        Task<int> ScheduleFacebookAsync(ScheduleFacebookDto dto);

        /// <summary>
        /// Schedules an email message to be sent at a specified time
        /// </summary>
        Task<int> ScheduleEmailAsync(ScheduleEmailDto dto);

        /// <summary>
        /// Reschedules a previously scheduled SMS message
        /// </summary>
        Task<bool> RescheduleSmsAsync(int id, DateTime newScheduledTime);

        /// <summary>
        /// Reschedules a previously scheduled WhatsApp message
        /// </summary>
        Task<bool> RescheduleWhatsAppAsync(int id, DateTime newScheduledTime);

        /// <summary>
        /// Reschedules a previously scheduled Facebook message
        /// </summary>
        Task<bool> RescheduleFacebookAsync(int id, DateTime newScheduledTime);

        /// <summary>
        /// Reschedules a previously scheduled email message
        /// </summary>
        Task<bool> RescheduleEmailAsync(int id, DateTime newScheduledTime);

        /// <summary>
        /// Cancels a scheduled SMS message
        /// </summary>
        Task<bool> CancelScheduledSmsAsync(int id);

        /// <summary>
        /// Cancels a scheduled WhatsApp message
        /// </summary>
        Task<bool> CancelScheduledWhatsAppAsync(int id);

        /// <summary>
        /// Cancels a scheduled Facebook message
        /// </summary>
        Task<bool> CancelScheduledFacebookAsync(int id);

        /// <summary>
        /// Cancels a scheduled email message
        /// </summary>
        Task<bool> CancelScheduledEmailAsync(int id);

        /// <summary>
        /// Gets all pending SMS messages scheduled for the current user
        /// </summary>
        Task<List<ScheduledMessageDetailsDto>> GetPendingSmsByUserAsync(string userId);

        /// <summary>
        /// Gets all pending WhatsApp messages scheduled for the current user
        /// </summary>
        Task<List<ScheduledMessageDetailsDto>> GetPendingWhatsAppByUserAsync(string userId);

        /// <summary>
        /// Gets all pending Facebook messages scheduled for the current user
        /// </summary>
        Task<List<ScheduledMessageDetailsDto>> GetPendingFacebookByUserAsync(string userId);

        /// <summary>
        /// Gets all pending email messages scheduled for the current user
        /// </summary>
        Task<List<ScheduledMessageDetailsDto>> GetPendingEmailsByUserAsync(string userId);

        /// <summary>
        /// Gets a scheduled SMS message by ID
        /// </summary>
        Task<ScheduledMessageDetailsDto?> GetScheduledSmsAsync(int id);

        /// <summary>
        /// Gets a scheduled WhatsApp message by ID
        /// </summary>
        Task<ScheduledMessageDetailsDto?> GetScheduledWhatsAppAsync(int id);

        /// <summary>
        /// Gets a scheduled Facebook message by ID
        /// </summary>
        Task<ScheduledMessageDetailsDto?> GetScheduledFacebookAsync(int id);

        /// <summary>
        /// Gets a scheduled email message by ID
        /// </summary>
        Task<ScheduledMessageDetailsDto?> GetScheduledEmailAsync(int id);

        /// <summary>
        /// Processes all pending scheduled messages that are due for sending
        /// This method should be called periodically by a background job
        /// </summary>
        Task ProcessPendingMessagesAsync();
    }
}
