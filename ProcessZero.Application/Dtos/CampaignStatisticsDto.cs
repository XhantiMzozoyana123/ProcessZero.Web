using ProcessZero.Domain.Entities;
using System;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// Campaign statistics DTO with engagement metrics
    /// </summary>
    public class CampaignStatisticsDto
    {
        public int TotalLeads { get; set; }
        public int EmailsSent { get; set; }
        public int EmailsDelivered { get; set; }
        public int EmailsOpened { get; set; }
        public int EmailsClicked { get; set; }
        public int EmailsReplied { get; set; }
        public int EmailsBounced { get; set; }
        public decimal OpenRate { get; set; }
        public decimal ClickRate { get; set; }
        public decimal ReplyRate { get; set; }
        public decimal BounceRate { get; set; }
    }
}
