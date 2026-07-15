using ProcessZero.Application.Dtos;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    /// <summary>
    /// Service interface for Process Zero Messenger functionality.
    /// Provides methods for conversation and message management.
    /// </summary>
    /// <remarks>
    /// IMPLEMENTATION: MessengerService in ProcessZero.Infrastructure.Services
    /// 
    /// SIGNALR INTEGRATION:
    /// For real-time messaging, the Angular frontend should:
    /// 1. Call the REST API endpoints via MessengerController
    /// 2. Use MessengerHub for live updates (messages, typing, presence)
    /// 
    /// SECURITY:
    /// All methods require userId to be passed for participant verification.
    /// The service validates that the user is a participant before operations.
    /// </remarks>
    public interface IMessengerService
    {
        #region Conversation Management

        /// <summary>
        /// Gets all conversations for a user.
        /// </summary>
        /// <param name="userId">Current user's ID</param>
        /// <returns>List of ConversationDto objects ordered by LastMessageAt descending</returns>
        Task<List<ConversationDto>> GetUserConversationsAsync(string userId);

        /// <summary>
        /// Gets a single conversation by ID.
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="userId">Current user's ID (for participant verification)</param>
        /// <returns>ConversationDto if user is participant, null otherwise</returns>
        Task<ConversationDto?> GetConversationByIdAsync(int conversationId, string userId);

        /// <summary>
        /// Creates a new conversation.
        /// </summary>
        /// <param name="dto">CreateConversationDto with participant IDs</param>
        /// <returns>Created Conversation entity</returns>
        Task<Conversation> CreateConversationAsync(CreateConversationDto dto);

        /// <summary>
        /// Gets existing conversation or creates new one if none exists.
        /// </summary>
        /// <param name="userOneId">First user's ID</param>
        /// <param name="userTwoId">Second user's ID</param>
        /// <returns>Existing or newly created Conversation</returns>
        Task<Conversation?> GetOrCreateConversationAsync(string userOneId, string userTwoId);

        /// <summary>
        /// Pins or unpins a conversation.
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="isPinned">True to pin, false to unpin</param>
        /// <returns>True if successful</returns>
        Task<bool> PinConversationAsync(int conversationId, bool isPinned);

        #endregion

        #region Message Management

        /// <summary>
        /// Gets all messages in a conversation.
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="userId">Current user's ID (for participant verification)</param>
        /// <returns>List of MessageDto objects ordered by SentAt ascending</returns>
        Task<List<MessageDto>> GetMessagesAsync(int conversationId, string userId);

        /// <summary>
        /// Sends a message to a conversation.
        /// </summary>
        /// <param name="senderId">Sender's user ID</param>
        /// <param name="text">Message content</param>
        /// <param name="conversationId">Target conversation ID</param>
        /// <returns>SendMessageResponseDto with created message details</returns>
        Task<SendMessageResponseDto> SendMessageAsync(string senderId, string text, int conversationId);

        /// <summary>
        /// Marks a single message as read.
        /// </summary>
        /// <param name="messageId">Message ID to mark as read</param>
        /// <param name="userId">Current user's ID (for participant verification)</param>
        /// <returns>True if successful</returns>
        Task<bool> MarkMessageAsReadAsync(int messageId, string userId);

        /// <summary>
        /// Marks all unread messages in a conversation as read.
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="userId">Current user's ID (for participant verification)</param>
        /// <returns>True if successful</returns>
        Task<bool> MarkConversationAsReadAsync(int conversationId, string userId);

        /// <summary>
        /// Deletes a message.
        /// </summary>
        /// <param name="messageId">Message ID to delete</param>
        /// <param name="userId">Current user's ID (for participant verification)</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteMessageAsync(int messageId, string userId);

        #endregion

        #region User Search and Presence

        /// <summary>
        /// Searches for users by username or email.
        /// </summary>
        /// <param name="searchTerm">Search query</param>
        /// <param name="currentUserId">Current user's ID (excluded from results)</param>
        /// <returns>List of UserStatusDto (max 20 results)</returns>
        Task<List<UserStatusDto>> SearchUsersAsync(string searchTerm, string currentUserId);

        /// <summary>
        /// Gets list of active users.
        /// </summary>
        /// <returns>List of UserStatusDto for all active users</returns>
        Task<List<UserStatusDto>> GetOnlineUsersAsync();

        #endregion

        #region Admin Operations

        /// <summary>
        /// Broadcasts a message to all users as an announcement.
        /// Creates individual conversation + message for each user.
        /// </summary>
        /// <param name="senderId">Admin's user ID (must have Admin role)</param>
        /// <param name="message">Message content to broadcast</param>
        /// <returns>True if successful</returns>
        Task<bool> BroadcastMessageAsync(string senderId, string message);

        #endregion
    }
}
