using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Represents an Agency profile created by an Admin on the admin portal.
    /// Each agency has its own login account (an ApplicationUser in the "Agency" role)
    /// whose Id is stored in LinkedUserId. Users listed in <see cref="AgencyManager"/>
    /// are granted write access; everyone else (except Admin) is read-only.
    /// </summary>
    public class AgencyProfile
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Agency display name (also used as the login UserName).</summary>
        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Free-text description of the agency.</summary>
        [StringLength(2000)]
        public string? Description { get; set; }

        /// <summary>
        /// The AspNetUsers (ApplicationUser) Id of the dedicated login account for this agency.
        /// Created automatically when the admin creates the agency profile.
        /// </summary>
        [Required, StringLength(450)]
        public string LinkedUserId { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AgencyManager> Managers { get; set; } = new List<AgencyManager>();
    }

    /// <summary>
    /// Join table: an existing ApplicationUser (from the entire application's AspNetUsers)
    /// granted write (read & write) access to an agency account by the Admin.
    /// Everyone else who can authenticate against the agency sees it read-only.
    /// </summary>
    public class AgencyManager
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AgencyProfileId { get; set; }

        public AgencyProfile AgencyProfile { get; set; } = null!;

        /// <summary>ApplicationUser Id of the manager granted write access.</summary>
        [Required, StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    }
}