using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    public class RelayLead : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Company { get; set; } = string.Empty;

        public string JobTitle { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public LeadLakeIndustry Industry { get; set; }

        public LeadIntent Intent { get; set; }

        public ICollection<RelayCampaignLead> Campaigns { get; set; }
            = new List<RelayCampaignLead>();

        public ICollection<RelayEmailActivity> Activities { get; set; }
            = new List<RelayEmailActivity>();
    }
}
