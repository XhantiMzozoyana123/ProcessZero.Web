using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Dtos
{
    // <summary>
    /// Represents an email message received from Gmail.
    /// Used for inbox tracking, reply detection, and campaign automation.
    /// </summary>
    public class ReceivedEmailMessageDto
    {
        /// <summary>
        /// Unique Gmail message ID (single email instance)
        /// </summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// Gmail conversation thread ID (CRITICAL for follow-ups + reply tracking)
        /// </summary>
        public string ThreadId { get; set; } = string.Empty;

        public string From { get; set; } = string.Empty;

        public string To { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// When Gmail says the message was received
        /// </summary>
        public DateTime ReceivedDate { get; set; }

        /// <summary>
        /// Gmail label system (UNREAD, INBOX, SPAM, etc.)
        /// </summary>
        public List<string> Labels { get; set; } = new();

        /// <summary>
        /// Whether message is unread
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// Helpful for campaign tracking (optional but powerful)
        /// </summary>
        public bool IsReply { get; set; }
    }
}
