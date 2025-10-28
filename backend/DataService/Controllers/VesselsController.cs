using Asp.Versioning;
using DataService.Services.Hydrostatics;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace DataService.Controllers;

/// <summary>
/// Controller for vessel management
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hydrostatics/vessels")]
public class VesselsController : ControllerBase
{
    private readonly IVesselService _vesselService;
    private readonly ILogger<VesselsController> _logger;

    public VesselsController(
        IVesselService vesselService,
        ILogger<VesselsController> logger)
    {
        _vesselService = vesselService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new vessel
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(VesselDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateVessel(
        [FromBody] VesselDto vesselDto,
        CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Get actual user ID from auth context
            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Placeholder

            var vessel = await _vesselService.CreateVesselAsync(vesselDto, userId, cancellationToken);
            var details = await _vesselService.GetVesselDetailsAsync(vessel.Id, cancellationToken);

            return CreatedAtAction(nameof(GetVessel), new { id = vessel.Id }, details);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating vessel");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating vessel");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An unexpected error occurred while creating the vessel", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets a vessel by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(VesselDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVessel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vessel = await _vesselService.GetVesselDetailsAsync(id, cancellationToken);

        if (vessel == null)
        {
            return NotFound(new { error = $"Vessel {id} not found" });
        }

        return Ok(vessel);
    }

    /// <summary>
    /// Lists all vessels for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<VesselDetailsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListVessels(CancellationToken cancellationToken)
    {
        // TODO: Get actual user ID from auth context
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Placeholder

        var vessels = await _vesselService.ListVesselsAsync(userId, cancellationToken);

        var vesselDetails = new List<object>();
        foreach (var vessel in vessels)
        {
            vesselDetails.Add(new
            {
                vessel.Id,
                vessel.Name,
                vessel.Description,
                vessel.Lpp,
                vessel.Beam,
                vessel.DesignDraft,
                vessel.CreatedAt,
                vessel.UpdatedAt
            });
        }

        return Ok(new { vessels = vesselDetails, total = vesselDetails.Count });
    }

    /// <summary>
    /// Updates a vessel
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(VesselDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateVessel(
        Guid id,
        [FromBody] VesselDto vesselDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var vessel = await _vesselService.UpdateVesselAsync(id, vesselDto, cancellationToken);

            if (vessel == null)
            {
                return NotFound(new { error = $"Vessel {id} not found" });
            }

            var details = await _vesselService.GetVesselDetailsAsync(vessel.Id, cancellationToken);
            return Ok(details);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating vessel {VesselId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a vessel (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVessel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _vesselService.DeleteVesselAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound(new { error = $"Vessel {id} not found" });
        }

        return NoContent();
    }
}

