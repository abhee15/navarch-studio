using System.Net.Http.Json;
using System.Text.Json;
using Polly;
using Polly.Extensions.Http;

namespace HullSizingService.Services.Integration;

/// <summary>
/// Resilient HTTP client for calling DataService with Polly policies
/// </summary>
public class DataServiceClient : IDataServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DataServiceClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DataServiceClient(
        HttpClient httpClient,
        ILogger<DataServiceClient> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public async Task<WaterPropertiesResponse> GetWaterPropertiesAsync(
        decimal tempC,
        decimal salinityPsu,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/v1/water-properties?temp={tempC}&salinity={salinityPsu}";

        _logger.LogInformation(
            "[DATASERVICE_CLIENT] Getting water properties: temp={TempC}°C, salinity={SalinityPsu} PSU",
            tempC, salinityPsu);

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            ForwardClaims(request);
            ForwardCorrelationId(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<WaterPropertiesResponse>(cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("DataService returned null water properties");
            }

            _logger.LogInformation(
                "[DATASERVICE_CLIENT] Water properties retrieved: rho={Rho} kg/m³, nu={Nu} m²/s",
                result.RhoKgM3, result.NuM2S);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "[DATASERVICE_CLIENT] HTTP error getting water properties: {Message}",
                ex.Message);
            throw;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            _logger.LogWarning("[DATASERVICE_CLIENT] Request cancelled by user");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex,
                "[DATASERVICE_CLIENT] Timeout getting water properties (> 2s)");
            throw new TimeoutException("Request to DataService timed out", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Guid> CreateVesselAsync(
        CreateVesselFromCandidateDto vesselDto,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var url = "/api/v1/vessels";

        _logger.LogInformation(
            "[DATASERVICE_CLIENT] Creating vessel: {VesselName}, IdempotencyKey={IdempotencyKey}",
            vesselDto.Name, idempotencyKey);

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(vesselDto)
            };

            ForwardClaims(request);
            ForwardCorrelationId(request);
            request.Headers.Add("Idempotency-Key", idempotencyKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CreateVesselResponse>(cancellationToken);

            if (result == null || result.Id == Guid.Empty)
            {
                throw new InvalidOperationException("DataService returned invalid vessel ID");
            }

            _logger.LogInformation(
                "[DATASERVICE_CLIENT] Vessel created: {VesselId}, IdempotencyKey={IdempotencyKey}",
                result.Id, idempotencyKey);

            return result.Id;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "[DATASERVICE_CLIENT] HTTP error creating vessel: {Message}",
                ex.Message);
            throw;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            _logger.LogWarning("[DATASERVICE_CLIENT] Request cancelled by user");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex,
                "[DATASERVICE_CLIENT] Timeout creating vessel (> 2s)");
            throw new TimeoutException("Request to DataService timed out", ex);
        }
    }

    /// <summary>
    /// Forward user claims from HttpContext to DataService request
    /// </summary>
    private void ForwardClaims(HttpRequestMessage request)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Items == null)
        {
            return;
        }

        // Forward claims that were extracted by ClaimsForwardingMiddleware
        if (httpContext.Items.TryGetValue("Claims:TenantId", out var tenantId))
            request.Headers.Add("X-Tenant-Id", tenantId?.ToString());

        if (httpContext.Items.TryGetValue("Claims:Sub", out var sub))
            request.Headers.Add("X-User-Id", sub?.ToString());

        if (httpContext.Items.TryGetValue("Claims:OrgId", out var orgId))
            request.Headers.Add("X-Org-Id", orgId?.ToString());

        if (httpContext.Items.TryGetValue("Claims:Roles", out var roles))
            request.Headers.Add("X-User-Roles", roles?.ToString());

        if (httpContext.Items.TryGetValue("Claims:Scope", out var scope))
            request.Headers.Add("X-Scope", scope?.ToString());

        // Also forward the Authorization header (JWT token)
        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader))
        {
            request.Headers.Add("Authorization", authHeader);
        }
    }

    /// <summary>
    /// Forward correlation ID for distributed tracing
    /// </summary>
    private void ForwardCorrelationId(HttpRequestMessage request)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Items == null)
        {
            return;
        }

        if (httpContext.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            request.Headers.Add("X-Correlation-Id", correlationId?.ToString());
        }
    }

    /// <summary>
    /// Response from vessel creation
    /// </summary>
    private record CreateVesselResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}

