using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ProductApi.Infrastructure.RateLimiting;

/// <summary>
/// Atomic fixed-window rate limiter: INCR then set TTL only on the first call in the window,
/// so the window never resets prematurely. Mirrors the Quarkus/Spring siblings' Redis Lua script
/// exactly (same 100 req / 60s window, same 429 + Retry-After, same fail-open on Redis errors).
/// </summary>
public sealed class RateLimitingMiddleware(RequestDelegate next, IConnectionMultiplexer redis, ILogger<RateLimitingMiddleware> logger)
{
    public const int MaxRequests = 100;
    private const int WindowSeconds = 60;
    private const string KeyPrefix = "rl:";
    private const string TargetPathPrefix = "/api/v1/products";

    private static readonly HashSet<string> TargetMethods = ["POST", "PUT", "DELETE"];

    private const string RateLimitScript = """
        local current = redis.call('INCR', KEYS[1])
        if current == 1 then
            redis.call('EXPIRE', KEYS[1], ARGV[1])
        end
        return current
        """;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!TargetMethods.Contains(context.Request.Method) ||
            !context.Request.Path.StartsWithSegments(TargetPathPrefix))
        {
            await next(context);
            return;
        }

        var ip = ExtractClientIp(context);
        var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / WindowSeconds;
        var key = $"{KeyPrefix}{ip}:{bucket}";

        try
        {
            var db = redis.GetDatabase();
            var result = (long)await db.ScriptEvaluateAsync(RateLimitScript,
                [new RedisKey(key)], [new RedisValue(WindowSeconds.ToString())]);

            if (result > MaxRequests)
            {
                logger.LogWarning("[RATE-LIMIT] IP '{Ip}' blocked — request #{Count} ({Method} {Path})",
                    ip, result, context.Request.Method, context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.RetryAfter = WindowSeconds.ToString();
                return;
            }
        }
        catch (Exception ex)
        {
            // Redis unavailable: fail-open to avoid blocking legitimate traffic.
            logger.LogWarning(ex, "[RATE-LIMIT] Redis unavailable (fail-open) — IP '{Ip}'", ip);
        }

        await next(context);
    }

    private static string ExtractClientIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded) &&
            !string.IsNullOrWhiteSpace(forwarded))
        {
            var parts = forwarded.ToString().Split(',');
            for (var i = parts.Length - 1; i >= 0; i--)
            {
                var ip = parts[i].Trim();
                if (!string.IsNullOrWhiteSpace(ip)) return ip;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
