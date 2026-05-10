using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AdvancedTobics.RateLimiting;

/// <summary>
/// Fixed-window counter per partition using atomic INCR + EXPIRE (Lua).
/// </summary>
public sealed class RedisRateLimiter : IRedisRateLimiter
{
    private const string IncrExpireScript = """
        local current = redis.call('INCR', KEYS[1])
        if current == 1 then
          redis.call('EXPIRE', KEYS[1], tonumber(ARGV[1]))
        end
        return current
        """;

    private readonly IConnectionMultiplexer _redis;
    private readonly RedisRateLimitOptions _options;

    public RedisRateLimiter(
        IConnectionMultiplexer redis,
        IOptions<RedisRateLimitOptions> options)
    {
        _redis = redis;
        _options = options.Value;
    }

    public async Task<RateLimitLease> TryAcquireAsync(
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        if (!_redis.IsConnected)
            return new RateLimitLease(Allowed: true, Count: 0, RetryAfter: null);

        var db = _redis.GetDatabase();
        var key = new RedisKey($"{_options.KeyPrefix}:{partitionKey}");

        try
        {
            var result = await db.ScriptEvaluateAsync(
                    IncrExpireScript,
                    keys: new RedisKey[] { key },
                    values: new RedisValue[] { _options.WindowSeconds })
                .ConfigureAwait(false);

            var count = (long)result;
            if (count <= _options.PermitLimit)
                return new RateLimitLease(Allowed: true, count, null);

            var ttl = await db.KeyTimeToLiveAsync(key).ConfigureAwait(false);
            var retryAfter = ttl.HasValue && ttl.Value > TimeSpan.Zero
                ? ttl
                : TimeSpan.FromSeconds(_options.WindowSeconds);

            return new RateLimitLease(Allowed: false, count, retryAfter);
        }
        catch (RedisException) when (_options.FailOpen)
        {
            return new RateLimitLease(Allowed: true, Count: 0, RetryAfter: null);
        }
    }
}
