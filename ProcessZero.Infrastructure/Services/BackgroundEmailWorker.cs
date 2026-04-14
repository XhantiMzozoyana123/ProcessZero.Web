using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Infrastructure.Services
{
    /// <summary>
    /// Default background worker that uses the existing EmailService to actually send messages.
    /// This will be executed by Hangfire and runs out-of-process from the HTTP request.
    /// </summary>
    public class BackgroundEmailWorker : IBackgroundEmailWorker
    {
        private readonly IEmailService _emailService;

        public BackgroundEmailWorker(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task SendAsync(EmailDto emailDto)
        {
            await _emailService.SendEmailAsync(emailDto);
        }
    }
}
