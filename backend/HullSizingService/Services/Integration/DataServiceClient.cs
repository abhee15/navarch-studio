using System.Text.Json;
using Shared.DTOs.Catalog;

namespace HullSizingService.Services.Integration;

/// <summary>
/// HTTP client for DataService with Polly resilience policies
/// </summary>
public class DataServiceClient : IDataServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DataServiceClient> _logger;

    public DataServiceClient(HttpClient httpClient, ILogger<DataServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<WaterPropertiesResponse?> GetWaterPropertiesAsync(
        decimal temperatureCelsius,
        decimal salinityPpt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[DATA_CLIENT] Fetching water properties for T={Temp}°C, S={Salinity}ppt", temperatureCelsius, salinityPpt);

            var response = await _httpClient.GetAsync(
                $"/api/v1/water/properties?temperature={temperatureCelsius}&salinity={salinityPpt}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[DATA_CLIENT] Water properties request failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<WaterPropertiesResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DATA_CLIENT] Failed to fetch water properties");
            return null;
        }
    }

    public async Task<HoltropResponse?> CalculateResistanceAsync(HoltropRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[DATA_CLIENT] Calculating resistance for Lpp={Lpp}m, V={Speed}kn", request.LppM, request.SpeedKn);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/v1/resistance/holtrop", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[DATA_CLIENT] Holtrop calculation failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<HoltropResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DATA_CLIENT] Failed to calculate resistance");
            return null;
        }
    }

    public async Task<KnnSearchResponse> SearchSimilarVesselsAsync(
        KnnSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[DATA_CLIENT] Searching similar vessels: Type={Type}, Displacement={Disp}t, K={K}",
                request.VesselType, request.TargetDisplacement, request.K);

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/v1/catalog/vessels/search-similar", 
                content, 
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[DATA_CLIENT] KNN search failed: {StatusCode}", response.StatusCode);
                return new KnnSearchResponse { SimilarVessels = new List<SimilarVesselDto>() };
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<KnnSearchResponse>(responseJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            _logger.LogInformation(
                "[DATA_CLIENT] KNN search returned {Count} similar vessels",
                result?.SimilarVessels?.Count ?? 0);

            return result ?? new KnnSearchResponse { SimilarVessels = new List<SimilarVesselDto>() };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DATA_CLIENT] Failed to search similar vessels");
            return new KnnSearchResponse { SimilarVessels = new List<SimilarVesselDto>() };
        }
    }
}
