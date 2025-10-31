using Asp.Versioning;
using DataService.Data;
using DataService.Services.Resistance;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;

namespace DataService.Controllers;

/// <summary>
/// Controller for resistance and powering calculations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/resistance")]
public class ResistanceCalculationController : ControllerBase
{
    private readonly DataDbContext _context;
    private readonly IResistanceCalculationService _resistanceCalc;
    private readonly HoltropMennenService _hmService;
    private readonly ILogger<ResistanceCalculationController> _logger;

    public ResistanceCalculationController(
        DataDbContext context,
        IResistanceCalculationService resistanceCalc,
        HoltropMennenService hmService,
        ILogger<ResistanceCalculationController> logger)
    {
        _context = context;
        _resistanceCalc = resistanceCalc;
        _hmService = hmService;
        _logger = logger;
    }

    /// <summary>
    /// Calculates ITTC-57 friction coefficients for a speed grid
    /// </summary>
    [HttpPost("ittc57")]
    [ProducesResponseType(typeof(Ittc57CalculationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CalculateIttc57(
        [FromBody] Ittc57CalculationRequest request,
        CancellationToken cancellationToken)
    {
        // Validate vessel exists
        var vessel = await _context.Vessels.FindAsync(new object[] { request.VesselId }, cancellationToken);
        if (vessel == null)
        {
            return NotFound(new { error = $"Vessel with ID {request.VesselId} not found" });
        }

        // Get speed grid
        var speedGrid = await _context.SpeedGrids
            .Include(sg => sg.SpeedPoints.OrderBy(sp => sp.DisplayOrder))
            .FirstOrDefaultAsync(sg => sg.Id == request.SpeedGridId && sg.VesselId == request.VesselId, cancellationToken);

        if (speedGrid == null)
        {
            return NotFound(new { error = $"Speed grid with ID {request.SpeedGridId} not found" });
        }

        if (speedGrid.SpeedPoints.Count == 0)
        {
            return BadRequest(new { error = "Speed grid has no speed points" });
        }

        // Get water properties
        var waterProps = new WaterPropertiesService(
            new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<WaterPropertiesService>());
        decimal nu = waterProps.GetKinematicViscosity(request.TempC ?? 15, request.SalinityPpt ?? 35.0m);

        // Prepare result
        var result = new Ittc57CalculationResult
        {
            SpeedGrid = speedGrid.SpeedPoints.Select(sp => sp.Speed).ToList(),
            ReynoldsNumbers = new List<decimal>(),
            FroudeNumbers = new List<decimal>(),
            FrictionCoefficients = new List<decimal>(),
            EffectiveFrictionCoefficients = new List<decimal>(),
            FormFactor = request.FormFactor
        };

        // Calculate for each speed
        foreach (var speedPoint in speedGrid.SpeedPoints)
        {
            decimal re = _resistanceCalc.CalculateReynoldsNumber(speedPoint.Speed, vessel.Lpp, nu);
            decimal fn = _resistanceCalc.CalculateFroudeNumber(speedPoint.Speed, vessel.Lpp);
            decimal cf = _resistanceCalc.CalculateIttc57Cf(re);
            decimal k = request.FormFactor ?? 0.20m; // Default from config
            decimal cfEff = _resistanceCalc.CalculateEffectiveCf(cf, k, request.ApplyFormFactor);

            result.ReynoldsNumbers.Add(re);
            result.FroudeNumbers.Add(fn);
            result.FrictionCoefficients.Add(cf);
            result.EffectiveFrictionCoefficients.Add(cfEff);
        }

        _logger.LogInformation(
            "ITTC-57 calculation complete for vessel {VesselId}, grid {GridId}, {Count} speeds",
            request.VesselId, request.SpeedGridId, result.SpeedGrid.Count);

        return Ok(result);
    }

    /// <summary>
    /// Calculates total resistance using Holtrop-Mennen 1982 method
    /// </summary>
    [HttpPost("holtrop-mennen")]
    [ProducesResponseType(typeof(HoltropMennenCalculationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CalculateHoltropMennen(
        [FromBody] HoltropMennenCalculationRequest request,
        CancellationToken cancellationToken)
    {
        // Validate vessel exists
        var vessel = await _context.Vessels.FindAsync(new object[] { request.VesselId }, cancellationToken);
        if (vessel == null)
        {
            return NotFound(new { error = $"Vessel with ID {request.VesselId} not found" });
        }

        // Get speed grid
        var speedGrid = await _context.SpeedGrids
            .Include(sg => sg.SpeedPoints.OrderBy(sp => sp.DisplayOrder))
            .FirstOrDefaultAsync(sg => sg.Id == request.SpeedGridId && sg.VesselId == request.VesselId, cancellationToken);

        if (speedGrid == null)
        {
            return NotFound(new { error = $"Speed grid with ID {request.SpeedGridId} not found" });
        }

        if (speedGrid.SpeedPoints.Count == 0)
        {
            return BadRequest(new { error = "Speed grid has no speed points" });
        }

        // Prepare inputs (use vessel defaults if not provided)
        var inputs = new HoltropMennenInputs
        {
            LWL = request.LWL ?? vessel.Lpp, // Use Lpp as approximation for LWL
            B = request.B ?? vessel.Beam,
            T = request.T ?? vessel.DesignDraft,
            CB = request.CB,
            CP = request.CP,
            CM = request.CM,
            LCB_pct = request.LCB_pct,
            S = request.S,
            AppendageFactor = request.AppendageFactor,
            A_transom = request.A_transom,
            WindageArea = request.WindageArea ?? 0m,
            TempC = request.TempC ?? 15,
            SalinityPpt = request.SalinityPpt ?? 35.0m,
            K = request.K,
            ApplyFormFactor = request.ApplyFormFactor
        };

        // Get speed grid values
        var speedGridValues = speedGrid.SpeedPoints.Select(sp => sp.Speed).ToList();

        // Calculate resistance
        var hmResult = _hmService.CalculateResistance(inputs, speedGridValues, cancellationToken);

        // Map to DTO
        var result = new HoltropMennenCalculationResult
        {
            SpeedGrid = hmResult.SpeedGrid,
            ReynoldsNumbers = hmResult.ReynoldsNumbers,
            FroudeNumbers = hmResult.FroudeNumbers,
            FrictionCoefficients = hmResult.FrictionCoefficients,
            EffectiveFrictionCoefficients = hmResult.EffectiveFrictionCoefficients,
            FrictionResistance = hmResult.FrictionResistance,
            ResiduaryResistance = hmResult.ResiduaryResistance,
            AppendageResistance = hmResult.AppendageResistance,
            CorrelationAllowance = hmResult.CorrelationAllowance,
            AirResistance = hmResult.AirResistance,
            TotalResistance = hmResult.TotalResistance,
            EffectivePower = hmResult.EffectivePower
        };

        _logger.LogInformation(
            "Holtrop-Mennen calculation complete for vessel {VesselId}, grid {GridId}, {Count} speeds, RT range {MinRT}-{MaxRT} N",
            request.VesselId, request.SpeedGridId, result.SpeedGrid.Count,
            result.TotalResistance.Min(), result.TotalResistance.Max());

        return Ok(result);
    }
}
