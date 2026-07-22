using System.Threading;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface IPayPalService
    {
        Task<(string OrderId, string ApprovalUrl)> CreateOrderAsync(decimal amount, string currency, string returnUrl, string cancelUrl, CancellationToken cancellationToken = default);
        Task<string> CaptureOrderAsync(string orderId, CancellationToken cancellationToken = default);
    }
}
