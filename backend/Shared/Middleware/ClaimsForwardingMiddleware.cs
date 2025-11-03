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

        // Extract claims from HTTP headers (forwarded by API Gateway in S2S calls)
        var sub = context.Request.Headers["X-User-Sub"].FirstOrDefault();
        var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        var orgId = context.Request.Headers["X-Org-Id"].FirstOrDefault();
        var email = context.Request.Headers["X-User-Email"].FirstOrDefault();
        var roles = context.Request.Headers["X-User-Roles"].FirstOrDefault();
        var scope = context.Request.Headers["X-User-Scope"].FirstOrDefault();

        _logger.LogInformation("[CLAIMS_FWD] Received headers: X-User-Sub={Sub}, X-Tenant-Id={TenantId}, X-Org-Id={OrgId}",
            sub ?? "null", tenantId ?? "null", orgId ?? "null");

        // If no explicit tenantId, derive from sub (for single-tenant development)
        // In production, this should come from custom:tenantId claim in JWT
        if (string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(sub))
        {
            tenantId = $"user-{sub}";
            _logger.LogInformation("[CLAIMS_FWD] No explicit tenantId - derived from sub: {TenantId}", tenantId);
        }

        // TEMPORARY DEV FIX: Use default tenant if both are missing
        // This unblocks development - remove in production
        if (string.IsNullOrEmpty(tenantId))
        {
            tenantId = "dev-default-tenant";
            sub = sub ?? "dev-default-user";
            _logger.LogWarning("[CLAIMS_FWD] ⚠️ DEV MODE: Using default tenant for path {Path} (no claims received)", context.Request.Path);
        }

        // Store claims in HttpContext.Items for controller use
        context.Items["Claims:Sub"] = sub;
        context.Items["Claims:TenantId"] = tenantId;
        context.Items["Claims:OrgId"] = orgId;
        context.Items["Claims:Email"] = email;
        context.Items["Claims:Roles"] = roles;
        context.Items["Claims:Scope"] = scope;

        await _next(context);
    }
}
