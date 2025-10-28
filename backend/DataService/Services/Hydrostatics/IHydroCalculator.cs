using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for hydrostatic calculations
/// </summary>
public interface IHydroCalculator
{
    /// <summary>
    /// Computes hydrostatic properties at a single draft
    /// </summary>
    Task<HydroResultDto> ComputeAtDraftAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal draft,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes hydrostatic table for multiple drafts
    /// </summary>
    Task<List<HydroResultDto>> ComputeTableAsync(
        Guid vesselId,
        Guid? loadcaseId,
        List<decimal> drafts,
        CancellationToken cancellationToken = default);
}

