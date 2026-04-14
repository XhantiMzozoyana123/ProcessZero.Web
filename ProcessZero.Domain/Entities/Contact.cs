using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    public class Contact : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Company { get; set; } = string.Empty;

        public string Job { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public decimal ClosedAmount { get; set; }

        public ContactStatus Status { get; set; }
    }

    public enum ContactStatus
    {
        Reached,
        FollowUp,
        Converted,
        Active,
        Deactivated
    }
}
