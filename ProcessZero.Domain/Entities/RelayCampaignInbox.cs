using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    public class RelayCampaignInbox : BaseEntity
    {
        public int RelayCampaignId { get; set; }

        public RelayCampaign RelayCampaign { get; set; }

        public int RelayInboxId { get; set; }

        public RelayEmailAccount RelayInbox { get; set; }
    }
}
