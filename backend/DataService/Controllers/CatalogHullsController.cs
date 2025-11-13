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
        [FromQuery] string? vesselCategory = null,
        [FromQuery] string? shipdVesselType = null,
        [FromQuery] string? bowFamily = null,
        [FromQuery] string? midshipFamily = null,
        [FromQuery] string? sternFamily = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CatalogVesselsReal.AsQueryable();

        // Filter by legacy vessel type (backward compatible)
        if (!string.IsNullOrEmpty(vesselType))
        {
            query = query.Where(v => v.VesselType == vesselType);
        }

        // Filter by ShipD taxonomy
        if (!string.IsNullOrEmpty(vesselCategory))
        {
            query = query.Where(v => v.VesselCategory == vesselCategory);
        }

        if (!string.IsNullOrEmpty(shipdVesselType))
        {
            query = query.Where(v => v.ShipdVesselType == shipdVesselType);
        }

        if (!string.IsNullOrEmpty(bowFamily))
        {
            query = query.Where(v => v.BowFamily == bowFamily);
        }

        if (!string.IsNullOrEmpty(midshipFamily))
        {
            query = query.Where(v => v.MidshipFamily == midshipFamily);
        }

        if (!string.IsNullOrEmpty(sternFamily))
        {
            query = query.Where(v => v.SternFamily == sternFamily);
        }

        var vessels = await query
            .OrderBy(v => v.VesselType)
            .ThenBy(v => v.VesselId)
            .ToListAsync(cancellationToken);

        var result = vessels.Select(v => new RealVesselDto
        {
            Id = v.Id.ToString(),
            Title = v.VesselId,
            Slug = v.VesselId.ToLower()
                .Replace(" ", "-")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(",", "")
                .Trim('-'),
            Description = v.Source,
            HullType = v.VesselType,
            Lpp = v.LppM,
            Beam = v.BeamM,
            Draft = v.DraftM,
            Cb = v.Cb,
            Displacement = v.DisplacementT,
            GeometryMissing = string.IsNullOrEmpty(v.HullGeometryFile),
            Units = "SI",
            VesselCategory = v.VesselCategory,
            ShipdVesselType = v.ShipdVesselType,
            BowFamily = v.BowFamily,
            MidshipFamily = v.MidshipFamily,
            SternFamily = v.SternFamily
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
            Title = vessel.VesselId,
            Slug = vessel.VesselId.ToLower()
                .Replace(" ", "-")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(",", "")
                .Trim('-'),
            Description = vessel.Source,
            HullType = vessel.VesselType,
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
            GeometryMissing = string.IsNullOrEmpty(vessel.HullGeometryFile),
            IsSystemData = vessel.IsSystemData,
            Units = "SI",
            CreatedAt = vessel.CreatedAt,
            UpdatedAt = vessel.UpdatedAt,
            VesselCategory = vessel.VesselCategory,
            ShipdVesselType = vessel.ShipdVesselType,
            BowFamily = vessel.BowFamily,
            MidshipFamily = vessel.MidshipFamily,
            SternFamily = vessel.SternFamily,
            FamilyMaskVersion = vessel.FamilyMaskVersion
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
        // Get the catalog vessel from real-world catalog
        var catalogVessel = await _context.CatalogVesselsReal
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (catalogVessel == null)
        {
            return NotFound(new { error = $"Catalog vessel with ID {id} not found" });
        }

        // Create vessel with principal particulars
        var vesselDto = new Shared.DTOs.VesselDto
        {
            Name = request.VesselName ?? $"{catalogVessel.VesselId} (Cloned)",
            Description = $"Cloned from real-world catalog: {catalogVessel.VesselId}. Source: {catalogVessel.Source}",
            Lpp = catalogVessel.LppM,
            Beam = catalogVessel.BeamM,
            DesignDraft = catalogVessel.DraftM
        };

        // Get user ID from claims or use provided userId
        var userId = request.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000001"); // Default dev user

        var vessel = await _vesselService.CreateVesselAsync(vesselDto, userId, cancellationToken);

        // Set source catalog reference
        vessel.SourceCatalogHullId = id;
        _context.Vessels.Update(vessel);

        // Add metadata if available
        if (!string.IsNullOrEmpty(catalogVessel.VesselType))
        {
            var metadata = new VesselMetadata
            {
                VesselId = vessel.Id,
                VesselType = catalogVessel.VesselType switch
                {
                    "Container" => "Ship",
                    "Tanker" => "Ship",
                    "Bulk carrier" => "Ship",
                    "Cruise ship" => "Ship",
                    "Naval combatant" => "Ship",
                    _ => "Ship"
                },
                BlockCoefficient = catalogVessel.Cb
            };
            _context.VesselMetadata.Add(metadata);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cloned catalog vessel {VesselId} ({VesselType}) to vessel {NewVesselId} for user {UserId}",
            catalogVessel.VesselId, catalogVessel.VesselType, vessel.Id, userId);

        return Ok(new CloneHullResponseDto
        {
            VesselId = vessel.Id,
            VesselName = vessel.Name,
            Message = $"Successfully cloned {catalogVessel.VesselId} to your vessels"
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
        public required string Title { get; set; }
        public required string Slug { get; set; }
        public string? Description { get; set; }
        public string? HullType { get; set; }
        public decimal? Lpp { get; set; }
        public decimal? Beam { get; set; }
        public decimal? Draft { get; set; }
        public decimal? Cb { get; set; }
        public decimal? Displacement { get; set; }
        public bool GeometryMissing { get; set; }
        public required string Units { get; set; }

        // ShipD taxonomy fields
        public string? VesselCategory { get; set; }
        public string? ShipdVesselType { get; set; }
        public string? BowFamily { get; set; }
        public string? MidshipFamily { get; set; }
        public string? SternFamily { get; set; }
    }

    /// <summary>
    /// DTO for real-world vessel detailed information
    /// </summary>
    public class RealVesselDetailsDto
    {
        public required string Id { get; set; }
        public required string Title { get; set; }
        public required string Slug { get; set; }
        public string? Description { get; set; }
        public required string HullType { get; set; }
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
        public bool GeometryMissing { get; set; }
        public bool IsSystemData { get; set; }
        public required string Units { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ShipD taxonomy fields
        public string? VesselCategory { get; set; }
        public string? ShipdVesselType { get; set; }
        public string? BowFamily { get; set; }
        public string? MidshipFamily { get; set; }
        public string? SternFamily { get; set; }
        public int? FamilyMaskVersion { get; set; }
    }
}
