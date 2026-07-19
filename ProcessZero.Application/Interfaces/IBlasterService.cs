using ProcessZero.Application.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface IBlasterService
    {
        /// <summary>
        /// Sends bulk emails to multiple users
        /// </summary>
        Task SendBulkEmailToUsersAsync(IEnumerable<EmailDto> emails);

        /// <summary>
        /// Sends bulk SMS messages to multiple recipients
        /// </summary>
        Task SendBulkSmsAsync(IEnumerable<TwilioSmsDto> messages);

        /// <summary>
        /// Sends bulk WhatsApp messages to multiple recipients
        /// </summary>
        Task SendBulkWhatsAppAsync(IEnumerable<TwilioWhatsAppDto> messages);

        /// <summary>
        /// Sends bulk Facebook messages to multiple recipients
        /// </summary>
        Task SendBulkFacebookAsync(IEnumerable<TwilioFacebookDto> messages);
    }
}
