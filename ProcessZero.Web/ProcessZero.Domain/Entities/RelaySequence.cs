using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    public class RelaySequence : BaseEntity
    {
        public int RelayCampaignId { get; set; }

        public RelayCampaign RelayCampaign { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool MessageRotationEnabled { get; set; }

        public bool InboxRotationEnabled { get; set; }

        public ICollection<RelaySequenceStep> Steps { get; set; }
            = new List<RelaySequenceStep>();
    }
}
