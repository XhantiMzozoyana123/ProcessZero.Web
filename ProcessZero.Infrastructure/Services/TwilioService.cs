using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace ProcessZero.Infrastructure.Services
{
    public class TwilioService : ITwilioService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TwilioService> _logger;

        public TwilioService(
            IConfiguration configuration,
            ILogger<TwilioService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes Twilio with credentials from configuration
        /// </summary>
        private void InitializeTwilio()
        {
            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];

            if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
            {
                throw new InvalidOperationException("Twilio credentials are not configured. Please set Twilio:AccountSid and Twilio:AuthToken in appsettings.json");
            }

            TwilioClient.Init(accountSid, authToken);
        }

        /// <summary>
        /// Sends an SMS message via Twilio
        /// </summary>
        public async Task<bool> SendSmsAsync(TwilioSmsDto smsDto)
        {
            try
            {
                if (smsDto == null)
                    throw new ArgumentNullException(nameof(smsDto));

                if (string.IsNullOrWhiteSpace(smsDto.PhoneNumber))
                    throw new ArgumentException("Phone number is required", nameof(smsDto));

                if (string.IsNullOrWhiteSpace(smsDto.Message))
                    throw new ArgumentException("Message is required", nameof(smsDto));

                InitializeTwilio();

                var fromPhone = _configuration["Twilio:PhoneNumber"];
                if (string.IsNullOrWhiteSpace(fromPhone))
                {
                    throw new InvalidOperationException("Twilio phone number is not configured. Please set Twilio:PhoneNumber in appsettings.json");
                }

                var message = await MessageResource.CreateAsync(
                    body: smsDto.Message,
                    from: new PhoneNumber(fromPhone),
                    to: new PhoneNumber(smsDto.PhoneNumber)
                );

                _logger.LogInformation($"SMS sent successfully. SID: {message.Sid}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending SMS: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sends a WhatsApp message via Twilio
        /// </summary>
        public async Task<bool> SendWhatsAppAsync(TwilioWhatsAppDto whatsAppDto)
        {
            try
            {
                if (whatsAppDto == null)
                    throw new ArgumentNullException(nameof(whatsAppDto));

                if (string.IsNullOrWhiteSpace(whatsAppDto.PhoneNumber))
                    throw new ArgumentException("Phone number is required", nameof(whatsAppDto));

                if (string.IsNullOrWhiteSpace(whatsAppDto.Message))
                    throw new ArgumentException("Message is required", nameof(whatsAppDto));

                InitializeTwilio();

                var fromPhone = _configuration["Twilio:WhatsAppNumber"];
                if (string.IsNullOrWhiteSpace(fromPhone))
                {
                    throw new InvalidOperationException("Twilio WhatsApp number is not configured. Please set Twilio:WhatsAppNumber in appsettings.json");
                }

                // WhatsApp phone numbers must be in format: whatsapp:+1234567890
                var toPhoneNumber = whatsAppDto.PhoneNumber.StartsWith("whatsapp:") 
                    ? whatsAppDto.PhoneNumber 
                    : $"whatsapp:{whatsAppDto.PhoneNumber}";

                var message = await MessageResource.CreateAsync(
                    body: whatsAppDto.Message,
                    from: new PhoneNumber(fromPhone),
                    to: new PhoneNumber(toPhoneNumber)
                );

                _logger.LogInformation($"WhatsApp message sent successfully. SID: {message.Sid}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending WhatsApp message: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sends a Facebook message via Twilio
        /// </summary>
        public async Task<bool> SendFacebookMessageAsync(TwilioFacebookDto facebookDto)
        {
            try
            {
                if (facebookDto == null)
                    throw new ArgumentNullException(nameof(facebookDto));

                if (string.IsNullOrWhiteSpace(facebookDto.RecipientId))
                    throw new ArgumentException("Recipient ID is required", nameof(facebookDto));

                if (string.IsNullOrWhiteSpace(facebookDto.Message))
                    throw new ArgumentException("Message is required", nameof(facebookDto));

                InitializeTwilio();

                var fromPhone = _configuration["Twilio:FacebookMessengerId"];
                if (string.IsNullOrWhiteSpace(fromPhone))
                {
                    throw new InvalidOperationException("Twilio Facebook Messenger ID is not configured. Please set Twilio:FacebookMessengerId in appsettings.json");
                }

                // Facebook Messenger format: messenger:recipient_id
                var toPhoneNumber = $"messenger:{facebookDto.RecipientId}";

                var message = await MessageResource.CreateAsync(
                    body: facebookDto.Message,
                    from: new PhoneNumber(fromPhone),
                    to: new PhoneNumber(toPhoneNumber)
                );

                _logger.LogInformation($"Facebook message sent successfully. SID: {message.Sid}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending Facebook message: {ex.Message}");
                throw;
            }
        }
    }
}
