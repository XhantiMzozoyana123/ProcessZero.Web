namespace ProcessZero.Application.Options
{
    /// <summary>
    /// Configuration options for cal.com API integration.
    /// Binds to "CalOptions" section in appsettings.json
    /// </summary>
    public class CalOptions
    {
        /// <summary>
        /// Base URL for the cal.com API (e.g. https://api.cal.com/v2)
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.cal.com/v2";

        /// <summary>
        /// cal.com API key (starts with cal_live_ or cal_test_)
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Default event-type ID to use when creating bookings.
        /// </summary>
        public int EventTypeId { get; set; }
    }
}