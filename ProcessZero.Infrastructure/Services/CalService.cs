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
    /// Uses an API key for authentication and supports booking CRUD,
    /// availability queries, and webhook processing.
    /// </summary>
    public class CalService : ICalService
    {
        private readonly HttpClient _httpClient;
        private readonly CalOptions _options;
        private readonly ILogger<CalService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        private const string CalApiVersionHeader = "cal-api-version";
        private const string CalApiVersionValue = "2024-06-14";

        public CalService(
            HttpClient httpClient,
            IOptions<CalOptions> options,
            ILogger<CalService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Normalize the incoming config value: ensure exactly one trailing slash
            var configuredBase = (_options.BaseUrl ?? string.Empty).TrimEnd('/');
            var normalizedBase = string.IsNullOrWhiteSpace(configuredBase)
                ? "https://api.cal.com/v2/"
                : configuredBase + "/";

            // Configure the HttpClient with the base URL and auth header
            _httpClient.BaseAddress = new Uri(normalizedBase);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
            _httpClient.DefaultRequestHeaders.Add(CalApiVersionHeader, CalApiVersionValue);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            _logger.LogInformation(
                "CalService initialized. BaseAddress={BaseAddress}, EventTypeId={EventTypeId}, ApiKeyPrefix={ApiKeyPrefix}",
                _httpClient.BaseAddress,
                _options.EventTypeId,
                string.IsNullOrWhiteSpace(_options.ApiKey) ? "<missing>" : _options.ApiKey[..Math.Min(10, _options.ApiKey.Length)]);
        }

        // ──── Create Booking ────────────────────────────────────────────────

        public async Task<CalBookingResponse> CreateBookingAsync(
            CreateCalBookingRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.EventTypeId <= 0)
                request.EventTypeId = _options.EventTypeId;

            var url = "bookings";

            // cal.com rejects null-valued optional fields; remove them from the graph
            // before serialization so System.Text.Json omits them from JSON.
            var attendee = request.Attendee;
            if (attendee != null)
            {
                if (string.IsNullOrWhiteSpace(attendee.TimeZone))
                    attendee.TimeZone = null;
                if (string.IsNullOrWhiteSpace(attendee.Language))
                    attendee.Language = null;
                if (attendee.Guests is { Count: 0 })
                    attendee.Guests = null;
                if (attendee.Metadata is { Count: 0 })
                    attendee.Metadata = null;
            }
            if (request.Metadata is { Count: 0 })
                request.Metadata = null;

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var fullUrl = new Uri(_httpClient.BaseAddress!, url);
            _logger.LogInformation(
                "Creating cal.com booking for {AttendeeEmail} (eventTypeId: {EventTypeId}). FullUrl={FullUrl}",
                request.Attendee.Email, request.EventTypeId, fullUrl);

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return await DeserializeResponseAsync<CalBookingResponse>(response, cancellationToken);
        }

        // ──── Get Booking by UID ───────────────────────────────────────────

        public async Task<CalBookingResponse> GetBookingByUidAsync(
            string uid,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(uid))
                throw new ArgumentException("Booking UID is required", nameof(uid));

            var url = $"bookings/{Uri.EscapeDataString(uid)}";

            _logger.LogInformation("Fetching cal.com booking by UID: {Uid}", uid);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            return await DeserializeResponseAsync<CalBookingResponse>(response, cancellationToken);
        }

        // ──── Get Booking by ID ────────────────────────────────────────────

        public async Task<CalBookingResponse> GetBookingByIdAsync(
            int bookingId,
            CancellationToken cancellationToken = default)
        {
            var url = $"bookings/{bookingId}";

            _logger.LogInformation("Fetching cal.com booking by ID: {BookingId}", bookingId);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            return await DeserializeResponseAsync<CalBookingResponse>(response, cancellationToken);
        }

        // ──── Cancel Booking ───────────────────────────────────────────────

        public async Task<CalBookingResponse> CancelBookingAsync(
            int bookingId,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            var url = $"bookings/{bookingId}/cancel";

            var body = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(reason))
                body["reason"] = reason;

            var json = JsonSerializer.Serialize(body, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Cancelling cal.com booking {BookingId}. Reason: {Reason}",
                bookingId, reason ?? "(no reason provided)");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return await DeserializeResponseAsync<CalBookingResponse>(response, cancellationToken);
        }

        // ──── Get Available Slots ──────────────────────────────────────────

        public async Task<CalAvailabilityResponse> GetAvailableSlotsAsync(
            CalAvailabilityRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.EventTypeId <= 0)
                request.EventTypeId = _options.EventTypeId;

            var queryParams = new List<string>
            {
                $"eventTypeId={request.EventTypeId}",
                $"startDate={Uri.EscapeDataString(request.StartDate)}",
                $"endDate={Uri.EscapeDataString(request.EndDate)}"
            };

            if (!string.IsNullOrWhiteSpace(request.TimeZone))
                queryParams.Add($"timeZone={Uri.EscapeDataString(request.TimeZone)}");

            var queryString = string.Join("&", queryParams);
            var url = $"slots/available?{queryString}";

            _logger.LogInformation("Fetching available slots for eventTypeId {EventTypeId} from {StartDate} to {EndDate}",
                request.EventTypeId, request.StartDate, request.EndDate);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            return await DeserializeResponseAsync<CalAvailabilityResponse>(response, cancellationToken);
        }

        // ──── Webhook Handler ──────────────────────────────────────────────

        public async Task HandleWebhookAsync(
            string triggerEvent,
            string payload,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(triggerEvent))
                throw new ArgumentException("Trigger event is required", nameof(triggerEvent));

            if (string.IsNullOrWhiteSpace(payload))
                throw new ArgumentException("Webhook payload is required", nameof(payload));

            _logger.LogInformation("Received cal.com webhook: {TriggerEvent}", triggerEvent);

            try
            {
                switch (triggerEvent.ToUpperInvariant())
                {
                    case "BOOKING_CREATED":
                        await HandleBookingCreatedAsync(payload, cancellationToken);
                        break;

                    case "BOOKING_CANCELLED":
                        await HandleBookingCancelledAsync(payload, cancellationToken);
                        break;

                    case "BOOKING_RESCHEDULED":
                        await HandleBookingRescheduledAsync(payload, cancellationToken);
                        break;

                    default:
                        _logger.LogWarning("Unknown cal.com webhook trigger event: {TriggerEvent}", triggerEvent);
                        break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize cal.com webhook payload for event {TriggerEvent}", triggerEvent);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing cal.com webhook for event {TriggerEvent}", triggerEvent);
                throw;
            }
        }

        // ──── Private webhook handlers ──────────────────────────────────────

        private Task HandleBookingCreatedAsync(string payload, CancellationToken cancellationToken)
        {
            var webhook = JsonSerializer.Deserialize<CalWebhookBookingCreated>(payload, _jsonOptions);
            if (webhook?.Payload == null)
            {
                _logger.LogWarning("Booking created webhook payload is null or invalid");
                return Task.CompletedTask;
            }

            var p = webhook.Payload;
            _logger.LogInformation(
                "Booking created: ID={BookingId}, Title=\"{Title}\", Start={Start}, Attendees={AttendeeCount}",
                p.BookingId,
                p.Title,
                p.StartTime,
                p.Attendees?.Count ?? 0);

            // FUTURE: Here we can persist the booking to our local database,
            // send notifications, update CRM, etc.
            //
            // Example:
            //   var meetingDto = new MeetingDto { ... };
            //   await _meetingService.AddMeetingAsync(meetingDto, "Created from cal.com webhook");

            return Task.CompletedTask;
        }

        private Task HandleBookingCancelledAsync(string payload, CancellationToken cancellationToken)
        {
            var webhook = JsonSerializer.Deserialize<CalWebhookBookingCancelled>(payload, _jsonOptions);
            if (webhook?.Payload == null)
            {
                _logger.LogWarning("Booking cancelled webhook payload is null or invalid");
                return Task.CompletedTask;
            }

            var p = webhook.Payload;
            _logger.LogInformation(
                "Booking cancelled: ID={BookingId}, Title=\"{Title}\", Reason=\"{Reason}\", CancelledBy={CancelledBy}",
                p.BookingId,
                p.Title,
                p.CancellationReason ?? "(no reason)",
                p.CancelledByEmail ?? "(unknown)");

            // FUTURE: Update local booking status, notify relevant parties, etc.

            return Task.CompletedTask;
        }

        private Task HandleBookingRescheduledAsync(string payload, CancellationToken cancellationToken)
        {
            var webhook = JsonSerializer.Deserialize<CalWebhookBookingCreated>(payload, _jsonOptions);
            if (webhook?.Payload == null)
            {
                _logger.LogWarning("Booking rescheduled webhook payload is null or invalid");
                return Task.CompletedTask;
            }

            var p = webhook.Payload;
            _logger.LogInformation(
                "Booking rescheduled: ID={BookingId}, Title=\"{Title}\", NewStart={Start}, NewEnd={End}",
                p.BookingId,
                p.Title,
                p.StartTime,
                p.EndTime);

            // FUTURE: Update local booking, notify relevant parties, etc.

            return Task.CompletedTask;
        }

        // ──── Helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Deserializes the HTTP response into the target type.
        /// Throws if the response is not successful, including any cal.com error details.
        /// </summary>
        private async Task<T> DeserializeResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) where T : class
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("cal.com API returned {StatusCode}: {Body}", (int)response.StatusCode, body);

                // Try to extract a CalError from the response
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<CalBookingResponse>(body, _jsonOptions);
                    var errorMsg = errorResponse?.Error?.Message ?? $"HTTP {(int)response.StatusCode}";
                    throw new InvalidOperationException($"cal.com API error: {errorMsg}. Code: {errorResponse?.Error?.Code}");
                }
                catch (JsonException)
                {
                    throw new InvalidOperationException($"cal.com API returned {(int)response.StatusCode}: {body}");
                }
            }

            try
            {
                var result = JsonSerializer.Deserialize<T>(body, _jsonOptions);
                return result ?? throw new InvalidOperationException("cal.com API returned null response");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize cal.com response: {Body}", body);
                throw;
            }
        }
    }
}