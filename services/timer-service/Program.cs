using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProcessZero.TimerService.Dtos;
using ProcessZero.TimerService.Jobs;
using System.Net.Http;
using Timer = System.Threading.Timer;

var builder = WebApplication.CreateBuilder(args);

var timerApiKey = builder.Configuration["TimerApiKey"]
    ?? throw new InvalidOperationException("TimerApiKey is required.");

var mainApiUrl = builder.Configuration["MainApi:BaseUrl"]
    ?? throw new InvalidOperationException("MainApi:BaseUrl is required.");

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddLogging();

// Configure base addresses for service communication
builder.Services.AddHttpClient("MainApi");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();
app.UseRouting();

app.MapGet("/health", () => Results.Ok(new
{
    service = "ProcessZero Timer Service",
    status = "running",
    time = DateTime.UtcNow
}));

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/timer"))
    {
        if (!context.Request.Headers.TryGetValue("X-Timer-Api-Key", out var apiKey) || apiKey != timerApiKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid or missing API key");
            return;
        }
    }
    await next();
});

var api = app.MapGroup("/api/timer");

// ── Session Management ──

// Start session
api.MapPost("/sessions/start", (StartSessionRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.UserId))
        return Results.BadRequest(new { error = "UserId is required" });

    var now = DateTime.UtcNow;
    SessionManager.StartSession(req.UserId, req.DeviceInfo, now);
    var session = SessionManager.GetSession(req.UserId);
    return Results.Ok(TimerSessionMapper.MapToTimerSessionDto(session));
});

