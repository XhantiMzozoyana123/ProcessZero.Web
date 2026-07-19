using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Application.Options;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ProcessZero.Infrastructure.Services
{
    /// <summary>
    /// Service for integrating with cal.com scheduling API (v2).
    /// Handles Date formatting, Timezone defaulting, and API versioning.
    /// </summary>
    public class CalService : ICalService
    {
        private readonly HttpClient _httpClient;
        private readonly CalOptions _options;
        private readonly ILogger<CalService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        private const string CalApiVersionHeader = "cal-api-version";
        private const string CalApiVersionValue = "2024-06-11";

        public CalService(HttpClient httpClient, IOptions<CalOptions> options, ILogger<CalService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            _jsonOptions.Converters.Add(new StrictUtcDateTimeOffsetConverter());

            var configuredBase = (_options.BaseUrl ?? "https://api.cal.com/v2/").TrimEnd('/');
            _httpClient.BaseAddress = new Uri(configuredBase + "/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
            _httpClient.DefaultRequestHeaders.Add(CalApiVersionHeader, CalApiVersionValue);
        }

        private string FormatCalDate(DateTimeOffset dt)
            => dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

        // ──── Create Booking (Now respecting Attendee Timezone) ───────────
        public async Task<CalBookingResponse> CreateBookingAsync(
    CreateCalBookingRequest request,
    CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            _httpClient.DefaultRequestHeaders.Add(CalApiVersionHeader, CalApiVersionValue);

            if (request.EventTypeId <= 0)
                request.EventTypeId = _options.EventTypeId;

            request.Attendee.TimeZone = string.IsNullOrWhiteSpace(request.Attendee.TimeZone)
                ? "Africa/Johannesburg"
                : request.Attendee.TimeZone;

            request.Attendee.Language ??= "en";

            _logger.LogInformation(
                "Creating booking for {Email} in TimeZone {TZ}",
                request.Attendee.Email,
                request.Attendee.TimeZone);

            var json = JsonSerializer.Serialize(request, _jsonOptions);

            _logger.LogInformation("Cal.com Payload: {Payload}", json);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "bookings");
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // ✅ FIX: important missing header
            httpRequest.Headers.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            httpRequest.Headers.Add("cal-api-version", "2026-02-25");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            return await DeserializeResponseAsync<CalBookingResponse>(response, cancellationToken);
        }

        public async Task<CalBookingResponse> GetBookingByUidAsync(string uid, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"bookings/{Uri.EscapeDataString(uid)}", cancellationToken);
            return await DeserializeResponseAsync<CalBookingResponse>(response, cancellationToken);
        }

        public async Task<CalBookingResponse> GetBookingByIdAsync(int bookingId, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"bookings/{bookingId}", cancellationToken);
            return await DeserializeResponseAsync<CalBookingResponse>(response, cancellationToken);
        }

        public async Task<CalBookingResponse> CancelBookingAsync(
            int bookingId,
            string? reason = null,
            CancellationToken cancellationToken = default)
                {
                    var body = JsonSerializer.Serialize(new
                    {
                        reason = reason ?? "Cancelled via application"
                    }, _jsonOptions);

                    using var request = new HttpRequestMessage(HttpMethod.Post, $"bookings/{bookingId}/cancel");
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                    request.Headers.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Add("cal-api-version", "2026-02-25");

                    var response = await _httpClient.SendAsync(request, cancellationToken);

                    return await DeserializeResponseAsync<CalBookingResponse>(response, cancellationToken);
        }

        // ──── Availability Logic (Respecting Dynamic Timezones) ───────────
        public async Task<CalAvailabilityResponse> GetAvailableSlotsAsync(
            CalAvailabilityRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var eventId = request.EventTypeId > 0 ? request.EventTypeId : _options.EventTypeId;

            var startParsed = DateTimeOffset.Parse(request.StartTime);
            var endParsed = DateTimeOffset.Parse(request.EndTime);

            var queryParams = new List<string>
            {
                $"eventTypeId={eventId}",
                $"startTime={Uri.EscapeDataString(FormatCalDate(startParsed))}",
                $"endTime={Uri.EscapeDataString(FormatCalDate(endParsed))}",
                $"timeZone={Uri.EscapeDataString(request.TimeZone ?? "Africa/Johannesburg")}"
            };

            var url = $"slots/available?{string.Join("&", queryParams)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            return await DeserializeResponseAsync<CalAvailabilityResponse>(response, cancellationToken);
        }

        public async Task<CalAvailabilityResponse> GetAllAvailableSlotsAsync(int eventTypeId, string? timeZone = null, CancellationToken cancellationToken = default)
        {
            var start = DateTimeOffset.UtcNow.Date;
            var request = new CalAvailabilityRequest
            {
                EventTypeId = eventTypeId,
                StartTime = FormatCalDate(start),
                EndTime = FormatCalDate(start.AddDays(90)),
                TimeZone = timeZone ?? "Africa/Johannesburg"
            };
            return await GetAvailableSlotsAsync(request, cancellationToken);
        }

        public async Task<List<DateTimeOffset>> GetAllAvailableDateTimesAsync(int eventTypeId, string? timeZone = null, CancellationToken cancellationToken = default)
        {
            var availability = await GetAllAvailableSlotsAsync(eventTypeId, timeZone, cancellationToken);
            var slots = new List<DateTimeOffset>();
            if (availability.Data?.Slots != null)
            {
                foreach (var day in availability.Data.Slots.Values)
                    slots.AddRange(day.Select(s => s.Time));
            }
            return slots.OrderBy(x => x).ToList();
        }

        public async Task HandleWebhookAsync(string triggerEvent, string payload, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Webhook: {Event}", triggerEvent);
            await Task.CompletedTask;
        }

        private async Task<T> DeserializeResponseAsync<T>(
     HttpResponseMessage response,
     CancellationToken cancellationToken) where T : class
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Cal.com Error Response: {Body}", body);

                throw new InvalidOperationException(
                    $"Cal.com request failed. Status: {response.StatusCode}. Body: {body}");
            }

            return JsonSerializer.Deserialize<T>(body, _jsonOptions)
                ?? throw new InvalidOperationException("Empty response from Cal.com");
        }
    }
}