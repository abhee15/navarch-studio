using HullSizingService.Services.Integration;

namespace HullSizingService.Services;

/// <summary>
/// Service for retrieving water properties with caching and stale fallback
/// </summary>
public interface IWaterPropertiesService
{
    /// <summary>
    /// Get water properties with 12h cache and stale fallback if DataService is down
    /// </summary>
    Task<WaterPropertiesResponse> GetWaterPropertiesAsync(
        decimal temperatureCelsius,
        decimal salinityPpt,
        CancellationToken cancellationToken = default);
}




