using ProcessZero.Application.Dtos;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface IGmailService
    {
        Task SendAsync(
            RelayEmailAccount account,
            string to,
            string subject,
            string body,
            CancellationToken cancellationToken = default);

        Task<List<ReceivedEmailMessageDto>> ReceiveAsync(
            RelayEmailAccount account,
            int maxResults = 10,
            CancellationToken cancellationToken = default);

        Task<ReceivedEmailMessageDto?> GetMessageAsync(
            RelayEmailAccount account,
            string messageId,
            CancellationToken cancellationToken = default);
    }
}
