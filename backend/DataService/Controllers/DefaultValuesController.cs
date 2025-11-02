using DataService.Services.Resistance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace DataService.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class DefaultValuesController : ControllerBase
{
    private readonly IDefaultValuesService _defaultValuesService;
    private readonly ILogger<DefaultValuesController> _logger;

    public DefaultValuesController(
        IDefaultValuesService defaultValuesService,
        ILogger<DefaultValuesController> logger)
    {
        _defaultValuesService = defaultValuesService;
        _logger = logger;
    }

    /// <summary>
    /// Gets typical/default values for resistance and powering parameters
    /// based on vessel characteristics
    /// </summary>
    /// <remarks>
    /// Provides smart defaults for:
    /// - Form factor (k)
    /// - Appendage area percentage
    /// - Roughness allowance (CA)
    /// - Propulsive efficiency (ηD) or decomposed (ηH, ηR, ηO)
    /// - Midship coefficient (CM)
    /// - Wetted surface area estimate
    /// </remarks>
    [HttpPost("typical")]
    [ProducesResponseType(typeof(DefaultValuesResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<DefaultValuesResponseDto> GetTypicalValues(
        [FromBody] DefaultValuesRequestDto request)
    {
        try
        {
            _logger.LogInformation(
                "Getting default values for VesselType={VesselType}, CB={CB}",
                request.VesselType, request.CB);

            var result = _defaultValuesService.GetDefaultValues(request);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for default values");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing default values");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets available vessel type categories
    /// </summary>
    [HttpGet("vessel-types")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public ActionResult<List<string>> GetVesselTypes()
    {
        var vesselTypes = new List<string>
        {
            "Tanker",
            "Bulker",
            "CargoShip",
            "ContainerShip",
            "Ferry",
            "Yacht",
            "Tug",
            "General"
        };

        return Ok(vesselTypes);
    }
}