// Heartbeat
api.MapPost("/sessions/{sessionId:int}/heartbeat", async (int sessionId, HttpRequest req, HttpClient http) =>
{
    var form = await req.ReadFromJsonAsync<HeartbeatRequest>();
    if (form == null || string.IsNullOrWhiteSpace(form.UserId))
        return Results.BadRequest(new { error = "UserId is required" });

    var session = SessionManager.GetSession(form.UserId);
    if (session == null || session.Id != sessionId)
        return Results.NotFound(new { error = "Session not found" });

    var previousHeartbeat = session.LastHeartbeatUtc;
    session.LastHeartbeatUtc = DateTime.UtcNow;

    var elapsedMinutes = (DateTime.UtcNow - session.SessionStartUtc).TotalMinutes;
    var creditsConsumed = decimal.Round((decimal)elapsedMinutes * TimerConfig.CreditsPerHour / 60.0m, 6);

    // Check actual credit balance from the main API
    var hasSufficientCredits = true;
    decimal? remainingBalance = null;
    try
    {
        using var balanceRequest = new HttpRequestMessage(HttpMethod.Post, $"{mainApiUrl}/api/credit/check");
        balanceRequest.Headers.Add("X-Timer-Api-Key", timerApiKey);
        balanceRequest.Headers.Add("X-User-Id", form.UserId);
        balanceRequest.Content = JsonContent.Create(0m); // Check balance with 0 required credits
        using var balanceResponse = await http.SendAsync(balanceRequest);
        if (balanceResponse.IsSuccessStatusCode)
        {
            var balanceResult = await balanceResponse.Content.ReadFromJsonAsync<CheckBalanceResponse>(cancellationToken: CancellationToken.None);
            if (balanceResult != null)
            {
                remainingBalance = balanceResult.CreditBalance;
                // User is blocked if they have no credits and consumption is enabled
                hasSufficientCredits = balanceResult.CreditBalance > 0 || !TimerConfig.IsEnabled;
            }
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to check credit balance for user {UserId} during heartbeat", form.UserId);
    }

    return Results.Ok(new TimerSessionHeartbeatResponseDto
    {
        Success = true,
        IsConsuming = session.IsActive,
        IsBlocked = !hasSufficientCredits,
        CreditsConsumed = creditsConsumed,
        MinutesElapsed = elapsedMinutes,
        RemainingCreditBalance = remainingBalance,
        Message = hasSufficientCredits
            ? "Heartbeat received"
            : "Insufficient credits. Please top up to continue."
    });
});

// End session
api.MapPost("/sessions/{sessionId:int}/end", async (int sessionId, HttpRequest req) =>
{
    var form = await req.ReadFromJsonAsync<HeartbeatRequest>();
    if (form == null || string.IsNullOrWhiteSpace(form.UserId))
        return Results.BadRequest(new { error = "UserId is required" });

    var session = SessionManager.GetSession(form.UserId);
    if (session == null || session.Id != sessionId)
        return Results.NotFound(new { error = "Session not found" });

    var elapsed = DateTime.UtcNow - session.SessionStartUtc;
    var creditsConsumed = decimal.Round((decimal)elapsed.TotalMinutes * TimerConfig.CreditsPerHour / 60.0m, 6);
    SessionManager.EndSession(form.UserId);

    return Results.Ok(new TimerSessionHeartbeatResponseDto
    {
        Success = true,
        IsConsuming = false,
        IsBlocked = false,
        CreditsConsumed = creditsConsumed,
        MinutesElapsed = elapsed.TotalMinutes,
        Message = "Session ended"
    });
});

// Active session
api.MapGet("/sessions/active", ([AsParameters] UserQuery q, HttpClient http) =>
{
    if (string.IsNullOrWhiteSpace(q.UserId))
        return Results.BadRequest(new { error = "UserId is required" });

    var session = SessionManager.GetSession(q.UserId);
    if (session == null)
        return Results.Ok(new TimerActiveSessionResponse { Session = null });

    return Results.Ok(new TimerActiveSessionResponse
    {
        Session = TimerSessionMapper.MapToTimerSessionDto(session)
    });
});

// ── Remaining Hours ──

// Remaining hours - calculates from session data and credit balance
api.MapGet("/remaining-hours", async ([AsParameters] UserQuery q, HttpClient http) =>
{
    if (string.IsNullOrWhiteSpace(q.UserId))
        return Results.BadRequest(new { error = "UserId is required" });

    try
    {
        // Get the user's credit balance from the main API
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{mainApiUrl}/api/credit/wallet");
        request.Headers.Add("X-Timer-Api-Key", timerApiKey);
        request.Headers.Add("X-User-Id", q.UserId);
        using var response = await http.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var wallet = await response.Content.ReadFromJsonAsync<CreditWalletResponse>(cancellationToken: CancellationToken.None);
            if (wallet != null)
            {
                var remainingHours = wallet.CreditBalance / TimerConfig.CreditsPerHour;
                return Results.Ok(new TimerRemainingHoursResponse
                {
                    RemainingHours = remainingHours
                });
            }
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to get wallet from main API for user {UserId}", q.UserId);
    }

    // Fallback: calculate from session data
    var session = SessionManager.GetSession(q.UserId);
    if (session != null)
    {
        var elapsedMinutes = (DateTime.UtcNow - session.SessionStartUtc).TotalMinutes;
        var creditsConsumed = (decimal)elapsedMinutes * TimerConfig.CreditsPerHour / 60.0m;
        var remainingHours = Math.Max(0, (session.InitialCreditBalance - creditsConsumed) / TimerConfig.CreditsPerHour);
        return Results.Ok(new TimerRemainingHoursResponse
        {
            RemainingHours = remainingHours
        });
    }

    return Results.Ok(new TimerRemainingHoursResponse { RemainingHours = 0 });
});

// ── Timer Configuration CRUD (Admin) ──

// GET: /api/timer/config - Read current configuration
api.MapGet("/config", () =>
{
    return Results.Ok(TimerConfig.Current);
});

// PUT: /api/timer/config - Update configuration
api.MapPut("/config", async (HttpRequest req) =>
{
    var dto = await req.ReadFromJsonAsync<TimerConfigDto>();
    if (dto == null)
        return Results.BadRequest(new { error = "Configuration data is required." });

    TimerConfig.Update(dto);
    return Results.Ok(TimerConfig.Current);
});

// DELETE: /api/timer/config - Reset configuration to defaults
api.MapDelete("/config", () =>
{
    TimerConfig.Reset();
    return Results.Ok(new { message = "Configuration reset to defaults", config = TimerConfig.Current });
});

// ── Admin Session Management ──

// GET: /api/timer/sessions - Get all active sessions
api.MapGet("/sessions", () =>
{
    var sessions = SessionManager.GetActiveSessions()
        .Select(TimerSessionMapper.MapToTimerSessionDto)
        .ToList();
    return Results.Ok(sessions);
});

// POST: /api/timer/sessions/{id}/force-end - Force end a session
api.MapPost("/sessions/{sessionId:int}/force-end", (int sessionId) =>
{
    var ended = SessionManager.ForceEndSession(sessionId);
    if (!ended)
        return Results.NotFound(new { error = $"Session with ID {sessionId} not found." });

    return Results.Ok(new { message = "Session force-ended." });
});

// GET: /api/timer/stats - Get consumption statistics
api.MapGet("/stats", () =>
{
    var sessions = SessionManager.GetActiveSessions().ToList();
    var now = DateTime.UtcNow;
    var today = now.Date;
    var thisMonth = new DateTime(now.Year, now.Month, 1);

    var activeSessions = sessions.Count(s => s.IsActive);
    var sessionsToday = SessionManager.GetAllSessions().Count(s => s.SessionStartUtc >= today);
    var sessionsThisMonth = SessionManager.GetAllSessions().Count(s => s.SessionStartUtc >= thisMonth);

    var totalCreditsToday = sessions
        .Where(s => s.SessionStartUtc >= today)
        .Sum(s => (decimal)((now - s.SessionStartUtc).TotalMinutes) * TimerConfig.CreditsPerHour / 60.0m);

    var totalCreditsThisMonth = SessionManager.GetAllSessions()
        .Where(s => s.SessionStartUtc >= thisMonth)
        .Sum(s => (decimal)((s.SessionEndUtc ?? now) - s.SessionStartUtc).TotalMinutes * TimerConfig.CreditsPerHour / 60.0m);

    var totalMinutesToday = sessions
        .Where(s => s.SessionStartUtc >= today)
        .Sum(s => (now - s.SessionStartUtc).TotalMinutes);

    var totalMinutesThisMonth = SessionManager.GetAllSessions()
        .Where(s => s.SessionStartUtc >= thisMonth)
        .Sum(s => ((s.SessionEndUtc ?? now) - s.SessionStartUtc).TotalMinutes);

    var stats = new TimerStatsDto
    {
        ActiveSessionsCount = activeSessions,
        TotalSessionsToday = sessionsToday,
        TotalSessionsThisMonth = sessionsThisMonth,
        TotalCreditsConsumedToday = Math.Round(totalCreditsToday, 6),
        TotalCreditsConsumedThisMonth = Math.Round(totalCreditsThisMonth, 6),
        TotalMinutesLoggedToday = Math.Round((decimal)totalMinutesToday, 2),
        TotalMinutesLoggedThisMonth = Math.Round((decimal)totalMinutesThisMonth, 2),
        Rate = TimerConfig.CreditsPerHour,
        IsEnabled = TimerConfig.IsEnabled
    };

    return Results.Ok(stats);
});

// ── Wallet Operations ──

// Consume credits
api.MapPost("/wallet/consume", async (WalletOperationRequest req, HttpClient http) =>
{
    if (string.IsNullOrWhiteSpace(req.UserId))
        return Results.BadRequest(new { error = "UserId is required" });

    try
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{mainApiUrl}/api/credit/consume");
        request.Headers.Add("X-Timer-Api-Key", timerApiKey);
        request.Headers.Add("X-User-Id", req.UserId);
        request.Content = JsonContent.Create(req);
        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<WalletOperationResponse>(cancellationToken: CancellationToken.None);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to consume credits via main API");
        return Results.Problem("Failed to consume credits");
    }
});

