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
            var template = @"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>Process Zero</title>
  <style>
    @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@600;700&family=DM+Sans:wght@300;400;500;600&display=swap');

    /* ── Reset ─────────────────────────────────────────── */
    *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

    /* ── Brand tokens ───────────────────────────────────── */
    :root {
      --pz-ink:    #172121;
      --pz-teal:   #1d7874;
      --pz-gold:   #d7a449;
      --pz-blue:   #335c67;
      --pz-sand:   #f6f1e8;
      --pz-paper:  #fffdf9;
      --pz-coral:  #c06c52;
      --pz-muted:  #607078;
      --pz-border: rgba(215,164,73,0.18);
      --radius:    14px;
      --font-display: 'Playfair Display', Georgia, serif;
      --font-body:    'DM Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif;
    }

    body {
      background-color: #edeae3;
      font-family: var(--font-body);
      -webkit-font-smoothing: antialiased;
      padding: 32px 16px 48px;
    }

    /* ── Wrapper ─────────────────────────────────────── */
    .pz-wrap {
      max-width: 600px;
      margin: 0 auto;
    }

    /* ── Header ──────────────────────────────────────── */
    .pz-header {
      background: linear-gradient(145deg, var(--pz-ink) 0%, var(--pz-blue) 100%);
      border-radius: var(--radius) var(--radius) 0 0;
      padding: 36px 40px 28px;
      text-align: center;
      position: relative;
      overflow: hidden;
    }

    /* decorative arc */
    .pz-header::after {
      content: '';
      position: absolute;
      bottom: -30px; left: 50%;
      transform: translateX(-50%);
      width: 120%; height: 60px;
      background: var(--pz-paper);
      border-radius: 50%;
    }

    .pz-logo-text {
      font-family: var(--font-display);
      font-size: 26px;
      font-weight: 700;
      color: #fff;
      letter-spacing: 3px;
      text-transform: uppercase;
    }

    .pz-logo-text span {
      color: var(--pz-gold);
    }

    .pz-tagline {
      font-size: 11px;
      letter-spacing: 2.5px;
      text-transform: uppercase;
      color: rgba(255,255,255,0.45);
      margin-top: 6px;
      font-weight: 500;
    }

    /* ── Gold rule ───────────────────────────────────── */
    .pz-rule {
      height: 2px;
      background: linear-gradient(90deg, transparent, var(--pz-gold), transparent);
      margin: 0;
      border: none;
    }

    /* ── Body card ───────────────────────────────────── */
    .pz-body {
      background: var(--pz-paper);
      padding: 52px 48px 40px;
      border-left: 1px solid var(--pz-border);
      border-right: 1px solid var(--pz-border);
    }

    /* ── Content typography ──────────────────────────── */
    .pz-body h2 {
      font-family: var(--font-display);
      font-size: 22px;
      font-weight: 700;
      color: var(--pz-ink);
      margin-bottom: 18px;
      line-height: 1.3;
    }

    .pz-body h3 {
      font-family: var(--font-display);
      font-size: 17px;
      font-weight: 600;
      color: var(--pz-ink);
      margin-bottom: 10px;
    }

    .pz-body p {
      font-size: 15px;
      line-height: 1.7;
      color: #3a4444;
      margin-bottom: 14px;
    }

    .pz-body ul {
      margin: 12px 0 18px 0;
      padding-left: 0;
      list-style: none;
    }

    .pz-body ul li {
      font-size: 14.5px;
      color: #3a4444;
      padding: 7px 0;
      border-bottom: 1px solid rgba(215,164,73,0.1);
      line-height: 1.5;
    }

    .pz-body ul li:last-child { border-bottom: none; }

    .pz-body a {
      color: var(--pz-teal);
      text-decoration: none;
      font-weight: 500;
      border-bottom: 1px solid rgba(29,120,116,0.25);
      transition: border-color 0.2s;
    }

    .pz-body a:hover { border-bottom-color: var(--pz-teal); }

    /* ── Info box ───────────────────────────────────── */
    .pz-info-box {
      background: var(--pz-sand);
      border: 1px solid var(--pz-border);
      border-left: 3px solid var(--pz-gold);
      border-radius: 10px;
      padding: 16px 20px;
      margin: 20px 0;
    }

    .pz-info-box p { margin-bottom: 0; font-size: 14.5px; }

    /* ── Action callout panels ───────────────────────── */
    .pz-panel-teal {
      background: rgba(29,120,116,0.06);
      border-left: 3px solid var(--pz-teal);
      border-radius: 0 10px 10px 0;
      padding: 14px 18px;
      margin: 14px 0;
    }

    .pz-panel-coral {
      background: rgba(192,108,82,0.06);
      border-left: 3px solid var(--pz-coral);
      border-radius: 0 10px 10px 0;
      padding: 14px 18px;
      margin: 14px 0;
    }

    .pz-panel-teal p,
    .pz-panel-coral p { margin-bottom: 0; font-size: 14px; color: var(--pz-ink); }

    /* ── CTA button ─────────────────────────────────── */
    .pz-btn {
      display: inline-block;
      background: linear-gradient(135deg, var(--pz-teal), var(--pz-blue));
      color: #fff !important;
      padding: 13px 28px;
      border-radius: 10px;
      font-weight: 600;
      font-size: 14px;
      letter-spacing: 0.4px;
      text-decoration: none !important;
      border-bottom: none !important;
      margin-top: 4px;
    }

    .pz-btn-green {
      display: inline-block;
      background: linear-gradient(135deg, #2e8b57, #1d7874);
      color: #fff !important;
      padding: 13px 28px;
      border-radius: 10px;
      font-weight: 600;
      font-size: 14px;
      letter-spacing: 0.4px;
      text-decoration: none !important;
      border-bottom: none !important;
      margin-top: 4px;
    }

    /* ── Divider ─────────────────────────────────────── */
    .pz-divider {
      height: 1px;
      background: linear-gradient(90deg, transparent, var(--pz-border), transparent);
      border: none;
      margin: 28px 0;
    }

    /* ── Footer ──────────────────────────────────────── */
    .pz-footer {
      background: var(--pz-ink);
      border-radius: 0 0 var(--radius) var(--radius);
      padding: 24px 40px;
      text-align: center;
    }

    .pz-footer p {
      font-size: 12px;
      color: rgba(255,255,255,0.35);
      line-height: 1.6;
      margin-bottom: 6px;
    }

    .pz-footer a {
      color: rgba(215,164,73,0.6);
      font-size: 11.5px;
      text-decoration: none;
      letter-spacing: 0.5px;
    }

    .pz-footer a:hover { color: var(--pz-gold); }

    /* ── Bottom shadow ───────────────────────────────── */
    .pz-shadow {
      height: 6px;
      background: linear-gradient(180deg, rgba(23,33,33,0.08), transparent);
      border-radius: 0 0 20px 20px;
    }
  </style>
</head>
<body>

<div class=""pz-wrap"">

  <!-- Header -->
  <div class=""pz-header"">
    <div class=""pz-logo-text"">Process <span>Zero</span></div>
    <div class=""pz-tagline"">Earn &nbsp;&middot;&nbsp; Sell &nbsp;&middot;&nbsp; Scale</div>
  </div>

  <hr class=""pz-rule"">

  <!-- Body -->
  <div class=""pz-body"">
    {CONTENT}
  </div>

  <!-- Footer -->
  <div class=""pz-footer"">
    <p>&copy; 2026 Process Zero. All rights reserved.</p>
    <a href=""#"">Unsubscribe</a> &nbsp;&middot;&nbsp; <a href=""#"">Preferences</a>
  </div>

  <div class=""pz-shadow""></div>

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
<h2>&#127881; Congratulations!</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>
<p>Well done — you passed the assessment <strong>{System.Net.WebUtility.HtmlEncode(assessmentTitle)}</strong> for <strong>{System.Net.WebUtility.HtmlEncode(productName)}</strong>.</p>

<div class=""pz-info-box"">
  <p><strong>Score:</strong> {score} / {total} &nbsp;&mdash;&nbsp; {percentage:F1}%</p>
</div>

<p>You now meet the requirements for this product. Keep up the excellent work.</p>

<hr class=""pz-divider"">

<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

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
                : $@"<div class=""pz-panel-teal""><p><strong>Note:</strong> {System.Net.WebUtility.HtmlEncode(note.Replace("\n", "<br />"))}</p></div>";

            var passMarkText = assessment.PassMark.HasValue
                ? $"<p><strong>Pass mark:</strong> {assessment.PassMark.Value}%</p>"
                : string.Empty;

            var html = $@"
<h2>&#128221; Assessment uploaded</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>
<p>An assessment has been uploaded or updated in the system. Details are shown below.</p>

<div class=""pz-info-box"">
  <h3>{System.Net.WebUtility.HtmlEncode(assessment.Title ?? string.Empty)}</h3>
  <p><strong>Product:</strong> {System.Net.WebUtility.HtmlEncode(productName ?? string.Empty)}</p>
  {passMarkText}
  <p style=""margin-bottom:0;""><strong>Uploaded at:</strong> {assessment.UploadedAt:yyyy-MM-dd HH:mm} UTC</p>
</div>

{noteText}

<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

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
            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $@"<div class=""pz-panel-teal""><p>{System.Net.WebUtility.HtmlEncode(note)}</p></div>";
            var byText = string.IsNullOrWhiteSpace(unbannedBy) ? string.Empty : $"<p><strong>Unbanned by:</strong> {System.Net.WebUtility.HtmlEncode(unbannedBy)}</p>";

            var html = $@"
<h2>&#9989; Account restored</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>
<p>Good news — your ProcessZero account was <strong>unbanned</strong> on <strong>{when} UTC</strong> and full access has been restored.</p>
{byText}
{noteText}
<p>If you need any assistance getting back up and running, please contact <a href=""mailto:support@processzero.com"">support@processzero.com</a>.</p>
<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

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
            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $@"<div class=""pz-panel-teal""><p><strong>Note:</strong> {note.Replace("\n", "<br />")}</p></div>";

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
            catch { adminLink = adminPortalBase; }

            var html = $@"
<h2>&#128229; Partner payroll CSV ready</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(adminName)},</p>
<p>The partner payroll CSV for <strong>{System.Net.WebUtility.HtmlEncode(monthName)}</strong> is now available. This link is for Admin users only.</p>

<div class=""pz-info-box"" style=""text-align:center;"">
  <p style=""margin-bottom:16px;"">Click below to download the CSV securely.</p>
  <a href=""{System.Net.WebUtility.HtmlEncode(adminLink)}"" target=""_blank"" class=""pz-btn"">Download payroll CSV</a>
</div>

<p style=""font-size:13.5px;color:#607078;"">The download link will expire shortly. Issues? Contact <a href=""mailto:payroll@processzero.com"">payroll@processzero.com</a>.</p>
{noteText}
<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Payroll Team</strong></p>";

            return new EmailDto
            {
                Subject = $"Partner payroll CSV ready: {monthName}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = adminEmail ?? string.Empty,
                RecipientName = adminName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify a sales representative that payroll has been generated for them.
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
            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $@"<div class=""pz-panel-teal""><p><strong>Note:</strong> {note.Replace("\n", "<br />")}</p></div>";

            var html = $@"
<h2>&#128176; Partner payroll generated</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>
<p>We have generated the partner payroll sheet for <strong>{System.Net.WebUtility.HtmlEncode(monthName)}</strong>.</p>

<div class=""pz-info-box"">
  <p style=""margin-bottom:0;""><strong>Expected processing:</strong> Payments will be fulfilled within 48 hours.</p>
</div>

<p>No action is required from you. If your bank details need updating, visit <a href=""https://app.processzero.com/account/payroll"">Payroll Settings</a> or contact <a href=""mailto:payroll@processzero.com"">payroll@processzero.com</a>.</p>
<p style=""font-size:13.5px;color:#607078;"">For confidentiality, detailed payroll sheets are restricted to Admin users. For a private breakdown of your payout, please contact payroll directly.</p>
{noteText}
<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Thank you for your continued partnership.<br/><br/>Regards,<br/><strong style=""color:#172121;"">ProcessZero Payroll Team</strong></p>";

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
<h2 style=""color:#c06c52;"">&#128683; Account banned</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>
<p>We're writing to inform you that your ProcessZero account was <strong>banned</strong> on <strong>{when} UTC</strong>.</p>

<div class=""pz-panel-coral"">
  {reasonText}
  {byText}
</div>

<p>If you believe this is a mistake or would like to appeal, please contact <a href=""mailto:support@processzero.com"">support@processzero.com</a> with any relevant details. We will review your case promptly.</p>
<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

            return new EmailDto
            {
                Subject = "Account banned — ProcessZero",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        // Backwards-compatible default wrapper (branded)
        private static string WrapWithTemplate(string contentHtml) => BrandedWrapWithTemplate(contentHtml);

        // Simple client-facing template: clean, premium, no heavy branding
        private static string SimpleWrapWithTemplate(string contentHtml)
        {
            var template = @"<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='utf-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1'>
  <title>ProcessZero</title>
  <style>
    @import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@300;400;500;600&display=swap');

    *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

    :root {
      --pz-ink:    #172121;
      --pz-teal:   #1d7874;
      --pz-gold:   #d7a449;
      --pz-sand:   #f6f1e8;
      --pz-paper:  #fffdf9;
      --pz-coral:  #c06c52;
      --pz-muted:  #607078;
    }

    body {
      background: #edeae3;
      font-family: 'DM Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif;
      -webkit-font-smoothing: antialiased;
      padding: 32px 16px 48px;
    }

    .pz-simple-wrap {
      max-width: 580px;
      margin: 0 auto;
    }

    .pz-simple-card {
      background: var(--pz-paper);
      border-radius: 14px;
      border: 1px solid rgba(215,164,73,0.15);
      box-shadow: 0 4px 32px rgba(23,33,33,0.08);
      overflow: hidden;
    }

    .pz-simple-top {
      height: 4px;
      background: linear-gradient(90deg, var(--pz-teal), var(--pz-gold), var(--pz-coral));
    }

    .pz-simple-body {
      padding: 40px 44px 36px;
    }

    .pz-simple-body h2,
    .pz-simple-body h3 {
      color: var(--pz-ink);
      margin-bottom: 14px;
      line-height: 1.3;
    }

    .pz-simple-body h2 { font-size: 20px; font-weight: 600; }
    .pz-simple-body h3 { font-size: 16px; font-weight: 600; }

    .pz-simple-body p {
      font-size: 15px;
      line-height: 1.7;
      color: #3a4444;
      margin-bottom: 14px;
    }

    .pz-simple-body ul {
      margin: 12px 0 18px;
      padding-left: 0;
      list-style: none;
    }

    .pz-simple-body ul li {
      font-size: 14.5px;
      color: #3a4444;
      padding: 8px 0;
      border-bottom: 1px solid rgba(215,164,73,0.1);
      line-height: 1.5;
    }

    .pz-simple-body ul li:last-child { border-bottom: none; }

    .pz-simple-body a {
      color: var(--pz-teal);
      text-decoration: none;
      font-weight: 500;
      border-bottom: 1px solid rgba(29,120,116,0.25);
    }

    .pz-info-box {
      background: var(--pz-sand);
      border: 1px solid rgba(215,164,73,0.18);
      border-left: 3px solid var(--pz-gold);
      border-radius: 10px;
      padding: 15px 20px;
      margin: 18px 0;
    }

    .pz-info-box p { font-size: 14.5px; margin-bottom: 0; }

    .pz-panel-teal {
      background: rgba(29,120,116,0.06);
      border-left: 3px solid var(--pz-teal);
      border-radius: 0 10px 10px 0;
      padding: 13px 18px;
      margin: 14px 0;
    }

    .pz-panel-teal p { font-size: 14px; color: var(--pz-ink); margin-bottom: 0; }

    .pz-divider {
      height: 1px;
      background: linear-gradient(90deg, transparent, rgba(215,164,73,0.2), transparent);
      border: none;
      margin: 24px 0;
    }

    .pz-simple-footer {
      background: var(--pz-sand);
      padding: 18px 44px;
      text-align: center;
      border-top: 1px solid rgba(215,164,73,0.12);
    }

    .pz-simple-footer p {
      font-size: 11.5px;
      color: var(--pz-muted);
      margin-bottom: 4px;
    }

    .pz-simple-footer a {
      color: rgba(29,120,116,0.6);
      font-size: 11px;
      text-decoration: none;
      letter-spacing: 0.4px;
    }
  </style>
</head>
<body>
  <div class='pz-simple-wrap'>
    <div class='pz-simple-card'>
      <div class='pz-simple-top'></div>
      <div class='pz-simple-body'>
        {CONTENT}
      </div>
      <div class='pz-simple-footer'>
        <p>&copy; 2026 Process Zero. All rights reserved.</p>
        <a href='#'>Unsubscribe</a> &nbsp;&middot;&nbsp; <a href='#'>Preferences</a>
      </div>
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
                : $@"<div class=""pz-panel-teal""><p><strong>&#128221; Update Notes:</strong> {note.Replace("\n", "<br />")}</p></div>";

            var productUrlSection = string.IsNullOrWhiteSpace(product.Url)
                ? string.Empty
                : $@"<div style=""margin:20px 0;""><a href=""{System.Net.WebUtility.HtmlEncode(product.Url)}"" target=""_blank"" class=""pz-btn"">&#128269; View Updated Product</a></div>";

            var negotiableSection = string.IsNullOrWhiteSpace(product.NegotiableAmounts)
                ? string.Empty
                : $"<p><strong>&#128172; Flexible Pricing:</strong> {System.Net.WebUtility.HtmlEncode(product.NegotiableAmounts)}</p>";

            var html = $@"
<h2>&#128640; Product Update</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>
<p>We've made important updates to one of our products. Please review the latest details below.</p>

<div class=""pz-info-box"">
  <h3>{System.Net.WebUtility.HtmlEncode(product.Name)}</h3>
  <p>{System.Net.WebUtility.HtmlEncode(product.Description ?? string.Empty)}</p>
  <p><strong>&#128176; Current Price:</strong> {product.ActualAmount:C}</p>
  {negotiableSection}
</div>

{productUrlSection}
{noteText}

<div class=""pz-panel-coral"">
  <p><strong>&#9888;&#65039; Action Required:</strong> Please ensure your sales materials and client discussions reflect these latest updates.</p>
</div>

<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

            return new EmailDto
            {
                Subject = $"Product Update: {product.Name}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify the sales rep that a client they introduced is no longer active.
        /// </summary>
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
                : $@"<div class=""pz-panel-coral""><p><strong>Reason:</strong> {System.Net.WebUtility.HtmlEncode(reason)}</p></div>";

            var html = $@"
<h2 style=""color:#c06c52;"">Client deactivated</h2>
<p>Dear {System.Net.WebUtility.HtmlEncode(salesRepName)},</p>
<p>The client you introduced to ProcessZero, <strong>{System.Net.WebUtility.HtmlEncode(clientName)}</strong>, is no longer active as of <strong>{date}</strong>.</p>
{reasonText}
<p>Please review your records and update any outstanding items related to this client — leads, meetings, invoices. Contact your account manager if you need further details.</p>
<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

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
        public static EmailDto NotifySalesRepInvoicePaid(
            string salesRepName,
            string salesRepEmail,
            Invoice invoice,
            DateTime? paidOn = null,
            string? note = null)
        {
            if (invoice == null) throw new ArgumentNullException(nameof(invoice));

            var date = (paidOn ?? invoice.PaidAt ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $@"<div class=""pz-panel-teal""><p><strong>Note:</strong> {System.Net.WebUtility.HtmlEncode(note)}</p></div>";

            var clientInfo = invoice.ClientId != 0 ? $"Client ID: {invoice.ClientId}" : "Client: unknown";
            var code = !string.IsNullOrWhiteSpace(invoice.InvoiceCode) ? invoice.InvoiceCode : invoice.Id.ToString();
            var customerCodeLine = !string.IsNullOrWhiteSpace(invoice.CustomerCode)
                ? $"<p><strong>Customer Code:</strong> {System.Net.WebUtility.HtmlEncode(invoice.CustomerCode)}</p>"
                : string.Empty;

            var html = $@"
<h2>Invoice paid</h2>
<p>Hello {System.Net.WebUtility.HtmlEncode(salesRepName)},</p>
<p>Invoice <strong>{System.Net.WebUtility.HtmlEncode(code)}</strong> has been marked as <strong>PAID</strong> on {date}.</p>

<div class=""pz-info-box"">
  <p><strong>Amount:</strong> {invoice.Amount:C}</p>
  <p><strong>{clientInfo}</strong></p>
  {customerCodeLine}
</div>

{noteText}
<p>Please update your records and follow up with the client if necessary.</p>
<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

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
            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $@"<div class=""pz-panel-teal""><p><strong>Note:</strong> {System.Net.WebUtility.HtmlEncode(note)}</p></div>";

            var html = $@"
<h2>KPI Policy Created</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>

<div class=""pz-info-box"">
  <p><strong>Policy ID:</strong> {policy.Id}</p>
  <p><strong>Product:</strong> {(product != null ? System.Net.WebUtility.HtmlEncode(product.Name) : (policy.ProductId.HasValue ? policy.ProductId.Value.ToString() : "All"))}</p>
  <p><strong>Effective From:</strong> {policy.EffectiveFrom:yyyy-MM-dd}</p>
  <p><strong>Effective To:</strong> {(policy.EffectiveTo.HasValue ? policy.EffectiveTo.Value.ToString("yyyy-MM-dd") : "N/A")}</p>
  <p><strong>Active:</strong> {policy.IsActive}</p>
  <p><strong>Target MRR:</strong> {policy.TargetMRR:C}</p>
  <p style=""margin-bottom:0;""><strong>Consequences:</strong> Grace period {policy.GracePeriodDays} days &mdash; Auto-freeze on breach: {policy.AutoFreezeOnBreach}</p>
</div>

{noteText}
<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

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
            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $@"<div class=""pz-panel-teal""><p><strong>Note:</strong> {System.Net.WebUtility.HtmlEncode(note)}</p></div>";

            var html = $@"
<h2>KPI Policy Updated</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>

<div class=""pz-info-box"">
  <p><strong>Policy ID:</strong> {policy.Id}</p>
  <p><strong>Product:</strong> {(product != null ? System.Net.WebUtility.HtmlEncode(product.Name) : (policy.ProductId.HasValue ? policy.ProductId.Value.ToString() : "All"))}</p>
  <p><strong>Effective From:</strong> {policy.EffectiveFrom:yyyy-MM-dd}</p>
  <p><strong>Effective To:</strong> {(policy.EffectiveTo.HasValue ? policy.EffectiveTo.Value.ToString("yyyy-MM-dd") : "N/A")}</p>
  <p><strong>Active:</strong> {policy.IsActive}</p>
  <p><strong>Updated Target MRR:</strong> {policy.TargetMRR:C}</p>
  <p style=""margin-bottom:0;""><strong>Consequences:</strong> Grace period {policy.GracePeriodDays} days &mdash; Auto-freeze on breach: {policy.AutoFreezeOnBreach}</p>
</div>

{noteText}
<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

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
            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $@"<div class=""pz-panel-teal""><p><strong>Note:</strong> {System.Net.WebUtility.HtmlEncode(note)}</p></div>";

            var html = $@"
<h2 style=""color:#c06c52;"">KPI Policy Deleted</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>
<p>The KPI policy below has been permanently deleted.</p>

<div class=""pz-info-box"">
  <p><strong>Policy ID:</strong> {policy.Id}</p>
  <p><strong>Product:</strong> {(product != null ? System.Net.WebUtility.HtmlEncode(product.Name) : (policy.ProductId.HasValue ? policy.ProductId.Value.ToString() : "All"))}</p>
  <p><strong>Originally effective:</strong> {policy.EffectiveFrom:yyyy-MM-dd} &mdash; {(policy.EffectiveTo.HasValue ? policy.EffectiveTo.Value.ToString("yyyy-MM-dd") : "N/A")}</p>
</div>

{noteText}
<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

            return new EmailDto
            {
                Subject = $"KPI Policy Deleted: Id {policy.Id}",
                Body = SimpleWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify the sales rep that a client they introduced has become active.
        /// </summary>
        public static EmailDto NotifySalesRepClientActivated(
            string salesRepName,
            string salesRepEmail,
            string clientName,
            DateTime? activatedOn = null,
            string? note = null)
        {
            var date = (activatedOn ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $@"<div class=""pz-panel-teal""><p><strong>Note:</strong> {System.Net.WebUtility.HtmlEncode(note)}</p></div>";

            var html = $@"
<h2>&#127881; Client activated</h2>
<p>Dear {System.Net.WebUtility.HtmlEncode(salesRepName)},</p>
<p>Good news — the client you introduced, <strong>{System.Net.WebUtility.HtmlEncode(clientName)}</strong>, has been marked as active as of <strong>{date}</strong>.</p>
{noteText}
<p>Please follow up with the client to ensure a smooth onboarding and capture any outstanding next steps.</p>
<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

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
                : $@"<div class=""pz-panel-teal""><p>{note.Replace("\n", "<br />")}</p></div>";

            var productUrlSection = string.IsNullOrWhiteSpace(product.Url)
                ? string.Empty
                : $@"<div style=""margin:20px 0;""><a href=""{System.Net.WebUtility.HtmlEncode(product.Url)}"" target=""_blank"" class=""pz-btn-green"">&#128640; View Product</a></div>";

            var negotiableSection = string.IsNullOrWhiteSpace(product.NegotiableAmounts)
                ? string.Empty
                : $"<p><strong>&#128172; Flexible Pricing:</strong> {System.Net.WebUtility.HtmlEncode(product.NegotiableAmounts)}</p>";

            var html = $@"
<h2 style=""color:#1d7874;"">&#127881; New Product Launch</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>
<p>We're excited to introduce a brand new addition to our catalog:</p>

<div class=""pz-info-box"">
  <h3>{System.Net.WebUtility.HtmlEncode(product.Name)}</h3>
  <p>{System.Net.WebUtility.HtmlEncode(product.Description ?? string.Empty)}</p>
  <p><strong>&#128176; Price:</strong> {product.ActualAmount:C}</p>
  {negotiableSection}
</div>

{productUrlSection}
{noteText}

<hr class=""pz-divider"">
<p>Stay tuned for more updates and opportunities from <strong>ProcessZero</strong>.</p>
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

            return new EmailDto
            {
                Subject = $"New Product Launch: {product.Name}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify stakeholders that a product has been discontinued.
        /// </summary>
        public static EmailDto NotifyProductDeleted(
            string recipientName,
            string recipientEmail,
            Product product,
            string? note = null)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $@"<div class=""pz-panel-teal""><p>{note.Replace("\n", "<br />")}</p></div>";
            var productUrlSection = string.IsNullOrWhiteSpace(product.Url) ? string.Empty : $"<p><a href='{System.Net.WebUtility.HtmlEncode(product.Url)}' target='_blank'>View product</a></p>";

            var html = $@"
<h2 style=""color:#c06c52;"">&#9888;&#65039; Product Discontinued</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>
<p>The following product has been discontinued and removed from our active catalog:</p>

<div class=""pz-info-box"">
  <h3>{System.Net.WebUtility.HtmlEncode(product.Name)}</h3>
  <p>{System.Net.WebUtility.HtmlEncode(product.Description ?? string.Empty)}</p>
  <p style=""margin-bottom:0;""><strong>Last listed price:</strong> {product.ActualAmount:C}</p>
</div>

{productUrlSection}
{noteText}

<div class=""pz-panel-coral"">
  <p>Please do not offer this product to new clients. Existing subscriptions should be handled according to company policy — contact your manager for guidance.</p>
</div>

<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

            return new EmailDto
            {
                Subject = $"Product discontinued: {product.Name}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify that a meeting has been booked.
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

            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $@"<div class=""pz-panel-teal""><p>{note.Replace("\n", "<br />")}</p></div>";
            var meetingTime = meeting.MeetingDate.ToString("yyyy-MM-dd HH:mm");
            var duration = MeetingConstant.DefaultDurationMinutes;
            var joinUrl = MeetingConstant.BuildJoinUrl(meeting.Id.ToString());

            var html = $@"
<h2>&#128197; New Meeting Booked</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>
<p>A meeting has been scheduled. See details and action items below.</p>

<div class=""pz-info-box"">
  <h3>{System.Net.WebUtility.HtmlEncode(product.Name)} &mdash; {System.Net.WebUtility.HtmlEncode(contact.FirstName + " " + contact.LastName)}</h3>
  <ul>
    <li><strong>When:</strong> {meetingTime}</li>
    <li><strong>Duration:</strong> {duration} minutes</li>
    <li><strong>Attendee:</strong> {System.Net.WebUtility.HtmlEncode(contact.FirstName + " " + contact.LastName)} &mdash; {System.Net.WebUtility.HtmlEncode(contact.Email)}</li>
    <li><strong>Meeting link:</strong> {(string.IsNullOrWhiteSpace(joinUrl) ? "TBD" : $"<a href='{System.Net.WebUtility.HtmlEncode(joinUrl)}' target='_blank'>Join meeting</a>")}</li>
  </ul>
</div>

<div class=""pz-panel-teal"">
  <p><strong>For Sales Rep:</strong> Review attendee details, prepare materials, and confirm pre-meeting deliverables. Follow up after the meeting to capture next steps.</p>
</div>

<div class=""pz-panel-coral"">
  <p><strong>For Admin:</strong> Add to the official calendar, allocate resources if needed, and update CRM. Coordinate any special setup with the sales rep.</p>
</div>

{noteText}
<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

            return new EmailDto
            {
                Subject = $"Meeting booked: {contact.FirstName} {contact.LastName} — {meetingTime}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify that an existing meeting was rescheduled.
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

            var reasonText = string.IsNullOrWhiteSpace(reason) ? string.Empty : $@"<div class=""pz-panel-teal""><p><strong>Reason:</strong> {reason.Replace("\n", "<br />")}</p></div>";
            var newTime = meeting.MeetingDate.ToString("yyyy-MM-dd HH:mm");
            var oldTimeText = previousStartTime.HasValue ? previousStartTime.Value.ToString("yyyy-MM-dd HH:mm") : "(previous time not available)";
            var duration = MeetingConstant.DefaultDurationMinutes;
            var joinUrl = MeetingConstant.BuildJoinUrl(meeting.Id.ToString());

            var html = $@"
<h2>&#128260; Meeting Rescheduled</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>
<p>The following meeting has been rescheduled. Please review the updated details.</p>

<div class=""pz-info-box"">
  <h3>{System.Net.WebUtility.HtmlEncode(product.Name)} &mdash; {System.Net.WebUtility.HtmlEncode(contact.FirstName + " " + contact.LastName)}</h3>
  <ul>
    <li><strong>Previous time:</strong> {oldTimeText}</li>
    <li><strong>New time:</strong> {newTime}</li>
    <li><strong>Duration:</strong> {duration} minutes</li>
    <li><strong>Attendee:</strong> {System.Net.WebUtility.HtmlEncode(contact.FirstName + " " + contact.LastName)} &mdash; {System.Net.WebUtility.HtmlEncode(contact.Email)}</li>
    <li><strong>Meeting link:</strong> {(string.IsNullOrWhiteSpace(joinUrl) ? "TBD" : $"<a href='{System.Net.WebUtility.HtmlEncode(joinUrl)}' target='_blank'>Join meeting</a>")}</li>
  </ul>
</div>

<div class=""pz-panel-teal"">
  <p><strong>For Sales Rep:</strong> Update your calendar and notify the attendee of the new time. Confirm availability and prepare materials for the updated slot.</p>
</div>

<div class=""pz-panel-coral"">
  <p><strong>For Admin:</strong> Update the calendar entry, adjust resource allocations, and ensure recording/guest access is configured for the new time.</p>
</div>

{reasonText}
<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

            return new EmailDto
            {
                Subject = $"Meeting rescheduled: {contact.FirstName} {contact.LastName} — {newTime}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify that an existing meeting was cancelled.
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
            var reasonText = string.IsNullOrWhiteSpace(reason) ? string.Empty : $@"<div class=""pz-panel-coral""><p><strong>Reason:</strong> {reason.Replace("\n", "<br />")}</p></div>";
            var meetingTime = meeting.MeetingDate.ToString("yyyy-MM-dd HH:mm");
            var duration = MeetingConstant.DefaultDurationMinutes;
            var joinUrl = MeetingConstant.BuildJoinUrl(meeting.Id.ToString());

            var html = $@"
<h2 style=""color:#c06c52;"">&#10060; Meeting Cancelled</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>
<p>The following meeting has been cancelled on <strong>{cancelledAt}</strong>.</p>

<div class=""pz-info-box"">
  <h3>{System.Net.WebUtility.HtmlEncode(product.Name)} &mdash; {System.Net.WebUtility.HtmlEncode(contact.FirstName + " " + contact.LastName)}</h3>
  <ul>
    <li><strong>Originally scheduled:</strong> {meetingTime}</li>
    <li><strong>Duration:</strong> {duration} minutes</li>
    <li><strong>Attendee:</strong> {System.Net.WebUtility.HtmlEncode(contact.FirstName + " " + contact.LastName)} &mdash; {System.Net.WebUtility.HtmlEncode(contact.Email)}</li>
    <li><strong>Meeting link:</strong> {(string.IsNullOrWhiteSpace(joinUrl) ? "TBD" : $"<a href='{System.Net.WebUtility.HtmlEncode(joinUrl)}' target='_blank'>Join meeting</a>")}</li>
  </ul>
</div>

<div class=""pz-panel-coral"">
  <p><strong>For Sales Rep:</strong> Notify the attendee, offer to reschedule if appropriate, and update CRM with the cancellation reason and next steps.</p>
</div>

<div class=""pz-panel-teal"">
  <p><strong>For Admin:</strong> Remove or update the calendar entry, release reserved resources, and ensure billing/internal records reflect the cancellation.</p>
</div>

{reasonText}
<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

            return new EmailDto
            {
                Subject = $"Meeting cancelled: {contact.FirstName} {contact.LastName} — {meetingTime}",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }

        /// <summary>
        /// Notify the client (attendee) that a meeting has been booked for them.
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

            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $@"<div class=""pz-panel-teal""><p>{note.Replace("\n", "<br />")}</p></div>";
            var meetingTime = meeting.MeetingDate.ToString("yyyy-MM-dd HH:mm");
            var duration = MeetingConstant.DefaultDurationMinutes;
            var joinUrl = MeetingConstant.BuildJoinUrl(meeting.Id.ToString());

            var html = $@"
<h2>&#128197; Your Meeting is Confirmed</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(contact.FirstName)},</p>
<p>Thank you — your meeting with <strong>{System.Net.WebUtility.HtmlEncode(product.Name)}</strong> has been confirmed.</p>

<div class=""pz-info-box"">
  <ul>
    <li><strong>When:</strong> {meetingTime}</li>
    <li><strong>Duration:</strong> {duration} minutes</li>
    <li><strong>Meeting link:</strong> {(string.IsNullOrWhiteSpace(joinUrl) ? "TBD" : $"<a href='{System.Net.WebUtility.HtmlEncode(joinUrl)}' target='_blank'>Join meeting</a>")}</li>
  </ul>
</div>

<p style=""font-size:13.5px;color:#607078;"">A meeting link will be issued within 24 hours. If you need it sooner, please contact your sales representative.</p>
<p>To reschedule or cancel, simply reply to this email.</p>

{noteText}
<p>We look forward to speaking with you.</p>
<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

            return new EmailDto
            {
                Subject = $"Meeting confirmation: {product.Name} — {meetingTime}",
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
<h2>&#128260; Your Meeting Has Been Rescheduled</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(contact.FirstName)},</p>
<p>Your meeting has been moved to a new time.</p>

<div class=""pz-info-box"">
  <ul>
    <li><strong>Previous time:</strong> {oldTimeText}</li>
    <li><strong>New time:</strong> {newTime}</li>
    <li><strong>Duration:</strong> {duration} minutes</li>
    <li><strong>Meeting link:</strong> {(string.IsNullOrWhiteSpace(joinUrl) ? "TBD" : $"<a href='{System.Net.WebUtility.HtmlEncode(joinUrl)}' target='_blank'>Join meeting</a>")}</li>
  </ul>
</div>

<p>If this time doesn't work for you, please reply to this email and we'll arrange a new slot.</p>
<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

            return new EmailDto
            {
                Subject = $"Meeting rescheduled: {product.Name} — {newTime}",
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
            var reasonText = string.IsNullOrWhiteSpace(reason) ? string.Empty : $@"<div class=""pz-panel-teal""><p><strong>Reason:</strong> {reason.Replace("\n", "<br />")}</p></div>";
            var meetingTime = meeting.MeetingDate.ToString("yyyy-MM-dd HH:mm");

            var html = $@"
<h2 style=""color:#c06c52;"">&#10060; Your Meeting Was Cancelled</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(contact.FirstName)},</p>
<p>We regret to inform you that your meeting scheduled for <strong>{meetingTime}</strong> has been cancelled on <strong>{cancelledAt}</strong>.</p>

{reasonText}

<p>If you'd like to reschedule, please reply to this email and we'll arrange a new time.</p>
<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

            return new EmailDto
            {
                Subject = $"Meeting cancelled: {product.Name} — {meetingTime}",
                Body = SimpleWrapWithTemplate(html),
                RecipientEmail = contact.Email ?? string.Empty,
                RecipientName = contact.FirstName + " " + contact.LastName
            };
        }

        /// <summary>
        /// Notify a sales rep that they passed the mandatory assessment and should book a call with the CEO.
        /// </summary>
        public static EmailDto NotifyBookMeetingWithTrainer(
            string recipientName,
            string recipientEmail,
            string asessmentName)
        {
            var html = $@"
<h2>&#127881; Congratulations — Assessment Passed!</h2>
<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>
<p>Well done! You have successfully passed the {asessmentName}.</p>

<div class=""pz-panel-teal"">
  <p><strong>Next Step:</strong> Please <a href=""https://cal.com/xhanti-mzozoyana-50g1ck/process-zero-executive-onboarding-call"" target=""_blank"" class=""pz-btn"">Book a Call with the CEO</a> to discuss your results and next steps in the ProcessZero journey.</p>
</div>

<hr class=""pz-divider"">
<p style=""color:#607078;font-size:14px;"">Regards,<br/><strong style=""color:#172121;"">ProcessZero Team</strong></p>";

            return new EmailDto
            {
                Subject = "Assessment Passed — Book a Call with the CEO",
                Body = BrandedWrapWithTemplate(html),
                RecipientEmail = recipientEmail ?? string.Empty,
                RecipientName = recipientName ?? string.Empty
            };
        }
    }
}
