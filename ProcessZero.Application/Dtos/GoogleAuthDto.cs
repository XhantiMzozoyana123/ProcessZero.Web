using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProcessZero.Application.Dtos
{
    public class GoogleAuthDto
    {
        public class GoogleUserInfo
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = default!;

            [JsonPropertyName("email")]
            public string Email { get; set; } = default!;

            [JsonPropertyName("verified_email")]
            public bool VerifiedEmail { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; } = default!;

            [JsonPropertyName("given_name")]
            public string GivenName { get; set; } = default!;

            [JsonPropertyName("family_name")]
            public string FamilyName { get; set; } = default!;

            [JsonPropertyName("picture")]
            public string Picture { get; set; } = default!;

            [JsonPropertyName("locale")]
            public string Locale { get; set; } = default!;
        }

        public class GoogleOAuthResult
        {
            public string AccessToken { get; set; } = default!;
            public string? RefreshToken { get; set; }
            public DateTime ExpiryUtc { get; set; }
            public string Scope { get; set; } = default!;
            public string IdToken { get; set; } = default!;
        }

        public class GoogleTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = default!;

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonPropertyName("refresh_token")]
            public string? RefreshToken { get; set; }

            [JsonPropertyName("scope")]
            public string Scope { get; set; } = default!;

            [JsonPropertyName("token_type")]
            public string TokenType { get; set; } = default!;

            [JsonPropertyName("id_token")]
            public string IdToken { get; set; } = default!;
        }

        public class GoogleIdTokenPayload
        {
            public string Email { get; set; } = default!;
            public string Name { get; set; } = default!;
            public string Subject { get; set; } = default!; // Google user ID
            public string Issuer { get; set; } = default!;
            public string Audience { get; set; } = default!;
            public DateTime ExpirationTime { get; set; }
            public bool EmailVerified { get; set; }
        }
    }
}
