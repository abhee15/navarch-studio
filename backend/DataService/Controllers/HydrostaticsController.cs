using System.Diagnostics;
using Asp.Versioning;
using DataService.Services.Hydrostatics;
using Microsoft.AspNetCore.Mvc;

namespace DataService.Controllers;

/// <summary>
/// Controller for hydrostatic calculations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hydrostatics/vessels/{vesselId}/compute")]
public class HydrostaticsController : ControllerBase
{
    private readonly IHydroCalculator _hydroCalculator;
    private readonly ITrimSolver _trimSolver;
    private readonly ILogger<HydrostaticsController> _logger;

    public HydrostaticsController(
        IHydroCalculator hydroCalculator,
        ITrimSolver trimSolver,
        ILogger<HydrostaticsController> logger)
    {
        _hydroCalculator = hydroCalculator;
        _trimSolver = trimSolver;
        _logger = logger;
    }

    /// <summary>
    /// Computes hydrostatic table for multiple drafts
    /// </summary>
    [HttpPost("table")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ComputeTable(
        Guid vesselId,
        [FromBody] ComputeTableRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            var results = await _hydroCalculator.ComputeTableAsync(
                vesselId,
                request.LoadcaseId,
                request.Drafts,
                cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Computed hydrostatic table for vessel {VesselId}: {Count} drafts in {ElapsedMs}ms",
                vesselId, results.Count, stopwatch.ElapsedMilliseconds);

            return Ok(new
            {
                results,
                computation_time_ms = stopwatch.ElapsedMilliseconds
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error computing hydrostatic table for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation computing hydrostatic table for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Computes hydrostatics at a single draft
    /// </summary>
    [HttpPost("single")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ComputeSingle(
        Guid vesselId,
        [FromBody] ComputeSingleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _hydroCalculator.ComputeAtDraftAsync(
                vesselId,
                request.LoadcaseId,
                request.Draft,
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error computing hydrostatics at draft {Draft} for vessel {VesselId}",
                request.Draft, vesselId);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation computing hydrostatics for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Solves for trim condition (forward and aft drafts) to achieve target displacement
    /// Uses Newton-Raphson iteration method
    /// </summary>
    [HttpPost("trim")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SolveTrim(
        Guid vesselId,
        [FromBody] SolveTrimRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            var result = await _trimSolver.SolveForDisplacementAsync(
                vesselId,
                request.LoadcaseId,
                request.TargetDisplacement,
                request.InitialDraftFwd ?? 5.0m,  // Default 5m if not provided
                request.InitialDraftAft ?? 5.0m,  // Default 5m if not provided
                request.MaxIterations ?? 20,
                request.Tolerance ?? 100m,
                cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Solved trim for vessel {VesselId}: {Converged} in {Iterations} iterations ({ElapsedMs}ms)",
                vesselId, result.Converged, result.Iterations, stopwatch.ElapsedMilliseconds);

            return Ok(new
            {
                result,
                computation_time_ms = stopwatch.ElapsedMilliseconds
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error solving trim for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Vessel {VesselId} not found", vesselId);
            return NotFound(new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request model for computing hydrostatic table
/// </summary>
public record ComputeTableRequest
{
    public Guid? LoadcaseId { get; init; }
    public List<decimal> Drafts { get; init; } = new();
}

/// <summary>
/// Request model for computing hydrostatics at single draft
/// </summary>
public record ComputeSingleRequest
{
    public Guid? LoadcaseId { get; init; }
    public decimal Draft { get; init; }
}

/// <summary>
/// Request model for trim solver
/// </summary>
public record SolveTrimRequest
{
    public Guid? LoadcaseId { get; init; }
    public decimal TargetDisplacement { get; init; }
    public decimal? InitialDraftFwd { get; init; }
    public decimal? InitialDraftAft { get; init; }
    public int? MaxIterations { get; init; }
    public decimal? Tolerance { get; init; }
}

