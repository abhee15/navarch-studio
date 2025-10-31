using Asp.Versioning;
using DataService.Data;
using DataService.Services.Resistance;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;
using Shared.Models;

namespace DataService.Controllers;

/// <summary>
/// Controller for managing speed grids for resistance calculations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/resistance/vessels/{vesselId}/speed-grids")]
public class SpeedGridController : ControllerBase
{
    private readonly DataDbContext _context;
    private readonly IResistanceCalculationService _resistanceService;
    private readonly ILogger<SpeedGridController> _logger;

    public SpeedGridController(
        DataDbContext context,
        IResistanceCalculationService resistanceService,
        ILogger<SpeedGridController> logger)
    {
        _context = context;
        _resistanceService = resistanceService;
        _logger = logger;
    }

    /// <summary>
    /// Lists all speed grids for a vessel
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListSpeedGrids(
        Guid vesselId,
        CancellationToken cancellationToken)
    {
        var vessel = await _context.Vessels.FindAsync(new object[] { vesselId }, cancellationToken);
        if (vessel == null)
        {
            return NotFound(new { error = $"Vessel with ID {vesselId} not found" });
        }

        var grids = await _context.SpeedGrids
            .Where(sg => sg.VesselId == vesselId)
            .Include(sg => sg.SpeedPoints.OrderBy(sp => sp.DisplayOrder))
            .OrderBy(sg => sg.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = grids.Select(g => new SpeedGridDto
        {
            Id = g.Id,
            VesselId = g.VesselId,
            Name = g.Name,
            Description = g.Description,
            CreatedAt = g.CreatedAt,
            UpdatedAt = g.UpdatedAt,
            SpeedPoints = g.SpeedPoints.Select(sp => new SpeedPointDto
            {
                Id = sp.Id,
                SpeedGridId = sp.SpeedGridId,
                Speed = sp.Speed,
                SpeedKnots = sp.SpeedKnots,
                FroudeNumber = sp.FroudeNumber,
                Notes = sp.Notes,
                DisplayOrder = sp.DisplayOrder
            }).ToList()
        }).ToList();

        return Ok(new { speedGrids = dtos });
    }

    /// <summary>
    /// Gets a specific speed grid by ID
    /// </summary>
    [HttpGet("{gridId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSpeedGrid(
        Guid vesselId,
        Guid gridId,
        CancellationToken cancellationToken)
    {
        var grid = await _context.SpeedGrids
            .Include(sg => sg.SpeedPoints.OrderBy(sp => sp.DisplayOrder))
            .FirstOrDefaultAsync(sg => sg.Id == gridId && sg.VesselId == vesselId, cancellationToken);

        if (grid == null)
        {
            return NotFound(new { error = $"Speed grid with ID {gridId} not found" });
        }

        var dto = new SpeedGridDto
        {
            Id = grid.Id,
            VesselId = grid.VesselId,
            Name = grid.Name,
            Description = grid.Description,
            CreatedAt = grid.CreatedAt,
            UpdatedAt = grid.UpdatedAt,
            SpeedPoints = grid.SpeedPoints.Select(sp => new SpeedPointDto
            {
                Id = sp.Id,
                SpeedGridId = sp.SpeedGridId,
                Speed = sp.Speed,
                SpeedKnots = sp.SpeedKnots,
                FroudeNumber = sp.FroudeNumber,
                Notes = sp.Notes,
                DisplayOrder = sp.DisplayOrder
            }).ToList()
        };

        return Ok(dto);
    }

    /// <summary>
    /// Creates a new speed grid
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSpeedGrid(
        Guid vesselId,
        [FromBody] CreateSpeedGridRequest request,
        CancellationToken cancellationToken)
    {
        var vessel = await _context.Vessels.FindAsync(new object[] { vesselId }, cancellationToken);
        if (vessel == null)
        {
            return NotFound(new { error = $"Vessel with ID {vesselId} not found" });
        }

        // Normalize speeds to m/s if provided in knots
        var normalizedPoints = request.SpeedPoints.Select((sp, index) => new SpeedPoint
        {
            Speed = sp.SpeedKnots.HasValue ? sp.SpeedKnots.Value * 0.514444m : sp.Speed, // Convert knots to m/s
            SpeedKnots = sp.SpeedKnots,
            Notes = sp.Notes,
            DisplayOrder = sp.DisplayOrder > 0 ? sp.DisplayOrder : index
        }).ToList();

        var grid = new SpeedGrid
        {
            VesselId = vesselId,
            Name = request.Name,
            Description = request.Description,
            SpeedPoints = normalizedPoints
        };

        // Calculate Froude numbers if vessel LWL is available
        if (vessel.Lpp > 0)
        {
            foreach (var point in grid.SpeedPoints)
            {
                try
                {
                    point.FroudeNumber = _resistanceService.CalculateFroudeNumber(
                        point.Speed, vessel.Lpp);
                }
                catch
                {
                    // Skip if calculation fails
                }
            }
        }

        _context.SpeedGrids.Add(grid);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created speed grid {GridId} for vessel {VesselId} with {Count} points",
            grid.Id, vesselId, grid.SpeedPoints.Count);

        var dto = new SpeedGridDto
        {
            Id = grid.Id,
            VesselId = grid.VesselId,
            Name = grid.Name,
            Description = grid.Description,
            CreatedAt = grid.CreatedAt,
            UpdatedAt = grid.UpdatedAt,
            SpeedPoints = grid.SpeedPoints.Select(sp => new SpeedPointDto
            {
                Id = sp.Id,
                SpeedGridId = sp.SpeedGridId,
                Speed = sp.Speed,
                SpeedKnots = sp.SpeedKnots,
                FroudeNumber = sp.FroudeNumber,
                Notes = sp.Notes,
                DisplayOrder = sp.DisplayOrder
            }).ToList()
        };

        return CreatedAtAction(
            nameof(GetSpeedGrid),
            new { vesselId, gridId = grid.Id },
            dto);
    }

    /// <summary>
    /// Updates a speed grid (name, description)
    /// </summary>
    [HttpPut("{gridId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSpeedGrid(
        Guid vesselId,
        Guid gridId,
        [FromBody] CreateSpeedGridRequest request,
        CancellationToken cancellationToken)
    {
        var grid = await _context.SpeedGrids
            .FirstOrDefaultAsync(sg => sg.Id == gridId && sg.VesselId == vesselId, cancellationToken);

        if (grid == null)
        {
            return NotFound(new { error = $"Speed grid with ID {gridId} not found" });
        }

        grid.Name = request.Name;
        grid.Description = request.Description;
        grid.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated speed grid {GridId}", gridId);

        return await GetSpeedGrid(vesselId, gridId, cancellationToken);
    }

    /// <summary>
    /// Updates speed points in bulk (replaces existing points)
    /// </summary>
    [HttpPost("{gridId}/points")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSpeedPoints(
        Guid vesselId,
        Guid gridId,
        [FromBody] UpdateSpeedPointsRequest request,
        CancellationToken cancellationToken)
    {
        var grid = await _context.SpeedGrids
            .Include(sg => sg.SpeedPoints)
            .FirstOrDefaultAsync(sg => sg.Id == gridId && sg.VesselId == vesselId, cancellationToken);

        if (grid == null)
        {
            return NotFound(new { error = $"Speed grid with ID {gridId} not found" });
        }

        var vessel = await _context.Vessels.FindAsync(new object[] { vesselId }, cancellationToken);
        if (vessel == null)
        {
            return NotFound(new { error = $"Vessel with ID {vesselId} not found" });
        }

        // Remove existing points
        _context.SpeedPoints.RemoveRange(grid.SpeedPoints);

        // Add new points
        var newPoints = request.SpeedPoints.Select((sp, index) => new SpeedPoint
        {
            SpeedGridId = gridId,
            Speed = sp.SpeedKnots.HasValue ? sp.SpeedKnots.Value * 0.514444m : sp.Speed,
            SpeedKnots = sp.SpeedKnots,
            Notes = sp.Notes,
            DisplayOrder = sp.DisplayOrder > 0 ? sp.DisplayOrder : index
        }).ToList();

        // Calculate Froude numbers
        if (vessel.Lpp > 0)
        {
            foreach (var point in newPoints)
            {
                try
                {
                    point.FroudeNumber = _resistanceService.CalculateFroudeNumber(
                        point.Speed, vessel.Lpp);
                }
                catch
                {
                    // Skip if calculation fails
                }
            }
        }

        grid.SpeedPoints = newPoints;
        grid.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated {Count} speed points for grid {GridId}",
            newPoints.Count, gridId);

        return await GetSpeedGrid(vesselId, gridId, cancellationToken);
    }

    /// <summary>
    /// Deletes a speed grid
    /// </summary>
    [HttpDelete("{gridId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSpeedGrid(
        Guid vesselId,
        Guid gridId,
        CancellationToken cancellationToken)
    {
        var grid = await _context.SpeedGrids
            .FirstOrDefaultAsync(sg => sg.Id == gridId && sg.VesselId == vesselId, cancellationToken);

        if (grid == null)
        {
            return NotFound(new { error = $"Speed grid with ID {gridId} not found" });
        }

        _context.SpeedGrids.Remove(grid);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted speed grid {GridId}", gridId);

        return NoContent();
    }
}
