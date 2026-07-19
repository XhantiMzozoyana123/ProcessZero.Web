using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// Notification information for a single user (sales representative) included in a payroll run.
    /// Columns/properties:
    /// - UserName (string): display name of the sales rep.
    /// - Email (string): recipient email address for the notification.
    /// - Amount (decimal): payout amount calculated for the user.
    /// </summary>
    public class PayrollUserNotificationDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Result returned by the payroll service containing the CSV rows and notification targets.
    /// Fields:
    /// - Rows (List&lt;string[]&gt;): CSV rows where Rows[0] is the header (e.g. ["UserId","UserName","AccountNumber","BankName","Amount"]).
    ///   Each subsequent row is a string array matching the header columns.
    /// - AdminEmails (List&lt;string&gt;): list of administrator emails to notify about the generated payroll CSV.
    /// - UserNotifications (List&lt;PayrollUserNotificationDto&gt;): per-user notifications to be sent to sales reps.
    /// </summary>
    public class PayrollReportResult
    {
        // Header + data rows
        public List<string[]> Rows { get; set; } = new List<string[]>();

        // Admin emails to notify
        public List<string> AdminEmails { get; set; } = new List<string>();

        // Per-user notifications (sales reps)
        public List<PayrollUserNotificationDto> UserNotifications { get; set; } = new List<PayrollUserNotificationDto>();
    }
}
