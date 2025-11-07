using Shared.DTOs.Sizing;

namespace HullSizingService.Services;

/// <summary>
/// Service for parametric design space exploration
/// Generates multiple design variants by sweeping parameter ranges
/// </summary>
public interface IDesignSpaceExplorationService
{
    /// <summary>
    /// Starts a design space exploration by generating variants across parameter ranges
    /// </summary>
    Task<DesignSpaceExplorationResponse> ExploreAsync(
        DesignSpaceExplorationRequest request,
        Guid userId,
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the results of an exploration batch
    /// </summary>
    Task<ExplorationResultsSummary> GetExplorationResultsAsync(
        Guid batchId,
        string tenantId,
        CancellationToken cancellationToken = default);
}
