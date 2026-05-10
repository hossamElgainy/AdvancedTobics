namespace AdvancedTobics.RateLimiting;

public readonly record struct RateLimitLease(bool Allowed, long Count, TimeSpan? RetryAfter);

public interface IRedisRateLimiter
{
    /// <summary>
    /// Increments the counter for <paramref name="partitionKey"/> and returns whether the request is allowed.
    /// </summary>
    Task<RateLimitLease> TryAcquireAsync(string partitionKey, CancellationToken cancellationToken = default);
}
