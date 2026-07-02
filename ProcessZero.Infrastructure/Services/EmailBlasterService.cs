using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    public class EmailBlasterService : IEmailBlasterService
    {
        private readonly IEmailService _emailService;
        public EmailBlasterService(IEmailService emailService)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public async Task SendBulkEmailToUsersAsync(IEnumerable<EmailDto> emails)
        {
            if (emails == null) throw new ArgumentNullException(nameof(emails));

            foreach (var email in emails)
            {
                if (email == null) continue;
                await _emailService.SendEmailAsync(email);
            }
        }
    }
}
