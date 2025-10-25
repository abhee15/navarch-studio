using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Interface for trim solver service
/// Solves for equilibrium trim condition (T_AP, T_FP) given target displacement
/// </summary>
public interface ITrimSolver
{
    /// <summary>
    /// Solves for forward and aft drafts that achieve target displacement
    /// Uses Newton-Raphson iteration to find equilibrium trim condition
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="loadcaseId">Loadcase ID (optional, uses default if null)</param>
    /// <param name="targetDisplacement">Target displacement in kg</param>
    /// <param name="initialDraftFwd">Initial guess for forward draft (m)</param>
    /// <param name="initialDraftAft">Initial guess for aft draft (m)</param>
    /// <param name="maxIterations">Maximum iterations (default 20)</param>
    /// <param name="tolerance">Convergence tolerance in kg (default 100kg = 0.01%)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Trim solution with drafts and hydrostatic properties</returns>
    Task<TrimSolutionDto> SolveForDisplacementAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal targetDisplacement,
        decimal initialDraftFwd,
        decimal initialDraftAft,
        int maxIterations = 20,
        decimal tolerance = 100m,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that target displacement is within vessel's capacity
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="targetDisplacement">Target displacement in kg</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if achievable, false otherwise</returns>
    Task<bool> IsDisplacementAchievableAsync(
        Guid vesselId,
        decimal targetDisplacement,
        CancellationToken cancellationToken = default);
}

