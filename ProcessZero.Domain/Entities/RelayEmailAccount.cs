using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    public class RelayEmailAccount : BaseEntity
    {
        // Identity
        public string EmailAddress { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        // Google OAuth
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? TokenExpiry { get; set; }

        // Campaign safety (VERY important)
        public int DailySendLimit { get; set; } = 50;
        public int SentToday { get; set; } = 0;

        // Health (driven by OAuth + sending results)
        public bool IsActive { get; set; } = true;
        public AccountHealthStatus HealthStatus { get; set; } = AccountHealthStatus.Healthy;
        public string? HealthCheckError { get; set; }

        // Optional tracking (useful later)
        public DateTime? LastUsedAt { get; set; }

        public ICollection<RelayCampaignInbox> Campaigns { get; set; }
     = new List<RelayCampaignInbox>();

        public ICollection<RelayEmailActivity> Activities { get; set; }
            = new List<RelayEmailActivity>();
    }

    public enum AccountHealthStatus
    {
        Healthy,
        Warning,
        Critical,
        Disabled
    }
}
