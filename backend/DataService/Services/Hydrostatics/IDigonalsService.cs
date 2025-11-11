using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for computing diagonal curves (45° lines from baseline) for hull fairing validation
/// </summary>
public interface IDigonalsService
{
    /// <summary>
    /// Computes diagonal curves for a vessel
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="numDiagonals">Number of diagonals to compute (typically 3-5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of diagonal curves</returns>
    Task<DiagonalsDto> GetDiagonalsAsync(Guid vesselId, int numDiagonals, CancellationToken cancellationToken);
}
