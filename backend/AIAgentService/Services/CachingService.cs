using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIAgentService.Models.Responses;
using Microsoft.Extensions.Caching.Memory;

namespace AIAgentService.Services;

public class CachingService : ICachingService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public CachingService(IMemoryCache cache, ILogger<CachingService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<MissionResult?> GetCachedMissionAsync(string input)
    {
        var cacheKey = GenerateCacheKey(input);

        if (_cache.TryGetValue(cacheKey, out MissionResult? cached))
        {
            _logger.LogInformation("Cache hit for mission request");
            return Task.FromResult<MissionResult?>(cached);
        }

        _logger.LogDebug("Cache miss for mission request");
        return Task.FromResult<MissionResult?>(null);
    }

    public Task CacheMissionAsync(string input, MissionResult result)
    {
        var cacheKey = GenerateCacheKey(input);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration,
            Size = 1 // Each entry counts as 1 unit
        };

        _cache.Set(cacheKey, result, cacheOptions);
        _logger.LogInformation("Cached mission result for key {Key}", cacheKey.Substring(0, 16) + "...");

        return Task.CompletedTask;
    }

    private string GenerateCacheKey(string input)
    {
        // Normalize input (lowercase, trim, remove extra whitespace)
        var normalized = input.ToLower().Trim();
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");

        // Generate SHA256 hash
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        return $"ai:mission:{Convert.ToHexString(hashBytes)}";
    }
}
