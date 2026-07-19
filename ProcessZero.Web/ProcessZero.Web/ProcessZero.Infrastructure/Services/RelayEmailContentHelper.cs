using System;
using System.Collections.Generic;
using System.Text;
using ProcessZero.Domain.Entities;

namespace ProcessZero.Infrastructure.Services
{
    /// <summary>
    /// Helper for cold-email content concerns shared by the sender service:
    ///   • Merge-tag personalization ({{firstName}}, {{company}}, ...)
    ///   • Appending a compliant unsubscribe footer
    ///   • Encoding/decoding the unsubscribe token used by the public endpoint
    ///
    /// The unsubscribe token is a URL-safe Base64 of "campaignId:leadId". It is not
    /// secret (it only lets someone opt a lead out), so a lightweight encoding is
    /// sufficient and avoids a schema/migration change.
    /// </summary>
    public static class RelayEmailContentHelper
    {
        // ─────────────────────────────────────────────
        // MERGE-TAG PERSONALIZATION
        // ─────────────────────────────────────────────
        public static string Personalize(string template, RelayLead lead)
        {
            if (string.IsNullOrEmpty(template) || lead == null)
                return template ?? string.Empty;

            // Support both {{firstName}} and {{first_name}} style tokens, case-insensitive.
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["firstName"] = lead.FirstName,
                ["first_name"] = lead.FirstName,
                ["lastName"] = lead.LastName,
                ["last_name"] = lead.LastName,
                ["fullName"] = $"{lead.FirstName} {lead.LastName}".Trim(),
                ["full_name"] = $"{lead.FirstName} {lead.LastName}".Trim(),
                ["email"] = lead.Email,
                ["company"] = lead.Company,
                ["jobTitle"] = lead.JobTitle,
                ["job_title"] = lead.JobTitle,
                ["location"] = lead.Location,
                ["phone"] = lead.Phone
            };

            var result = template;

            foreach (var kvp in tokens)
            {
                // Replace {{ token }} allowing optional surrounding whitespace.
                var value = string.IsNullOrWhiteSpace(kvp.Value) ? string.Empty : kvp.Value.Trim();
                result = ReplaceToken(result, kvp.Key, value);
            }

            return result;
        }

        private static string ReplaceToken(string input, string token, string value)
        {
            // Matches {{token}}, {{ token }} etc. without pulling in a regex dependency
            // for the common spacing variants.
            string[] variants =
            {
                "{{" + token + "}}",
                "{{ " + token + " }}",
                "{{" + token + " }}",
                "{{ " + token + "}}"
            };

            foreach (var v in variants)
            {
                input = input.Replace(v, value, StringComparison.OrdinalIgnoreCase);
            }

            return input;
        }

        // ─────────────────────────────────────────────
        // UNSUBSCRIBE FOOTER
        // ─────────────────────────────────────────────
        public static string AppendUnsubscribeFooter(string htmlBody, string baseUrl, int campaignId, int leadId)
        {
            var token = EncodeUnsubscribeToken(campaignId, leadId);
            var url = $"{baseUrl.TrimEnd('/')}/api/relay/unsubscribe?token={token}";

            var footer =
                "<br/><br/><hr style=\"border:none;border-top:1px solid #eee\"/>" +
                "<p style=\"font-size:12px;color:#888\">If you'd prefer not to receive these emails, " +
                $"you can <a href=\"{url}\">unsubscribe here</a>.</p>";

            return (htmlBody ?? string.Empty) + footer;
        }

        // ─────────────────────────────────────────────
        // UNSUBSCRIBE TOKEN
        // ─────────────────────────────────────────────
        public static string EncodeUnsubscribeToken(int campaignId, int leadId)
        {
            var raw = $"{campaignId}:{leadId}";
            var bytes = Encoding.UTF8.GetBytes(raw);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public static bool TryDecodeUnsubscribeToken(string token, out int campaignId, out int leadId)
        {
            campaignId = 0;
            leadId = 0;

            if (string.IsNullOrWhiteSpace(token))
                return false;

            try
            {
                var base64 = token.Replace("-", "+").Replace("_", "/");
                var padding = base64.Length % 4;
                if (padding > 0)
                    base64 += new string('=', 4 - padding);

                var bytes = Convert.FromBase64String(base64);
                var raw = Encoding.UTF8.GetString(bytes);

                var parts = raw.Split(':');
                if (parts.Length != 2)
                    return false;

                return int.TryParse(parts[0], out campaignId)
                       && int.TryParse(parts[1], out leadId);
            }
            catch
            {
                return false;
            }
        }
    }
}
