using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for generating hull projection views (waterlines, buttocks, body plan)
/// </summary>
public interface IHullProjectionsService
{
    /// <summary>
    /// Generate waterline projections (XY views at specific Z-levels)
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Waterline curves at each waterline height</returns>
    Task<WaterlinesDto> GetWaterlinesAsync(Guid vesselId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate buttock projections (XZ views at specific Y-offsets)
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="numButtocks">Number of buttock lines to generate (default: based on max half-breadth)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Buttock curves at each Y-offset</returns>
    Task<ButtocksDto> GetButtocksAsync(Guid vesselId, int? numButtocks = null, CancellationToken cancellationToken = default);
}
