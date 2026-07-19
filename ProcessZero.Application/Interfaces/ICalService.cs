using ProcessZero.Application.Dtos;

namespace ProcessZero.Application.Interfaces
{
    /// <summary>
    /// Service interface for Cal.com API v2 integration.
    /// Handles booking lifecycle, availability queries, and webhook ingestion.
    /// </summary>
    public interface ICalService
    {
        /// <summary>Creates a new booking in Cal.com.</summary>
        Task<CalBookingResponse> CreateBookingAsync(CreateCalBookingRequest request, CancellationToken cancellationToken = default);

        /// <summary>Retrieves booking details using the unique UID string.</summary>
        Task<CalBookingResponse> GetBookingByUidAsync(string uid, CancellationToken cancellationToken = default);

        /// <summary>Retrieves booking details using the numeric ID.</summary>
        Task<CalBookingResponse> GetBookingByIdAsync(int bookingId, CancellationToken cancellationToken = default);

        /// <summary>Cancels an existing booking with an optional reason.</summary>
        Task<CalBookingResponse> CancelBookingAsync(int bookingId, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>Queries available slots for a specific date range and timezone.</summary>
        Task<CalAvailabilityResponse> GetAvailableSlotsAsync(CalAvailabilityRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries all available slots for an event type over a 90-day window.
        /// <paramref name="timeZone"/>: Optional IANA timezone (e.g., "Africa/Johannesburg").
        /// </summary>
        Task<CalAvailabilityResponse> GetAllAvailableSlotsAsync(int eventTypeId, string? timeZone = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a flattened, sorted list of available start times for an event type.
        /// <paramref name="timeZone"/>: Optional IANA timezone (e.g., "Africa/Johannesburg").
        /// </summary>
        Task<List<DateTimeOffset>> GetAllAvailableDateTimesAsync(int eventTypeId, string? timeZone = null, CancellationToken cancellationToken = default);

        /// <summary>Processes incoming webhook payloads from Cal.com.</summary>
        Task HandleWebhookAsync(string triggerEvent, string payload, CancellationToken cancellationToken = default);
    }
}