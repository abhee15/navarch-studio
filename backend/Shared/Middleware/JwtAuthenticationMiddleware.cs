using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Services;

namespace Shared.Middleware;

public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    public JwtAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<JwtAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
    {
        var token = ExtractTokenFromHeader(context.Request);

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var principal = await jwtService.ValidateTokenAsync(token, context.RequestAborted);
                if (principal != null)
                {
                    context.User = principal;
                    _logger.LogDebug("JWT token validated successfully for user: {UserId}",
                        jwtService.GetUserId(principal));
                }
                else
                {
                    _logger.LogWarning("JWT token validation failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating JWT token");
            }
        }

        await _next(context);
    }

    private static string? ExtractTokenFromHeader(HttpRequest request)
    {
        var authHeader = request.Headers["Authorization"].FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }
        return null;
    }
}






