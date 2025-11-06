using Asp.Versioning;
using DataService.Services.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataService.Controllers;

/// <summary>
/// API for propeller calculations using Wageningen B-Series
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/propellers")]
public class PropellerController : ControllerBase
{
    private readonly WageningenBSeriesService _wageningen;
    private readonly ILogger<PropellerController> _logger;

    public PropellerController(
        WageningenBSeriesService wageningen,
        ILogger<PropellerController> logger)
    {
        _wageningen = wageningen;
        _logger = logger;
    }

    /// <summary>
    /// Calculate Wageningen B-Series propeller performance
    /// </summary>
    /// <param name="request">Propeller parameters and operating point</param>
    /// <returns>KT, KQ, efficiency</returns>
    [HttpPost("wageningen/calculate")]
    [ProducesResponseType(typeof(PropellerPerformance), 200)]
    [ProducesResponseType(400)]
    public ActionResult<PropellerPerformance> CalculateWageningen(
        [FromBody] WageningenCalculateRequest request)
    {
        try
        {
            var result = _wageningen.CalculatePerformance(
                request.J, 
                request.Z, 
                request.AeA0, 
                request.PD
            );

            _logger.LogInformation(
                "Wageningen calculation: J={J:F3}, Z={Z}, AE/A0={AeA0:F2}, P/D={PD:F2} → η={Eta:P1}",
                request.J, request.Z, request.AeA0, request.PD, result.Efficiency);

            return Ok(result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning("Invalid Wageningen parameters: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message, parameter = ex.ParamName });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Wageningen service not initialized: {Message}", ex.Message);
            return StatusCode(500, new { error = "Propeller service not available" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Wageningen calculation");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Find optimal propeller operating point for required thrust
    /// </summary>
    /// <param name="request">Thrust requirements and propeller constraints</param>
    /// <returns>Optimal RPM and performance</returns>
    [HttpPost("wageningen/optimize")]
    [ProducesResponseType(typeof(PropellerOperatingPoint), 200)]
    [ProducesResponseType(400)]
    public ActionResult<PropellerOperatingPoint> OptimizeWageningen(
        [FromBody] WageningenOptimizeRequest request)
    {
        try
        {
            var result = _wageningen.FindOptimalPoint(
                requiredThrustN: request.RequiredThrustN,
                speedMs: request.SpeedMs,
                diameterM: request.DiameterM,
                rpmRange: (request.MinRPM, request.MaxRPM),
                Z: request.Z,
                AeA0: request.AeA0,
                PD: request.PD
            );

            _logger.LogInformation(
                "Wageningen optimization: T={T:F0}N @ V={V:F2}m/s → RPM={RPM:F0}, η={Eta:P1}",
                request.RequiredThrustN, request.SpeedMs, result.RPM, result.Performance.Efficiency);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Could not find optimal point: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Wageningen optimization");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get parameter ranges and typical values
    /// </summary>
    [HttpGet("wageningen/parameters")]
    [AllowAnonymous]
    public ActionResult<WageningenParametersInfo> GetParameters()
    {
        return Ok(new WageningenParametersInfo
        {
            AdvanceCoefficient = new ParameterRange { Min = 0.0, Max = 1.5, Typical = 0.7, Symbol = "J", Unit = "-" },
            NumberOfBlades = new ParameterRange { Min = 2, Max = 7, Typical = 4, Symbol = "Z", Unit = "-" },
            BladeAreaRatio = new ParameterRange { Min = 0.3, Max = 1.05, Typical = 0.55, Symbol = "AE/A0", Unit = "-" },
            PitchDiameterRatio = new ParameterRange { Min = 0.5, Max = 1.4, Typical = 1.0, Symbol = "P/D", Unit = "-" }
        });
    }
}

/// <summary>
/// Request for Wageningen calculation
/// </summary>
public record WageningenCalculateRequest(
    double J,      // Advance coefficient (0-1.5)
    int Z,         // Number of blades (2-7)
    double AeA0,   // Blade area ratio (0.3-1.05)
    double PD      // Pitch/diameter ratio (0.5-1.4)
);

/// <summary>
/// Request for Wageningen optimization
/// </summary>
public record WageningenOptimizeRequest(
    double RequiredThrustN,  // Required thrust in Newtons
    double SpeedMs,          // Ship speed in m/s
    double DiameterM,        // Propeller diameter in meters
    double MinRPM,           // Minimum RPM to search
    double MaxRPM,           // Maximum RPM to search
    int Z = 4,               // Number of blades (default 4)
    double AeA0 = 0.55,      // Blade area ratio (default 0.55)
    double PD = 1.0          // Pitch/diameter ratio (default 1.0)
);

/// <summary>
/// Parameter ranges for UI
/// </summary>
public class WageningenParametersInfo
{
    public ParameterRange AdvanceCoefficient { get; set; } = new();
    public ParameterRange NumberOfBlades { get; set; } = new();
    public ParameterRange BladeAreaRatio { get; set; } = new();
    public ParameterRange PitchDiameterRatio { get; set; } = new();
}

/// <summary>
/// Parameter range metadata
/// </summary>
public class ParameterRange
{
    public double Min { get; set; }
    public double Max { get; set; }
    public double Typical { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}

