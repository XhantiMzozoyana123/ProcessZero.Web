using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    public class RelayCampaignLead : BaseEntity
    {
        public int RelayCampaignId { get; set; }

        public RelayCampaign RelayCampaign { get; set; }

        public int RelayLeadId { get; set; }

        public RelayLead RelayLead { get; set; }

        public int? CurrentSequenceStepId { get; set; }

        public RelaySequenceStep? CurrentSequenceStep { get; set; }

        public CampaignLeadStatus Status { get; set; }

        public bool Replied { get; set; }

        public bool Unsubscribed { get; set; }

        public bool Completed { get; set; }
    }

    public enum CampaignLeadStatus
    {
        Pending,
        Active,
        Replied,
        Completed,
        Bounced,
        Unsubscribed
    }
}
