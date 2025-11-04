using System.Security.Claims;

namespace ApiGateway.Middleware;

/// <summary>
/// Middleware to extract JWT claims and store in HttpContext.Items for downstream use
/// </summary>
public class JwtClaimsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtClaimsMiddleware> _logger;

    public JwtClaimsMiddleware(RequestDelegate next, ILogger<JwtClaimsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract claims from authenticated user
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var sub = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;

            var tenantId = context.User.FindFirst("custom:tenantId")?.Value;
            var orgId = context.User.FindFirst("custom:orgId")?.Value;
            var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                ?? context.User.FindFirst("email")?.Value;

            var roles = context.User.FindFirst(ClaimTypes.Role)?.Value
                ?? context.User.FindFirst("cognito:groups")?.Value
                ?? string.Join(",", context.User.FindAll(ClaimTypes.Role).Select(c => c.Value));

            var scope = context.User.FindFirst("scope")?.Value;

            // Store in HttpContext.Items for HttpClientService to forward
            if (!string.IsNullOrEmpty(sub))
                context.Items["Claims:Sub"] = sub;

            if (!string.IsNullOrEmpty(tenantId))
                context.Items["Claims:TenantId"] = tenantId;

            if (!string.IsNullOrEmpty(orgId))
                context.Items["Claims:OrgId"] = orgId;

            if (!string.IsNullOrEmpty(email))
                context.Items["Claims:Email"] = email;

            if (!string.IsNullOrEmpty(roles))
                context.Items["Claims:Roles"] = roles;

            if (!string.IsNullOrEmpty(scope))
                context.Items["Claims:Scope"] = scope;

            _logger.LogInformation("[JWT_CLAIMS] Extracted claims: Sub={Sub}, TenantId={TenantId}, OrgId={OrgId}",
                sub, tenantId ?? "null", orgId ?? "null");
        }
        else
        {
            _logger.LogWarning("[JWT_CLAIMS] User is not authenticated for path {Path}", context.Request.Path);
        }

        await _next(context);
    }
}


