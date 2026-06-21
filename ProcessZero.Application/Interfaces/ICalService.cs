using ProcessZero.Application.Dtos;

namespace ProcessZero.Application.Interfaces
{
    /// <summary>
    /// Service for integrating with cal.com scheduling API.
    /// Provides booking management, availability checks, and webhook processing.
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
        /// Processes a webhook payload from cal.com.
        /// Dispatches to the appropriate handler based on trigger event type.
        /// </summary>
        /// <param name="triggerEvent">The webhook trigger event (e.g. "BOOKING_CREATED", "BOOKING_CANCELLED").</param>
        /// <param name="payload">The raw JSON body of the webhook.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task HandleWebhookAsync(string triggerEvent, string payload, CancellationToken cancellationToken = default);
    }
}