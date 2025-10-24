using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Shared.Middleware;

/// <summary>
/// Middleware that adds a correlation ID to every request for distributed tracing.
/// The correlation ID follows a request through all services, making debugging easier.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get correlation ID from request header, or generate a new one
        var correlationId = GetOrCreateCorrelationId(context);

        // Add correlation ID to response headers (so caller can see it)
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Add correlation ID to ALL logs in this request using Serilog's LogContext
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger.LogDebug("Request started with CorrelationId: {CorrelationId}", correlationId);

            try
            {
                // Continue to next middleware
                await _next(context);
            }
            finally
            {
                _logger.LogDebug("Request completed with CorrelationId: {CorrelationId}", correlationId);
            }
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // Check if correlation ID already exists in request headers
        // (e.g., from API Gateway or previous service)
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) &&
            !string.IsNullOrEmpty(correlationId))
        {
            return correlationId!;
        }

        // Generate a new correlation ID (GUID)
        return Guid.NewGuid().ToString();
    }
}






