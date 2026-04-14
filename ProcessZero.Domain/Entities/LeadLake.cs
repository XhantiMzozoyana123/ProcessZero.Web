using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    public class LeadLake : BaseEntity
    {

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Company { get; set; } = string.Empty;

        public string Job { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public LeadLakeIndustry Industry { get; set; }
    }


    public enum LeadLakeIndustry
    {
        Technology,
        Finance,
        Healthcare,
        Education,
        Retail,
        Manufacturing,
        Energy,
        Transportation,
        Entertainment,
        Hospitality,
        Other
    }
}
