using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for computing section area curve (SAC) showing volume distribution along hull length
/// </summary>
public interface ISectionAreaCurveService
{
    /// <summary>
    /// Computes the section area curve for a vessel
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Section area curve data</returns>
    Task<SectionAreaCurveDto> GetSectionAreaCurveAsync(Guid vesselId, CancellationToken cancellationToken);
}
