using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Infrastructure.Services
{
    public class InboxService : IInboxService
    {
        private readonly ApplicationDbContext _context;

        public InboxService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<int> CreateInboxAsync(Inbox inbox, CancellationToken cancellationToken = default)
        {
            if (inbox == null) throw new ArgumentNullException(nameof(inbox));

            inbox.CreatedAt = DateTime.UtcNow;
            inbox.UpdatedAt = DateTime.UtcNow;

            await _context.Inboxes.AddAsync(inbox, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return inbox.Id;
        }

        public async Task UpdateInboxAsync(Inbox inbox, CancellationToken cancellationToken = default)
        {
            if (inbox == null) throw new ArgumentNullException(nameof(inbox));

            var existing = await _context.Inboxes.FirstOrDefaultAsync(i => i.Id == inbox.Id, cancellationToken);
            if (existing == null) throw new InvalidOperationException($"Inbox with id {inbox.Id} not found");

            // Ensure owner
            if (!string.Equals(existing.UserId, inbox.UserId, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Cannot update an inbox you do not own.");

            existing.Username = inbox.Username;
            existing.Password = inbox.Password;
            existing.SmtpHost = inbox.SmtpHost;
            existing.SmtpPort = inbox.SmtpPort;
            existing.SmtpUseSsl = inbox.SmtpUseSsl;
            existing.ImapHost = inbox.ImapHost;
            existing.ImapPort = inbox.ImapPort;
            existing.ImapUseSsl = inbox.ImapUseSsl;
            existing.IsPrimary = inbox.IsPrimary;

            existing.UpdatedAt = DateTime.UtcNow;

            _context.Inboxes.Update(existing);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteInboxAsync(int id, CancellationToken cancellationToken = default)
        {
            var existing = await _context.Inboxes.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null) return;

            _context.Inboxes.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Inbox?> GetInboxByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Inboxes.FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<List<Inbox>> GetInboxesByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));

            return await _context.Inboxes
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Inbox>> GetAllInboxesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Inboxes
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}
