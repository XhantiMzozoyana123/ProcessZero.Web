using ProcessZero.Application.Dtos;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Constants
{
    public static class NoticeConstant
    {
        private static string BrandedWrapWithTemplate(string contentHtml)
        {
            // Basic email HTML template using Bootstrap from the provided template.
            var template = @"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>Process Zero</title>
  <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css"" rel=""stylesheet"">
</head>
<body style=""background-color:#f4f4f4;"">

<div class=""container py-5"">
  <div class=""mx-auto bg-white rounded shadow"" style=""max-width:600px; overflow:hidden;"">

    <!-- Header -->
    <div class=""text-center text-white p-4"" style=""background:#1f3c88;"">
      <img src=""YOUR_LOGO_URL_HERE"" alt=""Logo"" width=""120"" class=""mb-2"">
      <h1 class=""h4 m-0"">PROCESS <span style=""color:#ff8c00;"">ZERO</span></h1>
      <p class=""mb-0 opacity-75"">Earn. Sell. Scale.</p>
    </div>

    <!-- Content -->
    <div class=""p-4"">{CONTENT}</div>

    <!-- Footer -->
    <div class=""text-center p-3 small text-muted"">
      <p class=""mb-1"">© 2026 Process Zero. All rights reserved.</p>
      <a href=""#"" class=""text-muted"">Unsubscribe</a> |
      <a href=""#"" class=""text-muted"">Preferences</a>
    </div>

  </div>
</div>

</body>
</html>";

            return template.Replace("{CONTENT}", contentHtml ?? string.Empty);
        }

        /// <summary>
        /// Congratulate a user for passing an assessment.
        /// </summary>
        public static EmailDto NotifyAssessmentPassed(
            string recipientName,
            string recipientEmail,
            string assessmentTitle,
            string productName,
            int score,
            int total,
            double percentage)
        {
            var html = $@"
<div style='font-family:Arial, sans-serif; line-height:1.6; color:#333;'>
    <h2 style='color:#1f3c88;'>🎉 Congratulations!</h2>
    <p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>
    <p>Well done — you passed the assessment <strong>{System.Net.WebUtility.HtmlEncode(assessmentTitle)}</strong> for <strong>{System.Net.WebUtility.HtmlEncode(productName)}</strong>.</p>
    <div style='margin:20px 0;padding:12px;border:1px solid #e9ecef;border-radius:6px;background:#fff;'>
        <p style='margin:0;'><strong>Score:</strong> {score} / {total} ({percentage:F1}%)</p>
    </div>
    <p>Keep up the great work! You now meet the requirements for this product.</p>
    <p>Regards,<br/><strong>ProcessZero Team</strong></p>
</div>";

            return new EmailDto
            {
                Subject = $"Congratulations — you passed: {assessmentTitle}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify stakeholders that an assessment was uploaded or updated.
        /// Includes Title, ProductId and PassMark (if present).
        /// </summary>
        public static EmailDto NotifyAssessmentUploaded(
            string recipientName,
            string recipientEmail,
            Assessment assessment,
            string productName,
            string? note = null)
        {
            if (assessment == null) throw new ArgumentNullException(nameof(assessment));

            var noteText = string.IsNullOrWhiteSpace(note)
                ? string.Empty
                : $"<p style='margin-top:12px;'><strong>Note:</strong> {System.Net.WebUtility.HtmlEncode(note.Replace("\n", "<br />"))}</p>";

            var passMarkText = assessment.PassMark.HasValue ? $"<p><strong>Pass mark:</strong> {assessment.PassMark.Value}%</p>" : string.Empty;

            var html = $@"
<div style='font-family:Arial, sans-serif; line-height:1.6; color:#333;'>

    <h2 style='color:#1f3c88;'>📝 Assessment uploaded/updated</h2>

    <p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>

    <p>An assessment has been uploaded or updated in the system. Details are shown below.</p>

    <div style='margin:20px 0;padding:15px;border:1px solid #e0e0e0;border-radius:8px;'>

        <h3 style='margin-bottom:8px;'>{System.Net.WebUtility.HtmlEncode(assessment.Title ?? string.Empty)}</h3>

        <p><strong>Product:</strong> {System.Net.WebUtility.HtmlEncode(productName ?? string.Empty)}</p>

        {passMarkText}

        <p><strong>Uploaded at:</strong> {assessment.UploadedAt:yyyy-MM-dd HH:mm} (UTC)</p>

    </div>

    {noteText}

    <p style='margin-top:20px;'>
        Regards,<br/>
        <strong>ProcessZero Team</strong>
    </p>

</div>";

            return new EmailDto
            {
                Subject = $"Assessment uploaded: {assessment.Title}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify a user that their account has been unbanned.
        /// </summary>
        public static EmailDto NotifyAccountUnbanned(
            string recipientName,
            string recipientEmail,
            string? note = null,
            DateTime? unbannedAt = null,
            string? unbannedBy = null)
        {
            var when = (unbannedAt ?? DateTime.UtcNow).ToString("yyyy-MM-dd HH:mm");
            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $"<p>{System.Net.WebUtility.HtmlEncode(note)}</p>";
            var byText = string.IsNullOrWhiteSpace(unbannedBy) ? string.Empty : $"<p><strong>Unbanned by:</strong> {System.Net.WebUtility.HtmlEncode(unbannedBy)}</p>";

            var html = $@"
<div style='font-family:Arial, sans-serif; line-height:1.5; color:#333;'>

  <h2 style='color:#1f3c88;'>✅ Account restored</h2>

  <p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>

  <p>Good news — your ProcessZero account was <strong>unbanned</strong> on <strong>{when} (UTC)</strong> and access has been restored.</p>

  {byText}

  {noteText}

  <p>If you have any questions or need assistance getting back up and running, please contact support at <a href='mailto:support@processzero.com'>support@processzero.com</a>.</p>

  <hr style='margin:18px 0;' />

  <p style='margin:0;'>Regards,<br/><strong>ProcessZero Team</strong></p>
</div>";

            return new EmailDto
            {
                Subject = "Account restored — ProcessZero",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify an Admin that the partner payroll CSV is ready for download.
        /// Includes a secure download link to retrieve the CSV file (admin-only).
        /// </summary>
        public static EmailDto NotifyPayrollGeneratedAdmin(
            string adminName,
            string adminEmail,
            int month,
            int year,
            string downloadUrl,
            string? note = null)
        {
            var monthName = new DateTime(year, Math.Clamp(month, 1, 12), 1).ToString("MMMM yyyy");
            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $"<p style='margin-top:12px;'><strong>Note:</strong> {note.Replace("\n", "<br />")}</p>";
            // For admin-facing emails, present a user-friendly admin portal link.
            // If a full downloadUrl is provided, extract the filename and point the link to admin.processzero.com/payroll/{filename}
            string adminPortalBase = "https://processzero.com/admin/payroll";
            string adminLink;
            try
            {
                if (!string.IsNullOrWhiteSpace(downloadUrl))
                {
                    var uri = new Uri(downloadUrl);
                    var fileName = System.IO.Path.GetFileName(uri.AbsolutePath);
                    adminLink = string.IsNullOrWhiteSpace(fileName) ? adminPortalBase : $"{adminPortalBase}/{fileName}";
                }
                else
                {
                    adminLink = adminPortalBase;
                }
            }
            catch
            {
                adminLink = adminPortalBase;
            }

            var html = $@"
<div style='font-family:Arial, sans-serif; line-height:1.5; color:#333;'>

  <h2 style='color:#1f3c88;'>📥 Partner payroll CSV ready</h2>

  <p>Hi {System.Net.WebUtility.HtmlEncode(adminName)},</p>

  <p>The partner payroll CSV for <strong>{System.Net.WebUtility.HtmlEncode(monthName)}</strong> is now available for download.</p>

  <div style='margin:12px 0;padding:12px;border:1px solid #e9ecef;border-radius:6px;background:#fff;'>
    <p style='margin:0;'>Click the button below to download the CSV. This link is intended for Admin users only.</p>
    <div style='margin-top:12px;'>
      <a href='{System.Net.WebUtility.HtmlEncode(adminLink)}' target='_blank' style='background-color:#1f3c88;color:#fff;padding:10px 16px;text-decoration:none;border-radius:6px;font-weight:bold;'>Download payroll CSV</a>
    </div>
  </div>

  <p style='margin-top:12px;'>For security, the download link will expire shortly. If you have any issues accessing the file, contact payroll at <a href='mailto:payroll@processzero.com'>payroll@processzero.com</a>.</p>

  {noteText}

  <hr style='margin:18px 0;' />

  <p style='margin:0;'>Regards,<br/><strong>ProcessZero Payroll Team</strong></p>
</div>";

            return new EmailDto
            {
                Subject = $"Partner payroll CSV ready: {monthName}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = adminEmail ?? string.Empty,
                RecipientName = adminName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify a sales representative that the payroll sheet has been generated for them.
        /// Informs the rep of the payout amount and that payment will be executed at the end of the month.
        /// </summary>
        public static EmailDto NotifyPayrollGenerated(
            string recipientName,
            string recipientEmail,
            decimal amount,
            int month,
            int year,
            string? note = null)
        {
            var monthName = new DateTime(year, Math.Clamp(month, 1, 12), 1).ToString("MMMM yyyy");
            var payoutDate = new DateTime(year, Math.Clamp(month, 1, 12), 1).AddMonths(1).AddDays(-1);

            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $"<p style='margin-top:12px;'><strong>Note:</strong> {note.Replace("\n", "<br />")}</p>";

            var html = $@"
<div style='font-family:Arial, sans-serif; line-height:1.5; color:#333;'>

  <h2 style='color:#1f3c88;'>💰 Partner payroll sheet generated</h2>

  <p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>

  <p>We have generated the <strong>partner payroll</strong> sheet for <strong>{System.Net.WebUtility.HtmlEncode(monthName)}</strong>.</p>

  <div style='margin:12px 0;padding:12px;border:1px solid #e9ecef;border-radius:6px;background:#fff;'>
    <p style='margin:0;'><strong>Expected processing:</strong> Payments/transactions will be fulfilled within 48 hours.</p>
  </div>

  <p style='margin-top:12px;'>No action is required from you. If your bank details need updating, please update them in <a href='https://app.processzero.com/account/payroll' target='_blank'>Payroll Settings</a> or contact payroll at <a href='mailto:payroll@processzero.com'>payroll@processzero.com</a>.</p>

  <p>Note: For confidentiality, detailed payroll sheets are restricted to Admin users only. Sales partners will not have access to other partners' payroll sheets or payout amounts. If you require a private breakdown of your own payout, please contact payroll directly and we will assist you in a secure manner.</p>

  {noteText}

  <hr style='margin:18px 0;' />

  <p style='margin:0;'>We appreciate your patience and continued partnership. Thank you for your business.<br/><br/>Regards,<br/><strong>ProcessZero Payroll Team</strong></p>
</div>";

            return new EmailDto
            {
                Subject = $"Partner payroll generated: {monthName}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify a user that their account has been banned.
        /// Intended for sales representatives when an admin has banned their account.
        /// </summary>
        public static EmailDto NotifyAccountBanned(
            string recipientName,
            string recipientEmail,
            string? reason = null,
            DateTime? bannedAt = null,
            string? bannedBy = null)
        {
            var when = (bannedAt ?? DateTime.UtcNow).ToString("yyyy-MM-dd HH:mm");
            var reasonText = string.IsNullOrWhiteSpace(reason) ? string.Empty : $"<p><strong>Reason:</strong> {System.Net.WebUtility.HtmlEncode(reason)}</p>";
            var byText = string.IsNullOrWhiteSpace(bannedBy) ? string.Empty : $"<p><strong>Banned by:</strong> {System.Net.WebUtility.HtmlEncode(bannedBy)}</p>";

            var html = $@"
<div style='font-family:Arial, sans-serif; line-height:1.5; color:#333;'>

  <h2 style='color:#b22222;'>🚫 Account banned</h2>

  <p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>

  <p>We’re writing to let you know that your ProcessZero account was <strong>banned</strong> on <strong>{when} (UTC)</strong>.</p>

  {reasonText}
  {byText}

  <p>If you believe this is a mistake or would like to appeal the decision, please contact our support team at <a href='mailto:support@processzero.com'>support@processzero.com</a> and include any relevant details. We will review your case and respond as soon as possible.</p>

  <hr style='margin:18px 0;' />

  <p style='margin:0;'>Regards,<br/><strong>ProcessZero Team</strong></p>
</div>";

            return new EmailDto
            {
                Subject = "Account banned — ProcessZero",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        // Backwards-compatible default wrapper (branded)
        private static string WrapWithTemplate(string contentHtml)
        {
            return BrandedWrapWithTemplate(contentHtml);
        }

        // Simple client-facing template: minimal Bootstrap, no branding, cleaner for external clients
        private static string SimpleWrapWithTemplate(string contentHtml)
        {
            var template = @"<!doctype html>
<html lang='en'>
  <head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' rel='stylesheet'>
    <title>ProcessZero</title>
    <style>
      body { background-color:#f8f9fa; }
      .card { margin: 24px auto; max-width:600px; }
      .card-body p { color:#333; }
    </style>
  </head>
  <body>
    <div class='card'>
      <div class='card-body'>
        {CONTENT}
      </div>
    </div>
  </body>
</html>";

            return template.Replace("{CONTENT}", contentHtml ?? string.Empty);
        }

        /// <summary>
        /// Announce that an existing product has been updated.
        /// </summary>
        public static EmailDto NotifyProductUpdated(
            string recipientName,
            string recipientEmail,
            Product product,
            string? note = null)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            var noteText = string.IsNullOrWhiteSpace(note)
                ? string.Empty
                : $@"
        <div style='margin-top:20px;padding:12px;background-color:#f5f7fa;border-left:4px solid #1f3c88;'>
            <strong>📝 Update Notes:</strong>
            <p style='margin:5px 0 0 0;'>{note.Replace("\n", "<br />")}</p>
        </div>";

            var productUrlSection = string.IsNullOrWhiteSpace(product.Url)
                ? string.Empty
                : $@"
        <div style='margin:20px 0;'>
            <a href='{System.Net.WebUtility.HtmlEncode(product.Url)}' target='_blank'
               style='background-color:#1f3c88;color:#fff;padding:12px 20px;
                      text-decoration:none;border-radius:6px;font-weight:bold;'>
                🔍 View Updated Product
            </a>
        </div>";

            var negotiableSection = string.IsNullOrWhiteSpace(product.NegotiableAmounts)
                ? string.Empty
                : $"<p><strong>💬 Flexible Pricing:</strong> {System.Net.WebUtility.HtmlEncode(product.NegotiableAmounts)}</p>";

            var html = $@"
<div style='font-family:Arial, sans-serif; line-height:1.6; color:#333;'>

    <h2 style='color:#1f3c88;'>🚀 Product Update Announcement</h2>

    <p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>

    <p>
        We’ve made important updates to one of our products. Please review the latest details below to stay aligned.
    </p>

    <div style='margin:20px 0;padding:15px;border:1px solid #e0e0e0;border-radius:8px;'>

        <h3 style='margin-bottom:8px;'>{System.Net.WebUtility.HtmlEncode(product.Name)}</h3>

        <p>{System.Net.WebUtility.HtmlEncode(product.Description ?? string.Empty)}</p>

        <p><strong>💰 Current Price:</strong> {product.ActualAmount:C}</p>

        {negotiableSection}

    </div>

    {productUrlSection}

    {noteText}

    <p style='margin-top:20px;'>
        ⚠️ <strong>Action Required:</strong> Please ensure any sales materials, outreach messaging, or client discussions reflect these latest updates.
    </p>

    <hr style='margin:30px 0;' />

    <p>
        We appreciate your attention and continued effort in delivering accurate and up-to-date information.
    </p>

    <p>
        Regards,<br/>
        <strong>ProcessZero Team</strong>
    </p>

</div>";

            return new EmailDto
            {
                Subject = $"🚀 Product Update: {product.Name}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Build an email notifying the sales representative that a client they introduced
        /// is no longer active with the company.
        /// </summary>
        /// <param name="salesRepName">Name of the sales representative (recipient)</param>
        /// <param name="salesRepEmail">Email of the sales representative (recipient)</param>
        /// <param name="clientName">Name of the client that was deactivated</param>
        /// <param name="deactivatedOn">Optional deactivation date; if null UtcNow is used</param>
        /// <param name="reason">Optional reason or short note about the deactivation</param>
        public static EmailDto NotifySalesRepClientDeactivated(
            string salesRepName,
            string salesRepEmail,
            string clientName,
            DateTime? deactivatedOn = null,
            string? reason = null)
        {
            var date = (deactivatedOn ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
            var reasonText = string.IsNullOrWhiteSpace(reason)
                ? string.Empty
                : $"\n\nReason: {reason}";

            var html = $@"<h3>Client deactivated: {clientName}</h3>
<p>Dear {salesRepName},</p>
<p>This is to inform you that the client you brought into ProcessZero, <strong>{clientName}</strong>, is no longer active with us as of {date}.{reasonText.Replace("\n", "<br />")}</p>
<p>Please review your records and update any outstanding items related to this client (leads, meetings, invoices). If you need assistance or further details, contact your account manager.</p>
<p>Regards,<br/>ProcessZero Team</p>";

            return new EmailDto
            {
                Subject = $"Client deactivated: {clientName}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = salesRepEmail ?? string.Empty,
                RecipientName = salesRepName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify the sales representative that an invoice for a client has been paid.
        /// </summary>
        /// <param name="salesRepName">Sales rep display name (recipient)</param>
        /// <param name="salesRepEmail">Sales rep email (recipient)</param>
        /// <param name="invoice">The invoice that was paid</param>
        /// <param name="paidOn">Optional paid date; if null invoice.PaidAt or UtcNow is used</param>
        /// <param name="note">Optional note</param>
        public static EmailDto NotifySalesRepInvoicePaid(
            string salesRepName,
            string salesRepEmail,
            Invoice invoice,
            DateTime? paidOn = null,
            string? note = null)
        {
            if (invoice == null) throw new ArgumentNullException(nameof(invoice));

            var date = (paidOn ?? invoice.PaidAt ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $"\n\nNote: {note}";

            var clientInfo = invoice.ClientId != 0 ? $"ClientId: {invoice.ClientId}" : "Client: unknown";
            var code = !string.IsNullOrWhiteSpace(invoice.InvoiceCode) ? invoice.InvoiceCode : invoice.Id.ToString();

            var html = $@"<h3>Invoice paid: {code}</h3>
<p>Hello {salesRepName},</p>
<p>The invoice <strong>Code: {code}</strong> (Id: {invoice.Id}) has been marked as <strong>PAID</strong> on {date}.</p>
<p>Amount: {invoice.Amount:C}<br/>{clientInfo}<br/>{(string.IsNullOrWhiteSpace(invoice.CustomerCode) ? string.Empty : "CustomerCode: " + invoice.CustomerCode)}</p>
<p>Please update your records and follow up with the client if necessary.</p>
<p>{noteText.Replace("\n", "<br />")}</p>
<p>Regards,<br/>ProcessZero Team</p>";

            return new EmailDto
            {
                Subject = $"Invoice paid: {code} ({invoice.Amount:C})",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = salesRepEmail ?? string.Empty,
                RecipientName = salesRepName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify stakeholders that a KPI policy was created.
        /// </summary>
        public static EmailDto NotifyKpiPolicyCreated(
            string recipientName,
            string recipientEmail,
            KpiPolicy policy,
            Product product,
            string? note = null)
        {
            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $"\n\nNote: {note}";
            var html = $@"<h3>KPI Policy Created: Id {policy.Id}</h3>
<p>Product: {(product != null ? product.Name : (policy.ProductId.HasValue ? policy.ProductId.Value.ToString() : "All"))}</p>
<p>Effective From: {policy.EffectiveFrom:yyyy-MM-dd}<br/>Effective To: {(policy.EffectiveTo.HasValue ? policy.EffectiveTo.Value.ToString("yyyy-MM-dd") : "N/A")}<br/>IsActive: {policy.IsActive}</p>
<h4>Thresholds</h4>
<ul>
  <li>MinMonthlyRevenue: {policy.MinMonthlyRevenue:C}</li>
  <li>MinOutreachAttempts: {policy.MinOutreachAttempts}</li>
  <li>MinCallsBooked: {policy.MinCallsBooked}</li>
</ul>
<p>Consequences: GracePeriodDays={policy.GracePeriodDays}, AutoFreezeOnBreach={policy.AutoFreezeOnBreach}</p>
<p>{noteText.Replace("\n", "<br />")}</p>";

            return new EmailDto
            {
                Subject = $"KPI Policy Created: Id {policy.Id}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify stakeholders that a KPI policy was updated.
        /// </summary>
        public static EmailDto NotifyKpiPolicyUpdated(
            string recipientName,
            string recipientEmail,
            KpiPolicy policy,
            Product? product,
            string? note = null)
        {
            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $"\n\nNote: {note}";
            var html = $@"<h3>KPI Policy Updated: Id {policy.Id}</h3>
<p>Product: {(product != null ? product.Name : (policy.ProductId.HasValue ? policy.ProductId.Value.ToString() : "All"))}</p>
<p>Effective From: {policy.EffectiveFrom:yyyy-MM-dd}<br/>Effective To: {(policy.EffectiveTo.HasValue ? policy.EffectiveTo.Value.ToString("yyyy-MM-dd") : "N/A")}<br/>IsActive: {policy.IsActive}</p>
<h4>Updated thresholds</h4>
<ul>
  <li>MinMonthlyRevenue: {policy.MinMonthlyRevenue:C}</li>
  <li>MinOutreachAttempts: {policy.MinOutreachAttempts}</li>
  <li>MinCallsBooked: {policy.MinCallsBooked}</li>
</ul>
<p>Consequences: GracePeriodDays={policy.GracePeriodDays}, AutoFreezeOnBreach={policy.AutoFreezeOnBreach}</p>
<p>{noteText.Replace("\n", "<br />")}</p>";

            return new EmailDto
            {
                Subject = $"KPI Policy Updated: Id {policy.Id}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify stakeholders that a KPI policy was deleted.
        /// </summary>
        public static EmailDto NotifyKpiPolicyDeleted(
            string recipientName,
            string recipientEmail,
            KpiPolicy policy,
            Product? product,
            string? note = null)
        {
            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $"\n\nNote: {note}";
            var body = new StringBuilder();
            body.AppendLine($"The KPI policy (Id: {policy.Id}) has been deleted.");
            body.AppendLine();
            body.AppendLine($"Originally effective from: {policy.EffectiveFrom:yyyy-MM-dd} to {(policy.EffectiveTo.HasValue ? policy.EffectiveTo.Value.ToString("yyyy-MM-dd") : "N/A")}");
            body.AppendLine($"Applied to level: All, ProductId: {(policy.ProductId.HasValue ? policy.ProductId.Value.ToString() : "All")}");
            body.AppendLine();
            body.Append(noteText);

            var html = body.ToString();

            return new EmailDto
            {
                Subject = $"KPI Policy Deleted: Id {policy.Id}",
                Body = SimpleWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Build an email notifying the sales representative that a client they introduced
        /// has become active with the company.
        /// </summary>
        /// <param name="salesRepName">Name of the sales representative (recipient)</param>
        /// <param name="salesRepEmail">Email of the sales representative (recipient)</param>
        /// <param name="clientName">Name of the client that has become active</param>
        /// <param name="activatedOn">Optional activation date; if null UtcNow is used</param>
        /// <param name="note">Optional short note about the activation</param>
        public static EmailDto NotifySalesRepClientActivated(
            string salesRepName,
            string salesRepEmail,
            string clientName,
            DateTime? activatedOn = null,
            string? note = null)
        {
            var date = (activatedOn ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $"\n\nNote: {note}";

            var html = $@"<h3>Client activated: {clientName}</h3>
<p>Dear {salesRepName},</p>
<p>Good news — the client you introduced to ProcessZero, <strong>{clientName}</strong>, has been marked as active as of {date}.</p>
<p>{noteText.Replace("\n", "<br />")}</p>
<p>Please follow up with the client to ensure a smooth onboarding and to capture any outstanding next steps (meetings, invoices, handoff).</p>
<p>Regards,<br/>ProcessZero Team</p>";

            return new EmailDto
            {
                Subject = $"Client activated: {clientName}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = salesRepEmail ?? string.Empty,
                RecipientName = salesRepName ?? string.Empty
            };
        }

        /// <summary>
        /// Announce that a new product was added to the catalog.
        /// </summary>
        public static EmailDto NotifyProductCreated(
            string recipientName,
            string recipientEmail,
            Product product,
            string? note = null)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            var noteText = string.IsNullOrWhiteSpace(note)
                ? string.Empty
                : $"<p style='margin-top:15px;'>{note.Replace("\n", "<br />")}</p>";

            var productUrlSection = string.IsNullOrWhiteSpace(product.Url)
                ? string.Empty
                : $@"
        <div style='margin:20px 0;'>
            <a href='{System.Net.WebUtility.HtmlEncode(product.Url)}' target='_blank'
               style='background-color:#4CAF50;color:#fff;padding:12px 20px;
                      text-decoration:none;border-radius:6px;font-weight:bold;'>
                🚀 View Product
            </a>
        </div>";

            var negotiableSection = string.IsNullOrWhiteSpace(product.NegotiableAmounts)
                ? string.Empty
                : $"<p><strong>💬 Flexible Pricing:</strong> {System.Net.WebUtility.HtmlEncode(product.NegotiableAmounts)}</p>";

            var html = $@"
<div style='font-family:Arial, sans-serif; line-height:1.6; color:#333;'>

    <h2 style='color:#4CAF50;'>🎉 New Product Announcement!</h2>

    <p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>

    <p>We’re excited to introduce a brand new addition to our catalog:</p>

    <h3 style='margin-bottom:5px;'>{System.Net.WebUtility.HtmlEncode(product.Name)}</h3>

    <p>{System.Net.WebUtility.HtmlEncode(product.Description ?? string.Empty)}</p>

    <p><strong>💰 Price:</strong> {product.ActualAmount:C}</p>

    {negotiableSection}

    {productUrlSection}

    {noteText}

    <hr style='margin:30px 0;' />

    <p>
        Stay tuned for more updates and opportunities from <strong>ProcessZero</strong>.
    </p>

    <p>
        Regards,<br/>
        <strong>ProcessZero Team</strong>
    </p>

</div>";

            return new EmailDto
            {
                Subject = $"🎉 New Product Launch: {product.Name}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify stakeholders that a product has been discontinued/removed from the catalog.
        /// </summary>
        public static EmailDto NotifyProductDeleted(
            string recipientName,
            string recipientEmail,
            Product product,
            string? note = null)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            var noteText = string.IsNullOrWhiteSpace(note)
                ? string.Empty
                : $"<p style='margin-top:15px;'>{note.Replace("\n", "<br />")}</p>";

            var productUrlSection = string.IsNullOrWhiteSpace(product.Url)
                ? string.Empty
                : $"<p><a href='{System.Net.WebUtility.HtmlEncode(product.Url)}' target='_blank'>View product</a></p>";

            var html = $@"
<div style='font-family:Arial, sans-serif; line-height:1.6; color:#333;'>

    <h2 style='color:#b22222;'>⚠️ Product Discontinued</h2>

    <p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>

    <p>We regret to announce that the following product has been discontinued and removed from our active catalog:</p>

    <h3 style='margin-bottom:5px;'>{System.Net.WebUtility.HtmlEncode(product.Name)}</h3>

    <p>{System.Net.WebUtility.HtmlEncode(product.Description ?? string.Empty)}</p>

    <p><strong>Price (last listed):</strong> {product.ActualAmount:C}</p>

    {productUrlSection}

    {noteText}

    <p>Please do not offer this product to new clients. Existing subscriptions or invoices should be handled according to company policy — contact your manager for guidance.</p>

    <p>
        Regards,<br/>
        <strong>ProcessZero Team</strong>
    </p>

</div>";

            return new EmailDto
            {
                Subject = $"⚠️ Product discontinued: {product.Name}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify that a meeting has been booked. Message speaks to both the sales rep and admin responsibilities.
        /// </summary>
        public static EmailDto NotifyMeetingBooked(
            string recipientName,
            string recipientEmail,
            Meeting meeting,
            Contact contact,
            Product product,
            string? note = null)
        {
            if (meeting == null) throw new ArgumentNullException(nameof(meeting));
            if (contact == null) throw new ArgumentNullException(nameof(contact));
            if (product == null) throw new ArgumentNullException(nameof(product));

            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $"<p style='margin-top:12px;'>{note.Replace("\n", "<br />")}</p>";

            var meetingTime = meeting.MeetingDate.ToString("yyyy-MM-dd HH:mm");
            var duration = MeetingConstant.DefaultDurationMinutes;
            var joinUrl = MeetingConstant.BuildJoinUrl(meeting.Id.ToString());

            var html = $@"
<div style='font-family:Arial, sans-serif; line-height:1.5; color:#333;'>
  <h2 style='color:#1f3c88;'>📅 New Meeting Booked</h2>

  <p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>

  <p>A meeting has been scheduled. The message below includes clear actions for both the sales representative and the admin team.</p>

  <h3 style='margin-bottom:6px;'>{System.Net.WebUtility.HtmlEncode(product.Name)} — {System.Net.WebUtility.HtmlEncode(contact.FirstName + " " + contact.LastName)}</h3>

    <ul>
    <li><strong>When:</strong> {meetingTime}</li>
    <li><strong>Duration:</strong> {duration} minutes</li>
    <li><strong>Attendee:</strong> {System.Net.WebUtility.HtmlEncode(contact.FirstName + " " + contact.LastName)} — {System.Net.WebUtility.HtmlEncode(contact.Email)}</li>
    <li><strong>Meeting link:</strong> {(string.IsNullOrWhiteSpace(joinUrl) ? "TBD" : $"<a href='{System.Net.WebUtility.HtmlEncode(joinUrl)}' target='_blank'>Join meeting</a>")}</li>
  </ul>

  <div style='margin-top:12px;padding:12px;background:#f8f9fb;border-left:4px solid #1f3c88;'>
    <p style='margin:0;'><strong>For Sales Rep:</strong> Please review the attendee details, prepare the product pitch and materials, and confirm any required pre-meeting deliverables. Follow up with the attendee after the meeting to capture next steps and update CRM.</p>
  </div>

  <div style='margin-top:12px;padding:12px;background:#fff7f6;border-left:4px solid #b22222;'>
    <p style='margin:0;'><strong>For Admin:</strong> Please ensure the meeting is added to the official calendar, resources are allocated if needed, and any billing/CRM records are updated. If the meeting requires special setup (room, recording, guests), coordinate with the sales rep.</p>
  </div>

  {noteText}

  <hr style='margin:18px 0;' />

  <p style='margin:0;'>Regards,<br/><strong>ProcessZero Team</strong></p>
</div>";

            return new EmailDto
            {
                Subject = $"📅 Meeting booked: {contact.FirstName} {contact.LastName} — {meetingTime}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify that an existing meeting was rescheduled. Message speaks to both the sales rep and admin.
        /// </summary>
        public static EmailDto NotifyMeetingRescheduled(
            string recipientName,
            string recipientEmail,
            Meeting meeting,
            Contact contact,
            Product product,
            DateTime? previousStartTime = null,
            string? reason = null)
        {
            if (meeting == null) throw new ArgumentNullException(nameof(meeting));
            if (contact == null) throw new ArgumentNullException(nameof(contact));
            if (product == null) throw new ArgumentNullException(nameof(product));

            var reasonText = string.IsNullOrWhiteSpace(reason) ? string.Empty : $"<p style='margin-top:12px;'><strong>Reason:</strong> {reason.Replace("\n", "<br />")}</p>";

            var newTime = meeting.MeetingDate.ToString("yyyy-MM-dd HH:mm");
            var oldTimeText = previousStartTime.HasValue ? previousStartTime.Value.ToString("yyyy-MM-dd HH:mm") : "(previous time not available)";
            var duration = MeetingConstant.DefaultDurationMinutes;
            var joinUrl = MeetingConstant.BuildJoinUrl(meeting.Id.ToString());

            var html = $@"
<div style='font-family:Arial, sans-serif; line-height:1.5; color:#333;'>
  <h2 style='color:#1f3c88;'>🔁 Meeting Rescheduled</h2>

  <p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>

  <p>The following meeting has been rescheduled. Please review the updated details and take any necessary actions.</p>

  <h3 style='margin-bottom:6px;'>{System.Net.WebUtility.HtmlEncode(product.Name)} — {System.Net.WebUtility.HtmlEncode(contact.FirstName + " " + contact.LastName)}</h3>

  <ul>
    <li><strong>Previous time:</strong> {oldTimeText}</li>
    <li><strong>New time:</strong> {newTime}</li>
    <li><strong>Duration:</strong> {duration} minutes</li>
    <li><strong>Attendee:</strong> {System.Net.WebUtility.HtmlEncode(contact.FirstName + " " + contact.LastName)} — {System.Net.WebUtility.HtmlEncode(contact.Email)}</li>
    <li><strong>Meeting link:</strong> {(string.IsNullOrWhiteSpace(joinUrl) ? "TBD" : $"<a href='{System.Net.WebUtility.HtmlEncode(joinUrl)}' target='_blank'>Join meeting</a>")}</li>
  </ul>

  <div style='margin-top:12px;padding:12px;background:#f8f9fb;border-left:4px solid #1f3c88;'>
    <p style='margin:0;'><strong>For Sales Rep:</strong> Update your calendar and outreach materials. Notify the attendee of the new time and confirm availability. Prepare any materials for the updated slot.</p>
  </div>

  <div style='margin-top:12px;padding:12px;background:#fff7f6;border-left:4px solid #b22222;'>
    <p style='margin:0;'><strong>For Admin:</strong> Update the official calendar entry, adjust resources/room allocations if needed, and ensure recording or guest access is configured for the new time.</p>
  </div>

  <hr style='margin:18px 0;' />

  <p style='margin:0;'>Regards,<br/><strong>ProcessZero Team</strong></p>
</div>";

            return new EmailDto
            {
                Subject = $"🔁 Meeting rescheduled: {contact.FirstName} {contact.LastName} — {newTime}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify that an existing meeting was cancelled. Message speaks to both the sales rep and admin.
        /// </summary>
        public static EmailDto NotifyMeetingCancelled(
            string recipientName,
            string recipientEmail,
            Meeting meeting,
            Contact contact,
            Product product,
            DateTime? cancelledOn = null,
            string? reason = null)
        {
            if (meeting == null) throw new ArgumentNullException(nameof(meeting));
            if (contact == null) throw new ArgumentNullException(nameof(contact));
            if (product == null) throw new ArgumentNullException(nameof(product));

            var cancelledAt = (cancelledOn ?? DateTime.UtcNow).ToString("yyyy-MM-dd HH:mm");
            var reasonText = string.IsNullOrWhiteSpace(reason) ? string.Empty : $"<p style='margin-top:12px;'><strong>Reason:</strong> {reason.Replace("\n", "<br />")}</p>";

            var meetingTime = meeting.MeetingDate.ToString("yyyy-MM-dd HH:mm");
            var duration = MeetingConstant.DefaultDurationMinutes;
            var joinUrl = MeetingConstant.BuildJoinUrl(meeting.Id.ToString());

            var html = $@"
<div style='font-family:Arial, sans-serif; line-height:1.5; color:#333;'>
  <h2 style='color:#b22222;'>❌ Meeting Cancelled</h2>

  <p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>

  <p>The following meeting has been cancelled on <strong>{cancelledAt}</strong>. Please see details and next steps below.</p>

  <h3 style='margin-bottom:6px;'>{System.Net.WebUtility.HtmlEncode(product.Name)} — {System.Net.WebUtility.HtmlEncode(contact.FirstName + " " + contact.LastName)}</h3>

  <ul>
    <li><strong>Originally scheduled:</strong> {meetingTime}</li>
    <li><strong>Duration:</strong> {duration} minutes</li>
    <li><strong>Attendee:</strong> {System.Net.WebUtility.HtmlEncode(contact.FirstName + " " + contact.LastName)} — {System.Net.WebUtility.HtmlEncode(contact.Email)}</li>
    <li><strong>Meeting link:</strong> {(string.IsNullOrWhiteSpace(joinUrl) ? "TBD" : $"<a href='{System.Net.WebUtility.HtmlEncode(joinUrl)}' target='_blank'>Join meeting</a>")}</li>
  </ul>

  <div style='margin-top:12px;padding:12px;background:#fff3f2;border-left:4px solid #b22222;'>
    <p style='margin:0;'><strong>For Sales Rep:</strong> Please notify the attendee about the cancellation, offer to reschedule if appropriate, and update CRM with the cancellation reason and next steps.</p>
  </div>

  <div style='margin-top:12px;padding:12px;background:#f8f9fb;border-left:4px solid #1f3c88;'>
    <p style='margin:0;'><strong>For Admin:</strong> Remove or update the calendar entry, release any reserved resources, and ensure any billing or internal records reflect the cancellation.</p>
  </div>

  {reasonText}

  <hr style='margin:18px 0;' />

  <p style='margin:0;'>Regards,<br/><strong>ProcessZero Team</strong></p>
</div>";

            return new EmailDto
            {
                Subject = $"❌ Meeting cancelled: {contact.FirstName} {contact.LastName} — {meetingTime}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify the client (attendee) that a meeting has been booked for them.
        /// This message is targeted only at the client/attendee.
        /// </summary>
        public static EmailDto NotifyMeetingBookedClient(
            Contact contact,
            Meeting meeting,
            Product product,
            string? note = null)
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));
            if (meeting == null) throw new ArgumentNullException(nameof(meeting));
            if (product == null) throw new ArgumentNullException(nameof(product));

            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $"<p style='margin-top:12px;'>{note.Replace("\n", "<br />")}</p>";
            var meetingTime = meeting.MeetingDate.ToString("yyyy-MM-dd HH:mm");
            var duration = MeetingConstant.DefaultDurationMinutes;
            var joinUrl = MeetingConstant.BuildJoinUrl(meeting.Id.ToString());

            var html = $@"
<div style='font-family:Arial, sans-serif; line-height:1.5; color:#333;'>
  <h2 style='color:#1f3c88;'>📅 Your Meeting is Booked</h2>

  <p>Hi {System.Net.WebUtility.HtmlEncode(contact.FirstName)},</p>

  <p>Thank you — your meeting has been scheduled with <strong>{System.Net.WebUtility.HtmlEncode(product.Name)}</strong>.</p>

    <ul>
    <li><strong>When:</strong> {meetingTime}</li>
    <li><strong>Duration:</strong> {duration} minutes</li>
    <li><strong>With:</strong> {System.Net.WebUtility.HtmlEncode("")}</li>
    <li><strong>Meeting link:</strong> {(string.IsNullOrWhiteSpace(joinUrl) ? "TBD" : $"<a href='{System.Net.WebUtility.HtmlEncode(joinUrl)}' target='_blank'>Join meeting</a>")}</li>
  </ul>

  <p><em>Note:</em> A meeting link will be issued to you within 24 hours so our admin can schedule the meeting using an external meeting provider. If you need the link sooner, please contact your sales representative.</p>

  <p>If you need to reschedule or cancel, please reply to this email or contact your sales representative.</p>

  {noteText}

  <p>We look forward to speaking with you.</p>

  <p>Regards,<br/><strong>ProcessZero Team</strong></p>
</div>";

            return new EmailDto
            {
                Subject = $"📅 Meeting confirmation: {product.Name} — {meetingTime}",
                Body = SimpleWrapWithTemplate(html),
                RecipientEmail = contact.Email ?? string.Empty,
                RecipientName = contact.FirstName + " " + contact.LastName
            };
        }

        /// <summary>
        /// Notify the client (attendee) that their meeting was rescheduled.
        /// </summary>
        public static EmailDto NotifyMeetingRescheduledClient(
            Contact contact,
            Meeting meeting,
            Product product,
            DateTime? previousStartTime = null,
            string? reason = null)
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));
            if (meeting == null) throw new ArgumentNullException(nameof(meeting));
            if (product == null) throw new ArgumentNullException(nameof(product));

            var newTime = meeting.MeetingDate.ToString("yyyy-MM-dd HH:mm");
            var oldTimeText = previousStartTime.HasValue ? previousStartTime.Value.ToString("yyyy-MM-dd HH:mm") : "(previous time not available)";
            var duration = MeetingConstant.DefaultDurationMinutes;
            var joinUrl = MeetingConstant.BuildJoinUrl(meeting.Id.ToString());

            var html = $@"
<div style='font-family:Arial, sans-serif; line-height:1.5; color:#333;'>
  <h2 style='color:#1f3c88;'>🔁 Your Meeting Has Been Rescheduled</h2>

  <p>Hi {System.Net.WebUtility.HtmlEncode(contact.FirstName)},</p>

  <p>Your meeting has been moved.</p>

  <ul>
    <li><strong>Previous:</strong> {oldTimeText}</li>
    <li><strong>New:</strong> {newTime}</li>
    <li><strong>Duration:</strong> {duration} minutes</li>
    <li><strong>Meeting link:</strong> {(string.IsNullOrWhiteSpace(joinUrl) ? "TBD" : $"<a href='{System.Net.WebUtility.HtmlEncode(joinUrl)}' target='_blank'>Join meeting</a>")}</li>
  </ul>

  <p>If this time does not work for you, please reply to this email to request a new time.</p>

  <p>Regards,<br/><strong>ProcessZero Team</strong></p>
</div>";

            return new EmailDto
            {
                Subject = $"🔁 Meeting rescheduled: {product.Name} — {newTime}",
                Body = SimpleWrapWithTemplate(html),
                RecipientEmail = contact.Email ?? string.Empty,
                RecipientName = contact.FirstName + " " + contact.LastName
            };
        }

        /// <summary>
        /// Notify the client (attendee) that their meeting was cancelled.
        /// </summary>
        public static EmailDto NotifyMeetingCancelledClient(
            Contact contact,
            Meeting meeting,
            Product product,
            DateTime? cancelledOn = null,
            string? reason = null)
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));
            if (meeting == null) throw new ArgumentNullException(nameof(meeting));
            if (product == null) throw new ArgumentNullException(nameof(product));

            var cancelledAt = (cancelledOn ?? DateTime.UtcNow).ToString("yyyy-MM-dd HH:mm");
            var reasonText = string.IsNullOrWhiteSpace(reason) ? string.Empty : $"<p style='margin-top:12px;'><strong>Reason:</strong> {reason.Replace("\n", "<br />")}</p>";
            var meetingTime = meeting.MeetingDate.ToString("yyyy-MM-dd HH:mm");

            var html = $@"
<div style='font-family:Arial, sans-serif; line-height:1.5; color:#333;'>
  <h2 style='color:#b22222;'>❌ Your Meeting Was Cancelled</h2>

  <p>Hi {System.Net.WebUtility.HtmlEncode(contact.FirstName)},</p>

  <p>We regret to inform you that your meeting scheduled for <strong>{meetingTime}</strong> has been cancelled on <strong>{cancelledAt}</strong>.</p>

  {reasonText}

  <p>If you would like to reschedule, please reply to this email and we will arrange a new time.</p>

  <p>Regards,<br/><strong>ProcessZero Team</strong></p>
</div>";

            return new EmailDto
            {
                Subject = $"❌ Meeting cancelled: {product.Name} — {meetingTime}",
                Body = SimpleWrapWithTemplate(html),
                RecipientEmail = contact.Email ?? string.Empty,
                RecipientName = contact.FirstName + " " + contact.LastName
            };
        }
    }
}
