namespace HullSizingService.Services.Geometry;

/// <summary>
/// Interface for hull optimization service
/// Provides inverse design optimization to meet target hydrostatic properties
/// </summary>
public interface IHullOptimizationService
{
    /// <summary>
    /// Optimizes hull geometry to meet target CB, CP, and LCB values
    /// </summary>
    /// <param name="targetCb">Target block coefficient</param>
    /// <param name="targetCp">Target prismatic coefficient</param>
    /// <param name="targetLcbPercent">Target LCB as % of Lpp from midship (positive = forward)</param>
    /// <param name="lppM">Length between perpendiculars (m)</param>
    /// <param name="beamM">Maximum beam (m)</param>
    /// <param name="draftM">Design draft (m)</param>
    /// <param name="initialGuess">Optional initial Control Point Grid guess (if null, generates from targets)</param>
    /// <param name="options">Optional optimization options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimization result with optimal Control Point Grid</returns>
    Task<HullOptimizationService.OptimizationResult> OptimizeAsync(
        decimal targetCb,
        decimal targetCp,
        decimal targetLcbPercent,
        decimal lppM,
        decimal beamM,
        decimal draftM,
        NurbsSurfaceGenerator.ControlPointGrid? initialGuess = null,
        HullOptimizationService.OptimizationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates hull offsets from optimized Control Point Grid
    /// </summary>
    /// <param name="controlPoints">Optimized Control Point Grid</param>
    /// <param name="lppM">Length between perpendiculars (m)</param>
    /// <param name="beamM">Maximum beam (m)</param>
    /// <param name="draftM">Design draft (m)</param>
    /// <param name="numStations">Number of stations to generate</param>
    /// <param name="numWaterlines">Number of waterlines to generate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Hull sections in standard format</returns>
    Task<HullSectionsDto> GenerateSectionsFromOptimizedGridAsync(
        NurbsSurfaceGenerator.ControlPointGrid controlPoints,
        decimal lppM,
        decimal beamM,
        decimal draftM,
        int numStations = 21,
        int numWaterlines = 13,
        CancellationToken cancellationToken = default);
}

