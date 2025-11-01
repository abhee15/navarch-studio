using Asp.Versioning;
using DataService.Services.Hydrostatics;
using Microsoft.AspNetCore.Mvc;

namespace DataService.Controllers;

/// <summary>
/// Controller for hydrostatic curves generation
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hydrostatics/vessels/{vesselId}/curves")]
public class CurvesController : ControllerBase
{
    private readonly ICurvesGenerator _curvesGenerator;
    private readonly ILogger<CurvesController> _logger;

    public CurvesController(
        ICurvesGenerator curvesGenerator,
        ILogger<CurvesController> logger)
    {
        _curvesGenerator = curvesGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Gets available curve types
    /// </summary>
    [HttpGet("types")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetCurveTypes()
    {
        var types = new[]
        {
            "displacement",
            "kb",
            "lcb",
            "awp",
            "gmt",
            "bonjean"
        };

        return Ok(new { curve_types = types });
    }

    /// <summary>
    /// Generates multiple curves
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateCurves(
        Guid vesselId,
        [FromBody] GenerateCurvesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var curves = await _curvesGenerator.GenerateMultipleCurvesAsync(
                vesselId,
                request.LoadcaseId,
                request.Types,
                request.MinDraft,
                request.MaxDraft,
                request.Points,
                cancellationToken);

            return Ok(new { curves });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error generating curves for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation generating curves for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating curves for vessel {VesselId}", vesselId);
            return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets Bonjean curves (sectional area vs draft)
    /// </summary>
    [HttpGet("bonjean")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBonjeanCurves(
        Guid vesselId,
        CancellationToken cancellationToken)
    {
        try
        {
            var curves = await _curvesGenerator.GenerateBonjeanCurvesAsync(vesselId, cancellationToken);
            return Ok(new { curves });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error generating Bonjean curves for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation generating Bonjean curves for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating Bonjean curves for vessel {VesselId}", vesselId);
            return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
        }
    }
}

/// <summary>
/// Request model for generating curves
/// </summary>
public record GenerateCurvesRequest
{
    public Guid? LoadcaseId { get; init; }
    public List<string> Types { get; init; } = new();
    public decimal MinDraft { get; init; }
    public decimal MaxDraft { get; init; }
    public int Points { get; init; } = 100;
}

