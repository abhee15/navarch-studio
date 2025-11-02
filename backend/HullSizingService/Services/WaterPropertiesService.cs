using HullSizingService.Services.Integration;
using Microsoft.Extensions.Caching.Memory;

namespace HullSizingService.Services;

/// <summary>
/// Service for retrieving water properties with caching and fallback
/// </summary>
public interface IWaterPropertiesService
{
    /// <summary>
    /// Get water properties (with caching and stale fallback if DataService is down)
    /// </summary>
    Task<WaterPropertiesResponse> GetPropertiesAsync(
        decimal tempC,
        decimal salinityPsu,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of water properties service with read-through cache
/// </summary>
public class WaterPropertiesService : IWaterPropertiesService
{
    private readonly IMemoryCache _cache;
    private readonly IDataServiceClient _dataServiceClient;
    private readonly ILogger<WaterPropertiesService> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(12);
    private const string CacheKeyPrefix = "water_props";
    private const string StaleCacheKeySuffix = "_stale";

    public WaterPropertiesService(
        IMemoryCache cache,
        IDataServiceClient dataServiceClient,
        ILogger<WaterPropertiesService> logger)
    {
        _cache = cache;
        _dataServiceClient = dataServiceClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<WaterPropertiesResponse> GetPropertiesAsync(
        decimal tempC,
        decimal salinityPsu,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}_{tempC}_{salinityPsu}";
        var staleCacheKey = $"{cacheKey}{StaleCacheKeySuffix}";

        // Try to get from cache (fresh, within TTL)
        if (_cache.TryGetValue(cacheKey, out WaterPropertiesResponse? cached))
        {
            _logger.LogDebug(
                "[WATER_CACHE] Cache hit: temp={TempC}°C, salinity={SalinityPsu} PSU",
                tempC, salinityPsu);
            return cached!;
        }

        // Cache miss - fetch from DataService
        _logger.LogInformation(
            "[WATER_CACHE] Cache miss: fetching from DataService (temp={TempC}°C, salinity={SalinityPsu} PSU)",
            tempC, salinityPsu);

        try
        {
            var result = await _dataServiceClient.GetWaterPropertiesAsync(tempC, salinityPsu, cancellationToken);

            // Store in cache with TTL
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl,
                Priority = CacheItemPriority.High
            };
            _cache.Set(cacheKey, result, cacheOptions);

            // Also store as "stale" cache (no expiration) for fallback
            var staleCacheOptions = new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.Low // Lower priority so it can be evicted if memory is low
            };
            _cache.Set(staleCacheKey, result, staleCacheOptions);

            _logger.LogInformation(
                "[WATER_CACHE] Cached water properties: rho={Rho} kg/m³, nu={Nu} m²/s (TTL={Ttl}h)",
                result.RhoKgM3, result.NuM2S, CacheTtl.TotalHours);

            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutException)
        {
            // DataService is down or slow - try to serve stale cache
            _logger.LogWarning(ex,
                "[WATER_CACHE] DataService unavailable ({ErrorType}), attempting stale cache fallback",
                ex.GetType().Name);

            if (_cache.TryGetValue(staleCacheKey, out WaterPropertiesResponse? stale))
            {
                _logger.LogWarning(
                    "[WATER_CACHE] Serving STALE cache: temp={TempC}°C, salinity={SalinityPsu} PSU, rho={Rho} kg/m³",
                    tempC, salinityPsu, stale!.RhoKgM3);

                // Re-cache as fresh (to avoid repeated fallback attempts)
                var refreshCacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5), // Short TTL, retry DataService soon
                    Priority = CacheItemPriority.High
                };
                _cache.Set(cacheKey, stale, refreshCacheOptions);

                return stale!;
            }

            // No stale cache available - propagate exception
            _logger.LogError(ex,
                "[WATER_CACHE] DataService unavailable and no stale cache found - failing request");
            throw;
        }
    }
}

