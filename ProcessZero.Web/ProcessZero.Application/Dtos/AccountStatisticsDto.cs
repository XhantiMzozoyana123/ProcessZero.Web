using ProcessZero.Domain.Entities;
using System;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// Email account statistics DTO with performance metrics
    /// </summary>
    public class AccountStatisticsDto
    {
        public int EmailsSentToday { get; set; }
        public int EmailsSentTotal { get; set; }
        public double ReputationScore { get; set; }
        public decimal DeliveryRate { get; set; }
    }
}
