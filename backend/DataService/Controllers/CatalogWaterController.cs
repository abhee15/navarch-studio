using Asp.Versioning;
using DataService.Services.Catalog;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;
using Shared.Models;

namespace DataService.Controllers;

/// <summary>
/// Controller for catalog water properties
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/catalog/water")]
[Produces("application/json")]
public class CatalogWaterController : ControllerBase
{
    private readonly CatalogWaterService _waterService;
    private readonly ILogger<CatalogWaterController> _logger;

    public CatalogWaterController(
        CatalogWaterService waterService,
        ILogger<CatalogWaterController> logger)
    {
        _waterService = waterService;
        _logger = logger;
    }

    /// <summary>
    /// Lists all water property anchor points
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CatalogWaterProperty>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CatalogWaterProperty>>> ListWaterProperties(
        CancellationToken cancellationToken = default)
    {
        var props = await _waterService.GetAllAsync(cancellationToken);
        return Ok(props);
    }

    /// <summary>
    /// Looks up water properties for a specific temperature and salinity with interpolation
    /// </summary>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(WaterPropertiesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WaterPropertiesDto>> LookupWaterProperties(
        [FromQuery] decimal temp,
        [FromQuery] decimal salinity = 35,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var props = await _waterService.GetWaterPropertiesAsync(temp, salinity, cancellationToken);
            return Ok(props);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets all anchor points for a specific medium (Fresh or Sea)
    /// </summary>
    [HttpGet("{medium}")]
    [ProducesResponseType(typeof(List<CatalogWaterProperty>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CatalogWaterProperty>>> GetAnchorPoints(
        string medium,
        CancellationToken cancellationToken = default)
    {
        if (medium != "Fresh" && medium != "Sea")
        {
            return BadRequest(new { error = "Medium must be 'Fresh' or 'Sea'" });
        }

        var points = await _waterService.GetAnchorPointsAsync(medium, cancellationToken);
        return Ok(points);
    }
}
