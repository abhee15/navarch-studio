using HullSizingService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Sizing;

namespace HullSizingService.Controllers;

/// <summary>
/// Controller for design space exploration
/// Allows parametric sweeps through design parameter ranges
/// </summary>
[ApiController]
[Route("api/v1/hull-sizing/exploration")]
public class DesignSpaceExplorationController : ControllerBase
{
    private readonly IDesignSpaceExplorationService _explorationService;
    private readonly ILogger<DesignSpaceExplorationController> _logger;

    public DesignSpaceExplorationController(
        IDesignSpaceExplorationService explorationService,
        ILogger<DesignSpaceExplorationController> logger)
    {
        _explorationService = explorationService;
        _logger = logger;
    }

    /// <summary>
    /// Starts a design space exploration by generating variants across parameter ranges
    /// </summary>
    /// <param name="request">Exploration parameters and ranges</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Exploration batch ID and status</returns>
    [HttpPost("start")]
    [ProducesResponseType(typeof(DesignSpaceExplorationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DesignSpaceExplorationResponse>> StartExploration(
        [FromBody] DesignSpaceExplorationRequest request,
        CancellationToken ct)
    {
        try
        {
            // Get user context
            var userIdStr = HttpContext.Items["Claims:Sub"]?.ToString();
            var userId = Guid.TryParse(userIdStr, out var parsedUserId) ? parsedUserId : Guid.NewGuid();
            var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-default-tenant";

            _logger.LogInformation(
                "[EXPLORATION] Starting exploration for mission {MissionId}, user {UserId}",
                request.MissionCaseId, userId);

            var result = await _explorationService.ExploreAsync(request, userId, tenantId, ct);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid exploration request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting exploration");
            return StatusCode(500, new { error = "An error occurred starting exploration" });
        }
    }

    /// <summary>
    /// Gets the results of an exploration batch
    /// </summary>
    /// <param name="batchId">Batch ID from exploration start</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Exploration results including all candidates</returns>
    [HttpGet("results/{batchId}")]
    [ProducesResponseType(typeof(ExplorationResultsSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExplorationResultsSummary>> GetResults(
        Guid batchId,
        CancellationToken ct)
    {
        try
        {
            var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-default-tenant";

            var results = await _explorationService.GetExplorationResultsAsync(batchId, tenantId, ct);

            if (results.Status == "not_found")
            {
                return NotFound(new { error = $"Exploration batch {batchId} not found" });
            }

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching exploration results for batch {BatchId}", batchId);
            return StatusCode(500, new { error = "An error occurred fetching results" });
        }
    }

    /// <summary>
    /// Gets the Pareto front from exploration results
    /// </summary>
    /// <param name="batchId">Batch ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of candidate IDs on the Pareto front</returns>
    [HttpGet("pareto/{batchId}")]
    [ProducesResponseType(typeof(List<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Guid>>> GetParetoFront(
        Guid batchId,
        CancellationToken ct)
    {
        try
        {
            var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-default-tenant";

            var results = await _explorationService.GetExplorationResultsAsync(batchId, tenantId, ct);

            if (results.Status == "not_found")
            {
                return NotFound(new { error = $"Exploration batch {batchId} not found" });
            }

            var paretoIds = results.ParetoAnalysis?.ParetoFrontIds ?? new List<Guid>();
            return Ok(paretoIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Pareto front for batch {BatchId}", batchId);
            return StatusCode(500, new { error = "An error occurred" });
        }
    }
}











