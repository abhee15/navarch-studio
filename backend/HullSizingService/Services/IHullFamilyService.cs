using Shared.Models.Sizing;

namespace HullSizingService.Services;

/// <summary>
/// Service for querying hull family presets
/// </summary>
public interface IHullFamilyService
{
    /// <summary>
    /// Get hull families suitable for the mission
    /// </summary>
    Task<List<HullFamilyPreset>> GetApplicableFamiliesAsync(
        string missionType,
        decimal? targetFn,
        decimal? maxDraft,
        decimal? maxBeam,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific family by name
    /// </summary>
    Task<HullFamilyPreset?> GetByFamilyAsync(string family, CancellationToken cancellationToken = default);
}


