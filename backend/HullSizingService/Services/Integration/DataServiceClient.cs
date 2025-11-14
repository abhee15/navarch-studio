using System.Text.Json;
using Shared.DTOs;
using Shared.DTOs.Catalog;
using Shared.DTOs.Hydrostatics;
using Shared.DTOs.ShipD;

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

    public async Task<ParametricSearchResponse> SearchSimilarParametricHullsAsync(
        ParametricSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[DATA_CLIENT] Searching parametric hulls: LOA={LOA}m, Volume={Vol}m³, K={K}",
                request.TargetLOA, request.TargetVolume, request.K);

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/v1/catalog/parametric/search-similar",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[DATA_CLIENT] Parametric KNN search failed: {StatusCode}", response.StatusCode);
                return new ParametricSearchResponse { SimilarHulls = new List<SimilarParametricHullDto>() };
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ParametricSearchResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation(
                "[DATA_CLIENT] Parametric KNN search returned {Count} similar hulls",
                result?.SimilarHulls?.Count ?? 0);

            return result ?? new ParametricSearchResponse { SimilarHulls = new List<SimilarParametricHullDto>() };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DATA_CLIENT] Failed to search parametric hulls");
            return new ParametricSearchResponse { SimilarHulls = new List<SimilarParametricHullDto>() };
        }
    }

    public async Task<IReadOnlyList<ShipDParameterMetadataDto>> GetShipDParameterMetadataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/shipd/parameters", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[DATA_CLIENT] ShipD parameter metadata request failed: {StatusCode}", response.StatusCode);
                return Array.Empty<ShipDParameterMetadataDto>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize<List<ShipDParameterMetadataDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return payload ?? new List<ShipDParameterMetadataDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DATA_CLIENT] Failed to fetch ShipD parameter metadata");
            return Array.Empty<ShipDParameterMetadataDto>();
        }
    }

    public async Task<IReadOnlyList<ShipDVesselTaxonomyDto>> GetShipDVesselTaxonomyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/shipd/taxonomy", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[DATA_CLIENT] ShipD taxonomy request failed: {StatusCode}", response.StatusCode);
                return Array.Empty<ShipDVesselTaxonomyDto>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize<List<ShipDVesselTaxonomyDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return payload ?? new List<ShipDVesselTaxonomyDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DATA_CLIENT] Failed to fetch ShipD taxonomy metadata");
            return Array.Empty<ShipDVesselTaxonomyDto>();
        }
    }

    public async Task<VesselDetailsDto?> ImportHydrostaticsVesselAsync(
        HydrostaticsImportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/v1/hydrostatics/vessels/import-from-sizing",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("[DATA_CLIENT] Import vessel failed: {StatusCode} - {Body}", response.StatusCode, body);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<VesselDetailsDto>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DATA_CLIENT] Failed to import Hydrostatics vessel");
            return null;
        }
    }
}
