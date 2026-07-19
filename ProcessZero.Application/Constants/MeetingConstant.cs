using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Constants
{
    public static class MeetingConstant
    {
        /// <summary>
        /// Default meeting duration in minutes when none is specified.
        /// </summary>
        public const int DefaultDurationMinutes = 60;

        /// <summary>
        /// Minimum allowed meeting duration (minutes).
        /// </summary>
        public const int MinDurationMinutes = 5;

        /// <summary>
        /// Maximum allowed meeting duration (minutes).
        /// </summary>
        public const int MaxDurationMinutes = 240;

        /// <summary>
        /// Common allowed durations (minutes).
        /// </summary>
        public static readonly int[] AllowedDurationsMinutes = new[] { 15, 30, 45, 60, 90, 120 };

        /// <summary>
        /// Default reminder time (minutes before start) for calendar notifications.
        /// </summary>
        public const int DefaultReminderMinutesBefore = 15;

        /// <summary>
        /// Default maximum number of participants for a meeting.
        /// </summary>
        public const int DefaultMaxParticipants = 50;

        /// <summary>
        /// Template used to generate a join URL. Replace {0} with the meeting id.
        /// </summary>
        public const string DefaultJoinUrlTemplate = "https://meet.processzero.com/{0}";

        /// <summary>
        /// Build a default meeting platform name shown in notifications.
        /// Accepts the contact, product and user (sales rep) so the returned name
        /// can include contextual information like product name or rep username.
        /// </summary>
        public static string DefaultPlatformName(Contact? contact, Product? product, ApplicationUser? user)
        {
            var baseName = "ProcessZero Meetings";

            var parts = new List<string> { baseName };

            if (product is not null && !string.IsNullOrWhiteSpace(product.Name))
                parts.Add(product.Name);

            if (contact is not null)
            {
                var contactName = (contact.FirstName + " " + contact.LastName).Trim();
                if (!string.IsNullOrWhiteSpace(contactName)) parts.Add(contactName);
            }

            if (user is not null && !string.IsNullOrWhiteSpace(user.UserName))
                parts.Add(user.UserName);

            return string.Join(" - ", parts);
        }

        /// <summary>
        /// When true, meetings are recorded by default (if supported by provider).
        /// </summary>
        public const bool DefaultAutoRecord = false;

        /// <summary>
        /// Regex pattern used to validate meeting id tokens (alphanumeric, dashes, 8-64 chars).
        /// </summary>
        public const string MeetingIdPattern = "^[A-Za-z0-9\\-]{8,64}$";

        /// <summary>
        /// Builds a join URL from the configured template and meeting id.
        /// </summary>
        public static string BuildJoinUrl(string meetingId)
        {
            if (string.IsNullOrWhiteSpace(meetingId)) return string.Empty;
            return string.Format(DefaultJoinUrlTemplate, meetingId);
        }

    }
}
