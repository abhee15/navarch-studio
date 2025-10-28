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
            _logger.LogInformation("JWT token found in request to {Path}, attempting validation", context.Request.Path);
            try
            {
                var principal = await jwtService.ValidateTokenAsync(token, context.RequestAborted);
                if (principal != null)
                {
                    context.User = principal;
                    var userId = jwtService.GetUserId(principal);
                    _logger.LogInformation("JWT token validated successfully for user: {UserId}", userId);
                }
                else
                {
                    _logger.LogWarning("JWT token validation failed for {Path}", context.Request.Path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating JWT token for {Path}", context.Request.Path);
            }
        }
        else
        {
            _logger.LogInformation("No JWT token found in request to {Path}", context.Request.Path);
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






