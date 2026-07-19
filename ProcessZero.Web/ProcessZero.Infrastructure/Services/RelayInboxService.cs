using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    public class RelayInboxService : IRelayInboxService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGmailService _gmailService;
        private readonly IContactService _contactService;

        public RelayInboxService(
            ApplicationDbContext context,
            IGmailService gmailService,
            IContactService contactService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _gmailService = gmailService ?? throw new ArgumentNullException(nameof(gmailService));
            _contactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
        }

        /// <summary>
        /// Saves an email reply to the database if it's from a known lead.
        /// </summary>
        public async Task<RelayEmailReply?> SaveEmailReplyAsync(
            ReceivedEmailMessageDto message,
            int relayEmailAccountId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));

            // Extract email from the "From" field
            var fromEmail = ExtractEmailAddress(message.From);

            // Check if this email exists in LeadLake
            var lead = await _context.RelayLeads
                .FirstOrDefaultAsync(l => l.Email == fromEmail && l.UserId == userId, cancellationToken);

            if (lead == null)
            {
                // Email is not from a known lead, skip it
                return null;
            }

            // Check if we already have this message (by MessageId)
            var existingReply = await _context.RelayEmailReplies
                .FirstOrDefaultAsync(r => r.MessageId == message.MessageId, cancellationToken);

            if (existingReply != null)
            {
                // Already saved
                return existingReply;
            }

            // Create new RelayEmailReply entity
            var emailReply = new RelayEmailReply
            {
                RelayEmailAccountId = relayEmailAccountId,
                LeadLakeId = lead.Id,
                MessageId = message.MessageId,
                FromEmail = fromEmail,
                Subject = message.Subject,
                Body = message.Body,
                ReceivedDate = message.ReceivedDate,
                IsRead = message.IsRead,
                UserId = userId
            };

            await _context.RelayEmailReplies.AddAsync(emailReply, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return emailReply;
        }

        /// <summary>
        /// Gets all unread replies for a specific user.
        /// </summary>
        public async Task<List<RelayEmailReply>> GetUnreadRepliesAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));

            return await _context.RelayEmailReplies
                .Where(r => r.UserId == userId && r.IsRead == false)
                .OrderByDescending(r => r.ReceivedDate)
                .Include(r => r.Lead)
                .Include(r => r.RelayEmailAccount)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets all replies from a specific lead.
        /// </summary>
        public async Task<List<RelayEmailReply>> GetRepliesByLeadAsync(
            int leadLakeId,
            CancellationToken cancellationToken = default)
        {
            return await _context.RelayEmailReplies
                .Where(r => r.LeadLakeId == leadLakeId)
                .OrderByDescending(r => r.ReceivedDate)
                .Include(r => r.RelayEmailAccount)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets all replies for a specific relay email account.
        /// </summary>
        public async Task<List<RelayEmailReply>> GetRepliesByRelayAccountAsync(
            int relayEmailAccountId,
            int? pageNumber = null,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var query = _context.RelayEmailReplies
                .Where(r => r.RelayEmailAccountId == relayEmailAccountId)
                .OrderByDescending(r => r.ReceivedDate)
                .Include(r => r.Lead) as IQueryable<RelayEmailReply>;

            if (pageNumber.HasValue && pageNumber > 0)
            {
                query = query.Skip((pageNumber.Value - 1) * pageSize);
            }

            return await query.Take(pageSize).ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Marks a reply as read.
        /// </summary>
        public async Task MarkAsReadAsync(
            int emailReplyId,
            CancellationToken cancellationToken = default)
        {
            var reply = await _context.RelayEmailReplies.FindAsync(new object[] { emailReplyId }, cancellationToken);
            if (reply == null) throw new InvalidOperationException($"Email reply with id {emailReplyId} not found");

            reply.IsRead = true;
            _context.RelayEmailReplies.Update(reply);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Marks multiple replies as read.
        /// </summary>
        public async Task MarkMultipleAsReadAsync(
            List<int> emailReplyIds,
            CancellationToken cancellationToken = default)
        {
            if (emailReplyIds == null || emailReplyIds.Count == 0) return;

            var replies = await _context.RelayEmailReplies
                .Where(r => emailReplyIds.Contains(r.Id))
                .ToListAsync(cancellationToken);

            foreach (var reply in replies)
            {
                reply.IsRead = true;
            }

            _context.RelayEmailReplies.UpdateRange(replies);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Adds tags to a reply for categorization.
        /// </summary>
        public async Task AddTagsAsync(
            int emailReplyId,
            List<string> tags,
            CancellationToken cancellationToken = default)
        {
            if (tags == null || tags.Count == 0) return;

            var reply = await _context.RelayEmailReplies.FindAsync(new object[] { emailReplyId }, cancellationToken);
            if (reply == null) throw new InvalidOperationException($"Email reply with id {emailReplyId} not found");

            // Add tags (append if not already present)
            var existingTags = string.IsNullOrEmpty(reply.Tags)
                ? new HashSet<string>()
                : new HashSet<string>(reply.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()));

            foreach (var tag in tags)
            {
                existingTags.Add(tag.Trim());
            }

            reply.Tags = string.Join(",", existingTags);
            _context.RelayEmailReplies.Update(reply);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Syncs email replies from a relay account by fetching recent messages. 
        /// </summary>
        public async Task SyncEmailRepliesAsync(
            int relayEmailAccountId,
            int maxResults = 20,
            CancellationToken cancellationToken = default)
        {
            var account = await _context.RelayEmailAccounts
                .FirstOrDefaultAsync(a => a.Id == relayEmailAccountId, cancellationToken);

            if (account == null)
                throw new InvalidOperationException($"Relay email account with id {relayEmailAccountId} not found");

            if (string.IsNullOrEmpty(account.AccessToken))
                throw new InvalidOperationException("Relay email account does not have a valid access token");

            try
            {
                // Fetch recent messages from Gmail
                var messages = await _gmailService.ReceiveAsync(account, maxResults, cancellationToken);

                // Save each message if it's from a known lead
                foreach (var message in messages)
                {
                    await SaveEmailReplyAsync(message, relayEmailAccountId, account.UserId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to sync email replies for account {relayEmailAccountId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Syncs email replies from all relay email accounts by fetching recent messages.
        /// </summary>
        public async Task SyncAllEmailRepliesAsync(
            int maxResults = 20,
            CancellationToken cancellationToken = default)
        {
            // Get all active relay email accounts
            var accounts = await _context.RelayEmailAccounts
                .Where(a => a.IsActive && !string.IsNullOrEmpty(a.AccessToken))
                .ToListAsync(cancellationToken);

            if (accounts.Count == 0)
                throw new InvalidOperationException("No active relay email accounts found with valid access tokens");

            var failedAccounts = new List<(int AccountId, string Error)>();

            // Sync each account
            foreach (var account in accounts)
            {
                try
                {
                    // Fetch recent messages from Gmail
                    var messages = await _gmailService.ReceiveAsync(account, maxResults, cancellationToken);

                    // Save each message if it's from a known lead
                    foreach (var message in messages)
                    {
                        await SaveEmailReplyAsync(message, account.Id, account.UserId, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue syncing other accounts
                    failedAccounts.Add((account.Id, ex.Message));
                }
            }

            // If all accounts failed, throw an exception
            if (failedAccounts.Count == accounts.Count)
            {
                var errorMessages = string.Join("; ", failedAccounts.Select(f => $"Account {f.AccountId}: {f.Error}"));
                throw new Exception($"Failed to sync email replies for all accounts: {errorMessages}");
            }

            // If some accounts failed, log warnings but don't throw
            if (failedAccounts.Count > 0)
            {
                var warningMessages = string.Join("; ", failedAccounts.Select(f => $"Account {f.AccountId}: {f.Error}"));
                System.Diagnostics.Debug.WriteLine($"Warning: Failed to sync some relay accounts: {warningMessages}");
            }
        }

        /// <summary>
        /// Searches for email replies by subject or body content.
        /// </summary>
        public async Task<List<RelayEmailReply>> SearchRepliesAsync(
            string userId,
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrWhiteSpace(searchTerm)) throw new ArgumentNullException(nameof(searchTerm));

            var lowerSearchTerm = searchTerm.ToLower();

            return await _context.RelayEmailReplies
                .Where(r => r.UserId == userId &&
                    (r.Subject.ToLower().Contains(lowerSearchTerm) ||
                     r.Body.ToLower().Contains(lowerSearchTerm) ||
                     r.FromEmail.ToLower().Contains(lowerSearchTerm)))
                .OrderByDescending(r => r.ReceivedDate)
                .Include(r => r.Lead)
                .Include(r => r.RelayEmailAccount)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Extracts email address from "Name <email@example.com>" format.
        /// </summary>
        private string ExtractEmailAddress(string emailString)
        {
            if (string.IsNullOrEmpty(emailString)) return string.Empty;

            var start = emailString.LastIndexOf('<');
            var end = emailString.LastIndexOf('>');

            if (start >= 0 && end > start)
            {
                return emailString.Substring(start + 1, end - start - 1).Trim();
            }

            return emailString.Trim();
        }

        /// <summary>
        /// Gets a specific relay email reply by ID.
        /// </summary>
        public async Task<RelayEmailReply?> GetEmailReplyByIdAsync(
            int emailReplyId,
            CancellationToken cancellationToken = default)
        {
            return await _context.RelayEmailReplies
                .Include(r => r.Lead)
                .Include(r => r.RelayEmailAccount)
                .FirstOrDefaultAsync(r => r.Id == emailReplyId, cancellationToken);
        }

        /// <summary>
        /// Admin feature: Adds or updates an email recipient as a contact in a sales rep's contact table.
        /// If the contact already exists (by email), updates the status. Otherwise, creates a new contact.
        /// The sales rep user ID is taken from RelayEmailReply.UserId.
        /// </summary>
        public async Task<Contact?> UpsertEmailRecipientContactAsync(
            RelayEmailReply relayEmailReply,
            ContactStatus contactStatus = ContactStatus.Active,
            CancellationToken cancellationToken = default)
        {
            if (relayEmailReply == null)
                throw new ArgumentNullException(nameof(relayEmailReply));

            if (string.IsNullOrWhiteSpace(relayEmailReply.UserId))
                throw new InvalidOperationException("RelayEmailReply must have a valid UserId");

            var salesRepUserId = relayEmailReply.UserId;

            // Fetch the lead if not already loaded
            var lead = relayEmailReply.Lead;
            if (lead == null && relayEmailReply.LeadLakeId > 0)
            {
                lead = await _context.LeadLakes
                    .FirstOrDefaultAsync(l => l.Id == relayEmailReply.LeadLakeId, cancellationToken);
            }

            // Check if contact already exists for this email
            var existingContact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Email == relayEmailReply.FromEmail && c.UserId == salesRepUserId, cancellationToken);

            if (existingContact != null)
            {
                // Contact exists, update status
                existingContact.Status = contactStatus;
                existingContact.UpdatedAt = DateTime.UtcNow;
                _context.Contacts.Update(existingContact);
                await _context.SaveChangesAsync(cancellationToken);
                return existingContact;
            }

            // Create new contact from email reply
            var contact = new Contact
            {
                UserId = salesRepUserId,
                FirstName = lead?.FirstName ?? "Unknown",
                LastName = lead?.LastName ?? "Recipient",
                Email = relayEmailReply.FromEmail,
                Phone = lead?.Phone ?? string.Empty,
                Company = lead?.Company ?? string.Empty,
                Job = lead?.Job ?? string.Empty,
                Location = lead?.Location ?? string.Empty,
                Status = contactStatus,
                ClosedAmount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Contacts.AddAsync(contact, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return contact;
        }
    }
}
