using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;

namespace AdvancedTobics.RateLimiting;

public sealed class RedisRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RedisRateLimitOptions _options;

    public RedisRateLimitingMiddleware(
        RequestDelegate next,
        IOptions<RedisRateLimitOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, IRedisRateLimiter limiter)
    {
        if (!_options.Enabled || ShouldBypass(context))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var partition = ResolvePartitionKey(context);
        var lease = await limiter.TryAcquireAsync(partition, context.RequestAborted)
            .ConfigureAwait(false);

        if (!lease.Allowed)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            if (lease.RetryAfter is { } ra)
            {
                var seconds = Math.Max(1, (int)Math.Ceiling(ra.TotalSeconds));
                context.Response.Headers.Append("Retry-After", seconds.ToString());
            }

            await context.Response.WriteAsync("Too many requests. Try again later.", context.RequestAborted)
                .ConfigureAwait(false);
            return;
        }

        context.Response.Headers.Append("X-RateLimit-Limit", _options.PermitLimit.ToString());
        context.Response.Headers.Append("X-RateLimit-Remaining",
            Math.Max(0, _options.PermitLimit - lease.Count).ToString());

        await _next(context).ConfigureAwait(false);
    }

    private static bool ShouldBypass(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/swagger");
    }

    private static string ResolvePartitionKey(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            var first = forwarded.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(first))
                ip = first;
        }

        return $"ip:{ip}";
    }
}
