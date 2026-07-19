using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ProcessZero.Application.Constants;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace ProcessZero.Infrastructure.Services
{
    public class UserService : IUserService
    {
        /// <summary>
        /// Must match the prefix used in CheckBannedUserMiddleware so we invalidate the right key.
        /// </summary>
        private const string BanCacheKeyPrefix = "ban:";

        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _cache;

        public UserService(
            ApplicationDbContext context,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache cache)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<List<ApplicationUser>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.UserName)
                .ToListAsync(cancellationToken);
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task BanUserAsync(string id, string? reason = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);
            if (user == null) throw new InvalidOperationException($"User with id {id} not found");

            user.IsBanned = true;
            user.BannedAt = DateTime.UtcNow;
            user.BanReason = reason;

            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            // Immediately invalidate the cached ban status so the middleware picks up the change
            _cache.Remove(BanCacheKeyPrefix + id);

            // Notify the user by email that their account was banned
            try
            {
                var bannedBy = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                // Use Username if available for recipient name, fall back to email
                var recipientName = !string.IsNullOrWhiteSpace(user.UserName) ? user.UserName : user.Email ?? string.Empty;
                var email = NoticeConstant.NotifyAccountBanned(recipientName, user.Email ?? string.Empty, reason, user.BannedAt, bannedBy);
                await _emailService.SendEmailAsync(email);
            }
            catch
            {
                // swallow email errors to avoid impacting the ban operation
            }
        }

        public async Task UnbanUserAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);
            if (user == null) throw new InvalidOperationException($"User with id {id} not found");

            user.IsBanned = false;
            user.BannedAt = null;
            user.BanReason = null;

            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            // Immediately invalidate the cached ban status so the middleware picks up the change
            _cache.Remove(BanCacheKeyPrefix + id);

            // Notify the user by email that their account was unbanned
            try
            {
                var unbannedBy = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var recipientName = !string.IsNullOrWhiteSpace(user.UserName) ? user.UserName : user.Email ?? string.Empty;
                var email = NoticeConstant.NotifyAccountUnbanned(recipientName, user.Email ?? string.Empty, null, DateTime.UtcNow, unbannedBy);
                await _emailService.SendEmailAsync(email);
            }
            catch
            {
                // swallow email errors to avoid impacting the unban operation
            }
        }

        public async Task<bool> IsUserBannedAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new { u.IsBanned })
                .FirstOrDefaultAsync(cancellationToken);

            return user?.IsBanned ?? false;
        }

        public async Task<List<ApplicationUser>> GetBannedUsersAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Where(u => u.IsBanned)
                .OrderBy(u => u.UserName)
                .ToListAsync(cancellationToken);
        }
    }
}
