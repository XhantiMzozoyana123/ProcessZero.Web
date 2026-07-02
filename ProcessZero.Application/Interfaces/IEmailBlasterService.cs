using ProcessZero.Application.Dtos;
using ProcessZero.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface IEmailBlasterService
    {
        Task SendBulkEmailToUsersAsync(IEnumerable<EmailDto> emails);
    }
}
