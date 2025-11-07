using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace AIAgentService.Middleware;

public class AIRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AIRateLimitMiddleware> _logger;
    private const int DailyLimit = 100;
    private const int MinuteLimit = 10;

    public AIRateLimitMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<AIRateLimitMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.Request.Headers["X-User-Sub"].FirstOrDefault()
            ?? "anonymous";

        // Check daily limit
        var dailyKey = $"ai:ratelimit:daily:{userId}:{DateTime.UtcNow:yyyyMMdd}";
        var dailyCount = GetRequestCount(dailyKey);

        if (dailyCount >= DailyLimit)
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Daily AI usage limit exceeded",
                limit = DailyLimit,
                used = dailyCount,
                resetAt = DateTime.UtcNow.Date.AddDays(1)
            });
            _logger.LogWarning("User {UserId} exceeded daily limit ({Count}/{Limit})", userId, dailyCount, DailyLimit);
            return;
        }

        // Check per-minute limit
        var minuteKey = $"ai:ratelimit:minute:{userId}:{DateTime.UtcNow:yyyyMMddHHmm}";
        var minuteCount = GetRequestCount(minuteKey);

        if (minuteCount >= MinuteLimit)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers.RetryAfter = "60";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                message = "Too many requests. Please wait a minute.",
                limit = MinuteLimit,
                used = minuteCount,
                retryAfter = 60
            });
            _logger.LogWarning("User {UserId} exceeded per-minute limit ({Count}/{Limit})", userId, minuteCount, MinuteLimit);
            return;
        }

        // Increment counters
        IncrementRequestCount(dailyKey, TimeSpan.FromDays(1));
        IncrementRequestCount(minuteKey, TimeSpan.FromMinutes(1));

        await _next(context);
    }

    private int GetRequestCount(string key)
    {
        return _cache.TryGetValue(key, out int count) ? count : 0;
    }

    private void IncrementRequestCount(string key, TimeSpan expiration)
    {
        var currentCount = GetRequestCount(key);
        var newCount = currentCount + 1;

        _cache.Set(key, newCount, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
            Size = 1
        });
    }
}
