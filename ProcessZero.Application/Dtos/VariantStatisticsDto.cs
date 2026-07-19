using System;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// A/B test variant statistics DTO
    /// </summary>
    public class VariantStatisticsDto
    {
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public int TotalSent { get; set; }
        public int TotalOpens { get; set; }
        public int TotalClicks { get; set; }
        public int TotalReplies { get; set; }
        public decimal OpenRate { get; set; }
        public decimal ClickRate { get; set; }
        public decimal ReplyRate { get; set; }
    }
}
