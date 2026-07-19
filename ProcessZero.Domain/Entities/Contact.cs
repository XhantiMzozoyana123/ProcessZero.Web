using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Represents a contact (lead or client) in the sales pipeline.
    /// Tracks personal details, company information, and sales status.
    /// </summary>
    public class Contact : BaseEntity
    {
        /// <summary>First name of the contact.</summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>Last name of the contact.</summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>Primary email address of the contact (unique per user).</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Phone number of the contact.</summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>Company the contact works for.</summary>
        public string Company { get; set; } = string.Empty;

        /// <summary>Job title or role of the contact.</summary>
        public string Job { get; set; } = string.Empty;

        /// <summary>Geographic location of the contact.</summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>Total amount of revenue closed with this contact.</summary>
        public decimal ClosedAmount { get; set; }

        /// <summary>Current status of the contact in the sales funnel.</summary>
        public ContactStatus Status { get; set; }
    }

    /// <summary>
    /// Defines the lifecycle stages of a contact through the sales pipeline.
    /// </summary>
    public enum ContactStatus
    {
        /// <summary>Initial outreach has been completed (email/call).</summary>
        Reached,

        /// <summary>Follow-up is in progress.</summary>
        FollowUp,

        /// <summary>Contact has been converted into a paying customer.</summary>
        Converted,

        /// <summary>Active ongoing client.</summary>
        Active,

        /// <summary>Contact has been removed or deactivated.</summary>
        Deactivated
    }
}
