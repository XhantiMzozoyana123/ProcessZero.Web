using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System.Security.Claims;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Controller for Cal.com Scheduling Integration (API v2).
    /// 
    /// <para><b>UPDATES FOR USER-DEFINED TIMEZONES:</b></para>
    /// <list type="bullet">
    ///   <item><description><b>Booking Creation:</b> The <c>CreateBooking</c> method now respects the <c>Attendee.TimeZone</c> passed in the request body.</description></item>
    ///   <item><description><b>Slot Availability:</b> Both <c>GetAvailableSlots</c> and <c>GetAllAvailableDateTimes</c> accept a <c>timeZone</c> query parameter.</description></item>
    ///   <item><description><b>Defaulting:</b> All methods fall back to <b>Africa/Johannesburg</b> if no timezone is provided by the client.</description></item>
    /// </list>
    ///
    /// <para><b>ENTITY COLUMNS & PROPERTIES:</b></para>
    /// <list type="table">
    ///   <listheader><term>Object</term><description>Columns / Properties</description></listheader>
    ///   <item>
    ///     <term><see cref="CalAttendee"/></term>
    ///     <description>
    ///       - <c>Name</c> (string): Attendee's full name.<br/>
    ///       - <c>Email</c> (string): Attendee's email address.<br/>
    ///       - <c>TimeZone</c> (string): <b>User-defined IANA timezone</b> (e.g., "Europe/London"). Used for the calendar invite.<br/>
    ///       - <c>Language</c> (string): Default "en".
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="CreateCalBookingRequest"/></term>
    ///     <description>
    ///       - <c>EventTypeId</c> (int): Cal.com meeting type ID.<br/>
    ///       - <c>Start</c> (DateTimeOffset): The chosen slot time (UTC).<br/>
    ///       - <c>Attendee</c> (CalAttendee): The user details including their preferred timezone.
    ///     </description>
    ///   </item>
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
        /// POST: api/cal/bookings
        /// Creates a booking. 
        /// IMPORTANT: Pass "timeZone" inside the "attendee" object to set the user's local time for the invite.
        /// </summary>
        [HttpPost("bookings")]
        [Authorize]
        public async Task<IActionResult> CreateBooking([FromBody] CreateCalBookingRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request?.Attendee?.Email))
                return BadRequest("Attendee details and email are required.");

            try
            {
                // The service will use request.Attendee.TimeZone if provided, otherwise defaults to Africa/Johannesburg
                var result = await _calService.CreateBookingAsync(request, cancellationToken);

                if (result.Error != null) return StatusCode(502, result.Error);
                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking for {Email}", request.Attendee.Email);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/cal/bookings/{id}
        /// </summary>
        [HttpGet("bookings/{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetBookingById(int id, CancellationToken cancellationToken)
        {
            var result = await _calService.GetBookingByIdAsync(id, cancellationToken);
            if (result.Data == null) return NotFound();
            return Ok(result.Data);
        }

        /// <summary>
        /// POST: api/cal/bookings/{id}/cancel
        /// </summary>
        [HttpPost("bookings/{id:int}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelBooking(int id, [FromBody] CancelBookingRequest? body, CancellationToken cancellationToken)
        {
            var result = await _calService.CancelBookingAsync(id, body?.Reason, cancellationToken);
            return Ok(result.Data);
        }

        /// <summary>
        /// GET: api/cal/slots/available
        /// Queries range availability. TimeZone parameter adjusts the slot times returned.
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
            if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
                return BadRequest("startDate and endDate are required.");

            var request = new CalAvailabilityRequest
            {
                EventTypeId = eventTypeId ?? 0,
                StartTime = startDate.Contains('T') ? startDate : $"{startDate}T00:00:00Z",
                EndTime = endDate.Contains('T') ? endDate : $"{endDate}T23:59:59Z",
                TimeZone = timeZone ?? "Africa/Johannesburg"
            };

            var result = await _calService.GetAvailableSlotsAsync(request, cancellationToken);
            return Ok(result.Data);
        }

        /// <summary>
        /// GET: api/cal/slots/datetimes
        /// Returns flattened slots. Passing ?timeZone=... will shift the times to the user's region.
        /// </summary>
        [HttpGet("slots/datetimes")]
        [Authorize]
        public async Task<IActionResult> GetAllAvailableDateTimes(
            [FromQuery] int? eventTypeId,
            [FromQuery] string? timeZone,
            CancellationToken cancellationToken)
        {
            var times = await _calService.GetAllAvailableDateTimesAsync(eventTypeId ?? 0, timeZone, cancellationToken);
            return Ok(times);
        }

        /// <summary>
        /// POST: api/cal/webhook
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync(cancellationToken);
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                var triggerEvent = doc.RootElement.GetProperty("triggerEvent").GetString() ?? "UNKNOWN";

                await _calService.HandleWebhookAsync(triggerEvent, body, cancellationToken);
                return Ok(new { status = "received" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook failed");
                return StatusCode(500);
            }
        }
    }

    public class CancelBookingRequest { public string? Reason { get; set; } }
}