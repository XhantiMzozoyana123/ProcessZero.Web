using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    public class GmailService : IGmailService
    {
        private readonly HttpClient _httpClient;
        private readonly IGoogleOAuthService _googleOAuth;

        public GmailService(
            HttpClient httpClient,
            IGoogleOAuthService googleOAuth)
        {
            _httpClient = httpClient;
            _googleOAuth = googleOAuth;
        }

        public async Task<List<ReceivedEmailMessageDto>> ReceiveAsync(
            RelayEmailAccount account,
            int maxResults = 10,
            CancellationToken cancellationToken = default)
        {
            // 1. Ensure token is valid
            await _googleOAuth.EnsureValidAccessTokenAsync(account, cancellationToken);

            // 2. Fetch message list from inbox
            var listRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://www.googleapis.com/gmail/v1/users/me/messages?q=in:inbox&maxResults={maxResults}");

            listRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", account.AccessToken);

            var listResponse = await _httpClient.SendAsync(listRequest, cancellationToken);

            if (!listResponse.IsSuccessStatusCode)
            {
                var error = await listResponse.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"Gmail list messages failed: {error}");
            }

            var listContent = await listResponse.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(listContent);
            var root = doc.RootElement;

            var messages = new List<ReceivedEmailMessageDto>();

            if (root.TryGetProperty("messages", out var messagesArray))
            {
                foreach (var msgRef in messagesArray.EnumerateArray())
                {
                    if (msgRef.TryGetProperty("id", out var idElement))
                    {
                        var messageId = idElement.GetString();
                        var message = await GetMessageAsync(account, messageId, cancellationToken);
                        if (message != null)
                        {
                            messages.Add(message);
                        }
                    }
                }
            }

            return messages;
        }

        public async Task<ReceivedEmailMessageDto?> GetMessageAsync(
            RelayEmailAccount account,
            string messageId,
            CancellationToken cancellationToken = default)
        {
            // 1. Ensure token is valid
            await _googleOAuth.EnsureValidAccessTokenAsync(account, cancellationToken);

            // 2. Fetch full message from Gmail API
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://www.googleapis.com/gmail/v1/users/me/messages/{messageId}?format=full");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", account.AccessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"Gmail get message failed: {error}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // 3. Parse the message
            var message = new ReceivedEmailMessageDto();

            if (root.TryGetProperty("id", out var idElement))
                message.MessageId = idElement.GetString() ?? string.Empty;

            if (root.TryGetProperty("internalDate", out var internalDateElement))
            {
                if (long.TryParse(internalDateElement.GetString(), out var timestamp))
                {
                    message.ReceivedDate = UnixTimeStampToDateTime(timestamp);
                }
            }

            // Parse headers
            if (root.TryGetProperty("payload", out var payloadElement))
            {
                if (payloadElement.TryGetProperty("headers", out var headersArray))
                {
                    foreach (var header in headersArray.EnumerateArray())
                    {
                        if (header.TryGetProperty("name", out var nameElement) && 
                            header.TryGetProperty("value", out var valueElement))
                        {
                            var headerName = nameElement.GetString() ?? string.Empty;
                            var headerValue = valueElement.GetString() ?? string.Empty;

                            switch (headerName.ToLower())
                            {
                                case "from":
                                    message.From = ExtractEmailAddress(headerValue);
                                    break;
                                case "to":
                                    message.To = ExtractEmailAddress(headerValue);
                                    break;
                                case "subject":
                                    message.Subject = headerValue;
                                    break;
                            }
                        }
                    }
                }

                // Parse body (simple text extraction)
                if (payloadElement.TryGetProperty("body", out var bodyElement))
                {
                    if (bodyElement.TryGetProperty("data", out var dataElement))
                    {
                        var encodedData = dataElement.GetString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(encodedData))
                        {
                            message.Body = DecodeBase64Url(encodedData);
                        }
                    }
                }
            }

            // Check if message is read
            if (root.TryGetProperty("labelIds", out var labelsArray))
            {
                var labels = labelsArray.EnumerateArray()
                    .Select(l => l.GetString() ?? string.Empty)
                    .Where(l => !string.IsNullOrEmpty(l))
                    .ToList();

                message.Labels = labels;
                message.IsRead = labels.Contains("UNREAD") == false;
            }

            return message;
        }

        public async Task SendAsync(
            RelayEmailAccount account,
            string to,
            string subject,
            string body,
            CancellationToken cancellationToken = default)
        {
            // 1. Ensure token is valid (auto refresh if needed)
            await _googleOAuth.EnsureValidAccessTokenAsync(account, cancellationToken);

            // 2. Build raw email (MIME format)
            var rawMessage = BuildRawMessage(account.EmailAddress, to, subject, body);

            var requestBody = new
            {
                raw = rawMessage
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://gmail.googleapis.com/gmail/v1/users/me/messages/send");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", account.AccessToken);

            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gmail send failed: {error}");
            }

            // 3. Update sending stats
            account.SentToday++;
            account.LastUsedAt = DateTime.UtcNow;
        }

        // ---------------------------
        // Helper: Build MIME message
        // ---------------------------
        private string BuildRawMessage(string from, string to, string subject, string body)
        {
            var message = new StringBuilder();

            message.AppendLine($"From: {from}");
            message.AppendLine($"To: {to}");
            message.AppendLine($"Subject: {subject}");
            message.AppendLine("MIME-Version: 1.0");
            // Email bodies are authored as HTML (RelayEmailVariant.HtmlBody), so they
            // must be sent as text/html — otherwise tags/links render as raw text.
            message.AppendLine("Content-Type: text/html; charset=utf-8");
            message.AppendLine();
            message.AppendLine(body);


            var bytes = Encoding.UTF8.GetBytes(message.ToString());

            // Gmail requires base64 URL-safe encoding
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        // ---------------------------
        // Helper: Decode Base64 URL-safe
        // ---------------------------
        private string DecodeBase64Url(string input)
        {
            // Reverse URL-safe encoding
            var base64 = input
                .Replace("-", "+")
                .Replace("_", "/");

            // Add padding if needed
            var padding = base64.Length % 4;
            if (padding > 0)
            {
                base64 += new string('=', 4 - padding);
            }

            try
            {
                var bytes = Convert.FromBase64String(base64);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        // ---------------------------
        // Helper: Extract email from "Name <email@example.com>"
        // ---------------------------
        private string ExtractEmailAddress(string emailString)
        {
            if (string.IsNullOrEmpty(emailString)) return string.Empty;

            var start = emailString.LastIndexOf('<');
            var end = emailString.LastIndexOf('>');

            if (start >= 0 && end > start)
            {
                return emailString.Substring(start + 1, end - start - 1);
            }

            return emailString.Trim();
        }

        // ---------------------------
        // Helper: Convert Unix timestamp to DateTime
        // ---------------------------
        private DateTime UnixTimeStampToDateTime(long timestamp)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddMilliseconds(timestamp);
        }
    }

}
