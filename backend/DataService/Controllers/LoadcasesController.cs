using Asp.Versioning;
using DataService.Services.Hydrostatics;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace DataService.Controllers;

/// <summary>
/// Controller for loadcase management
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hydrostatics/vessels/{vesselId}/loadcases")]
public class LoadcasesController : ControllerBase
{
    private readonly ILoadcaseService _loadcaseService;
    private readonly ILogger<LoadcasesController> _logger;

    public LoadcasesController(
        ILoadcaseService loadcaseService,
        ILogger<LoadcasesController> logger)
    {
        _loadcaseService = loadcaseService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new loadcase for a vessel
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLoadcase(
        Guid vesselId,
        [FromBody] LoadcaseDto loadcaseDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var loadcase = await _loadcaseService.CreateLoadcaseAsync(vesselId, loadcaseDto, cancellationToken);
            return CreatedAtAction(nameof(GetLoadcase), new { vesselId, id = loadcase.Id }, loadcase);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error creating loadcase for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a loadcase by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLoadcase(
        Guid vesselId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var loadcase = await _loadcaseService.GetLoadcaseAsync(id, cancellationToken);

        if (loadcase == null || loadcase.VesselId != vesselId)
        {
            return NotFound(new { error = $"Loadcase {id} not found for vessel {vesselId}" });
        }

        return Ok(loadcase);
    }

    /// <summary>
    /// Lists all loadcases for a vessel
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListLoadcases(
        Guid vesselId,
        CancellationToken cancellationToken)
    {
        var loadcases = await _loadcaseService.ListLoadcasesAsync(vesselId, cancellationToken);
        return Ok(new { loadcases });
    }

    /// <summary>
    /// Updates a loadcase
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateLoadcase(
        Guid vesselId,
        Guid id,
        [FromBody] LoadcaseDto loadcaseDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var loadcase = await _loadcaseService.UpdateLoadcaseAsync(id, loadcaseDto, cancellationToken);

            if (loadcase == null || loadcase.VesselId != vesselId)
            {
                return NotFound(new { error = $"Loadcase {id} not found for vessel {vesselId}" });
            }

            return Ok(loadcase);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error updating loadcase {LoadcaseId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a loadcase
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLoadcase(
        Guid vesselId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _loadcaseService.DeleteLoadcaseAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound(new { error = $"Loadcase {id} not found" });
        }

        return NoContent();
    }
}

