using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Constants;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Infrastructure.Services
{
    public class MeetingService : IMeetingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IKpiService _kpiService;
        private readonly IEmailService _emailService;

        public MeetingService(ApplicationDbContext context, IKpiService kpiService, IEmailService emailService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _kpiService = kpiService ?? throw new ArgumentNullException(nameof(kpiService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public async Task AddMeetingAsync(MeetingDto meetingDto, string notes)
        {
            if (meetingDto == null) throw new ArgumentNullException(nameof(meetingDto));
            if (meetingDto.Meeting == null) throw new ArgumentException("Meeting is required", nameof(meetingDto));
            if (meetingDto.Contact == null) throw new ArgumentException("Contact is required", nameof(meetingDto));
            if (meetingDto.Product == null) throw new ArgumentException("Product is required", nameof(meetingDto));

            var contact = await _context.Contacts.FindAsync(meetingDto.Contact.Id);
            if (contact == null) throw new InvalidOperationException($"Contact with id {meetingDto.Contact.Id} not found");

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var meeting = new Meeting
                {
                    UserId = meetingDto.UserId,
                    ClientId = contact.Id,
                    ProductId = meetingDto.Product.Id,
                    MeetingDate = meetingDto.Meeting.MeetingDate,
                    Notes = string.IsNullOrWhiteSpace(notes) ? (meetingDto.Meeting.Notes ?? string.Empty) : notes
                };

                await _context.Meetings.AddAsync(meeting);
                await _context.SaveChangesAsync(); // generates Id

                // update KPI snapshot
                await _kpiService.AddCallBookedAsync(meeting.UserId, meeting.ProductId);

                await NotifyMeetingBookedAsync(meeting);

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteMeetingAsync(int id, string notes)
        {
            var meeting = await _context.Meetings.FindAsync(id);
            if (meeting == null) return;

            // attach cancellation notes if provided
            if (!string.IsNullOrWhiteSpace(notes))
            {
                meeting.Notes = notes;
                _context.Meetings.Update(meeting);
                await _context.SaveChangesAsync();
            }

            _context.Meetings.Remove(meeting);
            await _context.SaveChangesAsync();

            await NotifyMeetingCancelledAsync(meeting, notes);
        }

        public async Task<List<MeetingDto>> GetAllMeetingsAsync()
        {
            return await GetAllMeetingsAsync(1, 100);
        }

        public async Task<List<MeetingDto>> GetAllMeetingsAsync(int page, int pageSize)
        {
            var skip = (Math.Max(1, page) - 1) * Math.Max(1, pageSize);
            var meetings = await _context.Meetings
                .OrderByDescending(m => m.MeetingDate)
                .Skip(skip)
                .Take(Math.Clamp(pageSize, 1, 500))
                .ToListAsync();

            var result = new List<MeetingDto>();
            if (!meetings.Any()) return result;

            var contactIds = meetings.Select(m => m.ClientId).Distinct().ToList();
            var productIds = meetings.Select(m => m.ProductId).Distinct().ToList();

            var contacts = await _context.Contacts.Where(c => contactIds.Contains(c.Id)).ToListAsync();
            var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

            var contactsById = contacts.ToDictionary(c => c.Id);
            var productsById = products.ToDictionary(p => p.Id);

            foreach (var m in meetings)
            {
                contactsById.TryGetValue(m.ClientId, out var contact);
                productsById.TryGetValue(m.ProductId, out var product);

                result.Add(new MeetingDto
                {
                    UserId = m.UserId,
                    Contact = contact,
                    Product = product,
                    Meeting = m
                });
            }

            return result;
        }

        public async Task<List<MeetingDto>> GetAllMeetingsByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
            return await GetAllMeetingsByUserIdAsync(userId, 1, 100);
        }

        public async Task<List<MeetingDto>> GetAllMeetingsByUserIdAsync(string userId, int page, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));

            var skip = (Math.Max(1, page) - 1) * Math.Max(1, pageSize);
            var meetings = await _context.Meetings
                .Where(x => x.UserId == userId)
                .OrderByDescending(m => m.MeetingDate)
                .Skip(skip)
                .Take(Math.Clamp(pageSize, 1, 500))
                .ToListAsync();

            var result = new List<MeetingDto>();
            if (!meetings.Any()) return result;

            var contactIds = meetings.Select(m => m.ClientId).Distinct().ToList();
            var productIds = meetings.Select(m => m.ProductId).Distinct().ToList();

            var contacts = await _context.Contacts.Where(c => contactIds.Contains(c.Id)).ToListAsync();
            var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

            var contactsById = contacts.ToDictionary(c => c.Id);
            var productsById = products.ToDictionary(p => p.Id);

            foreach (var m in meetings)
            {
                contactsById.TryGetValue(m.ClientId, out var contact);
                productsById.TryGetValue(m.ProductId, out var product);

                result.Add(new MeetingDto
                {
                    UserId = m.UserId,
                    Contact = contact,
                    Product = product,
                    Meeting = m
                });
            }

            return result;
        }

        public async Task<MeetingDto> GetMeetingByIdAsync(int id)
        {
            var meeting = await _context.Meetings.FindAsync(id);
            if (meeting == null) return null;

            var contact = await _context.Contacts.FindAsync(meeting.ClientId);
            var product = await _context.Products.FindAsync(meeting.ProductId);

            return new MeetingDto
            {
                UserId = meeting.UserId,
                Contact = contact,
                Product = product,
                Meeting = meeting
            };
        }

        public async Task UpdateMeetingAsync(MeetingDto meetingDto, string notes)
        {
            if (meetingDto == null) throw new ArgumentNullException(nameof(meetingDto));
            if (meetingDto.Meeting == null) throw new ArgumentException("Meeting is required", nameof(meetingDto));

            var existing = await _context.Meetings.FindAsync(meetingDto.Meeting.Id);
            if (existing == null) throw new InvalidOperationException($"Meeting with id {meetingDto.Meeting.Id} not found");

            var oldDate = existing.MeetingDate;
            existing.MeetingDate = meetingDto.Meeting.MeetingDate;
            existing.ClientId = meetingDto.Contact?.Id ?? existing.ClientId;
            existing.ProductId = meetingDto.Product?.Id ?? existing.ProductId;
            existing.Notes = string.IsNullOrWhiteSpace(notes) ? (meetingDto.Meeting.Notes ?? existing.Notes) : notes;

            _context.Meetings.Update(existing);
            await _context.SaveChangesAsync();

            await NotifyMeetingRescheduledAsync(existing, oldDate, notes);
        }

        private async Task NotifyMeetingBookedAsync(Meeting meeting)
        {

            EmailDto notice;

            // Load common related entities
            var contact = await _context.Contacts.FindAsync(meeting.ClientId);
            var product = await _context.Products.FindAsync(meeting.ProductId);
            var user = await _context.Users.FindAsync(meeting.UserId);

            if (user == null)
                throw new InvalidOperationException($"User with id {meeting.UserId} not found");

            if (contact == null)
                throw new InvalidOperationException($"Contact with id {meeting.ClientId} not found");

            if (product == null)
                throw new InvalidOperationException($"Product with id {meeting.ProductId} not found");

            // 1) Notify the sales rep / meeting owner
            var ownerNotice = NoticeConstant.NotifyMeetingBooked(
                user.UserName ?? string.Empty,
                user.Email ?? string.Empty,
                meeting,
                contact,
                product
            );
            await _emailService.SendEmailAsync(ownerNotice);

            // 2) Notify the client (attendee)
            var clientNotice = NoticeConstant.NotifyMeetingBookedClient(
                contact,
                meeting,
                product
            );
            await _emailService.SendEmailAsync(clientNotice);

            // 3) Notify all admins
            var admins = await (from u in _context.Users
                                join ur in _context.Set<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>() on u.Id equals ur.UserId
                                join r in _context.Set<Microsoft.AspNetCore.Identity.IdentityRole>() on ur.RoleId equals r.Id
                                where r.Name == "Admin"
                                select u).ToListAsync();

            if (admins == null || !admins.Any())
            {
                // fallback: notify owner as admin if no admins configured
                admins = new List<ProcessZero.Domain.ApplicationUser> { user };
            }

            foreach (var admin in admins)
            {
                // avoid sending duplicate to owner if owner is included in admins and already notified
                if (admin.Id == user.Id) continue;

                var adminNotice = NoticeConstant.NotifyMeetingBooked(
                    admin.UserName ?? string.Empty,
                    admin.Email ?? string.Empty,
                    meeting,
                    contact,
                    product
                );

                await _emailService.SendEmailAsync(adminNotice);
            }
        }

        private async Task NotifyMeetingCancelledAsync(Meeting meeting, string? notes)
        {
            if (meeting == null) throw new ArgumentNullException(nameof(meeting));

            // Load related entities
            var contact = await _context.Contacts.FindAsync(meeting.ClientId);
            var product = await _context.Products.FindAsync(meeting.ProductId);
            var user = await _context.Users.FindAsync(meeting.UserId);

            if (user == null)
                throw new InvalidOperationException($"User with id {meeting.UserId} not found");

            if (contact == null)
                throw new InvalidOperationException($"Contact with id {meeting.ClientId} not found");

            if (product == null)
                throw new InvalidOperationException($"Product with id {meeting.ProductId} not found");

            // 1) Notify the sales rep / meeting owner (include notes as reason)
            var ownerNotice = NoticeConstant.NotifyMeetingCancelled(
                user.UserName ?? string.Empty,
                user.Email ?? string.Empty,
                meeting,
                contact,
                product,
                null,
                string.IsNullOrWhiteSpace(notes) ? null : notes
            );
            await _emailService.SendEmailAsync(ownerNotice);

            // 2) Notify the client (attendee)
            // do not include cancellation reason/notes for client-facing message
            var clientNotice = NoticeConstant.NotifyMeetingCancelledClient(
                contact,
                meeting,
                product,
                null,
                null
            );
            await _emailService.SendEmailAsync(clientNotice);

            // 3) Notify all admins
            var admins = await (from u in _context.Users
                                join ur in _context.Set<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>() on u.Id equals ur.UserId
                                join r in _context.Set<Microsoft.AspNetCore.Identity.IdentityRole>() on ur.RoleId equals r.Id
                                where r.Name == "Admin"
                                select u).ToListAsync();

            if (admins == null || !admins.Any())
            {
                admins = new List<ProcessZero.Domain.ApplicationUser> { user };
            }

            foreach (var admin in admins)
            {
                // avoid sending duplicate to owner if owner is included in admins and already notified
                if (admin.Id == user.Id) continue;

                var adminNotice = NoticeConstant.NotifyMeetingCancelled(
                    admin.UserName ?? string.Empty,
                    admin.Email ?? string.Empty,
                    meeting,
                    contact,
                    product,
                    null,
                    string.IsNullOrWhiteSpace(notes) ? null : notes
                );

                await _emailService.SendEmailAsync(adminNotice);
            }
        }

        private async Task NotifyMeetingRescheduledAsync(Meeting meeting, DateTime oldStartTime, string? notes = null)
        {
            if (meeting == null) throw new ArgumentNullException(nameof(meeting));
            // Load related entities
            var contact = await _context.Contacts.FindAsync(meeting.ClientId);
            var product = await _context.Products.FindAsync(meeting.ProductId);
            var user = await _context.Users.FindAsync(meeting.UserId);
            if (user == null)
                throw new InvalidOperationException($"User with id {meeting.UserId} not found");
            if (contact == null)
                throw new InvalidOperationException($"Contact with id {meeting.ClientId} not found");
            if (product == null)
                throw new InvalidOperationException($"Product with id {meeting.ProductId} not found");
            // 1) Notify the sales rep / meeting owner
            var ownerNotice = NoticeConstant.NotifyMeetingRescheduled(
                user.UserName ?? string.Empty,
                user.Email ?? string.Empty,
                meeting,
                contact,
                product,
                oldStartTime
            );
            await _emailService.SendEmailAsync(ownerNotice);
            // 2) Notify the client (attendee)
            var clientNotice = NoticeConstant.NotifyMeetingRescheduledClient(
                contact,
                meeting,
                product,
                oldStartTime
            );
            await _emailService.SendEmailAsync(clientNotice);
            // 3) Notify all admins
            var admins = await (from u in _context.Users
                                join ur in _context.Set<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>() on u.Id equals ur.UserId
                                join r in _context.Set<Microsoft.AspNetCore.Identity.IdentityRole>() on ur.RoleId equals r.Id
                                where r.Name == "Admin"
                                select u).ToListAsync();
            if (admins == null || !admins.Any())
            {
                admins = new List<ProcessZero.Domain.ApplicationUser> { user };
            }
            foreach (var admin in admins)
            {
                // avoid sending duplicate to owner if owner is included in admins and already notified
                if (admin.Id == user.Id) continue;
                var adminNotice = NoticeConstant.NotifyMeetingRescheduled(
                    admin.UserName ?? string.Empty,
                    admin.Email ?? string.Empty,
                    meeting,
                    contact,
                    product,
                    oldStartTime,
                    meeting.Notes
                );
                await _emailService.SendEmailAsync(adminNotice);
            }
        }
    }
}
