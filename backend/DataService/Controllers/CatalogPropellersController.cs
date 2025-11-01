using Asp.Versioning;
using DataService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;
using Shared.Models;

namespace DataService.Controllers;

/// <summary>
/// Controller for catalog propeller series
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/catalog/propellers")]
[Produces("application/json")]
public class CatalogPropellersController : ControllerBase
{
    private readonly DataDbContext _context;
    private readonly ILogger<CatalogPropellersController> _logger;

    public CatalogPropellersController(
        DataDbContext context,
        ILogger<CatalogPropellersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lists all propeller series, optionally filtered by blade count
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CatalogPropellerSeriesDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CatalogPropellerSeriesDto>>> ListSeries(
        [FromQuery] int? bladeCount = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CatalogPropellerSeries.AsQueryable();

        if (bladeCount.HasValue)
        {
            query = query.Where(s => s.BladeCount == bladeCount.Value);
        }

        var series = await query
            .Include(s => s.OpenWaterPoints)
            .OrderBy(s => s.BladeCount)
            .ThenBy(s => s.ExpandedAreaRatio)
            .ToListAsync(cancellationToken);

        var result = series.Select(s => new CatalogPropellerSeriesDto
        {
            Id = s.Id,
            Name = s.Name,
            BladeCount = s.BladeCount,
            ExpandedAreaRatio = s.ExpandedAreaRatio,
            PitchDiameterRatio = s.PitchDiameterRatio,
            SourceUrl = s.SourceUrl,
            License = s.License,
            IsDemo = s.IsDemo,
            PointsCount = s.OpenWaterPoints.Count,
            CreatedAt = s.CreatedAt
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Gets a specific propeller series with all open-water points
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CatalogPropellerSeriesDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CatalogPropellerSeriesDetailsDto>> GetSeries(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var series = await _context.CatalogPropellerSeries
            .Include(s => s.OpenWaterPoints.OrderBy(p => p.J))
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (series == null)
        {
            return NotFound(new { error = $"Propeller series with ID {id} not found" });
        }

        var result = new CatalogPropellerSeriesDetailsDto
        {
            Id = series.Id,
            Name = series.Name,
            BladeCount = series.BladeCount,
            ExpandedAreaRatio = series.ExpandedAreaRatio,
            PitchDiameterRatio = series.PitchDiameterRatio,
            SourceUrl = series.SourceUrl,
            License = series.License,
            IsDemo = series.IsDemo,
            PointsCount = series.OpenWaterPoints.Count,
            CreatedAt = series.CreatedAt,
            Points = series.OpenWaterPoints.Select(p => new CatalogPropellerPointDto
            {
                Id = p.Id,
                SeriesId = p.SeriesId,
                J = p.J,
                Kt = p.Kt,
                Kq = p.Kq,
                Eta0 = p.Eta0,
                ReynoldsNumber = p.ReynoldsNumber
            }).ToList()
        };

        return Ok(result);
    }

    /// <summary>
    /// Gets open-water points for a series (for charting/fitting)
    /// </summary>
    [HttpGet("{id}/points")]
    [ProducesResponseType(typeof(List<CatalogPropellerPointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CatalogPropellerPointDto>>> GetPoints(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var series = await _context.CatalogPropellerSeries
            .Include(s => s.OpenWaterPoints)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (series == null)
        {
            return NotFound(new { error = $"Propeller series with ID {id} not found" });
        }

        var result = series.OpenWaterPoints
            .OrderBy(p => p.J)
            .Select(p => new CatalogPropellerPointDto
            {
                Id = p.Id,
                SeriesId = p.SeriesId,
                J = p.J,
                Kt = p.Kt,
                Kq = p.Kq,
                Eta0 = p.Eta0,
                ReynoldsNumber = p.ReynoldsNumber
            }).ToList();

        return Ok(result);
    }
}
