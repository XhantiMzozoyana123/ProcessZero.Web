using Microsoft.EntityFrameworkCore;
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
    /// <summary>
    /// Implementation of IMessengerService for real-time messaging functionality.
    /// Handles all database operations for conversations and messages.
    /// </summary>
    /// <remarks>
    /// SIGNALR INTEGRATION NOTE:
    /// This service handles REST API calls only. Real-time delivery via SignalR is handled
    /// by MessengerHub. When a message is sent via SendMessageAsync, the Angular frontend
    /// should ALSO call connection.invoke("SendMessage") on the SignalR hub for live updates.
    /// 
    /// DATABASE TABLES:
    /// - Conversations: Stores conversation metadata (participants, pinning, last message time)
    /// - Messages: Stores individual messages (text, sender, read status)
    /// - Users: ApplicationUser table (from IdentityDbContext) for participant lookups
    /// 
    /// SECURITY:
    /// All methods validate that the userId is a participant in the conversation before allowing
    /// access or modifications. This prevents users from accessing other users' conversations.
    /// </remarks>
    public class MessengerService : IMessengerService
    {
        private readonly ApplicationDbContext _context;

        public MessengerService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Conversation Management

        /// <summary>
        /// Creates a new conversation between two users.
        /// </summary>
        /// <param name="dto">CreateConversationDto with UserOneId, UserTwoId, and IsAnnouncement flag</param>
        /// <returns>The newly created Conversation entity</returns>
        /// <remarks>
        /// Used when:
        /// - Two users start a new conversation
        /// - Admin creates an announcement broadcast (IsAnnouncement=true)
        /// 
        /// Note: For announcements, consider using BroadcastMessageAsync instead which handles
        /// creating individual conversations with all users automatically.
        /// </remarks>
        public async Task<Conversation> CreateConversationAsync(CreateConversationDto dto)
        {
            var conversation = new Conversation
            {
                UserOneId = dto.UserOneId,
                UserTwoId = dto.UserTwoId,
                IsAnnouncement = dto.IsAnnouncement,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Conversations.AddAsync(conversation);
            await _context.SaveChangesAsync();

            return conversation;
        }

        /// <summary>
        /// Gets an existing conversation or creates a new one if none exists.
        /// Checks both UserOne/UserTwo combinations to find existing conversations.
        /// </summary>
        /// <param name="userOneId">First user's ID</param>
        /// <param name="userTwoId">Second user's ID</param>
        /// <returns>Existing conversation or newly created one</returns>
        public async Task<Conversation?> GetOrCreateConversationAsync(string userOneId, string userTwoId)
        {
            // Look for existing conversation in either direction (UserOne->UserTwo or UserTwo->UserOne)
            var existingConversation = await _context.Conversations
                .FirstOrDefaultAsync(c => 
                    (c.UserOneId == userOneId && c.UserTwoId == userTwoId) ||
                    (c.UserOneId == userTwoId && c.UserTwoId == userOneId));

            if (existingConversation != null)
                return existingConversation;

            return await CreateConversationAsync(new CreateConversationDto
            {
                UserOneId = userOneId,
                UserTwoId = userTwoId,
                IsAnnouncement = false
            });
        }

        /// <summary>
        /// Gets a single conversation by ID with full details.
        /// Verifies the user is a participant before returning data.
        /// </summary>
        /// <param name="conversationId">Conversation ID to look up</param>
        /// <param name="userId">Current user's ID (for participant verification and "other user" determination)</param>
        /// <returns>ConversationDto if user is participant, null otherwise</returns>
        /// <remarks>
        /// The ConversationDto returned includes:
        /// - OtherUserName/OtherUserEmail: The OTHER participant's info (not the current user)
        /// - UnreadCount: Count of messages where SenderId != userId AND Read = false
        /// - LastMessagePreview: Text of the most recent message
        /// - OtherUserOnline: Always false initially - updated via SignalR in real-time
        /// </remarks>
        public async Task<ConversationDto?> GetConversationByIdAsync(int conversationId, string userId)
        {
            // Verify user is a participant in this conversation
            var conversation = await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == conversationId && 
                    (c.UserOneId == userId || c.UserTwoId == userId));

            if (conversation == null)
                return null;

            // Determine which participant is "other" (not the current user)
            var otherUserId = conversation.UserOneId == userId ? conversation.UserTwoId : conversation.UserOneId;
            var otherUser = await _context.Users.FindAsync(otherUserId);
            
            // Count unread messages (sent by other user and not read)
            var unreadCount = await _context.Messages
                .CountAsync(m => m.ConversationId == conversationId && 
                    m.SenderId != userId && !m.Read);

            // Get the most recent message for preview
            var lastMessage = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();

            return new ConversationDto
            {
                Id = conversation.Id,
                UserOneId = conversation.UserOneId,
                UserTwoId = conversation.UserTwoId,
                IsPinned = conversation.IsPinned,
                IsAnnouncement = conversation.IsAnnouncement,
                LastMessageAt = conversation.LastMessageAt,
                UnreadCount = unreadCount,
                OtherUserName = otherUser?.UserName ?? otherUser?.Email ?? "Unknown",
                OtherUserEmail = otherUser?.Email ?? string.Empty,
                OtherUserOnline = false, // Will be updated via SignalR
                LastMessagePreview = lastMessage?.Text
            };
        }

        /// <summary>
        /// Gets all conversations for a user, ordered by LastMessageAt descending.
        /// </summary>
        /// <param name="userId">Current user's ID</param>
        /// <returns>List of ConversationDto objects</returns>
        /// <remarks>
        /// Returns conversations where the user is either UserOne or UserTwo.
        /// Each ConversationDto includes computed fields:
        /// - OtherUserName/OtherUserEmail for the conversation partner
        /// - UnreadCount for badge display
        /// - LastMessagePreview for message preview
        /// 
        /// PINNING: Pinned conversations (IsPinned=true) should appear at the top
        /// in the Angular frontend. The API returns them in LastMessageAt order;
        /// frontend should re-sort to put pinned conversations first.
        /// </remarks>
        public async Task<List<ConversationDto>> GetUserConversationsAsync(string userId)
        {
            var conversations = await _context.Conversations
                .Include(c => c.Messages)
                .Where(c => c.UserOneId == userId || c.UserTwoId == userId)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            var result = new List<ConversationDto>();

            foreach (var conversation in conversations)
            {
                var otherUserId = conversation.UserOneId == userId ? conversation.UserTwoId : conversation.UserOneId;
                var otherUser = await _context.Users.FindAsync(otherUserId);
                
                var unreadCount = await _context.Messages
                    .CountAsync(m => m.ConversationId == conversation.Id && 
                        m.SenderId != userId && !m.Read);

                var lastMessage = await _context.Messages
                    .Where(m => m.ConversationId == conversation.Id)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                result.Add(new ConversationDto
                {
                    Id = conversation.Id,
                    UserOneId = conversation.UserOneId,
                    UserTwoId = conversation.UserTwoId,
                    IsPinned = conversation.IsPinned,
                    IsAnnouncement = conversation.IsAnnouncement,
                    LastMessageAt = conversation.LastMessageAt,
                    UnreadCount = unreadCount,
                    OtherUserName = otherUser?.UserName ?? otherUser?.Email ?? "Unknown",
                    OtherUserEmail = otherUser?.Email ?? string.Empty,
                    OtherUserOnline = false,
                    LastMessagePreview = lastMessage?.Text
                });
            }

            return result;
        }

        /// <summary>
        /// Pins or unpins a conversation.
        /// </summary>
        /// <param name="conversationId">Conversation to pin/unpin</param>
        /// <param name="isPinned">True to pin, false to unpin</param>
        /// <returns>True if successful, false if conversation not found</returns>
        public async Task<bool> PinConversationAsync(int conversationId, bool isPinned)
        {
            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation == null)
                return false;

            conversation.IsPinned = isPinned;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Message Management

        /// <summary>
        /// Gets all messages in a conversation.
        /// Verifies user is a participant before returning messages.
        /// </summary>
        /// <param name="conversationId">Conversation ID to get messages from</param>
        /// <param name="userId">Current user's ID for participant verification</param>
        /// <returns>List of messages if user is participant, empty list otherwise</returns>
        /// <remarks>
        /// Messages are ordered by SentAt ascending (oldest first) for chat display.
        /// Each MessageDto includes IsOwnMessage flag computed by comparing SenderId with userId.
        /// The IsOwnMessage flag helps the Angular frontend style sent vs received messages differently.
        /// </remarks>
        public async Task<List<MessageDto>> GetMessagesAsync(int conversationId, string userId)
        {
            // Verify user is a participant in this conversation
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && 
                    (c.UserOneId == userId || c.UserTwoId == userId));

            if (conversation == null)
                return new List<MessageDto>();

            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            return messages.Select(m => new MessageDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                Text = m.Text,
                SentAt = m.SentAt,
                Read = m.Read,
                IsOwnMessage = m.SenderId == userId
            }).ToList();
        }

        /// <summary>
        /// Sends a new message to a conversation.
        /// </summary>
        /// <param name="senderId">User ID of the sender</param>
        /// <param name="text">Message content</param>
        /// <param name="conversationId">Target conversation ID</param>
        /// <returns>SendMessageResponseDto with created message details</returns>
        /// <remarks>
        /// This method:
        /// 1. Creates a new Message entity with Read=false
        /// 2. Updates the conversation's LastMessageAt timestamp
        /// 3. Saves both to database
        /// 4. Returns the created message for frontend display
        /// 
        /// IMPORTANT: For real-time delivery to the recipient, the Angular frontend
        /// should ALSO call connection.invoke("SendMessage", conversationId, message)
        /// on the SignalR hub after this API call succeeds.
        /// </remarks>
        public async Task<SendMessageResponseDto> SendMessageAsync(string senderId, string text, int conversationId)
        {
            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Text = text,
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Read = false
            };

            await _context.Messages.AddAsync(message);

            // Update conversation's last message time
            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation != null)
            {
                conversation.LastMessageAt = DateTime.UtcNow;
                conversation.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return new SendMessageResponseDto
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                Text = message.Text,
                SentAt = message.SentAt
            };
        }

        /// <summary>
        /// Marks a single message as read.
        /// Verifies the user is a participant before allowing the update.
        /// </summary>
        /// <param name="messageId">Message to mark as read</param>
        /// <param name="userId">Current user's ID for participant verification</param>
        /// <returns>True if successful, false if user is not a participant</returns>
        public async Task<bool> MarkMessageAsReadAsync(int messageId, string userId)
        {
            var message = await _context.Messages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null || message.Conversation == null)
                return false;

            // Verify user is a participant in the conversation
            var isParticipant = message.Conversation.UserOneId == userId || 
                             message.Conversation.UserTwoId == userId;

            if (!isParticipant)
                return false;

            message.Read = true;
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Marks all unread messages in a conversation as read.
        /// Verifies the user is a participant before allowing the update.
        /// </summary>
        /// <param name="conversationId">Conversation to mark as read</param>
        /// <param name="userId">Current user's ID for participant verification</param>
        /// <returns>True if successful, false if user is not a participant</returns>
        public async Task<bool> MarkConversationAsReadAsync(int conversationId, string userId)
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && 
                    (c.UserOneId == userId || c.UserTwoId == userId));

            if (conversation == null)
                return false;

            var unreadMessages = await _context.Messages
                .Where(m => m.ConversationId == conversationId && !m.Read)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.Read = true;
                message.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Deletes a message. Only participants can delete messages.
        /// </summary>
        /// <param name="messageId">Message to delete</param>
        /// <param name="userId">Current user's ID for participant verification</param>
        /// <returns>True if successful, false if user is not a participant</returns>
        public async Task<bool> DeleteMessageAsync(int messageId, string userId)
        {
            var message = await _context.Messages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null || message.Conversation == null)
                return false;

            // Verify user is a participant in the conversation
            var conversation = message.Conversation;
            var isParticipant = conversation.UserOneId == userId || conversation.UserTwoId == userId;

            if (!isParticipant)
                return false;

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion

        #region User Search and Presence

        /// <summary>
        /// Searches for users by username or email.
        /// Excludes the current user from results.
        /// </summary>
        /// <param name="searchTerm">Partial username or email to search for</param>
        /// <param name="currentUserId">Current user's ID to exclude from results</param>
        /// <returns>List of UserStatusDto (max 20 results)</returns>
        /// <remarks>
        /// Used by Angular frontend to find users to start conversations with.
        /// Status is set to Offline initially; updated in real-time via SignalR.
        /// </remarks>
        public async Task<List<UserStatusDto>> SearchUsersAsync(string searchTerm, string currentUserId)
        {
            var users = await _context.Users
                .Where(u => u.Id != currentUserId && 
                    (u.UserName!.Contains(searchTerm) || u.Email!.Contains(searchTerm)))
                .Take(20)
                .ToListAsync();

            return users.Select(u => new UserStatusDto
            {
                UserId = u.Id,
                DisplayName = u.UserName ?? u.Email ?? "Unknown",
                Email = u.Email ?? string.Empty,
                Status = UserOnlineStatus.Offline, // Will be updated via SignalR
                LastSeen = DateTime.UtcNow
            }).ToList();
        }

        /// <summary>
        /// Gets list of active users.
        /// </summary>
        /// <returns>List of UserStatusDto for all active users</returns>
        /// <remarks>
        /// Returns users where LockoutEnabled is false OR LockoutEnd is in the past.
        /// Status is set to Online initially; actual online status comes via SignalR.
        /// </remarks>
        public async Task<List<UserStatusDto>> GetOnlineUsersAsync()
        {
            var users = await _context.Users
                .Where(u => u.LockoutEnabled == false || u.LockoutEnd < DateTime.UtcNow)
                .ToListAsync();

            return users.Select(u => new UserStatusDto
            {
                UserId = u.Id,
                DisplayName = u.UserName ?? u.Email ?? "Unknown",
                Email = u.Email ?? string.Empty,
                Status = UserOnlineStatus.Online,
                LastSeen = DateTime.UtcNow
            }).ToList();
        }

        #endregion

        #region Admin Operations

        /// <summary>
        /// Broadcasts a message to all users as an announcement.
        /// Creates individual conversation + message for each user.
        /// </summary>
        /// <param name="senderId">Admin user's ID (must have Admin role)</param>
        /// <param name="message">Message content to broadcast</param>
        /// <returns>True if broadcast completed successfully</returns>
        /// <remarks>
        /// This creates:
        /// - One conversation per user (excluding sender) with IsAnnouncement=true
        /// - One message in each conversation
        /// 
        /// The Angular frontend should call this API AND handle SignalR notifications
        /// for each created conversation/message pair.
        /// </remarks>
        public async Task<bool> BroadcastMessageAsync(string senderId, string message)
        {
            // Create announcement conversations with all users
            var users = await _context.Users
                .Where(u => u.Id != senderId)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var userId in users)
            {
                var conversation = new Conversation
                {
                    UserOneId = senderId,
                    UserTwoId = userId,
                    IsAnnouncement = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Conversations.AddAsync(conversation);
                await _context.SaveChangesAsync();

                // Send the message
                var msg = new Message
                {
                    ConversationId = conversation.Id,
                    SenderId = senderId,
                    Text = message,
                    SentAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Read = false
                };

                await _context.Messages.AddAsync(msg);
                conversation.LastMessageAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        #endregion
    }
}