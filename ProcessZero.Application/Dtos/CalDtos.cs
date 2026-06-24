using System.Text.Json.Serialization;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// Request to create a cal.com booking.
    /// </summary>
    public class CreateCalBookingRequest
    {
        [JsonPropertyName("eventTypeId")]
        public int EventTypeId { get; set; }

        [JsonPropertyName("attendee")]
        public CalAttendee Attendee { get; set; } = new();

        [JsonPropertyName("start")]
        public DateTimeOffset Start { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class CalAttendee
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("timeZone")]
        public string TimeZone { get; set; } = "UTC";

        [JsonPropertyName("language")]
        public string Language { get; set; } = "en";
    }

    // ---------- Response DTOs ----------

    /// <summary>
    /// Root response from cal.com /bookings endpoint.
    /// </summary>
    public class CalBookingResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public CalBookingData? Data { get; set; }

        [JsonPropertyName("error")]
        public CalError? Error { get; set; }
    }

    public class CalBookingData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("startTime")]
        public DateTimeOffset StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTimeOffset EndTime { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("cancellationReason")]
        public string? CancellationReason { get; set; }

        [JsonPropertyName("cancelledByEmail")]
        public string? CancelledByEmail { get; set; }

        [JsonPropertyName("attendees")]
        public List<CalAttendeeData>? Attendees { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("meetingUrl")]
        public string? MeetingUrl { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        [JsonPropertyName("eventTypeId")]
        public int EventTypeId { get; set; }

        [JsonPropertyName("eventType")]
        public CalEventTypeData? EventType { get; set; }
    }

    public class CalAttendeeData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("timeZone")]
        public string? TimeZone { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }
    }

    public class CalEventTypeData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("length")]
        public int LengthMinutes { get; set; }
    }

    public class CalError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    // ---------- Availability ----------

    public class CalAvailabilityRequest
    {
        [JsonPropertyName("eventTypeId")]
        public int EventTypeId { get; set; }

        [JsonPropertyName("startTime")]
        public string StartTime { get; set; } = string.Empty;

        [JsonPropertyName("endTime")]
        public string EndTime { get; set; } = string.Empty;

        [JsonPropertyName("timeZone")]
        public string? TimeZone { get; set; }
    }

    public class CalAvailabilityResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public CalAvailabilityData? Data { get; set; }

        [JsonPropertyName("error")]
        public CalError? Error { get; set; }
    }

    public class CalAvailabilityData
    {
        [JsonPropertyName("slots")]
        public Dictionary<string, List<CalSlot>>? Slots { get; set; }

        [JsonPropertyName("minimumBookingNotice")]
        public int MinimumBookingNotice { get; set; }

        [JsonPropertyName("length")]
        public int LengthMinutes { get; set; }
    }

    public class CalSlot
    {
        [JsonPropertyName("time")]
        public DateTimeOffset Time { get; set; }

        [JsonPropertyName("attendees")]
        public int? Attendees { get; set; }
    }

    // ---------- Webhook payloads ----------

    public class CalWebhookBookingCreated
    {
        [JsonPropertyName("triggerEvent")]
        public string TriggerEvent { get; set; } = string.Empty;

        [JsonPropertyName("payload")]
        public CalWebhookBookingPayload? Payload { get; set; }
    }

    public class CalWebhookBookingPayload
    {
        [JsonPropertyName("bookingId")]
        public int BookingId { get; set; }

        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("startTime")]
        public DateTimeOffset StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTimeOffset EndTime { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("meetingUrl")]
        public string? MeetingUrl { get; set; }

        [JsonPropertyName("attendees")]
        public List<CalAttendeeData>? Attendees { get; set; }

        [JsonPropertyName("eventTypeId")]
        public int EventTypeId { get; set; }

        [JsonPropertyName("eventType")]
        public CalEventTypeData? EventType { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class CalWebhookBookingCancelled
    {
        [JsonPropertyName("triggerEvent")]
        public string TriggerEvent { get; set; } = string.Empty;

        [JsonPropertyName("payload")]
        public CalWebhookCancelledPayload? Payload { get; set; }
    }

    public class CalWebhookCancelledPayload
    {
        [JsonPropertyName("bookingId")]
        public int BookingId { get; set; }

        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("startTime")]
        public DateTimeOffset StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTimeOffset EndTime { get; set; }

        [JsonPropertyName("cancellationReason")]
        public string? CancellationReason { get; set; }

        [JsonPropertyName("cancelledByEmail")]
        public string? CancelledByEmail { get; set; }

        [JsonPropertyName("attendees")]
        public List<CalAttendeeData>? Attendees { get; set; }

        [JsonPropertyName("eventTypeId")]
        public int EventTypeId { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }
}