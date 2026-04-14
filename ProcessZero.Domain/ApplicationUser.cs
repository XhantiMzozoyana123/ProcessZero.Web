using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Domain
{
    public class ApplicationUser : IdentityUser
    {
        // Indicates whether the account is banned (blocked from accessing the system)
        public bool IsBanned { get; set; } = false;

        // When the account was banned (UTC)
        public DateTime? BannedAt { get; set; }

        // Optional reason for the ban — can be used for admin audit/logging
        public string? BanReason { get; set; }
    }
}
