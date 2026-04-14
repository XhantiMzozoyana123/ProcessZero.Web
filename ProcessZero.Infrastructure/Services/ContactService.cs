using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Infrastructure.Services
{
    public class ContactService : IContactService
    {
        private readonly ApplicationDbContext _context;

        public ContactService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task UpdateContactAsync(Contact contact)
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));

            var existing = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == contact.Id);
            if (existing == null) throw new InvalidOperationException($"Contact with id {contact.Id} not found");

            // Ensure ownership: only the owner can update
            if (!string.Equals(existing.UserId, contact.UserId, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Cannot update a contact you do not own.");

            existing.FirstName = contact.FirstName;
            existing.LastName = contact.LastName;
            existing.Email = contact.Email;
            existing.Phone = contact.Phone;
            existing.Company = contact.Company;
            existing.Job = contact.Job;
            existing.Location = contact.Location;
            existing.Status = contact.Status;

            existing.UpdatedAt = DateTime.Now;

            _context.Contacts.Update(existing);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Contact>> GetAllContactsByUserIdAsync(string userId)
        {
            return await _context.Contacts
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Contact>> GetAllContactTypesAsync(string type)
        {
            // Assuming "type" refers to Status for now
            if (!Enum.TryParse<ContactStatus>(type, true, out var status))
                return new List<Contact>();

            return await _context.Contacts
                .Where(c => c.Status == status)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Contact?> GetContactByIdAsync(string id)
        {
            if (!int.TryParse(id, out var ContactId))
                return null;

            return await _context.Contacts
                .FirstOrDefaultAsync(c => c.Id == ContactId);
        }

        public async Task AddContactAsync(LeadLake leadLake)
        {
            Contact Contact = new Contact()
            {
                UserId = leadLake.UserId,
                FirstName = leadLake.FirstName,
                LastName = leadLake.LastName,
                Email = leadLake.Email,
                Phone = leadLake.Phone,
                Company = leadLake.Company,
                Job = leadLake.Job,
                Location = leadLake.Location,
                Status = ContactStatus.Reached
            };

            await RecycleLeadAsync(Contact, false);

            await _context.Contacts.AddAsync(Contact);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteContactAsync(string id)
        {
            if (!int.TryParse(id, out var ContactId))
                return;

            var Contact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Id == ContactId);

            if (Contact == null)
                return;

            await RecycleLeadAsync(Contact, true);

            _context.Contacts.Remove(Contact);
            await _context.SaveChangesAsync();
        }

        public async Task AddBatchContactAsync(List<LeadLake> leadLakes)
        {
            if (leadLakes == null || leadLakes.Count == 0)
                return;

            // Collect distinct user ids from the incoming lead lakes
            var userIds = leadLakes
                .Where(ll => !string.IsNullOrWhiteSpace(ll.UserId))
                .Select(ll => ll.UserId)
                .Distinct()
                .ToList();

            if (userIds.Count == 0)
                return;

            var contacts = new List<Contact>();

            foreach (var lead in leadLakes)
            {
                if (lead == null || string.IsNullOrWhiteSpace(lead.UserId))
                    continue;

                var contact = new Contact()
                {
                    UserId = lead.UserId,
                    FirstName = lead.FirstName,
                    LastName = lead.LastName,
                    Email = lead.Email,
                    Phone = lead.Phone,
                    Company = lead.Company,
                    Job = lead.Job,
                    Location = lead.Location,
                    Status = ContactStatus.Reached
                };

                contacts.Add(contact);
            }

            if (contacts.Count == 0)
                return;

            await _context.Contacts.AddRangeAsync(contacts);
            await _context.SaveChangesAsync();
        }

        private async Task RecycleLeadAsync(Contact contact, bool deleted)
        {
            if (deleted)
            {
                LeadLake leadLake = new LeadLake()
                {
                    UserId = contact.UserId,
                    FirstName = contact.FirstName,
                    LastName = contact.LastName,
                    Email = contact.Email,
                    Phone = contact.Phone,
                    Company = contact.Company,
                    Job = contact.Job,
                    Location = contact.Location
                };

                _context.LeadLakes.Add(leadLake);
                await _context.SaveChangesAsync();
            }
            else
            {
                var leadExists = await _context.LeadLakes
                    .FirstOrDefaultAsync(c => c.UserId == contact.UserId && c.Email == contact.Email);

                if (leadExists != null)
                {
                    _context.LeadLakes.Remove(leadExists);
                    await _context.SaveChangesAsync();
                }
            }

        }
    }

}
