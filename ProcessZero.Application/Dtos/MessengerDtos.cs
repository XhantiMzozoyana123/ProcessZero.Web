using System;
using System.Collections.Generic;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// Data Transfer Object for Conversation entity returned to API clients.
    /// Contains conversation data plus computed fields for display purposes.
    /// </summary>
    /// <remarks>
    /// PROPERTIES USED BY ANGULAR FRONTEND:
    /// - Id: Unique conversation identifier
    /// - OtherUserName: Display name of the other participant (not the current user)
    /// - OtherUserEmail: Email of the other participant for avatar/initials
    /// - UnreadCount: Number of unread messages (computed in MessengerService)
    /// - LastMessagePreview: Preview text of the most recent message
    /// - IsPinned: Whether to show conversation at the top
    /// - IsAnnouncement: Whether this is an admin announcement
    /// - LastMessageAt: Sort key for conversation list ordering
    /// 
    /// USER IDENTIFICATION FLOW:
    /// When loading conversations, the service determines which participant is "other"
    /// by comparing UserOneId and UserTwoId with the current authenticated user's ID.
    /// </remarks>
    public class ConversationDto
    {
        /// <summary>
        /// Unique conversation identifier (primary key).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// First participant's user ID. Included for reference but not typically displayed.
        /// </summary>
        public string UserOneId { get; set; } = string.Empty;

        /// <summary>
        /// Second participant's user ID. Included for reference but not typically displayed.
        /// </summary>
        public string UserTwoId { get; set; } = string.Empty;

        /// <summary>
        /// Whether this conversation is pinned. Pinned conversations appear at the top
        /// of the conversation list in the Angular frontend.
        /// </summary>
        public bool IsPinned { get; set; }

        /// <summary>
        /// Whether this is an announcement conversation. These are created automatically
        /// when an admin broadcasts a message to all users.
        /// </summary>
        public bool IsAnnouncement { get; set; }

        /// <summary>
        /// Timestamp of the most recent message. Used for sorting conversations
        /// by activity (most recent first).
        /// </summary>
        public DateTime? LastMessageAt { get; set; }

        /// <summary>
        /// Number of unread messages in this conversation for the current user.
        /// Computed in MessengerService.GetUserConversationsAsync() by counting messages
        /// where Read=false and SenderId != current user.
        /// </summary>
        public int UnreadCount { get; set; }

        /// <summary>
        /// Display name of the other participant (not the current authenticated user).
        /// Used in the Angular frontend for showing conversation partner names.
        /// </summary>
        public string OtherUserName { get; set; } = string.Empty;

        /// <summary>
        /// Email address of the other participant.
        /// Used in the Angular frontend for avatar generation or email display.
        /// </summary>
        public string OtherUserEmail { get; set; } = string.Empty;

        /// <summary>
        /// Whether the other user is currently online.
        /// Updated in real-time via SignalR "UserOnline"/"UserOffline" events.
        /// Initially set to false by the API; frontend updates based on hub connection status.
        /// </summary>
        public bool OtherUserOnline { get; set; }

        /// <summary>
        /// Preview text of the most recent message in the conversation.
        /// Used in the Angular frontend conversation list to show message preview.
        /// </summary>
        public string? LastMessagePreview { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for Message entity returned to API clients.
    /// </summary>
    public class MessageDto
    {
        /// <summary>
        /// Unique message identifier (primary key).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The conversation this message belongs to.
        /// </summary>
        public int ConversationId { get; set; }

        /// <summary>
        /// User ID of the message sender.
        /// </summary>
        public string SenderId { get; set; } = string.Empty;

        /// <summary>
        /// The message content/text.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// When the message was sent. Used for chronological ordering.
        /// </summary>
        public DateTime SentAt { get; set; }

        /// <summary>
        /// Whether this message has been read by the recipient.
        /// </summary>
        public bool Read { get; set; }

        /// <summary>
        /// Convenience flag indicating if the message was sent by the authenticated user.
        /// Used in Angular for styling sent vs received messages differently.
        /// Computed in MessengerService.GetMessagesAsync().
        /// </summary>
        public bool IsOwnMessage { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for creating a new conversation.
    /// </summary>
    public class CreateConversationDto
    {
        /// <summary>
        /// First participant's user ID.
        /// </summary>
        public string UserOneId { get; set; } = string.Empty;

        /// <summary>
        /// Second participant's user ID.
        /// </summary>
        public string UserTwoId { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is an announcement conversation.
        /// Set to true when admin broadcasts to all users.
        /// </summary>
        public bool IsAnnouncement { get; set; } = false;
    }

    /// <summary>
    /// Data Transfer Object for sending a new message.
    /// Used as request body for POST /api/messenger/send.
    /// </summary>
    public class SendMessageDto
    {
        /// <summary>
        /// The conversation to send the message to.
        /// </summary>
        public int ConversationId { get; set; }

        /// <summary>
        /// The message content/text to send.
        /// </summary>
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response DTO returned after sending a message.
    /// Contains the created message details for frontend display.
    /// </summary>
    public class SendMessageResponseDto
    {
        /// <summary>
        /// The newly created message's ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The conversation ID the message was sent to.
        /// </summary>
        public int ConversationId { get; set; }

        /// <summary>
        /// The sender's user ID.
        /// </summary>
        public string SenderId { get; set; } = string.Empty;

        /// <summary>
        /// The message content that was sent.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the message was sent.
        /// </summary>
        public DateTime SentAt { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for user presence/status information.
    /// Used for the user search and online users endpoints.
    /// </summary>
    public class UserStatusDto
    {
        /// <summary>
        /// User's unique identifier.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// User's display name (UserName or Email fallback).
        /// Used in Angular for showing user names in search results.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// User's email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Current online status. Updated in real-time via SignalR.
        /// </summary>
        public UserOnlineStatus Status { get; set; }

        /// <summary>
        /// When the user was last seen online.
        /// </summary>
        public DateTime LastSeen { get; set; }
    }

    /// <summary>
    /// Enumerates the possible online statuses for a user.
    /// </summary>
    public enum UserOnlineStatus
    {
        /// <summary>
        /// User is currently connected to the SignalR hub.
        /// </summary>
        Online,

        /// <summary>
        /// User is connected but inactive (away from keyboard).
        /// </summary>
        Away,

        /// <summary>
        /// User is not connected to the SignalR hub.
        /// </summary>
        Offline
    }

    /// <summary>
    /// Data Transfer Object for typing indicator updates via SignalR.
    /// Used to show "User is typing..." UI in the Angular frontend.
    /// </summary>
    public class TypingIndicatorDto
    {
        /// <summary>
        /// The conversation where typing is occurring.
        /// </summary>
        public int ConversationId { get; set; }

        /// <summary>
        /// User ID of the user who is typing.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the user who is typing.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the user started typing (true) or stopped typing (false).
        /// </summary>
        public bool IsTyping { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for marking messages as read.
    /// Used by Angular frontend to update read status.
    /// </summary>
    public class MarkReadDto
    {
        /// <summary>
        /// The message ID to mark as read or unread.
        /// </summary>
        public int MessageId { get; set; }

        /// <summary>
        /// True to mark as read, false to mark as unread.
        /// Default is true (mark as read).
        /// </summary>
        public bool Read { get; set; } = true;
    }
}
