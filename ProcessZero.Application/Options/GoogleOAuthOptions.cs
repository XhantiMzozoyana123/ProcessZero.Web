namespace ProcessZero.Application.Options
{
    /// <summary>
    /// Configuration options for Google OAuth integration.
    /// Binds to "GoogleOAuth" section in appsettings.json
    /// </summary>
    public class GoogleOAuthOptions
    {
        /// <summary>
        /// Google OAuth 2.0 Client ID from Google Cloud Console
        /// Used to identify the application to Google
        /// </summary>
        public string ClientId { get; set; } = default!;

        /// <summary>
        /// Google OAuth 2.0 Client Secret from Google Cloud Console
        /// Must be kept confidential - never commit to source control
        /// </summary>
        public string ClientSecret { get; set; } = default!;

        /// <summary>
        /// Redirect URI registered in Google Cloud Console
        /// Where Google redirects user after OAuth consent
        /// (e.g., https://yourdomain.com/auth/google/callback)
        /// </summary>
        public string RedirectUri { get; set; } = default!;
    }
}
