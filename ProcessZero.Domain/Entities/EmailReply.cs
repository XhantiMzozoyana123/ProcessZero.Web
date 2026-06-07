using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Tracks email replies received from leads in the LeadLake through the Relay system.
    /// </summary>
    public class RelayEmailReply : BaseEntity
    {
        /// <summary>
        /// Reference to the RelayEmailAccount that received this reply.
        /// </summary>
        public int RelayEmailAccountId { get; set; }
        public RelayEmailAccount? RelayEmailAccount { get; set; }

        /// <summary>
        /// Reference to the LeadLake who sent the reply.
        /// </summary>
        public int LeadLakeId { get; set; }
        public LeadLake? Lead { get; set; }

        /// <summary>
        /// Gmail message ID for tracking and reference.
        /// </summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// The lead's email address (denormalized for queries).
        /// </summary>
        public string FromEmail { get; set; } = string.Empty;

        /// <summary>
        /// Subject of the reply.
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Body/content of the reply.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// When the email was received.
        /// </summary>
        public DateTime ReceivedDate { get; set; }

        /// <summary>
        /// Whether this reply has been read/processed by the user.
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// User who owns this inbox/relay account.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Tags for categorizing replies (e.g., "interested", "not-interested", "follow-up-needed").
        /// </summary>
        public string Tags { get; set; } = string.Empty; // Comma-separated
    }
}
