using System;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// Overall statistics DTO across all campaigns
    /// </summary>
    public class OverallStatisticsDto
    {
        public int TotalCampaigns { get; set; }
        public int ActiveCampaigns { get; set; }
        public int TotalLeads { get; set; }
        public int TotalEmailsSent { get; set; }
        public int TotalReplies { get; set; }
        public decimal OverallReplyRate { get; set; }
    }
}
