using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NavArch.UnitConversion.Services;
using System.Security.Claims;

namespace Shared.Middleware;

/// <summary>
/// Middleware to handle unit conversion preferences from headers or user settings
/// </summary>
public class UnitConversionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UnitConversionMiddleware> _logger;

    public UnitConversionMiddleware(
        RequestDelegate next,
        ILogger<UnitConversionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUnitConverter converter)
    {
        // Skip processing for file uploads (multipart/form-data)
        var contentType = context.Request.ContentType;
        if (!string.IsNullOrEmpty(contentType) && 
            contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Skipping unit conversion for file upload");
            await _next(context);
            return;
        }
        
        // Read preferred units from X-Preferred-Units header
        var preferredUnits = context.Request.Headers["X-Preferred-Units"].FirstOrDefault();
        
        // If header not present, try to get from user's stored preference
        if (string.IsNullOrEmpty(preferredUnits))
        {
            preferredUnits = GetUserPreferredUnits(context);
        }
        
        // Default to SI if nothing specified
        preferredUnits ??= "SI";
        
        // Store in HttpContext.Items for downstream use
        context.Items["PreferredUnits"] = preferredUnits;
        
        _logger.LogDebug("Unit preference set to: {PreferredUnits}", preferredUnits);
        
        await _next(context);
    }

    /// <summary>
    /// Get user's preferred units from claims or user data
    /// </summary>
    private string? GetUserPreferredUnits(HttpContext context)
    {
        // Try to get from JWT claims if available
        var user = context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            // Look for PreferredUnits claim
            var unitsClaim = user.FindFirst("PreferredUnits")?.Value;
            if (!string.IsNullOrEmpty(unitsClaim))
            {
                return unitsClaim;
            }
        }
        
        // Could also query database here if needed, but for now we rely on header
        // or the frontend to send the preference
        return null;
    }
}

