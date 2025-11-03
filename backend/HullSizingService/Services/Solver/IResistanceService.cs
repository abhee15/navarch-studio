namespace HullSizingService.Services.Solver;

/// <summary>
/// Service for resistance and powering calculations using Holtrop-Mennen method (simplified for MVP)
/// </summary>
public interface IResistanceService
{
    /// <summary>
    /// Calculate resistance and power for a hull at given speed
    /// </summary>
    Task<ResistanceResult> CalculateAsync(ResistanceRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request for resistance calculation
/// </summary>
public record ResistanceRequest(
    decimal LppM,
    decimal LwlM,
    decimal BeamM,
    decimal DraftM,
    decimal Cb,
    decimal Cp,
    decimal Cwp,
    decimal Cm,
    decimal SpeedKn,
    decimal WaterDensityKgM3,
    decimal KinematicViscosityM2S
);

/// <summary>
/// Result of resistance calculation
/// </summary>
public record ResistanceResult(
    decimal Fn,
    decimal Rn,
    decimal Cf,
    decimal FormFactor,
    decimal FrictionalResistanceKn,
    decimal WaveResistanceKn,
    decimal TotalResistanceKn,
    decimal EhpKw,
    decimal ShpKw
);

