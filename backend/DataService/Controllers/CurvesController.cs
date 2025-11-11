using Asp.Versioning;
using DataService.Services.Hydrostatics;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

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
    private readonly ISectionAreaCurveService _sectionAreaCurveService;
    private readonly IFairingQualityService _fairingQualityService;
    private readonly ILogger<CurvesController> _logger;

    public CurvesController(
        ICurvesGenerator curvesGenerator,
        ISectionAreaCurveService sectionAreaCurveService,
        IFairingQualityService fairingQualityService,
        ILogger<CurvesController> logger)
    {
        _curvesGenerator = curvesGenerator;
        _sectionAreaCurveService = sectionAreaCurveService;
        _fairingQualityService = fairingQualityService;
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

    /// <summary>
    /// Gets section area curve (volume distribution along hull length)
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Section area curve data</returns>
    [HttpGet("section-area-curve")]
    [ProducesResponseType(typeof(SectionAreaCurveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SectionAreaCurveDto>> GetSectionAreaCurve(
        Guid vesselId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting section area curve for vessel {VesselId}", vesselId);

        var result = await _sectionAreaCurveService.GetSectionAreaCurveAsync(vesselId, cancellationToken);

        if (result.StationPositions.Count == 0)
        {
            return NotFound(new { message = "No geometry data found for vessel. Import stations, waterlines, and offsets first." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets fairing quality analysis for all stations
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Fairing quality analysis</returns>
    [HttpGet("fairing-quality")]
    [ProducesResponseType(typeof(FairingQualityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FairingQualityDto>> GetFairingQuality(
        Guid vesselId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting fairing quality for vessel {VesselId}", vesselId);

        var result = await _fairingQualityService.AnalyzeFairingQualityAsync(vesselId, cancellationToken);

        return Ok(result);
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
