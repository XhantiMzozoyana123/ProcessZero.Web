using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace ProcessZero.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ApplicationDbContext _context;

        public EmailService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task SendEmailAsync(EmailDto emailDto)
        {
            if (emailDto == null) throw new ArgumentNullException(nameof(emailDto));
            if (string.IsNullOrWhiteSpace(emailDto.RecipientEmail)) throw new ArgumentException("Recipient email is required", nameof(emailDto));

            // Try to resolve primary inbox configuration from database
            Inbox? inbox = null;
            try
            {
                if (_context != null)
                {
                    inbox = await _context.Set<Inbox>().FirstOrDefaultAsync(i => i.IsPrimary);
                    if (inbox == null)
                        inbox = await _context.Set<Inbox>().FirstOrDefaultAsync();
                }
            }
            catch
            {
                // ignore DB errors here; will throw below if no config available
            }

            if (inbox == null)
                throw new InvalidOperationException("No inbox configuration available to send email");

            var fromEmail = !string.IsNullOrWhiteSpace(inbox.Username) ? inbox.Username : string.Empty;
            var fromName = inbox.Username ?? string.Empty;
            var fromAddress = new MailAddress(fromEmail, fromName);
            var toAddress = new MailAddress(emailDto.RecipientEmail, emailDto.RecipientName ?? string.Empty);

            var subject = emailDto.Subject.Replace("[name]", emailDto.RecipientName ?? string.Empty);
            var body = emailDto.Body.Replace("[name]", emailDto.RecipientName ?? string.Empty);

            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = ConvertPlainTextToHtml(body),
                IsBodyHtml = true
            };

            if (string.IsNullOrWhiteSpace(inbox.SmtpHost))
                throw new InvalidOperationException("SMTP host is not configured for the inbox");

            var smtpUser = inbox.Username ?? string.Empty;
            var smtpPass = inbox.Password ?? string.Empty;

            using var client = new SmtpClient(inbox.SmtpHost, inbox.SmtpPort)
            {
                EnableSsl = inbox.SmtpUseSsl,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            // Send asynchronously
            await client.SendMailAsync(message);
        }

        private static string ConvertPlainTextToHtml(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            // 1. Encode first so malicious code isn't injected
            string encoded = WebUtility.HtmlEncode(text);

            // 2. Replace newlines with HTML break tags
            return encoded.Replace("\r\n", "<br />").Replace("\n", "<br />");
        }
    }
}
