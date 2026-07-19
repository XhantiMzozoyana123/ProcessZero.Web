using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    public class RelayCampaign : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int DailySendLimit { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public ICollection<RelaySequence> Sequences { get; set; }
            = new List<RelaySequence>();

        public ICollection<RelayCampaignInbox> Inboxes { get; set; }
            = new List<RelayCampaignInbox>();

        public ICollection<RelayCampaignLead> Leads { get; set; }
            = new List<RelayCampaignLead>();
    }
}
