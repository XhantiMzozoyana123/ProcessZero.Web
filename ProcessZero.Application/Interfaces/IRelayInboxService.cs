using ProcessZero.Application.Dtos;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    /// <summary>
    /// Service for managing email replies from leads through the relay inbox system.
    /// </summary>
    public interface IRelayInboxService
    {
        /// <summary>
        /// Saves an email reply to the database if it's from a known lead.
        /// </summary>
        Task<RelayEmailReply?> SaveEmailReplyAsync(
            ReceivedEmailMessageDto message,
            int relayEmailAccountId,
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all unread replies for a specific user.
        /// </summary>
        Task<List<RelayEmailReply>> GetUnreadRepliesAsync(
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all replies from a specific lead.
        /// </summary>
        Task<List<RelayEmailReply>> GetRepliesByLeadAsync(
            int leadLakeId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all replies for a specific relay email account.
        /// </summary>
        Task<List<RelayEmailReply>> GetRepliesByRelayAccountAsync(
            int relayEmailAccountId,
            int? pageNumber = null,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a reply as read.
        /// </summary>
        Task MarkAsReadAsync(
            int emailReplyId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks multiple replies as read.
        /// </summary>
        Task MarkMultipleAsReadAsync(
            List<int> emailReplyIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds tags to a reply for categorization.
        /// </summary>
        Task AddTagsAsync(
            int emailReplyId,
            List<string> tags,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Syncs email replies from a relay account by fetching recent messages.
        /// </summary>
        Task SyncEmailRepliesAsync(
            int relayEmailAccountId,
            int maxResults = 20,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Syncs email replies from all relay email accounts by fetching recent messages.
        /// </summary>
        Task SyncAllEmailRepliesAsync(
            int maxResults = 20,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for email replies by subject or body content.
        /// </summary>
        Task<List<RelayEmailReply>> SearchRepliesAsync(
            string userId,
            string searchTerm,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific relay email reply by ID.
        /// </summary>
        Task<RelayEmailReply?> GetEmailReplyByIdAsync(
            int emailReplyId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin feature: Adds or updates an email recipient as a contact in a sales rep's contact table.
        /// If the contact already exists (by email), it updates the status. Otherwise, creates a new contact.
        /// The sales rep user ID is taken from the RelayEmailReply.UserId field.
        /// </summary>
        /// <param name="relayEmailReply">The relay email reply to use for creating or updating a contact</param>
        /// <param name="contactStatus">The contact status to set</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created or updated contact entity</returns>
        Task<Contact?> UpsertEmailRecipientContactAsync(
            RelayEmailReply relayEmailReply,
            ContactStatus contactStatus = ContactStatus.Active,
            CancellationToken cancellationToken = default);
    }
}
