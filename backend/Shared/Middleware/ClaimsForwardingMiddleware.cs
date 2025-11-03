using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shared.Middleware;

/// <summary>
/// Middleware to extract and forward JWT claims for service-to-service calls
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
        // Check if user is authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Extract tenantId claim (REQUIRED for multi-tenancy)
            var tenantId = context.User.FindFirst("tenantId")?.Value;

            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning(
                    "[CLAIMS_FORWARDING] Missing required 'tenantId' claim - denying request (User: {UserId})",
                    context.User.FindFirst("sub")?.Value ?? "unknown");

                context.Response.StatusCode = 403; // Forbidden
                context.Response.ContentType = "application/json";
                var errorResponse = JsonSerializer.Serialize(new
                {
                    error = "Forbidden",
                    message = "Missing required tenant claim. Please re-authenticate."
                });
                await context.Response.WriteAsync(errorResponse);
                return;
            }

            // Store claims in HttpContext.Items for S2S calls
            context.Items["Claims:TenantId"] = tenantId;

            var sub = context.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(sub))
            {
                context.Items["Claims:Sub"] = sub;
            }

            var orgId = context.User.FindFirst("orgId")?.Value;
            if (!string.IsNullOrEmpty(orgId))
            {
                context.Items["Claims:OrgId"] = orgId;
            }

            // Collect all role claims
            var roles = string.Join(",", context.User.FindAll("roles").Select(c => c.Value));
            if (!string.IsNullOrEmpty(roles))
            {
                context.Items["Claims:Roles"] = roles;
            }

            var scope = context.User.FindFirst("scope")?.Value;
            if (!string.IsNullOrEmpty(scope))
            {
                context.Items["Claims:Scope"] = scope;
            }

            _logger.LogDebug(
                "[CLAIMS_FORWARDING] Claims extracted: TenantId={TenantId}, Sub={Sub}, OrgId={OrgId}, Roles={Roles}",
                tenantId, sub, orgId, roles);
        }
        else
        {
            // Unauthenticated request - allow through (controller will check authorization)
            _logger.LogDebug("[CLAIMS_FORWARDING] Unauthenticated request - skipping claims extraction");
        }

        await _next(context);
    }
}

