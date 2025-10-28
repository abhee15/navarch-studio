using Asp.Versioning;
using DataService.Services.Hydrostatics;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace DataService.Controllers;

/// <summary>
/// Controller for stability calculations (GZ/KN curves, criteria checking)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/stability/vessels/{vesselId}")]
public class StabilityController : ControllerBase
{
    private readonly IStabilityCalculator _stabilityCalculator;
    private readonly IStabilityCriteriaChecker _criteriaChecker;
    private readonly ILogger<StabilityController> _logger;

    public StabilityController(
        IStabilityCalculator stabilityCalculator,
        IStabilityCriteriaChecker criteriaChecker,
        ILogger<StabilityController> logger)
    {
        _stabilityCalculator = stabilityCalculator;
        _criteriaChecker = criteriaChecker;
        _logger = logger;
    }

    /// <summary>
    /// Generates GZ curve for a vessel at specified loadcase
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="request">Stability computation request (loadcase, angles, method)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete stability curve with GZ/KN values</returns>
    [HttpPost("gz-curve")]
    [ProducesResponseType(typeof(StabilityCurveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateGZCurve(
        Guid vesselId,
        [FromBody] StabilityRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Generating GZ curve for vessel {VesselId} using {Method} method ({MinAngle}° to {MaxAngle}° @ {Increment}°)",
                vesselId, request.Method, request.MinAngle, request.MaxAngle, request.AngleIncrement);

            var result = await _stabilityCalculator.ComputeGZCurveAsync(vesselId, request, cancellationToken);

            _logger.LogInformation(
                "GZ curve generated for vessel {VesselId}: {PointCount} points, max GZ = {MaxGZ}m @ {AngleAtMaxGZ}°, computed in {ComputationTime}ms",
                vesselId, result.Points.Count, result.MaxGZ, result.AngleAtMaxGZ, result.ComputationTimeMs);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for GZ curve generation: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot compute GZ curve: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating GZ curve for vessel {VesselId}", vesselId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while computing stability curve" });
        }
    }

    /// <summary>
    /// Generates KN curve for a vessel at specified loadcase
    /// Note: KN values are included in GZ curve response; this endpoint returns same data with focus on KN
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="request">Stability computation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stability curve with KN values</returns>
    [HttpPost("kn-curve")]
    [ProducesResponseType(typeof(StabilityCurveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateKNCurve(
        Guid vesselId,
        [FromBody] StabilityRequestDto request,
        CancellationToken cancellationToken)
    {
        // KN curve computation is same as GZ (both are computed together)
        // This endpoint is provided for clarity/convenience
        return await GenerateGZCurve(vesselId, request, cancellationToken);
    }

    /// <summary>
    /// Checks IMO A.749(18) intact stability criteria for a vessel
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="request">Stability computation request (must include loadcase for KG)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Criteria check results with pass/fail for each criterion</returns>
    [HttpPost("check-criteria")]
    [ProducesResponseType(typeof(StabilityCriteriaCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckStabilityCriteria(
        Guid vesselId,
        [FromBody] StabilityRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Checking stability criteria for vessel {VesselId} with loadcase {LoadcaseId}",
                vesselId, request.LoadcaseId);

            // First compute GZ curve
            var curve = await _stabilityCalculator.ComputeGZCurveAsync(vesselId, request, cancellationToken);

            // Then check criteria
            var criteriaResult = _criteriaChecker.CheckIntactStabilityCriteria(curve);

            _logger.LogInformation(
                "Stability criteria checked for vessel {VesselId}: {PassedCount}/{TotalCount} criteria passed",
                vesselId, criteriaResult.Criteria.Count(c => c.Passed), criteriaResult.Criteria.Count);

            return Ok(new StabilityCriteriaCheckResponse
            {
                Curve = curve,
                Criteria = criteriaResult
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for criteria check: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking stability criteria for vessel {VesselId}", vesselId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while checking stability criteria" });
        }
    }

    /// <summary>
    /// Gets available stability calculation methods
    /// </summary>
    /// <returns>List of available methods with descriptions</returns>
    [HttpGet("methods")]
    [ProducesResponseType(typeof(List<StabilityMethodDto>), StatusCodes.Status200OK)]
    public IActionResult GetAvailableMethods()
    {
        var methods = _stabilityCalculator.GetAvailableMethods();
        return Ok(methods);
    }
}

/// <summary>
/// Response combining GZ curve and criteria check results
/// </summary>
public record StabilityCriteriaCheckResponse
{
    /// <summary>
    /// Complete GZ curve
    /// </summary>
    public StabilityCurveDto Curve { get; init; } = new();

    /// <summary>
    /// Criteria check results
    /// </summary>
    public StabilityCriteriaResultDto Criteria { get; init; } = new();
}

