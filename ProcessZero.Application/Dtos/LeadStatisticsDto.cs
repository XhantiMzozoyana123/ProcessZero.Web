using System;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// Lead engagement statistics DTO
    /// </summary>
    public class LeadStatisticsDto
    {
        public int TotalEmailsReceived { get; set; }
        public int TotalOpens { get; set; }
        public int TotalClicks { get; set; }
        public int TotalReplies { get; set; }
        public DateTime? LastEngagementDate { get; set; }
    }
}
