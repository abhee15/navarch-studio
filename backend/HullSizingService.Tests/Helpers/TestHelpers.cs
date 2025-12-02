using HullSizingService.Services.Solver;
using Shared.Models.Sizing;
using GeomGenStatus = Shared.Models.Sizing.GeometryGenerationStatus;

namespace HullSizingService.Tests.Helpers;

/// <summary>
/// Test helper utilities for mapping between test data types
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Maps a SolverCandidate (solver output) to CandidateDesign (entity model) for validation testing
    /// </summary>
    /// <param name="solver">The solver candidate to map</param>
    /// <param name="sizingRunId">The sizing run ID to associate with</param>
    /// <param name="rank">Optional rank (defaults to 1)</param>
    /// <returns>CandidateDesign entity ready for validation</returns>
    public static CandidateDesign MapSolverCandidateToCandidateDesign(
        SolverCandidate solver,
        Guid sizingRunId,
        int rank = 1)
    {
        return new CandidateDesign
        {
            Id = Guid.NewGuid(),
            SizingRunId = sizingRunId,
            HullFamily = solver.HullFamily,
            LppM = solver.LppM,
            LwlM = solver.LwlM,
            LoaM = solver.LoaM,
            BM = solver.BeamM, // CandidateDesign uses BM, SolverCandidate uses BeamM
            TM = solver.DraftM, // CandidateDesign uses TM, SolverCandidate uses DraftM
            DM = solver.DepthM, // CandidateDesign uses DM, SolverCandidate uses DepthM
            Cb = solver.Cb,
            Cp = solver.Cp,
            Cm = solver.Cm,
            Cwp = solver.Cwp,
            DisplacementT = solver.DisplacementT,
            Fn = solver.Fn,
            LcbPctLpp = solver.LcbPctLpp,
            KbM = solver.KbM,
            Score = solver.Score,
            Rank = rank,
            IsSelected = false,
            GeometryJson = null, // Geometry not populated by solver directly
            ShipdParametersJson = null, // SolverCandidate doesn't have this
            GeometryGenerationStatus = GeomGenStatus.Success, // Default to success
            GeometryGenerationError = null,
            ValidationResultsJson = null, // Will be populated by validation service
            CreatedAt = DateTime.UtcNow
            // Note: CandidateDesign doesn't have UpdatedAt property
        };
    }
}
