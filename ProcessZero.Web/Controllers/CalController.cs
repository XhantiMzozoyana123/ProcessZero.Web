using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System.Security.Claims;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Controller for cal.com scheduling integration.
    /// Provides endpoints for booking management, availability queries,
    /// and webhook handling from cal.com.
    ///
    /// Endpoints:
    ///   POST   /api/cal/bookings             — Create a new booking
    ///   GET    /api/cal/bookings/{id}         — Get booking by numeric ID
    ///   GET    /api/cal/bookings/uid/{uid}    — Get booking by UID
    ///   POST   /api/cal/bookings/{id}/cancel  — Cancel a booking
    ///   GET    /api/cal/slots/available       — Get available time slots
    ///   POST   /api/cal/webhook               — Receive webhook events from cal.com
    ///
    /// <para><b>Entities used:</b></para>
    /// <list type="bullet">
    ///   <item><description><see cref="CreateCalBookingRequest"/> — Request DTO for creating a booking (cal.com v2 API).
    ///     Columns: <c>EventTypeId</c> (int, cal.com event type ID),
    ///     <c>Attendee</c> (<see cref="CalAttendee"/>),
    ///     <c>Start</c> (DateTimeOffset, ISO 8601 UTC start time),
    ///     <c>Metadata</c> (Dictionary<string,string>?, optional custom data).</description></item>
    ///   <item><description><see cref="CalAttendee"/> — Attendee details.
    ///     Columns: <c>Name</c> (string), <c>Email</c> (string),
    ///     <c>TimeZone</c> (string, IANA timezone, default "UTC"),
    ///     <c>Language</c> (string, ISO language code, default "en").</description></item>
    ///   <item><description><see cref="CalBookingResponse"/> — Root wrapper from cal.com API.
    ///     Columns: <c>Status</c> (string), <c>Data</c> (<see cref="CalBookingData"/>?),
    ///     <c>Error</c> (<see cref="CalError"/>?).</description></item>
    ///   <item><description><see cref="CalBookingData"/> — Booking payload.
    ///     Columns: <c>Id</c> (int), <c>Uid</c> (string), <c>Title</c> (string),
    ///     <c>Description</c> (string?), <c>StartTime</c> / <c>EndTime</c> (DateTimeOffset),
    ///     <c>Status</c> (string), <c>CancellationReason</c> (string?),
    ///     <c>CancelledByEmail</c> (string?), <c>Attendees</c> (List<CalAttendeeData>?),
    ///     <c>Location</c> (string?), <c>MeetingUrl</c> (string?),
    ///     <c>Metadata</c> (Dictionary<string,string>?),
    ///     <c>EventTypeId</c> (int), <c>EventType</c> (<see cref="CalEventTypeData"/>?).</description></item>
    ///   <item><description><see cref="CalAttendeeData"/> — Attendee info in responses.
    ///     Columns: <c>Name</c>, <c>Email</c>, <c>TimeZone</c> (string?),
    ///     <c>Language</c> (string?).</description></item>
    ///   <item><description><see cref="CalEventTypeData"/> — cal.com event type metadata.
    ///     Columns: <c>Id</c> (int), <c>Slug</c> (string), <c>Title</c> (string),
    ///     <c>LengthMinutes</c> (int).</description></item>
    ///   <item><description><see cref="CalError"/> — Error payload.
    ///     Columns: <c>Code</c> (string), <c>Message</c> (string).</description></item>
    ///   <item><description><see cref="CalAvailabilityRequest"/> — Request DTO for slot queries.
    ///     Columns: <c>EventTypeId</c> (int), <c>StartTime</c> (string, ISO 8601 e.g. "2026-06-25T00:00:00Z"),
    ///     <c>EndTime</c> (string, ISO 8601 e.g. "2026-06-26T00:00:00Z"), <c>TimeZone</c> (string?, IANA timezone).</description></item>
    ///   <item><description><see cref="CalAvailabilityResponse"/> — Root wrapper for availability.
    ///     Columns: <c>Status</c> (string), <c>Data</c> (<see cref="CalAvailabilityData"/>?),
    ///     <c>Error</c> (<see cref="CalError"/>?).</description></item>
    ///   <item><description><see cref="CalAvailabilityData"/> — Availability payload.
    ///     Columns: <c>Slots</c> (Dictionary<string, List<CalSlot>>?, keyed by date),
    ///     <c>MinimumBookingNotice</c> (int, minutes),
    ///     <c>LengthMinutes</c> (int, event duration).</description></item>
    ///   <item><description><see cref="CalSlot"/> — A single available time slot.
    ///     Columns: <c>Time</c> (DateTimeOffset), <c>Attendees</c> (int?, current bookings).</description></item>
    ///   <item><description><see cref="CancelBookingRequest"/> — Cancel reason.
    ///     Column: <c>Reason</c> (string?).</description></item>
    ///   <item><description><see cref="Product"/> (domain entity) — <c>CalEventTypeId</c> (int?, links a product to a cal.com event type).</description></item>
    /// </list>
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CalController : ControllerBase
    {
        private readonly ICalService _calService;
        private readonly ILogger<CalController> _logger;

        public CalController(ICalService calService, ILogger<CalController> logger)
        {
            _calService = calService ?? throw new ArgumentNullException(nameof(calService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Helper to extract the authenticated user's id from JWT claims.
        /// </summary>
        private string GetUserId()
            => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        /// <summary>
        /// POST: api/cal/bookings
        /// Creates a new booking in cal.com.
        /// Requires authentication.
        /// </summary>
        [HttpPost("bookings")]
        [Authorize]
        public async Task<IActionResult> CreateBooking([FromBody] CreateCalBookingRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                return BadRequest("Request body is required");

            if (string.IsNullOrWhiteSpace(request.Attendee?.Email))
                return BadRequest("Attendee email is required");

            if (string.IsNullOrWhiteSpace(request.Attendee?.Name))
                return BadRequest("Attendee name is required");

            if (request.Start == default)
                return BadRequest("Start time is required");

            try
            {
                var result = await _calService.CreateBookingAsync(request, cancellationToken);

                if (result.Error != null)
                {
                    _logger.LogWarning("cal.com booking creation failed: {Code} - {Message}",
                        result.Error.Code, result.Error.Message);

                    return StatusCode(StatusCodes.Status502BadGateway, new
                    {
                        error = result.Error.Message,
                        code = result.Error.Code
                    });
                }

                _logger.LogInformation("cal.com booking created successfully: ID={BookingId}, UID={Uid}",
                    result.Data?.Id, result.Data?.Uid);

                return Ok(result.Data);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "cal.com API error during booking creation");
                return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating cal.com booking");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred" });
            }
        }

        /// <summary>
        /// GET: api/cal/bookings/{id}
        /// Retrieves a booking by its cal.com numeric ID.
        /// Requires authentication.
        /// </summary>
        [HttpGet("bookings/{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetBookingById(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
                return BadRequest("Valid booking ID is required");

            try
            {
                var result = await _calService.GetBookingByIdAsync(id, cancellationToken);

                if (result.Error != null)
                    return StatusCode(StatusCodes.Status502BadGateway, new { error = result.Error.Message });

                if (result.Data == null)
                    return NotFound(new { error = "Booking not found" });

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cal.com booking {BookingId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred" });
            }
        }

        /// <summary>
        /// GET: api/cal/bookings/uid/{uid}
        /// Retrieves a booking by its cal.com UID.
        /// Requires authentication.
        /// </summary>
        [HttpGet("bookings/uid/{uid}")]
        [Authorize]
        public async Task<IActionResult> GetBookingByUid(string uid, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(uid))
                return BadRequest("Booking UID is required");

            try
            {
                var result = await _calService.GetBookingByUidAsync(uid, cancellationToken);

                if (result.Error != null)
                    return StatusCode(StatusCodes.Status502BadGateway, new { error = result.Error.Message });

                if (result.Data == null)
                    return NotFound(new { error = "Booking not found" });

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cal.com booking by UID {Uid}", uid);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred" });
            }
        }

        /// <summary>
        /// POST: api/cal/bookings/{id}/cancel
        /// Cancels a cal.com booking.
        /// Requires authentication.
        /// </summary>
        [HttpPost("bookings/{id:int}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelBooking(int id, [FromBody] CancelBookingRequest? body = null, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
                return BadRequest("Valid booking ID is required");

            try
            {
                var result = await _calService.CancelBookingAsync(id, body?.Reason, cancellationToken);

                if (result.Error != null)
                    return StatusCode(StatusCodes.Status502BadGateway, new { error = result.Error.Message });

                _logger.LogInformation("cal.com booking {BookingId} cancelled successfully", id);
                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling cal.com booking {BookingId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred" });
            }
        }

        /// <summary>
        /// GET: api/cal/slots/available
        /// Queries available time slots from cal.com.
        /// Requires authentication.
        /// </summary>
        [HttpGet("slots/available")]
        [Authorize]
        public async Task<IActionResult> GetAvailableSlots(
            [FromQuery] int? eventTypeId,
            [FromQuery] string startDate,
            [FromQuery] string endDate,
            [FromQuery] string? timeZone,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(startDate))
                return BadRequest("startDate is required (YYYY-MM-DD)");

            if (string.IsNullOrWhiteSpace(endDate))
                return BadRequest("endDate is required (YYYY-MM-DD)");

            var request = new CalAvailabilityRequest
            {
                EventTypeId = eventTypeId ?? 0,
                StartTime = startDate.Contains('T') ? startDate : $"{startDate}T00:00:00Z",
                EndTime = endDate.Contains('T') ? endDate : $"{endDate}T00:00:00Z",
                TimeZone = timeZone
            };

            try
            {
                var result = await _calService.GetAvailableSlotsAsync(request, cancellationToken);

                if (result.Error != null)
                    return StatusCode(StatusCodes.Status502BadGateway, new { error = result.Error.Message });

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available slots from cal.com");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred" });
            }
        }

        /// <summary>
        /// POST: api/cal/webhook
        /// Receives webhook events from cal.com.
        /// This endpoint is publicly accessible (no auth) and uses a shared secret
        /// for verification, or relies on cal.com IP whitelisting.
        ///
        /// The request body must include a "triggerEvent" field (e.g. "BOOKING_CREATED")
        /// and a "payload" field with the booking details.
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
        {
            string body;
            using (var reader = new System.IO.StreamReader(Request.Body))
            {
                body = await reader.ReadToEndAsync(cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogWarning("Received empty cal.com webhook body");
                return BadRequest("Empty webhook body");
            }

            // Extract the trigger event from the payload
            string triggerEvent;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                triggerEvent = doc.RootElement.TryGetProperty("triggerEvent", out var te)
                    ? te.GetString() ?? string.Empty
                    : string.Empty;
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse cal.com webhook JSON");
                return BadRequest("Invalid JSON payload");
            }

            if (string.IsNullOrWhiteSpace(triggerEvent))
            {
                _logger.LogWarning("cal.com webhook missing triggerEvent field");
                return BadRequest("Missing triggerEvent field");
            }

            _logger.LogInformation("Processing cal.com webhook: {TriggerEvent}", triggerEvent);

            try
            {
                await _calService.HandleWebhookAsync(triggerEvent, body, cancellationToken);
                return Ok(new { received = true, triggerEvent });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing cal.com webhook for event {TriggerEvent}", triggerEvent);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to process webhook" });
            }
        }
    }

    /// <summary>
    /// Request body for cancelling a booking.
    /// </summary>
    public class CancelBookingRequest
    {
        public string? Reason { get; set; }
    }
}