using Asp.Versioning;
using DataService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;
using Shared.Models;

namespace DataService.Controllers;

/// <summary>
/// Controller for speed grid management
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/resistance/vessels/{vesselId}/speed-grids")]
public class SpeedGridController : ControllerBase
{
    private readonly DataDbContext _context;
    private readonly ILogger<SpeedGridController> _logger;

    public SpeedGridController(
        DataDbContext context,
        ILogger<SpeedGridController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lists all speed grids for a vessel
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SpeedGridDetailsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSpeedGrids(
        Guid vesselId,
        CancellationToken cancellationToken)
    {
        var grids = await _context.SpeedGrids
            .Include(g => g.SpeedPoints.OrderBy(p => p.DisplayOrder))
            .Where(g => g.VesselId == vesselId)
            .OrderBy(g => g.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = grids.Select(g => new SpeedGridDetailsDto
        {
            Id = g.Id,
            VesselId = g.VesselId,
            Name = g.Name,
            Description = g.Description,
            CreatedAt = g.CreatedAt,
            UpdatedAt = g.UpdatedAt,
            SpeedPointsCount = g.SpeedPoints.Count,
            SpeedPoints = g.SpeedPoints.Select(p => new SpeedPointDto
            {
                Id = p.Id,
                SpeedGridId = p.SpeedGridId,
                Speed = p.Speed,
                SpeedKnots = p.SpeedKnots,
                FroudeNumber = p.FroudeNumber,
                Notes = p.Notes,
                DisplayOrder = p.DisplayOrder
            }).ToList()
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Gets a specific speed grid by ID
    /// </summary>
    [HttpGet("{gridId}")]
    [ProducesResponseType(typeof(SpeedGridDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSpeedGrid(
        Guid vesselId,
        Guid gridId,
        CancellationToken cancellationToken)
    {
        var grid = await _context.SpeedGrids
            .Include(g => g.SpeedPoints.OrderBy(p => p.DisplayOrder))
            .FirstOrDefaultAsync(g => g.Id == gridId && g.VesselId == vesselId, cancellationToken);

        if (grid == null)
        {
            return NotFound(new { error = $"Speed grid {gridId} not found for vessel {vesselId}" });
        }

        var dto = new SpeedGridDetailsDto
        {
            Id = grid.Id,
            VesselId = grid.VesselId,
            Name = grid.Name,
            Description = grid.Description,
            CreatedAt = grid.CreatedAt,
            UpdatedAt = grid.UpdatedAt,
            SpeedPointsCount = grid.SpeedPoints.Count,
            SpeedPoints = grid.SpeedPoints.Select(p => new SpeedPointDto
            {
                Id = p.Id,
                SpeedGridId = p.SpeedGridId,
                Speed = p.Speed,
                SpeedKnots = p.SpeedKnots,
                FroudeNumber = p.FroudeNumber,
                Notes = p.Notes,
                DisplayOrder = p.DisplayOrder
            }).ToList()
        };

        return Ok(dto);
    }

    /// <summary>
    /// Creates a new speed grid
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SpeedGridDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSpeedGrid(
        Guid vesselId,
        [FromBody] SpeedGridDto request,
        CancellationToken cancellationToken)
    {
        // Verify vessel exists
        var vesselExists = await _context.Vessels.AnyAsync(v => v.Id == vesselId, cancellationToken);
        if (!vesselExists)
        {
            return BadRequest(new { error = $"Vessel {vesselId} not found" });
        }

        var grid = new SpeedGrid
        {
            VesselId = vesselId,
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SpeedGrids.Add(grid);
        await _context.SaveChangesAsync(cancellationToken);

        // Add speed points
        if (request.SpeedPoints.Any())
        {
            int displayOrder = 0;
            foreach (var pointDto in request.SpeedPoints)
            {
                var point = new SpeedPoint
                {
                    SpeedGridId = grid.Id,
                    Speed = pointDto.Speed, // Already in m/s from DTO conversion
                    SpeedKnots = pointDto.SpeedKnots,
                    FroudeNumber = pointDto.FroudeNumber,
                    Notes = pointDto.Notes,
                    DisplayOrder = displayOrder++
                };
                grid.SpeedPoints.Add(point);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Created speed grid {GridId} for vessel {VesselId} with {Count} points",
            grid.Id, vesselId, grid.SpeedPoints.Count);

        var responseDto = new SpeedGridDetailsDto
        {
            Id = grid.Id,
            VesselId = grid.VesselId,
            Name = grid.Name,
            Description = grid.Description,
            CreatedAt = grid.CreatedAt,
            UpdatedAt = grid.UpdatedAt,
            SpeedPointsCount = grid.SpeedPoints.Count,
            SpeedPoints = grid.SpeedPoints.Select(p => new SpeedPointDto
            {
                Id = p.Id,
                SpeedGridId = p.SpeedGridId,
                Speed = p.Speed,
                SpeedKnots = p.SpeedKnots,
                FroudeNumber = p.FroudeNumber,
                Notes = p.Notes,
                DisplayOrder = p.DisplayOrder
            }).ToList()
        };

        return CreatedAtAction(
            nameof(GetSpeedGrid),
            new { vesselId, gridId = grid.Id },
            responseDto);
    }

    /// <summary>
    /// Updates a speed grid
    /// </summary>
    [HttpPut("{gridId}")]
    [ProducesResponseType(typeof(SpeedGridDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSpeedGrid(
        Guid vesselId,
        Guid gridId,
        [FromBody] SpeedGridDto request,
        CancellationToken cancellationToken)
    {
        var grid = await _context.SpeedGrids
            .Include(g => g.SpeedPoints)
            .FirstOrDefaultAsync(g => g.Id == gridId && g.VesselId == vesselId, cancellationToken);

        if (grid == null)
        {
            return NotFound(new { error = $"Speed grid {gridId} not found for vessel {vesselId}" });
        }

        // Update grid properties
        grid.Name = request.Name;
        grid.Description = request.Description;
        grid.UpdatedAt = DateTime.UtcNow;

        // Remove existing points
        _context.SpeedPoints.RemoveRange(grid.SpeedPoints);

        // Add new points
        int displayOrder = 0;
        foreach (var pointDto in request.SpeedPoints)
        {
            var point = new SpeedPoint
            {
                SpeedGridId = grid.Id,
                Speed = pointDto.Speed,
                SpeedKnots = pointDto.SpeedKnots,
                FroudeNumber = pointDto.FroudeNumber,
                Notes = pointDto.Notes,
                DisplayOrder = displayOrder++
            };
            grid.SpeedPoints.Add(point);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated speed grid {GridId} for vessel {VesselId}", gridId, vesselId);

        var responseDto = new SpeedGridDetailsDto
        {
            Id = grid.Id,
            VesselId = grid.VesselId,
            Name = grid.Name,
            Description = grid.Description,
            CreatedAt = grid.CreatedAt,
            UpdatedAt = grid.UpdatedAt,
            SpeedPointsCount = grid.SpeedPoints.Count,
            SpeedPoints = grid.SpeedPoints.OrderBy(p => p.DisplayOrder).Select(p => new SpeedPointDto
            {
                Id = p.Id,
                SpeedGridId = p.SpeedGridId,
                Speed = p.Speed,
                SpeedKnots = p.SpeedKnots,
                FroudeNumber = p.FroudeNumber,
                Notes = p.Notes,
                DisplayOrder = p.DisplayOrder
            }).ToList()
        };

        return Ok(responseDto);
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
            .FirstOrDefaultAsync(g => g.Id == gridId && g.VesselId == vesselId, cancellationToken);

        if (grid == null)
        {
            return NotFound(new { error = $"Speed grid {gridId} not found for vessel {vesselId}" });
        }

        _context.SpeedGrids.Remove(grid);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted speed grid {GridId} for vessel {VesselId}", gridId, vesselId);

        return NoContent();
    }

    /// <summary>
    /// Updates speed points for a grid
    /// </summary>
    [HttpPost("{gridId}/points")]
    [ProducesResponseType(typeof(SpeedGridDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSpeedPoints(
        Guid vesselId,
        Guid gridId,
        [FromBody] List<SpeedPointDto> points,
        CancellationToken cancellationToken)
    {
        var grid = await _context.SpeedGrids
            .Include(g => g.SpeedPoints)
            .FirstOrDefaultAsync(g => g.Id == gridId && g.VesselId == vesselId, cancellationToken);

        if (grid == null)
        {
            return NotFound(new { error = $"Speed grid {gridId} not found for vessel {vesselId}" });
        }

        // Remove existing points
        _context.SpeedPoints.RemoveRange(grid.SpeedPoints);

        // Add new points
        int displayOrder = 0;
        foreach (var pointDto in points)
        {
            var point = new SpeedPoint
            {
                SpeedGridId = grid.Id,
                Speed = pointDto.Speed,
                SpeedKnots = pointDto.SpeedKnots,
                FroudeNumber = pointDto.FroudeNumber,
                Notes = pointDto.Notes,
                DisplayOrder = displayOrder++
            };
            grid.SpeedPoints.Add(point);
        }

        grid.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated {Count} speed points for grid {GridId}", points.Count, gridId);

        var responseDto = new SpeedGridDetailsDto
        {
            Id = grid.Id,
            VesselId = grid.VesselId,
            Name = grid.Name,
            Description = grid.Description,
            CreatedAt = grid.CreatedAt,
            UpdatedAt = grid.UpdatedAt,
            SpeedPointsCount = grid.SpeedPoints.Count,
            SpeedPoints = grid.SpeedPoints.OrderBy(p => p.DisplayOrder).Select(p => new SpeedPointDto
            {
                Id = p.Id,
                SpeedGridId = p.SpeedGridId,
                Speed = p.Speed,
                SpeedKnots = p.SpeedKnots,
                FroudeNumber = p.FroudeNumber,
                Notes = p.Notes,
                DisplayOrder = p.DisplayOrder
            }).ToList()
        };

        return Ok(responseDto);
    }
}

