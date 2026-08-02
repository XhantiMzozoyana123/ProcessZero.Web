using System.Threading;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    /// <summary>
    /// Service for interacting with payment gateways.
    /// Includes PayGate.to (crypto payments) and PayShap (manual bank transfer with verification).
    /// </summary>
    public interface IPayGateService
    {
        #region PayGate (Crypto Payment Gateway)

        /// <summary>
        /// Step 1: Create a temporary encrypted wallet address for a customer payment.
        /// </summary>
        /// <param name="payoutWallet">Your USDC (Polygon) wallet for instant payouts.</param>
        /// <param name="callbackUrl">Unique callback URL (must include unique GET param per request).</param>
        /// <returns>Encrypted address_in, polygon_address_in, callback_url, ipn_token</returns>
        Task<PayGateWalletResponse> CreateWalletAsync(string payoutWallet, string callbackUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Step 2: Build the payment URL the customer is redirected to.
        /// Returns the full checkout URL.
        /// </summary>
        string BuildPaymentUrl(string encryptedAddressIn, decimal amount, string provider, string email, string currency);

        /// <summary>
        /// Fetch the list of available providers and their statuses.
        /// </summary>
        Task<PayGateProviderListResponse?> GetProvidersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Check payment status using the ipn_token.
        /// </summary>
        Task<PayGatePaymentStatusResponse?> GetPaymentStatusAsync(string ipnToken, CancellationToken cancellationToken = default);

        #endregion

        #region PayShap (Manual Bank Transfer)

        /// <summary>
        /// Generates a unique PayShap reference for a payment order.
        /// </summary>
        string GeneratePayShapReference();

        /// <summary>
        /// Validates if a PayShap reference format is correct.
        /// </summary>
        bool ValidatePayShapReference(string reference);

        #endregion
    }

    public class PayGateWalletResponse
    {
        public string AddressIn { get; set; } = string.Empty;
        public string PolygonAddressIn { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
        public string IpnToken { get; set; } = string.Empty;
    }

    public class PayGateProviderListResponse
    {
        public List<PayGateProvider> Providers { get; set; } = new();
    }

    public class PayGateProvider
    {
        public string Id { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string MinimumCurrency { get; set; } = string.Empty;
        public decimal MinimumAmount { get; set; }
    }

    public class PayGatePaymentStatusResponse
    {
        public string Status { get; set; } = string.Empty;
        public string ValueCoin { get; set; } = string.Empty;
        public string TxidOut { get; set; } = string.Empty;
        public string Coin { get; set; } = string.Empty;
    }
}