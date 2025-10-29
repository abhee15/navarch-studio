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
        }
    }

    private string GetServiceBaseUrl(string service)
    {
        return service.ToLower() switch
        {
            "identity" => _configuration["Services:IdentityService"] ?? "http://localhost:5001",
            "data" => _configuration["Services:DataService"] ?? "http://localhost:5003",
            _ => throw new ArgumentException($"Unknown service: {service}")
        };
    }
}





