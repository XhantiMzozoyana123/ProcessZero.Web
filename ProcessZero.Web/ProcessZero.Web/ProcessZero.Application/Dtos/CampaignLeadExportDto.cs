using System;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// CSV export record for campaign lead data
    /// Contains comprehensive metrics for lead performance in campaigns
    /// </summary>
    public class CampaignLeadExportDto
    {
        // Lead Information
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string LinkedInUrl { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;

        // Campaign Status
        public string Status { get; set; } = string.Empty;
        public int CurrentStep { get; set; }
        public string AddedDate { get; set; } = string.Empty;

        // Engagement Metrics
        public int TotalEmailsSent { get; set; }
        public int TotalOpens { get; set; }
        public int TotalClicks { get; set; }
        public int TotalReplies { get; set; }
        public int TotalBounces { get; set; }

        // Calculated Rates
        public decimal OpenRate { get; set; }
        public decimal ClickRate { get; set; }
        public decimal ReplyRate { get; set; }

        // Timing Information
        public string FirstEmailSent { get; set; } = string.Empty;
        public string LastEmailSent { get; set; } = string.Empty;
        public string NextEmailScheduled { get; set; } = string.Empty;
        public string RepliedDate { get; set; } = string.Empty;
        public string ClickedDate { get; set; } = string.Empty;
        public string CompletedDate { get; set; } = string.Empty;

        // Last Email Details
        public string LastEmailSubject { get; set; } = string.Empty;
        public string LastEmailStatus { get; set; } = string.Empty;

        // Additional Information
        public string AssignedVariant { get; set; } = string.Empty;
        public string StopReason { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        // Quality Indicators
        public string IsUnsubscribed { get; set; } = string.Empty;
        public string IsInvalid { get; set; } = string.Empty;
        public string IsEngaged { get; set; } = string.Empty;
        public string IsQualified { get; set; } = string.Empty;
    }
}
