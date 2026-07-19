using System.Text.Json;
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
        [JsonConverter(typeof(StrictUtcDateTimeOffsetConverter))]
        public DateTimeOffset Start { get; set; }
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

    /// <summary>
    /// Ensures DateTimeOffset values are always serialized in strict UTC ISO 8601.
    /// Added to both Requests and Responses for consistency.
    /// </summary>
    public class StrictUtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value)) return default;

            // Cal.com sometimes returns offset strings; we force them to UTC
            if (DateTimeOffset.TryParse(value, out var dto))
                return dto.ToUniversalTime();

            return default;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }
    }

    // ---------- Response DTOs ----------

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
        [JsonConverter(typeof(StrictUtcDateTimeOffsetConverter))]
        public DateTimeOffset StartTime { get; set; }

        [JsonPropertyName("endTime")]
        [JsonConverter(typeof(StrictUtcDateTimeOffsetConverter))]
        public DateTimeOffset EndTime { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("attendees")]
        public List<CalAttendeeData> Attendees { get; set; } = new();

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("meetingUrl")]
        public string? MeetingUrl { get; set; }

        [JsonPropertyName("eventTypeId")]
        public int EventTypeId { get; set; }
    }

    public class CalAttendeeData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("timeZone")]
        public string? TimeZone { get; set; }
    }

    public class CalError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    // ---------- Availability (CRITICAL FOR 0 SLOTS FIX) ----------

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
        /// <summary>
        /// Keyed by date "YYYY-MM-DD". Initialized to prevent null iteration.
        /// </summary>
        [JsonPropertyName("slots")]
        public Dictionary<string, List<CalSlot>> Slots { get; set; } = new();

        [JsonPropertyName("minimumBookingNotice")]
        public int MinimumBookingNotice { get; set; }

        /// <summary>
        /// Cal.com v2 uses "length" for the meeting duration.
        /// </summary>
        [JsonPropertyName("length")]
        public int LengthMinutes { get; set; }
    }

    public class CalSlot
    {
        /// <summary>
        /// Cal.com v2 returns "time" for the start of the slot.
        /// </summary>
        [JsonPropertyName("time")]
        [JsonConverter(typeof(StrictUtcDateTimeOffsetConverter))]
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

        [JsonPropertyName("startTime")]
        [JsonConverter(typeof(StrictUtcDateTimeOffsetConverter))]
        public DateTimeOffset StartTime { get; set; }

        [JsonPropertyName("endTime")]
        [JsonConverter(typeof(StrictUtcDateTimeOffsetConverter))]
        public DateTimeOffset EndTime { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("attendees")]
        public List<CalAttendeeData> Attendees { get; set; } = new();
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

        [JsonPropertyName("cancellationReason")]
        public string? CancellationReason { get; set; }

        [JsonPropertyName("cancelledByEmail")]
        public string? CancelledByEmail { get; set; }
    }
}