using Asp.Versioning;
using DataService.Services.Hydrostatics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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
    private readonly ILogger<HydrostaticsController> _logger;

    public HydrostaticsController(
        IHydroCalculator hydroCalculator,
        ILogger<HydrostaticsController> logger)
    {
        _hydroCalculator = hydroCalculator;
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

