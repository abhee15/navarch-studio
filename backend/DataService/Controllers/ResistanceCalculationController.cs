using System.ComponentModel.DataAnnotations;
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
    private readonly PowerCalculationService _powerService;
    private readonly KcsBenchmarkService _kcsBenchmarkService;
    private readonly SpeedDraftMatrixService _matrixService;
    private readonly ILogger<ResistanceCalculationController> _logger;

    public ResistanceCalculationController(
        DataDbContext context,
        IResistanceCalculationService resistanceCalc,
        HoltropMennenService hmService,
        PowerCalculationService powerService,
        KcsBenchmarkService kcsBenchmarkService,
        SpeedDraftMatrixService matrixService,
        ILogger<ResistanceCalculationController> logger)
    {
        _context = context;
        _resistanceCalc = resistanceCalc;
        _hmService = hmService;
        _powerService = powerService;
        _kcsBenchmarkService = kcsBenchmarkService;
        _matrixService = matrixService;
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

    /// <summary>
    /// Calculates delivered and installed power from effective power
    /// </summary>
    [HttpPost("power-curves")]
    [ProducesResponseType(typeof(PowerCurveResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculatePowerCurves(
        [FromBody] PowerCurveRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = _powerService.CalculatePowerCurves(request);

            _logger.LogInformation(
                "Power curve calculation complete: {Count} speeds, SM={ServiceMargin}%",
                result.SpeedGrid.Count,
                result.ServiceMargin);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid power curve request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating power curves");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Validates resistance calculations against KCS benchmark reference data
    /// </summary>
    [HttpPost("kcs-benchmark")]
    [ProducesResponseType(typeof(KcsBenchmarkResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateKcsBenchmark(
        [FromBody] KcsBenchmarkRequest request,
        CancellationToken cancellationToken)
    {
        // Validate vessel exists
        var vessel = await _context.Vessels.FindAsync(new object[] { request.VesselId }, cancellationToken);
        if (vessel == null)
        {
            return NotFound(new { error = $"Vessel with ID {request.VesselId} not found" });
        }

        // Validate speed grid exists (optional - might use reference data speeds)
        if (request.SpeedGridId != Guid.Empty)
        {
            var speedGrid = await _context.SpeedGrids
                .FirstOrDefaultAsync(sg => sg.Id == request.SpeedGridId && sg.VesselId == request.VesselId, cancellationToken);

            if (speedGrid == null)
            {
                return NotFound(new { error = $"Speed grid with ID {request.SpeedGridId} not found" });
            }
        }

        try
        {
            var result = _kcsBenchmarkService.ValidateBenchmark(request, cancellationToken);

            _logger.LogInformation(
                "KCS benchmark validation complete for vessel {VesselId}: Pass={Pass}, MAE={MAE}%, Max={Max}%",
                request.VesselId,
                result.Pass,
                result.MeanAbsoluteError,
                result.MaxError);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid KCS benchmark request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating KCS benchmark");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Calculates speed-draft matrix (heatmap) for resistance and power analysis
    /// </summary>
    [HttpPost("speed-draft-matrix")]
    [ProducesResponseType(typeof(SpeedDraftMatrixResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CalculateSpeedDraftMatrix(
        [FromBody] SpeedDraftMatrixRequest request,
        CancellationToken cancellationToken)
    {
        // Validate vessel exists
        var vessel = await _context.Vessels.FindAsync(new object[] { request.VesselId }, cancellationToken);
        if (vessel == null)
        {
            return NotFound(new { error = $"Vessel with ID {request.VesselId} not found" });
        }

        // Validate input ranges
        if (request.MinSpeed >= request.MaxSpeed)
        {
            return BadRequest(new { error = "MaxSpeed must be greater than MinSpeed" });
        }

        if (request.MinDraft >= request.MaxDraft)
        {
            return BadRequest(new { error = "MaxDraft must be greater than MinDraft" });
        }

        if (request.SpeedSteps < 2 || request.SpeedSteps > 100)
        {
            return BadRequest(new { error = "SpeedSteps must be between 2 and 100" });
        }

        if (request.DraftSteps < 2 || request.DraftSteps > 100)
        {
            return BadRequest(new { error = "DraftSteps must be between 2 and 100" });
        }

        try
        {
            var result = await _matrixService.CalculateMatrixAsync(request, cancellationToken);

            _logger.LogInformation(
                "Speed-draft matrix calculation complete for vessel {VesselId}: {TotalPoints} points, Power range {MinPower}-{MaxPower} kW",
                request.VesselId,
                result.TotalPoints,
                result.PowerMatrix.SelectMany(row => row).Min(),
                result.PowerMatrix.SelectMany(row => row).Max());

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid speed-draft matrix request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating speed-draft matrix");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }
}
