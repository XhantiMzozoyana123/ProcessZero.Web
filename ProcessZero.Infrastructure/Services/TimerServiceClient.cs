using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcessZero.Application.Dtos;

namespace ProcessZero.Infrastructure.Services;

/// <summary>
/// HTTP client that proxies timer/session management calls to the standalone
/// ProcessZero.TimerService. This ensures the countdown timer system runs
/// independently and doesn't reset during API deployments.
/// </summary>
public class TimerServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TimerServiceClient> _logger;

    public TimerServiceClient(HttpClient httpClient, IOptions<TimerServiceOptions> options, ILogger<TimerServiceClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var opts = options.Value;
        _httpClient.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/api/timer/");
        _httpClient.DefaultRequestHeaders.Add("X-Timer-Api-Key", opts.ApiKey);
    }

    // ── Session Management ──

    public async Task<TimerUserSessionDto?> StartSessionAsync(string userId, string? deviceInfo = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("sessions/start", new { userId, deviceInfo }, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TimerUserSessionDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start session for user {UserId}", userId);
            return null;
        }
    }

    public async Task<TimerSessionHeartbeatResponseDto?> HeartbeatAsync(int sessionId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"sessions/{sessionId}/heartbeat", new { userId }, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TimerSessionHeartbeatResponseDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send heartbeat for session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<TimerSessionHeartbeatResponseDto?> EndSessionAsync(int sessionId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"sessions/{sessionId}/end", new { userId }, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TimerSessionHeartbeatResponseDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<TimerActiveSessionResponse?> GetActiveSessionAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"sessions/active?userId={Uri.EscapeDataString(userId)}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TimerActiveSessionResponse>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active session for user {UserId}", userId);
            return null;
        }
    }

    // ── Remaining Hours ──

    public async Task<TimerRemainingHoursResponse?> GetRemainingHoursAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"remaining-hours?userId={Uri.EscapeDataString(userId)}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TimerRemainingHoursResponse>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get remaining hours for user {UserId}", userId);
            return null;
        }
    }

    // ── Wallet Operations ──

    public async Task<TimerConsumeCreditsResponse?> ConsumeCreditsAsync(string userId, decimal creditAmount, string description, string? relatedEntityType = null, int? relatedEntityId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("wallet/consume", new
            {
                userId,
                creditAmount,
                description,
                relatedEntityType,
                relatedEntityId
            }, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TimerConsumeCreditsResponse>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to consume credits for user {UserId}", userId);
            return null;
        }
    }

    public async Task<TimerCheckBalanceResponse?> CheckCreditBalanceAsync(string userId, decimal requiredCredits, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("wallet/check-balance", new
            {
                userId,
                requiredCredits
            }, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TimerCheckBalanceResponse>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check credit balance for user {UserId}", userId);
            return null;
        }
    }

    // ── Timer Configuration CRUD (Admin) ──

    public async Task<TimerConfigDto?> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("config", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TimerConfigDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get timer config");
            return null;
        }
    }

    public async Task<TimerConfigDto?> UpdateConfigAsync(TimerConfigDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("config", dto, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TimerConfigDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update timer config");
            return null;
        }
    }

    public async Task<TimerConfigDto?> DeleteConfigAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync("config", cancellationToken);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<DeleteConfigResponse>(cancellationToken: cancellationToken);
            return result?.Config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete timer config");
            return null;
        }
    }

    // ── Admin Session Management ──

    public async Task<List<TimerUserSessionDto>?> GetAllActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("sessions", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<TimerUserSessionDto>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all active sessions");
            return null;
        }
    }

    public async Task<bool> ForceEndSessionAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"sessions/{sessionId}/force-end", null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to force-end session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<TimerStatsDto?> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("stats", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TimerStatsDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get timer stats");
            return null;
        }
    }

}

// ── Options ──

public class TimerServiceOptions
{
    public const string SectionName = "TimerService";
    public string BaseUrl { get; set; } = "http://localhost:8082";
    public string ApiKey { get; set; } = string.Empty;
}

// ── DTOs matching the timer service responses ──
// These are prefixed with "Timer" to avoid naming conflicts with Application-layer DTOs.

public class TimerUserSessionDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime SessionStartUtc { get; set; }
    public DateTime? SessionEndUtc { get; set; }
    public double MinutesConsumed { get; set; }
    public decimal CreditsConsumed { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastHeartbeatUtc { get; set; }
    public string? DeviceInfo { get; set; }
    public double ElapsedMinutes { get; set; }
    public decimal EstimatedCreditsConsumed { get; set; }
    public string? TimeRemainingDisplay { get; set; }
}

public class TimerSessionHeartbeatResponseDto
{
    public bool Success { get; set; }
    public bool IsConsuming { get; set; }
    public bool IsBlocked { get; set; }
    public decimal CreditsConsumed { get; set; }
    public double MinutesElapsed { get; set; }
    public decimal? RemainingCreditBalance { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class TimerRemainingHoursResponse
{
    public decimal RemainingHours { get; set; }
}

public class TimerConsumeCreditsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal NewBalance { get; set; }
    public decimal CreditsConsumed { get; set; }
}

public class TimerCheckBalanceResponse
{
    public decimal CreditBalance { get; set; }
    public bool HasSufficientCredits { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class TimerActiveSessionResponse
{
    public TimerUserSessionDto? Session { get; set; }
}

public class TimerConfigDto
{
    public int Id { get; set; } = 1;
    public decimal CreditsPerHour { get; set; } = 0.2m;
    public int CheckIntervalMinutes { get; set; } = 1;
    public int MaxSessionMinutes { get; set; } = 480;
    public bool IsEnabled { get; set; } = true;
    public int GracePeriodMinutes { get; set; } = 0;
    public decimal InitialFreeHours { get; set; } = 5;
    public bool EnforceAccessBlock { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class TimerStatsDto
{
    public int ActiveSessionsCount { get; set; }
    public int TotalSessionsToday { get; set; }
    public int TotalSessionsThisMonth { get; set; }
    public decimal TotalCreditsConsumedToday { get; set; }
    public decimal TotalCreditsConsumedThisMonth { get; set; }
    public decimal TotalMinutesLoggedToday { get; set; }
    public decimal TotalMinutesLoggedThisMonth { get; set; }
    public decimal Rate { get; set; }
    public bool IsEnabled { get; set; }
}

public class DeleteConfigResponse
{
    public string Message { get; set; } = string.Empty;
    public TimerConfigDto? Config { get; set; }
}
