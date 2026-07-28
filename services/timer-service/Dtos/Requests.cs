namespace ProcessZero.TimerService.Dtos;

public record StartSessionRequest(string UserId, string? DeviceInfo);
public record HeartbeatRequest(string UserId);
public record UserQuery(string UserId);

public class RemainingHoursResponse
{
    public decimal RemainingHours { get; set; }
}

public class TimerRemainingHoursResponse
{
    public decimal RemainingHours { get; set; }
}

public class CheckBalanceResponse
{
    public decimal CreditBalance { get; set; }
    public bool HasSufficientCredits { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class WalletOperationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal NewBalance { get; set; }
    public decimal CreditsConsumed { get; set; }
}

// ── DTOs matching the TimerServiceClient expectations ──

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
