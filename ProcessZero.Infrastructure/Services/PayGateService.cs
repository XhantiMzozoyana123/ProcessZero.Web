using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProcessZero.Application.Interfaces;

namespace ProcessZero.Infrastructure.Services
{
    /// <summary>
    /// PayGate.to payment gateway integration.
    /// No API keys required — just a USDC (Polygon) wallet address.
    ///
    /// Flow:
    /// 1. CreateWalletAsync() → get encrypted address_in
    /// 2. BuildPaymentUrl() → redirect customer to checkout
    /// 3. Callback hits our server with payment confirmation
    /// 4. GetPaymentStatusAsync() to verify
    /// </summary>
    public class PayGateService : IPayGateService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PayGateService> _logger;

        private const string BaseApiUrl = "https://api.paygate.to";
        private const string CheckoutUrl = "https://checkout.paygate.to";
        private const int HttpTimeoutSeconds = 30;

        public PayGateService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<PayGateService> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Returns the configured USDC (Polygon) payout wallet address.
        /// </summary>
        private string GetPayoutWallet()
        {
            return _configuration["PayGate:PayoutWallet"] ?? string.Empty;
        }

        /// <summary>
        /// Returns the configured web URL for callback URLs.
        /// </summary>
        private string GetWebUrl()
        {
            return _configuration["PayGate:WebUrl"] ?? "https://processzero.xyz";
        }

