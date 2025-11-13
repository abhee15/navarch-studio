using System.Collections.Generic;
using Shared.Models.Sizing;

namespace HullSizingService.Services.Solver;

/// <summary>
/// First-principles hull sizing solver (orchestrator)
/// Coordinates displacement closure, resistance, and stability calculations
/// </summary>
public interface IFirstPrinciplesSolver
{
    /// <summary>
    /// Generate candidate hull designs from mission requirements
    /// Returns tuple of (Candidates, Diagnostics)
    /// </summary>
    Task<(List<SolverCandidate> Candidates, SolverDiagnostics Diagnostics)> SolveAsync(SolverRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request for first-principles solver
/// </summary>
public record SolverRequest(
    MissionCase MissionCase,
    SizingLocksDto? Locks,
    SizingOptionsDto? Options
);

/// <summary>
/// Lock configuration for solver
/// </summary>
public record SizingLocksDto(
    bool KeepFn,
    bool KeepLOverB,
    bool KeepBOverT,
    bool KeepDOverT,
    bool KeepCbBand
);

/// <summary>
/// Solver options
/// </summary>
public record SizingOptionsDto(
    List<string>? FamilyHints,
    int MaxCandidates,
    decimal? MinFn,
    decimal? MaxFn,
    IDictionary<string, object>? AdditionalParameters
);

/// <summary>
/// Candidate hull from solver (before saving to DB)
/// </summary>
public record SolverCandidate(
    string HullFamily,
    decimal LppM,
    decimal LwlM,
    decimal LoaM,
    decimal BeamM,
    decimal DraftM,
    decimal DepthM,
    decimal Cb,
    decimal Cp,
    decimal Cwp,
    decimal Cm,
    decimal DisplacementT,
    decimal Fn,
    decimal? LwlOverLambda,
    decimal? KbM,
    decimal? LcbPctLpp,
    decimal? GmEstM,
    decimal? EhpKw,
    decimal? ShpKw,
    decimal Score,
    List<string> Flags,
    // Data-Driven provenance (optional)
    string? ReferenceVesselId = null,
    string? ReferenceVesselName = null,
    decimal? SimilarityScore = null,
    string? SolverMode = null
);
