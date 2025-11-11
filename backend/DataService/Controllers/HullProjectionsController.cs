using Asp.Versioning;
using DataService.Services.Hydrostatics;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace DataService.Controllers;

/// <summary>
/// API endpoints for hull projection views (waterlines, buttocks)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hydrostatics/vessels/{vesselId}/projections")]
public class HullProjectionsController : ControllerBase
{
    private readonly IHullProjectionsService _projectionsService;
    private readonly IDigonalsService _diagonalsService;
    private readonly ILogger<HullProjectionsController> _logger;

    public HullProjectionsController(
        IHullProjectionsService projectionsService,
        IDigonalsService diagonalsService,
        ILogger<HullProjectionsController> logger)
    {
        _projectionsService = projectionsService;
        _diagonalsService = diagonalsService;
        _logger = logger;
    }

    /// <summary>
    /// Get waterline projections (XY views at Z-levels)
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Waterline curves</returns>
    [HttpGet("waterlines")]
    [ProducesResponseType(typeof(WaterlinesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WaterlinesDto>> GetWaterlines(
        Guid vesselId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting waterlines for vessel {VesselId}", vesselId);

        var result = await _projectionsService.GetWaterlinesAsync(vesselId, cancellationToken);

        if (result.Waterlines.Count == 0)
        {
            return NotFound(new { message = "No geometry data found for vessel. Import stations, waterlines, and offsets first." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get buttock projections (XZ views at Y-offsets)
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="numButtocks">Optional: Number of buttock lines to generate (default: 5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Buttock curves</returns>
    [HttpGet("buttocks")]
    [ProducesResponseType(typeof(ButtocksDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ButtocksDto>> GetButtocks(
        Guid vesselId,
        [FromQuery] int? numButtocks,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting buttocks for vessel {VesselId} (numButtocks: {NumButtocks})", vesselId, numButtocks);

        var result = await _projectionsService.GetButtocksAsync(vesselId, numButtocks, cancellationToken);

        if (result.Buttocks.Count == 0)
        {
            return NotFound(new { message = "No geometry data found for vessel. Import stations, waterlines, and offsets first." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get diagonal curves for hull form (45° lines from baseline for fairing validation)
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="numDiagonals">Number of diagonal curves to generate (default: 3)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Diagonal curves</returns>
    [HttpGet("diagonals")]
    [ProducesResponseType(typeof(DiagonalsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiagonalsDto>> GetDiagonals(
        Guid vesselId,
        CancellationToken cancellationToken,
        [FromQuery] int numDiagonals = 3)
    {
        _logger.LogInformation("Getting {NumDiagonals} diagonals for vessel {VesselId}", numDiagonals, vesselId);

        var result = await _diagonalsService.GetDiagonalsAsync(vesselId, numDiagonals, cancellationToken);

        if (result.Diagonals.Count == 0)
        {
            return NotFound(new { message = "No geometry data found for vessel. Import stations, waterlines, and offsets first." });
        }

        return Ok(result);
    }
}
