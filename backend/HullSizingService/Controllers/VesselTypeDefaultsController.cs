using Asp.Versioning;
using HullSizingService.Services.ShipD;
using Microsoft.AspNetCore.Mvc;

namespace HullSizingService.Controllers;

/// <summary>
/// Controller for vessel type hull shape defaults.
/// Provides automatic selection of hull families and parameters based on vessel type.
/// </summary>
[ApiController]
[Route("api/v1/hull-sizing/vessel-types")]
public class VesselTypeDefaultsController : ControllerBase
{
    private readonly IVesselTypeHullMappingService _mappingService;
    private readonly ILogger<VesselTypeDefaultsController> _logger;

    public VesselTypeDefaultsController(
        IVesselTypeHullMappingService mappingService,
        ILogger<VesselTypeDefaultsController> logger)
    {
        _mappingService = mappingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets default hull families and parameters for a vessel type.
    /// </summary>
    /// <param name="category">Vessel category (e.g., "commercial", "recreational")</param>
    /// <param name="type">Vessel type (e.g., "container", "yacht")</param>
    /// <returns>Default hull families and parameters</returns>
    [HttpGet("{category}/{type}/defaults")]
    [ProducesResponseType(typeof(VesselHullDefaults), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetDefaults(string category, string type)
    {
        if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(type))
        {
            return BadRequest(new { error = "Category and type are required" });
        }

        // Normalize to lowercase for case-insensitive matching
        var normalizedCategory = category.ToLowerInvariant();
        var normalizedType = type.ToLowerInvariant();

        var defaults = _mappingService.GetDefaultsForVesselType(normalizedCategory, normalizedType);

        if (defaults == null)
        {
            _logger.LogWarning("[VESSEL_DEFAULTS] No defaults found for {Category}:{Type}", category, type);
            return NotFound(new { error = $"No defaults found for vessel type: {category}:{type}" });
        }

        _logger.LogDebug("[VESSEL_DEFAULTS] Returning defaults for {Category}:{Type}: Bow={Bow}, Mid={Mid}, Stern={Stern}",
            category, type, defaults.BowFamily, defaults.MidshipFamily, defaults.SternFamily);

        return Ok(defaults);
    }
}

