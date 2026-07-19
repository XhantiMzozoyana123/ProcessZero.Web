using ProcessZero.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Returns a read-only list of all application users (Identity users).
        /// </summary>
        Task<List<ApplicationUser>> GetAllUsersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a single application user by id, or null if not found.
        /// </summary>
        Task<ApplicationUser?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a user account as banned. Implementations should set a flag on the user
        /// and persist any optional ban metadata (reason, bannedAt) as appropriate.
        /// </summary>
        Task BanUserAsync(string id, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the banned flag from a user account.
        /// </summary>
        Task UnbanUserAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns true when the user account is currently banned.
        /// </summary>
        Task<bool> IsUserBannedAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a list of users that are currently banned.
        /// </summary>
        Task<List<ApplicationUser>> GetBannedUsersAsync(CancellationToken cancellationToken = default);
    }
}
