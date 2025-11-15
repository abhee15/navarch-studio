namespace HullSizingService.Services.Solver;

/// <summary>
/// Service for displacement closure using Newton-Raphson iteration
/// </summary>
public interface IDisplacementClosureService
{
    /// <summary>
    /// Solve for hull dimensions that match target displacement within tolerance
    /// </summary>
    /// <param name="request">Closure request with target displacement and constraints</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Closure result with converged dimensions or error flags</returns>
    Task<ClosureResult> SolveAsync(ClosureRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request for displacement closure
/// </summary>
public record ClosureRequest(
    decimal TargetDisplacementT,
    decimal WaterDensityKgM3,
    decimal FroudeNumber,
    decimal LOverB,
    decimal BOverT,
    decimal DOverT,
    decimal Cb,
    decimal? MaxBeamM,
    decimal? MaxDraftM,
    decimal? MaxLoaM,
    bool KeepLOverB,
    bool KeepBOverT,
    bool KeepCb,
    decimal LOverBMin,
    decimal LOverBMax,
    decimal BOverTMin,
    decimal BOverTMax,
    decimal CbMin,
    decimal CbMax
);

/// <summary>
/// Result of displacement closure
/// </summary>
public record ClosureResult(
    decimal LppM,
    decimal BeamM,
    decimal DraftM,
    decimal DepthM,
    decimal Cb,
    decimal DisplacementT,
    decimal DisplacementError,
    bool Converged,
    int Iterations,
    List<string> Flags
);















