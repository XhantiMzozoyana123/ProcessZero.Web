using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain.Entities;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Messenger API Controller - Provides RESTful endpoints for real-time messaging functionality.
    /// All endpoints require JWT authentication via the [Authorize] attribute.
    /// </summary>
    /// <remarks>
    /// ENTITIES REFERENCED:
    /// 
    /// 1. Conversation (ProcessZero.Domain.Entities)
    ///    - Id (int): Primary key, auto-incremented
    ///    - UserOneId (string): First participant's user ID (450 chars)
    ///    - UserTwoId (string): Second participant's user ID (450 chars)
    ///    - IsPinned (bool): Whether conversation is pinned to top
    ///    - IsAnnouncement (bool): Whether this is an announcement broadcast
    ///    - LastMessageAt (DateTime?): Timestamp of last message sent
    ///    - Messages (ICollection<Message>): Navigation property for messages
    ///    - CreatedAt/UpdatedAt: Inherited from BaseEntity
    /// 
    /// 2. Message (ProcessZero.Domain.Entities)
    ///    - Id (int): Primary key, auto-incremented
    ///    - ConversationId (int): FK to Conversation
    ///    - SenderId (string): User ID of message sender (450 chars)
    ///    - Text (string): Message content
    ///    - SentAt (DateTime): When message was sent
    ///    - Read (bool): Whether message has been read
    ///    - CreatedAt/UpdatedAt: Inherited from BaseEntity
    /// 
    /// SIGNALR INTEGRATION:
    /// - Hub endpoint: /hubs/messenger
    /// - Real-time events: ReceiveMessage, UserTyping, UserStoppedTyping, UserOnline, UserOffline
    /// - Angular frontend should connect via SignalR to receive live updates
    /// </remarks>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MessengerController : ControllerBase
    {
        private readonly IMessengerService _messengerService;

        public MessengerController(IMessengerService messengerService)
        {
            _messengerService = messengerService;
        }

        /// <summary>
        /// Extracts the authenticated user's ID from JWT claims.
        /// </summary>
        private string GetUserId() => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        #region Conversation Endpoints

        /// <summary>
        /// Gets all conversations for the authenticated user.
        /// Returns conversations ordered by LastMessageAt descending (most recent first).
        /// </summary>
        /// <returns>List of ConversationDto with other participant info and unread count</returns>
        /// <response code="200">Returns user conversations with pagination info</response>
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = GetUserId();
            var conversations = await _messengerService.GetUserConversationsAsync(userId);
            return Ok(conversations);
        }

        /// <summary>
        /// Gets a specific conversation by ID if the user is a participant.
        /// </summary>
        /// <param name="id">Conversation ID</param>
        /// <returns>ConversationDto with messages included, or 404 if not found</returns>
        /// <response code="200">Returns conversation details</response>
        /// <response code="404">User is not a participant in this conversation</response>
        [HttpGet("conversations/{id}")]
        public async Task<IActionResult> GetConversation(int id)
        {
            var userId = GetUserId();
            var conversation = await _messengerService.GetConversationByIdAsync(id, userId);
            
            if (conversation == null)
                return NotFound();
                
            return Ok(conversation);
        }

        /// <summary>
        /// Creates a new conversation between two users.
        /// </summary>
        /// <param name="dto">CreateConversationDto containing UserOneId, UserTwoId, and IsAnnouncement flag</param>
        /// <returns>Created Conversation entity</returns>
        [HttpPost("conversations")]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationDto dto)
        {
            var conversation = await _messengerService.CreateConversationAsync(dto);
            return Ok(conversation);
        }

        /// <summary>
        /// Pins or unpins a conversation for the authenticated user.
        /// Pinned conversations appear at the top of the conversation list.
        /// </summary>
        /// <param name="id">Conversation ID</param>
        /// <param name="pinned">True to pin, false to unpin (default: true)</param>
        /// <returns>Success status</returns>
        [HttpPost("conversations/{id}/pin")]
        public async Task<IActionResult> PinConversation(int id, [FromQuery] bool pinned = true)
        {
            var result = await _messengerService.PinConversationAsync(id, pinned);
            return Ok(new { success = result });
        }

        #endregion

        #region Message Endpoints

        /// <summary>
        /// Gets all messages in a conversation if the user is a participant.
        /// Messages are ordered by SentAt ascending (oldest first).
        /// </summary>
        /// <param name="id">Conversation ID</param>
        /// <returns>List of MessageDto for the conversation</returns>
        [HttpGet("conversations/{id}/messages")]
        public async Task<IActionResult> GetMessages(int id)
        {
            var userId = GetUserId();
            var messages = await _messengerService.GetMessagesAsync(id, userId);
            return Ok(messages);
        }

        /// <summary>
        /// Sends a message to a conversation. Does NOT send via SignalR - that's handled client-side.
        /// The frontend should call this REST endpoint AND invoke SendMessage on the SignalR hub.
        /// </summary>
        /// <param name="dto">SendMessageDto with ConversationId and message Text</param>
        /// <returns>SendMessageResponseDto with created message details</returns>
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            var userId = GetUserId();
            var message = await _messengerService.SendMessageAsync(userId, dto.Text, dto.ConversationId);
            return Ok(message);
        }

        /// <summary>
        /// Marks a specific message as read. Only works if the user is a participant in that conversation.
        /// </summary>
        /// <param name="id">Message ID to mark as read</param>
        /// <returns>Success status</returns>
        [HttpPost("messages/{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetUserId();
            var result = await _messengerService.MarkMessageAsReadAsync(id, userId);
            return Ok(new { success = result });
        }

        #endregion

        #region User Search Endpoints

        /// <summary>
        /// Searches for users by username or email.
        /// Excludes the current user from results. Used for starting new conversations.
        /// </summary>
        /// <param name="q">Search query string (partial match on UserName or Email)</param>
        /// <returns>List of UserStatusDto (max 20 results)</returns>
        [HttpGet("users/search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string q)
        {
            var userId = GetUserId();
            var users = await _messengerService.SearchUsersAsync(q, userId);
            return Ok(users);
        }

        /// <summary>
        /// Gets list of online users.
        /// Note: Actual online status is determined by SignalR connection state.
        /// This endpoint returns all active users; frontend should filter by real-time status.
        /// </summary>
        /// <returns>List of UserStatusDto</returns>
        [HttpGet("users/online")]
        public async Task<IActionResult> GetOnlineUsers()
        {
            var users = await _messengerService.GetOnlineUsersAsync();
            return Ok(users);
        }

        #endregion

        #region Admin Endpoints

        /// <summary>
        /// Broadcasts a message to all users as an announcement.
        /// Creates individual conversations (IsAnnouncement=true) with each user.
        /// Requires Admin role - JWT must contain "Admin" role claim.
        /// </summary>
        /// <param name="dto">SendMessageDto with message Text</param>
        /// <returns>Success status</returns>
        /// <response code="200">Broadcast completed successfully</response>
        /// <response code="403">User is not in Admin role</response>
        [HttpPost("broadcast")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Broadcast([FromBody] SendMessageDto dto)
        {
            var userId = GetUserId();
            var result = await _messengerService.BroadcastMessageAsync(userId, dto.Text);
            return Ok(new { success = result });
        }

        #endregion
    }
}
