using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace ApiGateway.Services;

public class HttpClientService : IHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpClientService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpClientService(
        HttpClient httpClient,
        ILogger<HttpClientService> logger,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<HttpResponseMessage> GetAsync(string service, string endpoint, CancellationToken cancellationToken)
    {
        var baseUrl = GetServiceBaseUrl(service);
        var url = $"{baseUrl}/{endpoint.TrimStart('/')}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        ForwardAuthorizationHeader(request);

        _logger.LogInformation("[HTTP_CLIENT] Forwarding GET request to {Url}", url);
        Console.WriteLine($"[HTTP_CLIENT] GET {url}");

        try
        {
            var startTime = DateTime.UtcNow;
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation("[HTTP_CLIENT] Response from {Url}: {StatusCode} in {Elapsed}ms", url, (int)response.StatusCode, elapsed);
            Console.WriteLine($"[HTTP_CLIENT] Response: {(int)response.StatusCode} {response.StatusCode} in {elapsed}ms");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HTTP_CLIENT] Error calling {Url}: {Message}", url, ex.Message);
            Console.WriteLine($"[HTTP_CLIENT] ERROR calling {url}: {ex.Message}");
            Console.WriteLine($"[HTTP_CLIENT] Exception Type: {ex.GetType().FullName}");
            throw;
        }
    }

    public async Task<HttpResponseMessage> PostAsync(string service, string endpoint, HttpContent content, CancellationToken cancellationToken)
    {
        var baseUrl = GetServiceBaseUrl(service);
        var url = $"{baseUrl}/{endpoint.TrimStart('/')}";

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        ForwardAuthorizationHeader(request);

        _logger.LogInformation("Forwarding POST request to {Url}", url);
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PutAsync(string service, string endpoint, HttpContent content, CancellationToken cancellationToken)
    {
        var baseUrl = GetServiceBaseUrl(service);
        var url = $"{baseUrl}/{endpoint.TrimStart('/')}";

        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = content
        };
        ForwardAuthorizationHeader(request);

        _logger.LogInformation("Forwarding PUT request to {Url}", url);
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string service, string endpoint, CancellationToken cancellationToken)
    {
        var baseUrl = GetServiceBaseUrl(service);
        var url = $"{baseUrl}/{endpoint.TrimStart('/')}";

        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        ForwardAuthorizationHeader(request);

        _logger.LogInformation("Forwarding DELETE request to {Url}", url);
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    private void ForwardAuthorizationHeader(HttpRequestMessage request)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader))
            {
                request.Headers.Add("Authorization", authHeader);
                _logger.LogInformation("Forwarding Authorization header to downstream service");
            }

            // Forward claims from JWT (context.User) to downstream service as headers
            // Read directly from ClaimsPrincipal populated by JwtAuthenticationMiddleware
            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                var sub = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User.FindFirst("sub")?.Value;
                if (!string.IsNullOrEmpty(sub))
                {
                    request.Headers.Add("X-User-Sub", sub);
                }

                var tenantId = httpContext.User.FindFirst("custom:tenantId")?.Value;
                if (!string.IsNullOrEmpty(tenantId))
                {
                    request.Headers.Add("X-Tenant-Id", tenantId);
                    _logger.LogInformation("Forwarding X-Tenant-Id: {TenantId}", tenantId);
                }
                else
                {
                    _logger.LogWarning("No tenantId claim found in JWT for downstream forwarding");
                }

                var orgId = httpContext.User.FindFirst("custom:orgId")?.Value;
                if (!string.IsNullOrEmpty(orgId))
                {
                    request.Headers.Add("X-Org-Id", orgId);
                }

                var email = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                    ?? httpContext.User.FindFirst("email")?.Value;
                if (!string.IsNullOrEmpty(email))
                {
                    request.Headers.Add("X-User-Email", email);
                }

                var roles = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                    ?? httpContext.User.FindFirst("cognito:groups")?.Value
                    ?? string.Join(",", httpContext.User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value));
                if (!string.IsNullOrEmpty(roles))
                {
                    request.Headers.Add("X-User-Roles", roles);
                }

                var scope = httpContext.User.FindFirst("scope")?.Value;
                if (!string.IsNullOrEmpty(scope))
                {
                    request.Headers.Add("X-User-Scope", scope);
                }

                _logger.LogInformation("Forwarded claims headers to downstream service");
            }
            else
            {
                // TEMPORARY DEV FIX: If user is not authenticated, use default dev tenant
                // This ensures consistent tenant IDs across all requests in development
                // TODO: Remove this before production - all requests should be authenticated
                _logger.LogWarning("[HTTP_CLIENT] ⚠️ DEV MODE: User not authenticated, using default tenant");
                request.Headers.Add("X-User-Sub", "dev-default-user");
                request.Headers.Add("X-Tenant-Id", "dev-default-tenant");
                Console.WriteLine("[HTTP_CLIENT] ⚠️ DEV MODE: Using default tenant (user not authenticated)");
            }
        }
    }

    private string GetServiceBaseUrl(string service)
    {
        return service.ToLower() switch
        {
            "identity" => _configuration["Services:IdentityService"] ?? "http://localhost:5001",
            "data" => _configuration["Services:DataService"] ?? "http://localhost:5003",
            "hullsizing" => _configuration["Services:HullSizingService"] ?? "http://localhost:5004",
            _ => throw new ArgumentException($"Unknown service: {service}")
        };
    }
}
