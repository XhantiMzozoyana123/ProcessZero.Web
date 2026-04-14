using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Constants;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Infrastructure.Services
{
    public class ClientService : IClientService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public ClientService(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public async Task<int> CreateContactAsync(Contact contact, CancellationToken cancellationToken = default)
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));

            contact.CreatedAt = DateTime.UtcNow;
            contact.UpdatedAt = DateTime.UtcNow;

            await _context.Contacts.AddAsync(contact, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return contact.Id;
        }

        public async Task UpdateContactAsync(Contact contact, CancellationToken cancellationToken = default)
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));

            if (contact.Status == ContactStatus.Deactivated)
            {
                await NotifySalesRepOfDeactivatedClientAsync(contact);
            }

            var existing = await _context.Contacts.FindAsync(new object[] { contact.Id }, cancellationToken);
            if (existing == null) throw new InvalidOperationException($"Contact with id {contact.Id} not found");

            _context.Entry(existing).CurrentValues.SetValues(contact);
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteContactAsync(int id, CancellationToken cancellationToken = default)
        {
            var existing = await _context.Contacts.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null) return;

            await NotifySalesRepOfDeactivatedClientAsync(existing);

            _context.Contacts.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<Contact>> GetAllContactsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Contacts
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Contact>> GetAllContactsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var skip = (Math.Max(1, page) - 1) * Math.Max(1, pageSize);
            return await _context.Contacts
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip)
                .Take(Math.Clamp(pageSize, 1, 500))
                .ToListAsync(cancellationToken);
        }

        public async Task<Contact?> GetContactByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Contacts.FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<List<Contact>> GetContactsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));

            return await _context.Contacts
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        private async Task NotifySalesRepOfDeactivatedClientAsync(Contact contact)
        {
            var user = await _context.Users.FindAsync(contact.UserId);
            if (user == null) return;
            var notice = NoticeConstant.NotifySalesRepClientDeactivated(
                user.UserName,
                user.Email,
                contact.FirstName + " " + contact.LastName
                );
            await _emailService.SendEmailAsync(notice);
        }
    }
}