        /// <summary>
        /// Creates an HttpClient with a configured timeout for PayGate API calls.
        /// </summary>
        private HttpClient CreateHttpClient()
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds);
            return client;
        }

        /// <summary>
        /// Step 1: Create a temporary encrypted wallet address for a customer payment.
        /// POST https://api.paygate.to/control/wallet.php
        /// Body: address={wallet}&callback={callback}
        /// </summary>
        public async Task<PayGateWalletResponse> CreateWalletAsync(
            string payoutWallet,
            string callbackUrl,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(payoutWallet))
                payoutWallet = GetPayoutWallet();

            if (string.IsNullOrWhiteSpace(payoutWallet))
                throw new InvalidOperationException("PayGate payout wallet is not configured. Check 'PayGate:PayoutWallet' in app settings.");

            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new ArgumentException("Callback URL is required.", nameof(callbackUrl));

            var client = CreateHttpClient();
            var url = $"{BaseApiUrl}/control/wallet.php";

            _logger.LogInformation(
                "Creating PayGate wallet - URL: {BaseUrl}/control/wallet.php, Callback: {Callback}",
                BaseApiUrl, callbackUrl);

            try
            {
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("address", payoutWallet),
                    new KeyValuePair<string, string>("callback", callbackUrl)
                });

                var response = await client.PostAsync(url, formContent, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogInformation(
                    "PayGate wallet creation response: StatusCode={StatusCode}, Body={Body}",
                    response.StatusCode, body);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "PayGate create wallet failed with status {StatusCode}: {Body}",
                        response.StatusCode, body);
                    throw new InvalidOperationException(
                        $"PayGate wallet creation failed with HTTP {(int)response.StatusCode}: {body}");
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    throw new InvalidOperationException("PayGate wallet creation returned an empty response body.");
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                // Validate required fields exist in the response
                if (!root.TryGetProperty("address_in", out var addressInProp) ||
                    !root.TryGetProperty("polygon_address_in", out var polygonAddressInProp) ||
                    !root.TryGetProperty("callback_url", out var callbackUrlProp) ||
                    !root.TryGetProperty("ipn_token", out var ipnTokenProp))
                {
                    _logger.LogError(
                        "PayGate wallet creation response is missing required fields. Response: {Body}",
                        body);
                    throw new InvalidOperationException(
                        "PayGate wallet creation returned an incomplete response. Missing required fields.");
                }

                var result = new PayGateWalletResponse
                {
                    AddressIn = addressInProp.GetString() ?? string.Empty,
                    PolygonAddressIn = polygonAddressInProp.GetString() ?? string.Empty,
                    CallbackUrl = callbackUrlProp.GetString() ?? string.Empty,
                    IpnToken = ipnTokenProp.GetString() ?? string.Empty
                };

                if (string.IsNullOrWhiteSpace(result.AddressIn) || string.IsNullOrWhiteSpace(result.IpnToken))
                {
                    _logger.LogError(
                        "PayGate wallet creation returned empty address_in or ipn_token. Response: {Body}",
                        body);
                    throw new InvalidOperationException(
                        "PayGate wallet creation returned empty address or token.");
                }

                _logger.LogInformation(
                    "PayGate wallet created successfully: polygon_address_in={Polygon}, ipn_token={Ipn}",
                    result.PolygonAddressIn, result.IpnToken);

                return result;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(
                    "PayGate wallet creation request timed out after {TimeoutSeconds} seconds for callback: {Callback}",
                    HttpTimeoutSeconds, callbackUrl);
                throw new InvalidOperationException(
                    $"PayGate API request timed out after {HttpTimeoutSeconds} seconds. The payment gateway may be temporarily unavailable.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "PayGate wallet creation HTTP request failed for callback: {Callback}. Message: {Message}",
                    callbackUrl, ex.Message);
                throw new InvalidOperationException(
                    $"PayGate API request failed: {ex.Message}. Check network connectivity and API availability.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to parse PayGate wallet creation response for callback: {Callback}",
                    callbackUrl);
                throw new InvalidOperationException(
                    "PayGate returned an invalid response format. The payment gateway may need to be updated.");
            }
        }

        /// <summary>
        /// Step 2: Build the payment URL the customer is redirected to.
        /// GET https://checkout.paygate.to/process-payment.php?address={address_in}&amount={amount}&provider={provider}&email={email}&currency={currency}
        /// </summary>
        public string BuildPaymentUrl(
            string encryptedAddressIn,
            decimal amount,
            string provider,
            string email,
            string currency)
        {
            if (string.IsNullOrWhiteSpace(encryptedAddressIn))
                throw new ArgumentException("Encrypted address is required.", nameof(encryptedAddressIn));
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentException("Provider is required.", nameof(provider));
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(email));
            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency is required.", nameof(currency));

            var encodedAddress = HttpUtility.UrlEncode(encryptedAddressIn);
            var encodedEmail = HttpUtility.UrlEncode(email);

            return $"{CheckoutUrl}/process-payment.php" +
                   $"?address={encodedAddress}" +
                   $"&amount={amount.ToString("0.00", CultureInfo.InvariantCulture)}" +
                   $"&provider={provider}" +
                   $"&email={encodedEmail}" +
                   $"&currency={currency}";
        }

        /// <summary>
        /// Fetch the list of available providers and their statuses.
        /// GET https://api.paygate.to/control/provider-status
        /// </summary>
        public async Task<PayGateProviderListResponse?> GetProvidersAsync(CancellationToken cancellationToken = default)
        {
            var client = CreateHttpClient();
            var url = $"{BaseApiUrl}/control/provider-status";

            _logger.LogInformation("Fetching PayGate providers from {Url}", url);

            try
            {
                var response = await client.GetAsync(url, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayGate get providers failed with status {StatusCode}: {Body}", response.StatusCode, body);
                    return null;
                }

                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;

                    var providers = new List<PayGateProvider>();
                    if (root.TryGetProperty("providers", out var providersElement))
                    {
                        foreach (var p in providersElement.EnumerateArray())
                        {
                            providers.Add(new PayGateProvider
                            {
                                Id = p.GetProperty("id").GetString() ?? string.Empty,
                                ProviderName = p.GetProperty("provider_name").GetString() ?? string.Empty,
                                Status = p.GetProperty("status").GetString() ?? string.Empty,
                                MinimumCurrency = p.GetProperty("minimum_currency").GetString() ?? string.Empty,
                                MinimumAmount = p.GetProperty("minimum_amount").GetDecimal()
                            });
                        }
                    }

                    return new PayGateProviderListResponse { Providers = providers };
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse PayGate providers response: {Body}", body);
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while fetching PayGate providers");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request timed out while fetching PayGate providers");
                return null;
            }
        }

        /// <summary>
        /// Check payment status using the ipn_token.
        /// GET https://api.paygate.to/control/payment-status.php?ipn_token={token}
        /// </summary>
        public async Task<PayGatePaymentStatusResponse?> GetPaymentStatusAsync(
            string ipnToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ipnToken))
            {
                _logger.LogWarning("GetPaymentStatusAsync called with empty IPN token");
                return null;
            }

            var client = CreateHttpClient();
            var encodedToken = HttpUtility.UrlEncode(ipnToken);
            var url = $"{BaseApiUrl}/control/payment-status.php?ipn_token={encodedToken}";

            _logger.LogInformation("Checking PayGate payment status for IPN token: {IpnToken}", ipnToken);

            try
            {
                var response = await client.GetAsync(url, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayGate payment status check failed with status {StatusCode}: {Body}", response.StatusCode, body);
                    return null;
                }

                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;

                    return new PayGatePaymentStatusResponse
                    {
                        Status = root.GetProperty("status").GetString() ?? string.Empty,
                        ValueCoin = root.GetProperty("value_coin").GetString() ?? string.Empty,
                        TxidOut = root.GetProperty("txid_out").GetString() ?? string.Empty,
                        Coin = root.GetProperty("coin").GetString() ?? string.Empty
                    };
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse PayGate payment status response: {Body}", body);
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while checking PayGate payment status for IPN token: {IpnToken}", ipnToken);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request timed out while checking PayGate payment status for IPN token: {IpnToken}", ipnToken);
                return null;
            }
        }

        #region PayShap (Manual Bank Transfer)

        /// <summary>
        /// Generates a unique PayShap reference for a payment order.
        /// Format: PZ-YYYYMMDD-XXXXXXXX
        /// </summary>
        public string GeneratePayShapReference()
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var guid = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            return $"PZ-{date}-{guid}";
        }

        /// <summary>
        /// Validates if a PayShap reference format is correct.
        /// Expected format: PZ-YYYYMMDD-XXXXXXXX where X is alphanumeric
        /// </summary>
        public bool ValidatePayShapReference(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                return false;

            var pattern = @"^PZ-\d{8}-[A-Z0-9]{8}$";
            return System.Text.RegularExpressions.Regex.IsMatch(reference, pattern);
        }

        #endregion
    }
}
