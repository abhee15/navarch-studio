using Asp.Versioning;
using DataService.Data;
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
    private readonly SampleVesselSeedService _seedService;
    private readonly DataDbContext _context;
    private readonly ILogger<VesselsController> _logger;

    public VesselsController(
        IVesselService vesselService,
        SampleVesselSeedService seedService,
        DataDbContext context,
        ILogger<VesselsController> logger)
    {
        _vesselService = vesselService;
        _seedService = seedService;
        _context = context;
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

            // Convert to DTOs with proper structure for unit conversion filter
            // Get counts efficiently using batch queries
            var vesselIds = vessels.Select(v => v.Id).ToList();

            var stationsCounts = await _context.Stations
                .Where(s => vesselIds.Contains(s.VesselId))
                .GroupBy(s => s.VesselId)
                .Select(g => new { VesselId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.VesselId, x => x.Count, cancellationToken);

            var waterlinesCounts = await _context.Waterlines
                .Where(w => vesselIds.Contains(w.VesselId))
                .GroupBy(w => w.VesselId)
                .Select(g => new { VesselId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.VesselId, x => x.Count, cancellationToken);

            var offsetsCounts = await _context.Offsets
                .Where(o => vesselIds.Contains(o.VesselId))
                .GroupBy(o => o.VesselId)
                .Select(g => new { VesselId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.VesselId, x => x.Count, cancellationToken);

            var vesselDetails = new List<VesselDetailsDto>();
            foreach (var vessel in vessels)
            {
                var isTemplate = vessel.UserId == Shared.Constants.TemplateVessels.SystemUserId;

                vesselDetails.Add(new VesselDetailsDto
                {
                    Id = vessel.Id,
                    Name = vessel.Name,
                    Description = vessel.Description,
                    Lpp = vessel.Lpp,  // In SI units, will be converted by filter
                    Beam = vessel.Beam,
                    DesignDraft = vessel.DesignDraft,
                    StationsCount = stationsCounts.GetValueOrDefault(vessel.Id, 0),
                    WaterlinesCount = waterlinesCounts.GetValueOrDefault(vessel.Id, 0),
                    OffsetsCount = offsetsCounts.GetValueOrDefault(vessel.Id, 0),
                    IsTemplate = isTemplate,
                    Units = "SI",  // Data is stored in SI, filter will convert
                    CreatedAt = vessel.CreatedAt,
                    UpdatedAt = vessel.UpdatedAt
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

    /// <summary>
    /// Gets predefined vessel templates
    /// </summary>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(List<VesselTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await _vesselService.GetTemplatesAsync();
        return Ok(templates);
    }

    /// <summary>
    /// Seeds sample vessels (KCS, Wigley) for the current user
    /// </summary>
    [HttpPost("seed-samples")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SeedSampleVessels(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Seed sample vessels requested");

            // TODO: Get actual user ID from auth context
            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Placeholder

            // Seed both sample vessels
            await _seedService.SeedAllSampleVesselsAsync(userId, cancellationToken);

            return Ok(new { message = "Sample vessels seeded successfully (KCS and Wigley)" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding sample vessels");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An unexpected error occurred while seeding sample vessels", details = ex.Message });
        }
    }

    /// <summary>
    /// Manually triggers template vessel seeding (useful for diagnostics)
    /// </summary>
    [HttpPost("seed-template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SeedTemplateVessel(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Manual template vessel seeding requested");

            var templateSeeder = HttpContext.RequestServices.GetRequiredService<DataService.Services.Hydrostatics.ITemplateVesselSeeder>();
            await templateSeeder.SeedHydrostaticsTemplateAsync(cancellationToken);

            return Ok(new { message = "Template vessel seeding completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding template vessel");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An unexpected error occurred while seeding template vessel", details = ex.Message });
        }
    }

    /// <summary>
    /// Diagnoses template vessel status
    /// </summary>
    [HttpGet("diagnose-template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DiagnoseTemplateVessel(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataService.Data.DataDbContext>();
            var templateId = Shared.Constants.TemplateVessels.HydrostaticsVesselId;
            var systemUserId = Shared.Constants.TemplateVessels.SystemUserId;

            // Check if template vessel exists (ignore soft-delete filter)
            var vessel = await dbContext.Vessels
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(v => v.Id == templateId, cancellationToken);

            var exists = vessel != null;
            var isSoftDeleted = vessel?.DeletedAt != null;
            var hasCorrectUserId = vessel?.UserId == systemUserId;
            var name = vessel?.Name ?? "N/A";
            var createdAt = vessel?.CreatedAt;

            // Count related entities
            var stationsCount = exists ? await dbContext.Stations.CountAsync(s => s.VesselId == templateId, cancellationToken) : 0;
            var waterlinesCount = exists ? await dbContext.Waterlines.CountAsync(w => w.VesselId == templateId, cancellationToken) : 0;
            var offsetsCount = exists ? await dbContext.Offsets.CountAsync(o => o.VesselId == templateId, cancellationToken) : 0;
            var loadcasesCount = exists ? await dbContext.Loadcases.CountAsync(l => l.VesselId == templateId, cancellationToken) : 0;

            return Ok(new
            {
                exists,
                isSoftDeleted,
                hasCorrectUserId,
                vesselId = templateId.ToString(),
                expectedUserId = systemUserId.ToString(),
                actualUserId = vessel?.UserId.ToString() ?? "N/A",
                name,
                createdAt,
                geometry = new
                {
                    stations = stationsCount,
                    waterlines = waterlinesCount,
                    offsets = offsetsCount,
                    loadcases = loadcasesCount
                },
                status = exists && !isSoftDeleted && hasCorrectUserId && stationsCount > 0 && waterlinesCount > 0 && offsetsCount > 0
                    ? "healthy"
                    : exists && isSoftDeleted
                        ? "soft-deleted"
                        : exists && !hasCorrectUserId
                            ? "incorrect-user-id"
                            : exists && (stationsCount == 0 || waterlinesCount == 0 || offsetsCount == 0)
                                ? "incomplete-geometry"
                                : "missing"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error diagnosing template vessel");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An unexpected error occurred while diagnosing template vessel", details = ex.Message });
        }
    }
}
