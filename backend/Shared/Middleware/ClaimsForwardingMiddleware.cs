using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shared.Middleware;

/// <summary>
/// Middleware to extract JWT claims and forward them for service-to-service calls
/// Denies requests if tenantId is missing (enforces tenant isolation)
/// </summary>
public class ClaimsForwardingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ClaimsForwardingMiddleware> _logger;

    public ClaimsForwardingMiddleware(RequestDelegate next, ILogger<ClaimsForwardingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip tenant validation for health check and swagger endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path == "/health" || path.StartsWith("/swagger"))
        {
            await _next(context);
            return;
        }

        // Extract claims from HttpContext.Items (set by JwtAuthenticationMiddleware)
        var sub = context.Items["Claims:Sub"]?.ToString();
        var tenantId = context.Items["Claims:TenantId"]?.ToString();
        var orgId = context.Items["Claims:OrgId"]?.ToString();
        var roles = context.Items["Claims:Roles"]?.ToString();
        var scope = context.Items["Claims:Scope"]?.ToString();

        _logger.LogDebug("[CLAIMS_FWD] Extracted claims: sub={Sub}, tenantId={TenantId}, orgId={OrgId}", sub, tenantId, orgId);

        // DENY if tenantId is missing (enforce tenant isolation)
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("[CLAIMS_FWD] Request denied - missing tenantId for path {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var errorJson = JsonSerializer.Serialize(new
            {
                error = "Forbidden",
                message = "Tenant ID is required for this operation"
            });

            await context.Response.WriteAsync(errorJson);
            return;
        }

        // Store claims in HttpContext.Items for downstream use
        context.Items["Claims:Sub"] = sub;
        context.Items["Claims:TenantId"] = tenantId;
        context.Items["Claims:OrgId"] = orgId;
        context.Items["Claims:Roles"] = roles;
        context.Items["Claims:Scope"] = scope;

        await _next(context);
    }
}
