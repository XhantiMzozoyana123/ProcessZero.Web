using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    public class BlasterService : IBlasterService
    {
        private readonly IEmailService _emailService;
        private readonly ITwilioService _twilioService;

        public BlasterService(
            IEmailService emailService,
            ITwilioService twilioService)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _twilioService = twilioService ?? throw new ArgumentNullException(nameof(twilioService));
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

        public async Task SendBulkSmsAsync(IEnumerable<TwilioSmsDto> messages)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));

            foreach (var message in messages)
            {
                if (message == null) continue;
                await _twilioService.SendSmsAsync(message);
            }
        }

        public async Task SendBulkWhatsAppAsync(IEnumerable<TwilioWhatsAppDto> messages)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));

            foreach (var message in messages)
            {
                if (message == null) continue;
                await _twilioService.SendWhatsAppAsync(message);
            }
        }

        public async Task SendBulkFacebookAsync(IEnumerable<TwilioFacebookDto> messages)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));

            foreach (var message in messages)
            {
                if (message == null) continue;
                await _twilioService.SendFacebookMessageAsync(message);
            }
        }
    }
}

