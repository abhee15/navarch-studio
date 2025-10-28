using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for computing stability curves (GZ, KN) and related parameters
/// </summary>
public interface IStabilityCalculator
{
    /// <summary>
    /// Computes GZ curve for a vessel at specified draft and loadcase
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="request">Stability computation request with angles and method</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete stability curve with GZ/KN values</returns>
    Task<StabilityCurveDto> ComputeGZCurveAsync(
        Guid vesselId,
        StabilityRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes GZ using wall-sided formula (fast, small angles only)
    /// </summary>
    /// <param name="gmt">Initial transverse metacentric height (m)</param>
    /// <param name="bmt">Transverse metacentric radius (m)</param>
    /// <param name="heelAngle">Heel angle in degrees</param>
    /// <returns>GZ value in meters</returns>
    decimal ComputeGZWallSided(decimal gmt, decimal bmt, decimal heelAngle);

    /// <summary>
    /// Computes GZ using full immersion/emersion method (accurate, all angles)
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="draft">Draft in meters</param>
    /// <param name="kg">Center of gravity height (m)</param>
    /// <param name="heelAngle">Heel angle in degrees</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>GZ value in meters</returns>
    Task<decimal> ComputeGZFullMethodAsync(
        Guid vesselId,
        decimal draft,
        decimal kg,
        decimal heelAngle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available stability calculation methods
    /// </summary>
    /// <returns>List of available methods with descriptions</returns>
    List<StabilityMethodDto> GetAvailableMethods();
}




