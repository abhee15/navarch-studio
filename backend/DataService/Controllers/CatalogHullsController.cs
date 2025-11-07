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
    /// Lists all catalog hulls from the real-world vessel catalog (600+ vessels)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RealVesselDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RealVesselDto>>> ListHulls(
        [FromQuery] string? vesselType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CatalogVesselsReal.AsQueryable();

        if (!string.IsNullOrEmpty(vesselType))
        {
            query = query.Where(v => v.VesselType == vesselType);
        }

        var vessels = await query
            .OrderBy(v => v.VesselType)
            .ThenBy(v => v.VesselId)
            .ToListAsync(cancellationToken);

        var result = vessels.Select(v => new RealVesselDto
        {
            Id = v.Id.ToString(),
            Name = v.VesselId,
            VesselType = v.VesselType,
            Lpp = v.LppM,
            Beam = v.BeamM,
            Draft = v.DraftM,
            Displacement = v.DisplacementT,
            BlockCoefficient = v.Cb
        }).ToList();

        _logger.LogInformation("Returning {Count} vessels from real-world catalog", result.Count);

        return Ok(result);
    }

    /// <summary>
    /// Gets a specific real-world vessel by ID with detailed information
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RealVesselDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RealVesselDetailsDto>> GetHull(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var vessel = await _context.CatalogVesselsReal
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (vessel == null)
        {
            return NotFound(new { error = $"Catalog vessel with ID {id} not found" });
        }

        var result = new RealVesselDetailsDto
        {
            Id = vessel.Id.ToString(),
            Name = vessel.VesselId,
            VesselType = vessel.VesselType,
            Lpp = vessel.LppM,
            Beam = vessel.BeamM,
            Draft = vessel.DraftM,
            Depth = vessel.DepthM,
            Displacement = vessel.DisplacementT,
            Cb = vessel.Cb,
            Cp = vessel.Cp,
            Cm = vessel.Cm,
            Cw = vessel.Cw,
            ServiceSpeed = vessel.ServiceSpeedMs,
            Dwt = vessel.DwtT,
            EngineType = vessel.EngineType,
            YearBuilt = vessel.YearBuilt,
            Source = vessel.Source,
            DataQuality = vessel.DataQuality,
            HullGeometryFile = vessel.HullGeometryFile,
            IsSystemData = vessel.IsSystemData,
            CreatedAt = vessel.CreatedAt,
            UpdatedAt = vessel.UpdatedAt
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
    /// Gets the geometry data for a catalog hull
    /// </summary>
    [HttpGet("{id}/geometry")]
    [ProducesResponseType(typeof(CatalogGeometryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CatalogGeometryDto>> GetHullGeometry(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var hull = await _context.BenchmarkCases
            .Include(h => h.Geometries)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (hull == null)
        {
            return NotFound(new { error = $"Catalog hull with ID {id} not found" });
        }

        if (hull.GeometryMissing || !hull.Geometries.Any())
        {
            return NotFound(new { error = "Geometry data not available for this hull" });
        }

        var geometry = hull.Geometries.First();

        return Ok(new CatalogGeometryDto
        {
            StationsJson = geometry.StationsJson,
            WaterlinesJson = geometry.WaterlinesJson,
            OffsetsJson = geometry.OffsetsJson,
            Type = geometry.Type,
            SourceUrl = geometry.SourceUrl
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

    /// <summary>
    /// Response DTO for catalog geometry
    /// </summary>
    public class CatalogGeometryDto
    {
        public required string? StationsJson { get; set; }
        public required string? WaterlinesJson { get; set; }
        public required string? OffsetsJson { get; set; }
        public required string? Type { get; set; }
        public required string? SourceUrl { get; set; }
    }

    /// <summary>
    /// DTO for real-world vessel catalog listing
    /// </summary>
    public class RealVesselDto
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public string? VesselType { get; set; }
        public decimal? Lpp { get; set; }
        public decimal? Beam { get; set; }
        public decimal? Draft { get; set; }
        public decimal? Displacement { get; set; }
        public decimal? BlockCoefficient { get; set; }
    }

    /// <summary>
    /// DTO for real-world vessel detailed information
    /// </summary>
    public class RealVesselDetailsDto
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string VesselType { get; set; }
        public decimal Lpp { get; set; }
        public decimal Beam { get; set; }
        public decimal Draft { get; set; }
        public decimal? Depth { get; set; }
        public decimal Displacement { get; set; }
        public decimal Cb { get; set; }
        public decimal? Cp { get; set; }
        public decimal? Cm { get; set; }
        public decimal? Cw { get; set; }
        public decimal? ServiceSpeed { get; set; }
        public decimal? Dwt { get; set; }
        public string? EngineType { get; set; }
        public int? YearBuilt { get; set; }
        public string? Source { get; set; }
        public string? DataQuality { get; set; }
        public string? HullGeometryFile { get; set; }
        public bool IsSystemData { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
