using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProcessZero.Domain
{
    /// <summary>
    /// Application user entity extending ASP.NET Core Identity.
    /// In Clean Architecture, this extends IdentityUser but contains
    /// only domain-relevant properties.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// User's first name
        /// </summary>
        [StringLength(100)]
        public string? FirstName { get; set; }

        /// <summary>
        /// User's last name
        /// </summary>
        [StringLength(100)]
        public string? LastName { get; set; }

        /// <summary>
        /// Indicates whether the account is banned (blocked from accessing the system)
        /// </summary>
        public bool IsBanned { get; set; } = false;

        /// <summary>
        /// When the account was banned (UTC)
        /// </summary>
        public DateTime? BannedAt { get; set; }

        /// <summary>
        /// Optional reason for the ban — can be used for admin audit/logging
        /// </summary>
        public string? BanReason { get; set; }
    }
}