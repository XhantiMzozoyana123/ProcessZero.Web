using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    /// Implements Agency profile management and access resolution.
    /// Creating an agency also creates its dedicated login account (ApplicationUser in the
    /// "Agency" role). Managers are chosen by the Admin from existing AspNetUsers and are
    /// the only non-admin users with write access; the agency login itself also has write
    /// access to its own CRM. Everything else is read-only.
    /// </summary>
    public class AgencyService : IAgencyService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AgencyService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        // ───────────────────────────────────────────────
        // Profile management (Admin)
        // ───────────────────────────────────────────────

        public async Task<AgencyDto> CreateAsync(CreateAgencyDto dto)
        {
            if (dto is null) throw new ArgumentException("Payload is required.");
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new ArgumentException("Agency name is required.");
            if (string.IsNullOrWhiteSpace(dto.Password)) throw new ArgumentException("Password is required.");

            var name = dto.Name.Trim();
            var email = string.IsNullOrWhiteSpace(dto.Email) ? $"{Slug(name)}@agencies.processzero.xyz" : dto.Email!.Trim();

            if (await _userManager.FindByEmailAsync(email) is not null)
                throw new InvalidOperationException("A user with this email already exists.");
            if (await _userManager.FindByNameAsync(name) is not null)
                throw new InvalidOperationException("A user with this username already exists.");

            // Create the dedicated login account for the agency in the "Agency" role.
            var login = new ApplicationUser
            {
                UserName = name,
                Email = email,
                EmailConfirmed = true,
                FirstName = name
            };

            var createResult = await _userManager.CreateAsync(login, dto.Password);
            if (!createResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(login, "Agency");

            var profile = new AgencyProfile
            {
                Name = name,
                Description = dto.Description,
                LinkedUserId = login.Id
            };

            _db.AgencyProfiles.Add(profile);
            await _db.SaveChangesAsync();

            await SetManagersAsync(profile, dto.ManagerUserIds ?? new List<string>());
            return (await GetByIdAsync(profile.Id))!;
        }

        public async Task<AgencyDto> UpdateAsync(int id, UpdateAgencyDto dto)
        {
            var profile = await _db.AgencyProfiles.Include(a => a.Managers).FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new InvalidOperationException("Agency not found.");

            profile.Name = dto.Name.Trim();
            profile.Description = dto.Description;
            profile.IsActive = dto.IsActive;
            profile.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await SetManagersAsync(profile, dto.ManagerUserIds ?? new List<string>());
            return (await GetByIdAsync(id))!;
        }

        public async Task DeleteAsync(int id)
        {
            var profile = await _db.AgencyProfiles.FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new InvalidOperationException("Agency not found.");

            // Remove the linked login account as well (agency profile is meaningless without it).
            var login = await _userManager.FindByIdAsync(profile.LinkedUserId);
            _db.AgencyProfiles.Remove(profile);
            await _db.SaveChangesAsync();
            if (login is not null)
                await _userManager.DeleteAsync(login);
        }

        public async Task ResetPasswordAsync(int id, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword)) throw new ArgumentException("New password is required.");

            var profile = await _db.AgencyProfiles.FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new InvalidOperationException("Agency not found.");
            var login = await _userManager.FindByIdAsync(profile.LinkedUserId)
                ?? throw new InvalidOperationException("Agency login account not found.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(login);
            var result = await _userManager.ResetPasswordAsync(login, token, newPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        public async Task<List<AgencyDto>> GetAllAsync()
        {
            var profiles = await _db.AgencyProfiles
                .Include(a => a.Managers)
                .OrderBy(a => a.Name)
                .ToListAsync();

            var dtos = new List<AgencyDto>();
            foreach (var p in profiles) dtos.Add(await ToDtoAsync(p));
            return dtos;
        }

        public async Task<AgencyDto?> GetByIdAsync(int id)
        {
            var profile = await _db.AgencyProfiles.Include(a => a.Managers).FirstOrDefaultAsync(a => a.Id == id);
            return profile is null ? null : await ToDtoAsync(profile);
        }

        // ───────────────────────────────────────────────
        // Access resolution
        // ───────────────────────────────────────────────

        public async Task<List<AgencyDto>> GetForUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return new List<AgencyDto>();

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return new List<AgencyDto>();

            bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var query = _db.AgencyProfiles.Include(a => a.Managers).AsQueryable();
            if (!isAdmin)
            {
                query = query.Where(a => a.LinkedUserId == userId || a.Managers.Any(m => m.UserId == userId));
            }

            var profiles = await query.OrderBy(a => a.Name).ToListAsync();
            var dtos = new List<AgencyDto>();
            foreach (var p in profiles) dtos.Add(await ToDtoAsync(p));
            return dtos;
        }

        public async Task<bool> CanWriteAsync(int agencyProfileId, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;

            var profile = await _db.AgencyProfiles.Include(a => a.Managers)
                .FirstOrDefaultAsync(a => a.Id == agencyProfileId);
            if (profile is null) return false;

            // Read & write is granted to the Admin and to managers chosen by the Admin.
            // The agency login account (username/password) is read-only by default.
            var user = await _userManager.FindByIdAsync(userId);
            if (user is not null && await _userManager.IsInRoleAsync(user, "Admin")) return true;

            return profile.Managers.Any(m => m.UserId == userId);
        }

        public async Task<bool> CanViewAsync(int agencyProfileId, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;

            var profile = await _db.AgencyProfiles.Include(a => a.Managers)
                .FirstOrDefaultAsync(a => a.Id == agencyProfileId);
            if (profile is null) return false;

            if (profile.LinkedUserId == userId) return true;

            var user = await _userManager.FindByIdAsync(userId);
            if (user is not null && await _userManager.IsInRoleAsync(user, "Admin")) return true;

            return profile.Managers.Any(m => m.UserId == userId);
        }

        // ───────────────────────────────────────────────
        // Helpers
        // ───────────────────────────────────────────────

        private async Task SetManagersAsync(AgencyProfile profile, List<string> managerUserIds)
        {
            var desired = (managerUserIds ?? new List<string>()).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();

            // Validate that every requested manager exists in AspNetUsers.
            foreach (var uid in desired)
            {
                if (await _userManager.FindByIdAsync(uid) is null)
                    throw new InvalidOperationException($"Manager user '{uid}' does not exist.");
            }

            var current = profile.Managers.ToList();

            foreach (var existing in current.Where(m => !desired.Contains(m.UserId)).ToList())
                _db.AgencyManagers.Remove(existing);

            foreach (var uid in desired.Where(uid => current.All(m => m.UserId != uid)))
                _db.AgencyManagers.Add(new AgencyManager { AgencyProfileId = profile.Id, UserId = uid });

            await _db.SaveChangesAsync();
            profile.Managers = await _db.AgencyManagers.Where(m => m.AgencyProfileId == profile.Id).ToListAsync();
        }

        private async Task<AgencyDto> ToDtoAsync(AgencyProfile profile)
        {
            var dto = new AgencyDto
            {
                Id = profile.Id,
                Name = profile.Name,
                Description = profile.Description,
                LinkedUserId = profile.LinkedUserId,
                IsActive = profile.IsActive
            };

            var login = await _userManager.FindByIdAsync(profile.LinkedUserId);
            dto.LinkedUserName = login?.UserName;

            foreach (var m in profile.Managers)
            {
                var user = await _userManager.FindByIdAsync(m.UserId);
                dto.Managers.Add(new AgencyManagerDto
                {
                    UserId = m.UserId,
                    UserName = user?.UserName,
                    Email = user?.Email
                });
            }

            return dto;
        }

        private static string Slug(string name)
        {
            var s = new string(name.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
            return string.IsNullOrEmpty(s) ? "agency" : s;
        }
    }
}