// Check balance
api.MapPost("/wallet/check-balance", async (CheckBalanceRequest req, HttpClient http) =>
{
    if (string.IsNullOrWhiteSpace(req.UserId))
        return Results.BadRequest(new { error = "UserId is required" });

    try
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{mainApiUrl}/api/credit/check");
        request.Headers.Add("X-Timer-Api-Key", timerApiKey);
        request.Headers.Add("X-User-Id", req.UserId);
        request.Content = JsonContent.Create(req.RequiredCredits);
        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CheckBalanceResponse>(cancellationToken: CancellationToken.None);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to check balance via main API");
        return Results.Problem("Failed to check balance");
    }
});


// Background timer: every minute, process active sessions for credit consumption
var timer = new Timer(async _ =>
{
    try
    {
        await new ConsumptionBackgroundJob(app.Logger, app.Services.GetRequiredService<IConfiguration>())
            .ProcessActiveSessionsAsync(mainApiUrl);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error in background consumption job");
    }
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

app.Run();

// ── In-memory configuration ──

public class TimerConfig
{
    private static readonly object _lock = new();
    private static TimerConfigDto _current = new();

    public static TimerConfigDto Current
    {
        get
        {
            lock (_lock)
            {
                return new TimerConfigDto
                {
                    Id = 1,
                    CreditsPerHour = _current.CreditsPerHour,
                    CheckIntervalMinutes = _current.CheckIntervalMinutes,
                    MaxSessionMinutes = _current.MaxSessionMinutes,
                    IsEnabled = _current.IsEnabled,
                    GracePeriodMinutes = _current.GracePeriodMinutes,
                    InitialFreeHours = _current.InitialFreeHours,
                    EnforceAccessBlock = _current.EnforceAccessBlock,
                    UpdatedAt = _current.UpdatedAt
                };
            }
        }
    }

    public static decimal CreditsPerHour => _current.CreditsPerHour;
    public static bool IsEnabled => _current.IsEnabled;
    public static int CheckIntervalMinutes => _current.CheckIntervalMinutes;
    public static int MaxSessionMinutes => _current.MaxSessionMinutes;
    public static int GracePeriodMinutes => _current.GracePeriodMinutes;
    public static decimal InitialFreeHours => _current.InitialFreeHours;
    public static bool EnforceAccessBlock => _current.EnforceAccessBlock;

    public static void Update(TimerConfigDto dto)
    {
        lock (_lock)
        {
            _current = new TimerConfigDto
            {
                Id = 1,
                CreditsPerHour = dto.CreditsPerHour,
                CheckIntervalMinutes = dto.CheckIntervalMinutes,
                MaxSessionMinutes = dto.MaxSessionMinutes,
                IsEnabled = dto.IsEnabled,
                GracePeriodMinutes = dto.GracePeriodMinutes,
                InitialFreeHours = dto.InitialFreeHours,
                EnforceAccessBlock = dto.EnforceAccessBlock,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }

    public static void Reset()
    {
        lock (_lock)
        {
            _current = new TimerConfigDto();
        }
    }
}

// ── In-memory session store ──

public static class SessionManager
{
    private static readonly Dictionary<string, UserSession> _sessions = new();
    private static readonly List<UserSession> _sessionHistory = new();
    private static int _nextId = 1;
    private static readonly object _lock = new();

    public static void StartSession(string userId, string? deviceInfo, DateTime startedAt)
    {
        lock (_lock)
        {
            EndSession(userId);
            var session = new UserSession
            {
                Id = _nextId++,
                UserId = userId,
                SessionStartUtc = startedAt,
                LastHeartbeatUtc = startedAt,
                DeviceInfo = deviceInfo,
                IsActive = true,
                SessionEndUtc = null,
                MinutesConsumed = 0,
                CreditsConsumed = 0,
                ElapsedMinutes = 0,
                EstimatedCreditsConsumed = 0,
                TimeRemainingDisplay = "00:00:00",
                InitialCreditBalance = 0,
                LastProcessedUtc = startedAt
            };
            _sessions[userId] = session;
            _sessionHistory.Add(session);
        }
    }

    public static UserSession? GetSession(string userId)
    {
        lock (_lock)
        {
            return _sessions.TryGetValue(userId, out var session) ? session : null;
        }
    }

    public static string GetSessionId(string userId)
    {
        return GetSession(userId)?.Id.ToString() ?? string.Empty;
    }

    public static void EndSession(string userId)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(userId, out var session))
            {
                session.IsActive = false;
                session.SessionEndUtc = DateTime.UtcNow;
                _sessions.Remove(userId);
            }
        }
    }

    public static bool ForceEndSession(int sessionId)
    {
        lock (_lock)
        {
            var entry = _sessions.FirstOrDefault(kvp => kvp.Value.Id == sessionId);
            if (entry.Key == null)
                return false;

            entry.Value.IsActive = false;
            entry.Value.SessionEndUtc = DateTime.UtcNow;
            _sessions.Remove(entry.Key);
            return true;
        }
    }

    public static IEnumerable<UserSession> GetActiveSessions()
    {
        lock (_lock)
        {
            return _sessions.Values.Where(s => s.IsActive).ToList();
        }
    }

    public static IEnumerable<UserSession> GetAllSessions()
    {
        lock (_lock)
        {
            return _sessionHistory.ToList();
        }
    }
}

// ── DTOs ──

public class UserSession
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime SessionStartUtc { get; set; }
    public DateTime? SessionEndUtc { get; set; }
    public decimal MinutesConsumed { get; set; }
    public decimal CreditsConsumed { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastHeartbeatUtc { get; set; }
    public string? DeviceInfo { get; set; }

    // Computed fields for live display
    public double ElapsedMinutes { get; set; }
    public decimal EstimatedCreditsConsumed { get; set; }
    public string? TimeRemainingDisplay { get; set; }

    // Used for fallback remaining-hours calculation
    public decimal InitialCreditBalance { get; set; }

    // Tracks when credits were last consumed by the background job.
    // Used to calculate incremental consumption (not total) on each tick.
    public DateTime? LastProcessedUtc { get; set; }
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

public class CreditWalletResponse
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal CreditBalance { get; set; }
    public decimal TotalCreditsPurchased { get; set; }
    public decimal TotalCreditsConsumed { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public string? SubscriptionId { get; set; }
    public string? SubscriptionStatus { get; set; }
}

// ── Mapping helpers ──

public static class TimerSessionMapper
{
    public static TimerUserSessionDto MapToTimerSessionDto(UserSession session)
    {
        var now = DateTime.UtcNow;
        var elapsedMinutes = (now - session.SessionStartUtc).TotalMinutes;
        var estimatedCredits = decimal.Round((decimal)elapsedMinutes * TimerConfig.CreditsPerHour / 60.0m, 6);
        var remainingHours = Math.Max(0, (session.InitialCreditBalance - estimatedCredits) / TimerConfig.CreditsPerHour);
        var remainingSeconds = (int)(remainingHours * 3600);
        var timeRemainingDisplay = $"{remainingSeconds / 3600:D2}:{(remainingSeconds % 3600) / 60:D2}:{remainingSeconds % 60:D2}";

        return new TimerUserSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            SessionStartUtc = session.SessionStartUtc,
            SessionEndUtc = session.SessionEndUtc,
            MinutesConsumed = elapsedMinutes,
            CreditsConsumed = estimatedCredits,
            IsActive = session.IsActive,
            LastHeartbeatUtc = session.LastHeartbeatUtc,
            DeviceInfo = session.DeviceInfo,
            ElapsedMinutes = elapsedMinutes,
            EstimatedCreditsConsumed = estimatedCredits,
            TimeRemainingDisplay = timeRemainingDisplay
        };
    }
}
