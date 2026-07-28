using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProcessZero.TimerService.Dtos;
using System.Net.Http.Json;

namespace ProcessZero.TimerService.Jobs;

/// <summary>
/// Processes active usage sessions and consumes credits via HTTP calls to the main API.
/// </summary>
public class ConsumptionBackgroundJob
{
    private readonly ILogger _logger;
    private readonly string _timerApiKey;

    public ConsumptionBackgroundJob(ILogger logger, IConfiguration configuration)
    {
        _logger = logger;
        _timerApiKey = configuration["TimerApiKey"]
            ?? throw new InvalidOperationException("TimerApiKey is required.");
    }

    /// <summary>
    /// Processes all active sessions, consuming credits for elapsed time since last processing.
    /// Called periodically by the background timer (every minute).
    /// Uses LastProcessedUtc to calculate incremental consumption so credits are not overcharged.
    /// </summary>
    public async Task ProcessActiveSessionsAsync(string mainApiUrl)
    {
        try
        {
            _logger.LogInformation("Starting active session consumption processing at {Time}", DateTime.UtcNow);

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("X-Timer-Api-Key", _timerApiKey);
            var sessions = SessionManager.GetActiveSessions();
            var processed = 0;

            foreach (var session in sessions)
            {
                try
                {
                    var now = DateTime.UtcNow;

                    // Calculate minutes elapsed since last processing (incremental)
                    var lastProcessed = session.LastProcessedUtc ?? session.SessionStartUtc;
                    var incrementalMinutes = (now - lastProcessed).TotalMinutes;
                    if (incrementalMinutes <= 0) continue;

                    var creditsToConsume = decimal.Round((decimal)incrementalMinutes * TimerConfig.CreditsPerHour / 60.0m, 6);
                    if (creditsToConsume <= 0) continue;

                    if (creditsToConsume > 0)
                    {
                        var request = new HttpRequestMessage(HttpMethod.Post, $"{mainApiUrl}/api/credit/consume")
                        {
                            Content = JsonContent.Create(new
                            {
                                UserId = session.UserId,
                                CreditAmount = creditsToConsume,
                                Description = "Auto consumption from active session",
                                RelatedEntityType = "Session",
                                RelatedEntityId = session.Id
                            })
                        };
                        request.Headers.Add("X-Timer-Api-Key", _timerApiKey);
                        request.Headers.Add("X-User-Id", session.UserId);

                        var response = await http.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            // Update last processed timestamp only on successful consumption
                            session.LastProcessedUtc = now;
                            processed++;
                        }
                        else
                        {
                            _logger.LogWarning("Failed to consume credits for user {UserId}: {StatusCode}",
                                session.UserId, response.StatusCode);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing session for user {UserId}", session.UserId);
                }
            }

            _logger.LogInformation("Completed active session consumption processing at {Time}. Sessions processed: {Count}",
                DateTime.UtcNow, processed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing active sessions at {Time}", DateTime.UtcNow);
            throw;
        }
    }
}
