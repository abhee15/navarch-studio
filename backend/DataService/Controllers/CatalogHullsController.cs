using Asp.Versioning;
using DataService.Data;
using DataService.Data.Seeds;
using DataService.Services.Hydrostatics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;
using Shared.Models;

namespace DataService.Controllers;

/// <summary>
/// Controller for catalog hull reference data
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/catalog/hulls")]
[Produces("application/json")]
public class CatalogHullsController : ControllerBase
{
    private readonly DataDbContext _context;
    private readonly ILogger<CatalogHullsController> _logger;
    private readonly IVesselService _vesselService;
    private readonly IGeometryService _geometryService;

    public CatalogHullsController(
        DataDbContext context,
        ILogger<CatalogHullsController> logger,
        IVesselService vesselService,
        IGeometryService geometryService)
    {
        _context = context;
        _logger = logger;
        _vesselService = vesselService;
        _geometryService = geometryService;
    }

    /// <summary>
    /// Lists all catalog hulls, optionally filtered by hull type
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CatalogHullListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CatalogHullListItemDto>>> ListHulls(
        [FromQuery] string? hullType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BenchmarkCases.AsQueryable();

        if (!string.IsNullOrEmpty(hullType))
        {
            query = query.Where(h => h.HullType == hullType);
        }

        var hulls = await query
            .Include(h => h.Geometries)
            .OrderBy(h => h.HullType)
            .ThenBy(h => h.Title)
            .ToListAsync(cancellationToken);

        var result = hulls.Select(h => new CatalogHullListItemDto
        {
            Id = h.Id,
            Slug = h.Slug,
            Title = h.Title,
            Description = h.Description,
            HullType = h.HullType,
            Lpp = h.Lpp_m,
            Beam = h.B_m,
            Draft = h.T_m,
            Cb = h.Cb,
            GeometryMissing = h.GeometryMissing,
            Units = "SI"
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Gets a specific catalog hull by ID with detailed information
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CatalogHullDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CatalogHullDto>> GetHull(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var hull = await _context.BenchmarkCases
            .Include(h => h.Geometries)
            .Include(h => h.TestPoints)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (hull == null)
        {
            return NotFound(new { error = $"Catalog hull with ID {id} not found" });
        }

        // Calculate LCB/LCF from % to meters
        decimal? lcb_m = hull.Lpp_m.HasValue && hull.LCB_pctLpp.HasValue
            ? hull.Lpp_m.Value * hull.LCB_pctLpp.Value / 100m
            : null;

        decimal? lcf_m = hull.Lpp_m.HasValue && hull.LCF_pctLpp.HasValue
            ? hull.Lpp_m.Value * hull.LCF_pctLpp.Value / 100m
            : null;

        var result = new CatalogHullDto
        {
            Id = hull.Id,
            Slug = hull.Slug,
            Title = hull.Title,
            Description = hull.Description,
            CanonicalRefs = hull.CanonicalRefs,
            HullType = hull.HullType,
            Lpp = hull.Lpp_m,
            Beam = hull.B_m,
            Draft = hull.T_m,
            Cb = hull.Cb,
            Cp = hull.Cp,
            LCB = lcb_m,
            LCF = lcf_m,
            GeometryMissing = hull.GeometryMissing,
            StationsCount = hull.Geometries.Count,
            WaterlinesCount = hull.Geometries.Count,
            OffsetsCount = hull.Geometries.Count,
            CreatedAt = hull.CreatedAt,
            Units = "SI"
        };

        return Ok(result);
    }

    /// <summary>
    /// Clones a catalog hull to create a new user vessel with the geometry
    /// </summary>
    [HttpPost("{id}/clone")]
    [ProducesResponseType(typeof(CloneHullResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CloneHullResponseDto>> CloneHull(
        Guid id,
        [FromBody] CloneHullRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Get the catalog hull
        var catalogHull = await _context.BenchmarkCases
            .Include(h => h.Geometries)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (catalogHull == null)
        {
            return NotFound(new { error = $"Catalog hull with ID {id} not found" });
        }

        if (catalogHull.GeometryMissing)
        {
            return BadRequest(new
            {
                error = "Cannot clone: catalog hull geometry is missing",
                slug = catalogHull.Slug
            });
        }

        // TODO: Parse geometry from BenchmarkGeometry and populate vessel
        // For now, create vessel with principal particulars only
        var vesselDto = new Shared.DTOs.VesselDto
        {
            Name = request.VesselName ?? $"{catalogHull.Title} (Cloned)",
            Description = $"Cloned from catalog hull: {catalogHull.Slug}",
            Lpp = catalogHull.Lpp_m ?? 100m,
            Beam = catalogHull.B_m ?? 20m,
            DesignDraft = catalogHull.T_m ?? 5m
        };

        // Get or create user
        var userId = request.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000001"); // Default dev user

        var vessel = await _vesselService.CreateVesselAsync(vesselDto, userId, cancellationToken);

        // Set source catalog reference
        vessel.SourceCatalogHullId = id;
        _context.Vessels.Update(vessel);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cloned catalog hull {Slug} to vessel {VesselId} for user {UserId}",
            catalogHull.Slug, vessel.Id, userId);

        return Ok(new CloneHullResponseDto
        {
            VesselId = vessel.Id,
            VesselName = vessel.Name,
            Message = $"Successfully cloned {catalogHull.Title} to your vessels"
        });
    }

    /// <summary>
    /// Request DTO for cloning a hull
    /// </summary>
    public class CloneHullRequestDto
    {
        public string? VesselName { get; set; }
        public Guid? UserId { get; set; }
    }

    /// <summary>
    /// Response DTO for cloning a hull
    /// </summary>
    public class CloneHullResponseDto
    {
        public required Guid VesselId { get; set; }
        public required string VesselName { get; set; }
        public required string Message { get; set; }
    }
}
