using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time messaging functionality.
    /// Provides live updates for messages, typing indicators, and user presence.
    /// </summary>
    /// <remarks>
    /// ANGULAR FRONTEND USAGE:
    /// 
    /// connection = new signalR.HubConnectionBuilder()
    ///     .withUrl("/hubs/messenger")
    ///     .build();
    /// 
    /// // Event handlers
    /// connection.on("ReceiveMessage", (conversationId, senderId, message) => { ... });
    /// connection.on("UserTyping", (conversationId, userId) => { ... });
    /// connection.on("UserOnline", (userId) => { ... });
    /// 
    /// // Methods to invoke
    /// connection.invoke("SendMessage", conversationId, text);
    /// connection.invoke("JoinConversation", conversationId);
    /// connection.invoke("TypingIndicator", conversationId);
    /// 
    /// AUTHENTICATION:
    /// - JWT token must be included in the query string: connection.queryParams = { access_token: jwt }
    /// - Configure in Angular: .withUrl("/hubs/messenger", { accessTokenFactory: () => jwtToken })
    /// </remarks>
    public class MessengerHub : Hub
    {
        #region Message Methods

        /// <summary>
        /// Sends a message to all users in the conversation group.
        /// Called by Angular frontend after POST /api/messenger/send succeeds.
        /// </summary>
        /// <param name="conversationId">The conversation to send the message to</param>
        /// <param name="message">The message text to send</param>
        public async Task SendMessage(int conversationId, string message)
        {
            await Clients.Group(conversationId.ToString())
                .SendAsync("ReceiveMessage", conversationId, Context.UserIdentifier, message);
        }

        #endregion

        #region Conversation Group Methods

        /// <summary>
        /// Adds the current connection to the specified conversation group.
        /// User will receive all messages sent to that conversation.
        /// </summary>
        /// <param name="conversationId">Conversation to join</param>
        /// <remarks>
        /// Called when user opens a conversation in the Angular frontend.
        /// Groups are used to efficiently broadcast to all participants in a conversation.
        /// </remarks>
        public async Task JoinConversation(int conversationId)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                conversationId.ToString());
        }

        /// <summary>
        /// Removes the current connection from the specified conversation group.
        /// User will no longer receive messages from that conversation.
        /// </summary>
        /// <param name="conversationId">Conversation to leave</param>
        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                conversationId.ToString());
        }

        #endregion

        #region Typing Indicator Methods

        /// <summary>
        /// Notifies other users in the conversation that this user is typing.
        /// Called when user starts typing in the Angular frontend.
        /// </summary>
        /// <param name="conversationId">Conversation where typing occurred</param>
        public async Task TypingIndicator(int conversationId)
        {
            await Clients.OthersInGroup(conversationId.ToString())
                .SendAsync("UserTyping", conversationId, Context.UserIdentifier);
        }

        /// <summary>
        /// Notifies other users in the conversation that this user stopped typing.
        /// Called when user stops typing (debounced) in the Angular frontend.
        /// </summary>
        /// <param name="conversationId">Conversation where typing stopped</param>
        public async Task StopTypingIndicator(int conversationId)
        {
            await Clients.OthersInGroup(conversationId.ToString())
                .SendAsync("UserStoppedTyping", conversationId, Context.UserIdentifier);
        }

        #endregion

        #region Connection Lifecycle Methods

        /// <summary>
        /// Called when a user connects to the hub.
        /// Notifies all users that this user is online.
        /// </summary>
        /// <remarks>
        /// The UserIdentifier is set from the JWT token's name claim.
        /// Ensure your JWT configuration in Program.cs sets NameClaimType correctly.
        /// </remarks>
        public override async Task OnConnectedAsync()
        {
            // Track user connection - notify all clients this user is online
            if (!string.IsNullOrEmpty(Context.UserIdentifier))
            {
                await Clients.All.SendAsync("UserOnline", Context.UserIdentifier);
            }
            
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a user disconnects from the hub.
        /// Notifies all users that this user is offline.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Track user disconnection - notify all clients this user is offline
            if (!string.IsNullOrEmpty(Context.UserIdentifier))
            {
                await Clients.All.SendAsync("UserOffline", Context.UserIdentifier);
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        #endregion
    }
}