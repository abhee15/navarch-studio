using HullSizingService.Services.Integration;
using Microsoft.Extensions.Caching.Memory;

namespace HullSizingService.Services;

/// <summary>
/// Cached water properties service with stale fallback
/// </summary>
public class WaterPropertiesService : IWaterPropertiesService
{
    private readonly IDataServiceClient _dataClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WaterPropertiesService> _logger;

    // Default seawater properties at 15°C, 35ppt (ISO standard)
    private static readonly WaterPropertiesResponse DefaultProperties = new(
        DensityKgM3: 1025.87m,
        KinematicViscosityM2S: 0.000001188m,
        TemperatureCelsius: 15.0m,
        SalinityPpt: 35.0m
    );

    public WaterPropertiesService(
        IDataServiceClient dataClient,
        IMemoryCache cache,
        ILogger<WaterPropertiesService> logger)
    {
        _dataClient = dataClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<WaterPropertiesResponse> GetWaterPropertiesAsync(
        decimal temperatureCelsius,
        decimal salinityPpt,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"water_{temperatureCelsius:F1}_{salinityPpt:F1}";

        // Try to get from cache (12h TTL)
        if (_cache.TryGetValue<WaterPropertiesResponse>(cacheKey, out var cached))
        {
            _logger.LogDebug("[WATER_CACHE] HIT for {CacheKey}", cacheKey);
            return cached!;
        }

        _logger.LogDebug("[WATER_CACHE] MISS for {CacheKey}, fetching from DataService", cacheKey);

        // Fetch from DataService
        var properties = await _dataClient.GetWaterPropertiesAsync(temperatureCelsius, salinityPpt, cancellationToken);

        if (properties == null)
        {
            _logger.LogWarning("[WATER_CACHE] DataService unavailable, returning default properties");
            return DefaultProperties;
        }

        // Cache for 12 hours
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12),
            SlidingExpiration = TimeSpan.FromHours(6)
        };

        _cache.Set(cacheKey, properties, cacheOptions);
        _logger.LogInformation("[WATER_CACHE] Cached water properties for {CacheKey} (12h TTL)", cacheKey);

        return properties;
    }
}
