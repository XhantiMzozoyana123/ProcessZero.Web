using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProcessZero.Application.Interfaces;

namespace ProcessZero.Infrastructure.Services
{
    public class PayPalService : IPayPalService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PayPalService> _logger;

        private string AccessToken { get; set; } = string.Empty;
        private DateTime AccessTokenExpiresAt { get; set; }

        public PayPalService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<PayPalService> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(AccessToken) && AccessTokenExpiresAt > DateTime.UtcNow.AddMinutes(5))
                return AccessToken;

            var clientId = _configuration["PayPal:ClientId"];
            var clientSecret = _configuration["PayPal:ClientSecret"];
            var environment = _configuration["PayPal:Environment"] ?? "Sandbox";
            var baseUrl = environment.Equals("Live", StringComparison.OrdinalIgnoreCase)
                ? "https://api-m.paypal.com"
                : "https://api-m.sandbox.paypal.com";

            var client = _httpClientFactory.CreateClient();
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            var requestBody = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await client.PostAsync($"{baseUrl}/v1/oauth2/token", requestBody, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to get PayPal access token: {Error}", error);
                throw new InvalidOperationException("Failed to get PayPal access token.");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            AccessToken = doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

            return AccessToken;
        }

        public async Task<(string OrderId, string ApprovalUrl)> CreateOrderAsync(decimal amount, string currency, string returnUrl, string cancelUrl, CancellationToken cancellationToken = default)
        {
            var environment = _configuration["PayPal:Environment"] ?? "Sandbox";
            var baseUrl = environment.Equals("Live", StringComparison.OrdinalIgnoreCase)
                ? "https://api-m.paypal.com"
                : "https://api-m.sandbox.paypal.com";

            var token = await GetAccessTokenAsync(cancellationToken);
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        amount = new
                        {
                            currency_code = currency,
                            value = amount.ToString("0.00", CultureInfo.InvariantCulture)
                        }
                    }
                },
                application_context = new
                {
                    return_url = returnUrl,
                    cancel_url = cancelUrl,
                    brand_name = "Process Zero",
                    landing_page = "LOGIN",
                    user_action = "PAY_NOW"
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{baseUrl}/v2/checkout/orders", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("PayPal create order failed: {Error}", error);
                throw new InvalidOperationException("Failed to create PayPal order.");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var orderId = doc.RootElement.GetProperty("id").GetString() ?? string.Empty;

            // Extract approval URL from links — safely handle missing "approve" link
            var approveLink = doc.RootElement
                .GetProperty("links")
                .EnumerateArray()
                .FirstOrDefault(link => link.GetProperty("rel").GetString() == "approve");

            var approvalUrl = string.Empty;
            if (approveLink.ValueKind != JsonValueKind.Undefined)
            {
                approvalUrl = approveLink.GetProperty("href").GetString() ?? string.Empty;
            }

            if (string.IsNullOrEmpty(approvalUrl))
            {
                _logger.LogError("PayPal order {OrderId} created but no approval URL found in response: {Response}", orderId, json);
                throw new InvalidOperationException("Failed to get PayPal approval URL.");
            }

            return (orderId, approvalUrl);
        }

        public async Task<string> CaptureOrderAsync(string orderId, CancellationToken cancellationToken = default)
        {
            var environment = _configuration["PayPal:Environment"] ?? "Sandbox";
            var baseUrl = environment.Equals("Live", StringComparison.OrdinalIgnoreCase)
                ? "https://api-m.paypal.com"
                : "https://api-m.sandbox.paypal.com";

            var token = await GetAccessTokenAsync(cancellationToken);
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{baseUrl}/v2/checkout/orders/{orderId}/capture", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("PayPal capture order failed: {Error}", error);
                throw new InvalidOperationException("Failed to capture PayPal order.");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetRawText();
        }
    }
}