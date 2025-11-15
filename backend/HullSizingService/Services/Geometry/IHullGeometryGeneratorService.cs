using Shared.DTOs;
using Shared.Models.Sizing;

namespace HullSizingService.Services.Geometry;

/// <summary>
/// Service for generating hull geometry (offsets) from solver candidates
/// Uses form-coefficient-based parametric generation
/// </summary>
public interface IHullGeometryGeneratorService
{
    /// <summary>
    /// Generate offsets grid from a solver candidate
    /// </summary>
    /// <param name="candidate">Solver candidate with form coefficients</param>
    /// <param name="numStations">Number of stations (default: 23 for BSRA-compatible)</param>
    /// <param name="numWaterlines">Number of waterlines (default: 13)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Offsets grid DTO, or null if generation fails</returns>
    Task<OffsetsGridDto?> GenerateOffsetsFromCandidateAsync(
        Solver.SolverCandidate candidate,
        int numStations = 23,
        int numWaterlines = 13,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that generated offsets match solver form coefficients
    /// </summary>
    /// <param name="candidate">Original solver candidate</param>
    /// <param name="offsets">Generated offsets</param>
    /// <param name="tolerance">Tolerance for comparison (default: 0.10 = 10%)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with computed coefficients and errors</returns>
    Task<GeometryValidationResult> ValidateFormCoefficientsAsync(
        Solver.SolverCandidate candidate,
        OffsetsGridDto offsets,
        decimal tolerance = 0.10m,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of form coefficient validation
/// </summary>
public record GeometryValidationResult
{
    public bool IsValid { get; init; }
    public decimal ComputedCb { get; init; }
    public decimal ComputedCp { get; init; }
    public decimal ComputedCm { get; init; }
    public decimal ComputedCwp { get; init; }
    public decimal? ComputedLcbPercent { get; init; }
    public decimal CbError { get; init; }
    public decimal CpError { get; init; }
    public decimal CmError { get; init; }
    public decimal CwpError { get; init; }
    public decimal? LcbError { get; init; }
    public List<string> Warnings { get; init; } = new();
}
