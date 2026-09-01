using ProcessZero.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    /// <summary>
    /// DTO describing an Agency profile together with its login username and managers.
    /// </summary>
    public class AgencyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string LinkedUserId { get; set; } = string.Empty;
        public string? LinkedUserName { get; set; }
        public bool IsActive { get; set; }
        public List<AgencyManagerDto> Managers { get; set; } = new();
    }

    public class AgencyManagerDto
    {
        public string UserId { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>
    /// Payload for creating an agency profile. The admin supplies the name, description,
    /// the login password for the new agency account, and the list of existing user Ids
    /// (from AspNetUsers) who should receive write access.
    /// </summary>
    public class CreateAgencyDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Email { get; set; }
        public string Password { get; set; } = string.Empty;
        public List<string> ManagerUserIds { get; set; } = new();
    }

    public class UpdateAgencyDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public List<string> ManagerUserIds { get; set; } = new();
    }

    public interface IAgencyService
    {
        // ── Profile management (Admin) ──────────────────────────
        Task<AgencyDto> CreateAsync(CreateAgencyDto dto);
        Task<AgencyDto> UpdateAsync(int id, UpdateAgencyDto dto);
        Task DeleteAsync(int id);
        Task<List<AgencyDto>> GetAllAsync();
        Task<AgencyDto?> GetByIdAsync(int id);
        Task ResetPasswordAsync(int id, string newPassword);

        // ── Access resolution ───────────────────────────────────
        /// <summary>Agencies the given user may see: admin sees all, the agency login sees its own, managers see theirs.</summary>
        Task<List<AgencyDto>> GetForUserAsync(string userId);
        /// <summary>True if the user is an Admin, the agency's linked login, or a manager granted write access.</summary>
        Task<bool> CanWriteAsync(int agencyProfileId, string userId);
        /// <summary>True if the user can see the agency at all.</summary>
        Task<bool> CanViewAsync(int agencyProfileId, string userId);
    }
}