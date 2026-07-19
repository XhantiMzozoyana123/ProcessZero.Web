using ProcessZero.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface ITwilioService
    {
        /// <summary>
        /// Sends an SMS message via Twilio
        /// </summary>
        Task<bool> SendSmsAsync(TwilioSmsDto smsDto);

        /// <summary>
        /// Sends a WhatsApp message via Twilio
        /// </summary>
        Task<bool> SendWhatsAppAsync(TwilioWhatsAppDto whatsAppDto);

        /// <summary>
        /// Sends a Facebook message via Twilio
        /// </summary>
        Task<bool> SendFacebookMessageAsync(TwilioFacebookDto facebookDto);
    }
}
