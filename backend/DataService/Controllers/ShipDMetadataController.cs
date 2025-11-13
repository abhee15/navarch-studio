using DataService.Services.ShipD;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.ShipD;

namespace DataService.Controllers;

[ApiController]
[Route("api/v1/shipd")]
public class ShipDMetadataController : ControllerBase
{
    private readonly IShipDMetadataService _service;
    private readonly ILogger<ShipDMetadataController> _logger;

    public ShipDMetadataController(IShipDMetadataService service, ILogger<ShipDMetadataController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("parameters")]
    [ProducesResponseType(typeof(IReadOnlyList<ShipDParameterMetadataDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ShipDParameterMetadataDto>>> GetParameters(CancellationToken cancellationToken)
    {
        var metadata = await _service.GetParameterMetadataAsync(cancellationToken);
        return Ok(metadata);
    }

    [HttpGet("taxonomy")]
    [ProducesResponseType(typeof(IReadOnlyList<ShipDVesselTaxonomyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ShipDVesselTaxonomyDto>>> GetTaxonomy(CancellationToken cancellationToken)
    {
        var taxonomy = await _service.GetVesselTaxonomyAsync(cancellationToken);
        return Ok(taxonomy);
    }
}

