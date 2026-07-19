using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    public class RelayEmailActivity : BaseEntity
    {
        public int RelayCampaignId { get; set; }
        public RelayCampaign RelayCampaign { get; set; }

        public int RelayLeadId { get; set; }
        public RelayLead RelayLead { get; set; }

        public int RelayInboxId { get; set; }
        public RelayEmailAccount RelayInbox { get; set; }

        public int EmailVariantId { get; set; }
        public RelayEmailVariant EmailVariant { get; set; }

        public string GmailMessageId { get; set; } = string.Empty;
        public string GmailThreadId { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }

        public EmailStatus Status { get; set; }

        public bool Replied { get; set; }

        public DateTime? RepliedAt { get; set; }
    }

    public enum EmailStatus
    {
        Pending,
        Sent,
        Delivered,
        Replied,
        Bounced,
        Failed
    }
}
