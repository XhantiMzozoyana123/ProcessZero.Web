using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using PayPalHttp;
using ProcessZero.Application.Interfaces;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ProcessZero.Infrastructure.Services
{
    public class PayPalService : IPayPalService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PayPalService> _logger;
        private readonly PayPalHttpClient _payPalClient;

        public PayPalService(IConfiguration configuration, ILogger<PayPalService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var clientId = _configuration["PayPal:ClientId"];
            var clientSecret = _configuration["PayPal:ClientSecret"];
            var environment = _configuration["PayPal:Environment"] ?? "Sandbox";

            if (string.IsNullOrWhiteSpace(clientId))
                throw new InvalidOperationException("PayPal ClientId is not configured.");

            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new InvalidOperationException("PayPal ClientSecret is not configured.");

            PayPalEnvironment payPalEnvironment;
            if (environment.Equals("Live", StringComparison.OrdinalIgnoreCase))
            {
                payPalEnvironment = new LiveEnvironment(clientId, clientSecret);
            }
            else
            {
                payPalEnvironment = new SandboxEnvironment(clientId, clientSecret);
            }

            _payPalClient = new PayPalHttpClient(payPalEnvironment);
        }

        public async Task<(string OrderId, string ApprovalUrl)> CreateOrderAsync(
            decimal amount,
            string currency,
            string returnUrl,
            string cancelUrl,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var payloadCurrency = _configuration["PayPal:Currency"] ?? currency;

                var orderRequest = new OrderRequest()
                {
                    CheckoutPaymentIntent = "CAPTURE",
                    ApplicationContext = new ApplicationContext
                    {
                        BrandName = "Process Zero",
                        LandingPage = "LOGIN",
                        UserAction = "PAY_NOW",
                        ReturnUrl = returnUrl,
                        CancelUrl = cancelUrl
                    },
                    PurchaseUnits = new List<PurchaseUnitRequest>
                    {
                        new PurchaseUnitRequest
                        {
                            AmountWithBreakdown = new AmountWithBreakdown
                            {
                                CurrencyCode = payloadCurrency,
                                Value = amount.ToString("0.00", CultureInfo.InvariantCulture)
                            }
                        }
                    }
                };

                var request = new OrdersCreateRequest();
                request.Prefer("return=representation");
                request.RequestBody(orderRequest);

                _logger.LogInformation("Creating PayPal order for amount {Amount} {Currency}", amount, payloadCurrency);

                // Note: The legacy SDK's Execute method doesn't accept a CancellationToken natively,
                // but we keep it in the signature for interface compliance.
                var response = await _payPalClient.Execute(request);
                var result = response.Result<Order>();

                var approvalLink = result.Links.FirstOrDefault(l => l.Rel == "approve" || l.Rel == "payer-action");
                if (approvalLink == null || string.IsNullOrWhiteSpace(approvalLink.Href))
                {
                    throw new InvalidOperationException("PayPal did not return an approval URL.");
                }

                _logger.LogInformation("PayPal order created successfully. OrderId: {OrderId}", result.Id);

                return (result.Id, approvalLink.Href);
            }
            catch (HttpException ex)
            {
                // Extract the deep debug information provided by PayPal's API
                string debugId = ex.Headers.Contains("PayPal-Debug-Id")
                    ? string.Join(",", ex.Headers.GetValues("PayPal-Debug-Id"))
                    : "Unknown";

                _logger.LogError(ex, "PayPal order creation failed. Status: {StatusCode}, DebugID: {DebugId}, Content: {ResponseBody}",
                    ex.StatusCode, debugId, ex.Message);

                throw new InvalidOperationException(
                    $"PayPal order creation failed ({(int)ex.StatusCode} {ex.StatusCode}). DebugID: {debugId}. See logs for details.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during PayPal order creation");
                throw;
            }
        }

        public async Task<string> CaptureOrderAsync(
            string orderId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                throw new ArgumentException("Order ID cannot be null or empty.", nameof(orderId));
            }

            try
            {
                _logger.LogInformation("Capturing PayPal order {OrderId}", orderId);

                var request = new OrdersCaptureRequest(orderId);
                request.RequestBody(new OrderActionRequest());

                var response = await _payPalClient.Execute(request);
                var result = response.Result<Order>();

                _logger.LogInformation("PayPal order captured successfully. OrderId: {OrderId}, Status: {Status}",
                    result.Id, result.Status);

                var jsonResult = JsonSerializer.Serialize(result);
                return jsonResult;
            }
            catch (HttpException ex)
            {
                string debugId = ex.Headers.Contains("PayPal-Debug-Id")
                    ? string.Join(",", ex.Headers.GetValues("PayPal-Debug-Id"))
                    : "Unknown";

                _logger.LogError(ex, "PayPal capture failed. Status: {StatusCode}, DebugID: {DebugId}, Content: {ResponseBody}",
                    ex.StatusCode, debugId, ex.Message);

                throw new InvalidOperationException(
                    $"PayPal capture failed ({(int)ex.StatusCode} {ex.StatusCode}). DebugID: {debugId}. See logs for details.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during PayPal order capture");
                throw;
            }
        }
    }
}