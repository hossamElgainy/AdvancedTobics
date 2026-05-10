namespace AdvancedTobics.RateLimiting;

public class RedisRateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    /// <summary>Max requests allowed per client within the window.</summary>
    public int PermitLimit { get; set; } = 5;

    /// <summary>Window length in seconds (fixed window from first request in period).</summary>
    public int WindowSeconds { get; set; } = 60;

    public string KeyPrefix { get; set; } = "rl";

    /// <summary>If true, allow requests when Redis errors (recommended for availability).</summary>
    public bool FailOpen { get; set; } = true;
}
