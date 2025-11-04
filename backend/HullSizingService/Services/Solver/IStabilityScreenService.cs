namespace HullSizingService.Services.Solver;

/// <summary>
/// Service for quick stability screening (preliminary design estimates)
/// </summary>
public interface IStabilityScreenService
{
    /// <summary>
    /// Perform quick stability check and estimate GMt
    /// </summary>
    Task<StabilityResult> ScreenAsync(StabilityRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request for stability screening
/// </summary>
public record StabilityRequest(
    decimal LwlM,
    decimal BeamM,
    decimal DraftM,
    decimal DepthM,
    decimal Cb,
    decimal Cwp,
    string VesselType, // container, tanker, bulker, etc.
    decimal DisplacementT
);

/// <summary>
/// Result of stability screening
/// </summary>
public record StabilityResult(
    decimal KbM,
    decimal BmtM,
    decimal KgEstM,
    decimal GmEstM,
    decimal TRollSeconds,
    List<string> Flags
);


