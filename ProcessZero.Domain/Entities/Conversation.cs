using System;
using System.Collections.Generic;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Represents a conversation between two users or an announcement broadcast.
    /// Each conversation belongs to exactly two users (UserOne and UserTwo).
    /// For announcements (IsAnnouncement=true), UserOne is typically an admin user.
    /// </summary>
    /// <remarks>
    /// DATABASE TABLE: Conversations
    /// 
    /// COLUMNS:
    /// - Id (int): Primary key, auto-incremented. Inherited from BaseEntity.
    /// - UserOneId (string, 450 chars): First participant's user ID. Indexed as IX_Conversations_UserOneId.
    /// - UserTwoId (string, 450 chars): Second participant's user ID. Indexed as IX_Conversations_UserTwoId.
    /// - IsPinned (bool, default false): Pinned conversations appear at the top of the user's conversation list.
    /// - IsAnnouncement (bool, default false): True if this is an announcement broadcast from admin.
    /// - LastMessageAt (DateTime?, nullable): Timestamp of the most recent message. Updated on each new message.
    /// - CreatedAt (DateTime): When the conversation was created. Inherited from BaseEntity.
    /// - UpdatedAt (DateTime): When the conversation was last updated. Inherited from BaseEntity.
    /// 
    /// INDEXES:
    /// - IX_Conversations_UserOneId: For querying conversations by first participant
    /// - IX_Conversations_UserTwoId: For querying conversations by second participant
    /// - IX_Conversations_LastMessageAt: For ordering conversations by recency
    /// 
    /// RELATIONSHIPS:
    /// - Messages: Collection of all messages in this conversation (one-to-many)
    /// </remarks>
    public class Conversation : BaseEntity
    {
        /// <summary>
        /// First participant's user ID. Links to ApplicationUser.Id (string, max 450 chars for Identity).
        /// </summary>
        public string UserOneId { get; set; } = string.Empty;

        /// <summary>
        /// Second participant's user ID. Links to ApplicationUser.Id.
        /// </summary>
        public string UserTwoId { get; set; } = string.Empty;

        /// <summary>
        /// Whether this conversation is pinned to the top of the user's conversation list.
        /// Pinned conversations appear before unpinned ones in the UI.
        /// </summary>
        public bool IsPinned { get; set; } = false;

        /// <summary>
        /// Whether this is an announcement conversation.
        /// Announcement conversations are created by admins for broadcasts to all users.
        /// </summary>
        public bool IsAnnouncement { get; set; } = false;

        /// <summary>
        /// Timestamp of the most recent message in this conversation.
        /// Used for ordering conversations by activity. Null until first message is sent.
        /// </summary>
        public DateTime? LastMessageAt { get; set; }

        // Navigation properties
        /// <summary>
        /// All messages in this conversation, loaded via Entity Framework Include().
        /// Ordered by SentAt ascending (oldest first) in queries.
        /// </summary>
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }

    /// <summary>
    /// Represents a single message within a conversation.
    /// All users in the conversation can read the message; Read flag tracks read status per message.
    /// </summary>
    /// <remarks>
    /// DATABASE TABLE: Messages
    /// 
    /// COLUMNS:
    /// - Id (int): Primary key, auto-incremented. Inherited from BaseEntity.
    /// - ConversationId (int): Foreign key to Conversations.Id. Indexed as IX_Messages_ConversationId.
    /// - SenderId (string, 450 chars): User ID of the message sender. Indexed as IX_Messages_SenderId.
    /// - Text (string): The message content. No length limit specified.
    /// - SentAt (DateTime, default UtcNow): When the message was sent. Indexed as IX_Messages_SentAt.
    /// - Read (bool, default false): Whether this message has been marked as read.
    /// - CreatedAt (DateTime): When the message was created. Inherited from BaseEntity.
    /// - UpdatedAt (DateTime): When the message was last updated. Inherited from BaseEntity.
    /// 
    /// INDEXES:
    /// - IX_Messages_ConversationId: For loading all messages in a conversation
    /// - IX_Messages_SenderId: For finding all messages sent by a user
    /// - IX_Messages_SentAt: For ordering messages chronologically
    /// 
    /// RELATIONSHIPS:
    /// - Conversation: The parent conversation (many-to-one)
    /// </remarks>
    public class Message : BaseEntity
    {
        /// <summary>
        /// Foreign key to the parent Conversation.
        /// All users in the conversation (UserOne and UserTwo) can read this message.
        /// </summary>
        public int ConversationId { get; set; }

        /// <summary>
        /// User ID of the message sender. Links to ApplicationUser.Id.
        /// </summary>
        public string SenderId { get; set; } = string.Empty;

        /// <summary>
        /// The message content/text.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// When the message was sent. Defaults to DateTime.UtcNow.
        /// Used for sorting messages chronologically within a conversation.
        /// </summary>
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether this message has been read by the recipient.
        /// Set to true when recipient views the conversation.
        /// Also used to calculate UnreadCount for conversations.
        /// </summary>
        public bool Read { get; set; } = false;

        // Navigation properties
        /// <summary>
        /// The parent conversation this message belongs to.
        /// Loaded via Entity Framework Include() when needed.
        /// </summary>
        public Conversation? Conversation { get; set; }
    }
}
