using System.Text;
using System.Text.Json;

namespace ApiGateway.Services;

public class HttpClientService : IHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpClientService> _logger;
    private readonly IConfiguration _configuration;

    public HttpClientService(
        HttpClient httpClient,
        ILogger<HttpClientService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<HttpResponseMessage> GetAsync(string service, string endpoint, CancellationToken cancellationToken)
    {
        var baseUrl = GetServiceBaseUrl(service);
        var url = $"{baseUrl}/{endpoint.TrimStart('/')}";

        _logger.LogInformation("Forwarding GET request to {Url}", url);
        return await _httpClient.GetAsync(url, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsync(string service, string endpoint, HttpContent content, CancellationToken cancellationToken)
    {
        var baseUrl = GetServiceBaseUrl(service);
        var url = $"{baseUrl}/{endpoint.TrimStart('/')}";

        _logger.LogInformation("Forwarding POST request to {Url}", url);
        return await _httpClient.PostAsync(url, content, cancellationToken);
    }

    public async Task<HttpResponseMessage> PutAsync(string service, string endpoint, HttpContent content, CancellationToken cancellationToken)
    {
        var baseUrl = GetServiceBaseUrl(service);
        var url = $"{baseUrl}/{endpoint.TrimStart('/')}";

        _logger.LogInformation("Forwarding PUT request to {Url}", url);
        return await _httpClient.PutAsync(url, content, cancellationToken);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string service, string endpoint, CancellationToken cancellationToken)
    {
        var baseUrl = GetServiceBaseUrl(service);
        var url = $"{baseUrl}/{endpoint.TrimStart('/')}";

        _logger.LogInformation("Forwarding DELETE request to {Url}", url);
        return await _httpClient.DeleteAsync(url, cancellationToken);
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





