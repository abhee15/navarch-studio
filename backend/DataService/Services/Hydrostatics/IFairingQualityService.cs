using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for analyzing hull fairing quality through curvature analysis
/// </summary>
public interface IFairingQualityService
{
    /// <summary>
    /// Analyzes fairing quality for all stations of a vessel
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Fairing quality analysis with scores and flagged regions</returns>
    Task<FairingQualityDto> AnalyzeFairingQualityAsync(Guid vesselId, CancellationToken cancellationToken);
}
