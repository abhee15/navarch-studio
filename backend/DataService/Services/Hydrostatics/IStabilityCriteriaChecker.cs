using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for checking intact stability criteria per IMO A.749(18)
/// </summary>
public interface IStabilityCriteriaChecker
{
    /// <summary>
    /// Checks all IMO A.749 basic intact stability criteria
    /// </summary>
    /// <param name="curve">GZ curve to check</param>
    /// <returns>Criteria check results</returns>
    StabilityCriteriaResultDto CheckIntactStabilityCriteria(StabilityCurveDto curve);

    /// <summary>
    /// Calculates area under GZ curve between two angles using trapezoidal integration
    /// </summary>
    /// <param name="points">Curve points</param>
    /// <param name="fromAngle">Start angle (degrees)</param>
    /// <param name="toAngle">End angle (degrees)</param>
    /// <returns>Area in m·rad</returns>
    decimal CalculateAreaUnderCurve(List<StabilityPointDto> points, decimal fromAngle, decimal toAngle);

    /// <summary>
    /// Finds maximum GZ value and the angle at which it occurs
    /// </summary>
    /// <param name="points">Curve points</param>
    /// <returns>Tuple of (maxGZ, angleAtMaxGZ)</returns>
    (decimal maxGZ, decimal angleAtMaxGZ) FindMaxGZ(List<StabilityPointDto> points);

    /// <summary>
    /// Interpolates GZ value at a specific angle
    /// </summary>
    /// <param name="points">Curve points</param>
    /// <param name="angle">Angle in degrees</param>
    /// <returns>Interpolated GZ value</returns>
    decimal InterpolateGZ(List<StabilityPointDto> points, decimal angle);
}








