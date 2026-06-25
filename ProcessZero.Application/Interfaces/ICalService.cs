using ProcessZero.Application.Dtos;

namespace ProcessZero.Application.Interfaces
{
    /// <summary>
    /// Service for integrating with cal.com scheduling API.
    /// Provides booking management, availability checks, and webhook processing.
    ///
    /// Supports both targeted date-range slot queries and large-range availability retrieval
    /// that returns every available booking slot for an event type over a broad window.
    /// </summary>
    public interface ICalService
    {
        /// <summary>
        /// Creates a new booking in cal.com.
        /// </summary>
        /// <param name="request">The booking details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cal.com booking response with meeting details.</returns>
        Task<CalBookingResponse> CreateBookingAsync(CreateCalBookingRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a booking by its cal.com UID.
        /// </summary>
        Task<CalBookingResponse> GetBookingByUidAsync(string uid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a booking by its cal.com numeric ID.
        /// </summary>
        Task<CalBookingResponse> GetBookingByIdAsync(int bookingId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels an existing booking in cal.com.
        /// </summary>
        /// <param name="bookingId">The cal.com booking ID.</param>
        /// <param name="reason">Optional cancellation reason.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<CalBookingResponse> CancelBookingAsync(int bookingId, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries available time slots for a given event type and date range.
        /// </summary>
        Task<CalAvailabilityResponse> GetAvailableSlotsAsync(CalAvailabilityRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns every available booking slot for an event type in a large date range (default next 90 days).
        /// Queries Cal.com with a larger window so all slots are captured in a single call.
        /// </summary>
        /// <param name="eventTypeId">The Cal.com event type ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<CalAvailabilityResponse> GetAllAvailableSlotsAsync(int eventTypeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Flattens all available slots for an event type into a sorted list of date/times.
        /// </summary>
        /// <param name="eventTypeId">The Cal.com event type ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<List<DateTimeOffset>> GetAllAvailableDateTimesAsync(int eventTypeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a webhook payload from cal.com.
        /// Dispatches to the appropriate handler based on trigger event type.
        /// </summary>
        /// <param name="triggerEvent">The webhook trigger event (e.g. "BOOKING_CREATED", "BOOKING_CANCELLED").</param>
        /// <param name="payload">The raw JSON body of the webhook.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task HandleWebhookAsync(string triggerEvent, string payload, CancellationToken cancellationToken = default);
    }
}