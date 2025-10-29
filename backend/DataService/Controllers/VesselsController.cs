using Asp.Versioning;
using DataService.Services.Hydrostatics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            _logger.LogInformation("Creating vessel: {VesselName}", vesselDto.Name);

            // TODO: Get actual user ID from auth context
            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Placeholder

            var startTime = DateTime.UtcNow;
            var vessel = await _vesselService.CreateVesselAsync(vesselDto, userId, cancellationToken);
            _logger.LogInformation("Vessel created in {Elapsed}ms", (DateTime.UtcNow - startTime).TotalMilliseconds);

            startTime = DateTime.UtcNow;
            var details = await _vesselService.GetVesselDetailsAsync(vessel.Id, cancellationToken);
            _logger.LogInformation("Vessel details fetched in {Elapsed}ms", (DateTime.UtcNow - startTime).TotalMilliseconds);

            return CreatedAtAction(nameof(GetVessel), new { id = vessel.Id }, details);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating vessel");
            return BadRequest(new { error = ex.Message });
        }
        catch (Npgsql.NpgsqlException ex)
        {
            _logger.LogError(ex, "Database error creating vessel: {Message}", ex.Message);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "Database error occurred", details = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error creating vessel: {Message}", ex.Message);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "Failed to save vessel to database", details = ex.InnerException?.Message ?? ex.Message });
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
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListVessels(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[VESSELS] ListVessels endpoint called");
            Console.WriteLine("[VESSELS] === ListVessels Request Received ===");

            // TODO: Get actual user ID from auth context
            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Placeholder
            _logger.LogInformation("[VESSELS] Using placeholder userId: {UserId}", userId);

            var startTime = DateTime.UtcNow;
            Console.WriteLine("[VESSELS] Calling VesselService.ListVesselsAsync...");
            var vessels = await _vesselService.ListVesselsAsync(userId, cancellationToken);
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation("[VESSELS] Retrieved {VesselCount} vessels in {Elapsed}ms", vessels.Count, elapsed);
            Console.WriteLine($"[VESSELS] Retrieved {vessels.Count} vessels in {elapsed}ms");

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

            Console.WriteLine("[VESSELS] Returning response with {0} vessels", vesselDetails.Count);
            return Ok(new { vessels = vesselDetails, total = vesselDetails.Count });
        }
        catch (Npgsql.NpgsqlException ex)
        {
            _logger.LogError(ex, "[VESSELS] Database error listing vessels: {Message}", ex.Message);
            Console.WriteLine($"[VESSELS] ERROR: Database error - {ex.Message}");
            Console.WriteLine($"[VESSELS] Stack trace: {ex.StackTrace}");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "Database error occurred", details = ex.Message, stackTrace = ex.StackTrace });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "[VESSELS] Database update error listing vessels: {Message}", ex.Message);
            Console.WriteLine($"[VESSELS] ERROR: Database update error - {ex.Message}");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "Database query failed", details = ex.InnerException?.Message ?? ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VESSELS] Unexpected error listing vessels");
            Console.WriteLine($"[VESSELS] ERROR: Unexpected error - {ex.Message}");
            Console.WriteLine($"[VESSELS] ERROR Type: {ex.GetType().FullName}");
            Console.WriteLine($"[VESSELS] Stack trace: {ex.StackTrace}");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An unexpected error occurred", details = ex.Message, type = ex.GetType().FullName, stackTrace = ex.StackTrace });
        }
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
