using DataService.Data;
using DataService.Services.Hydrostatics;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;

namespace DataService.Services.Resistance;

/// <summary>
/// Service for generating speed-draft matrix (heatmap) calculations
/// </summary>
public class SpeedDraftMatrixService
{
    private readonly DataDbContext _context;
    private readonly HoltropMennenService _hmService;
    private readonly IHydroCalculator _hydroCalculator;
    private readonly ILogger<SpeedDraftMatrixService> _logger;

    public SpeedDraftMatrixService(
        DataDbContext context,
        HoltropMennenService hmService,
        IHydroCalculator hydroCalculator,
        ILogger<SpeedDraftMatrixService> logger)
    {
        _context = context;
        _hmService = hmService;
        _hydroCalculator = hydroCalculator;
        _logger = logger;
    }

    /// <summary>
    /// Computes a matrix of power/resistance values for a grid of speeds and drafts
    /// </summary>
    public async Task<SpeedDraftMatrixResult> CalculateMatrixAsync(
        SpeedDraftMatrixRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Computing speed-draft matrix for vessel {VesselId}: Speed [{MinSpeed}-{MaxSpeed}]×{SpeedSteps}, Draft [{MinDraft}-{MaxDraft}]×{DraftSteps}",
            request.VesselId, request.MinSpeed, request.MaxSpeed, request.SpeedSteps,
            request.MinDraft, request.MaxDraft, request.DraftSteps);

        // Validate vessel exists
        var vessel = await _context.Vessels.FindAsync(new object[] { request.VesselId }, cancellationToken);
        if (vessel == null)
        {
            throw new InvalidOperationException($"Vessel with ID {request.VesselId} not found");
        }

        // Generate speed grid
        var speedGrid = GenerateLinearGrid(request.MinSpeed, request.MaxSpeed, request.SpeedSteps);

        // Generate draft grid
        var draftGrid = GenerateLinearGrid(request.MinDraft, request.MaxDraft, request.DraftSteps);

        // Initialize result
        var result = new SpeedDraftMatrixResult
        {
            SpeedGrid = speedGrid,
            DraftGrid = draftGrid,
            PowerMatrix = new List<List<decimal>>(),
            ResistanceMatrix = new List<List<decimal>>(),
            FroudeNumberMatrix = new List<List<decimal>>(),
            PointDetails = new List<MatrixPointDetails>(),
            DesignPoints = request.DesignPoints,
            TrialPoints = request.TrialPoints,
            TotalPoints = speedGrid.Count * draftGrid.Count,
            CalculationMethod = "Holtrop-Mennen 1982"
        };

        // Calculate for each draft
        for (int draftIdx = 0; draftIdx < draftGrid.Count; draftIdx++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var draft = draftGrid[draftIdx];

            // Get hydrostatic properties at this draft (for form coefficients)
            HydroResultDto? hydroResult = null;
            try
            {
                hydroResult = await _hydroCalculator.ComputeAtDraftAsync(
                    request.VesselId,
                    loadcaseId: null,
                    draft,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Could not compute hydrostatics at draft {Draft}m, using defaults", draft);
            }

            // Prepare HM inputs for this draft
            var inputs = new HoltropMennenInputs
            {
                LWL = request.LWL ?? vessel.Lpp,
                B = request.B ?? vessel.Beam,
                T = draft, // Use current draft from grid
                CB = request.CB ?? hydroResult?.Cb ?? 0.65m,
                CP = request.CP ?? hydroResult?.Cp ?? 0.70m,
                CM = request.CM ?? hydroResult?.Cm ?? 0.98m,
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

            // Calculate resistance for all speeds at this draft
            var hmResult = _hmService.CalculateResistance(inputs, speedGrid, cancellationToken);

            // Add to matrices
            result.PowerMatrix.Add(hmResult.EffectivePower);
            result.ResistanceMatrix.Add(hmResult.TotalResistance);
            result.FroudeNumberMatrix.Add(hmResult.FroudeNumbers);

            // Store detailed breakdown for each point
            for (int speedIdx = 0; speedIdx < speedGrid.Count; speedIdx++)
            {
                var pointDetail = new MatrixPointDetails
                {
                    SpeedIndex = speedIdx,
                    DraftIndex = draftIdx,
                    Speed = speedGrid[speedIdx],
                    Draft = draft,
                    FrictionResistance = hmResult.FrictionResistance[speedIdx],
                    ResiduaryResistance = hmResult.ResiduaryResistance[speedIdx],
                    AppendageResistance = hmResult.AppendageResistance[speedIdx],
                    CorrelationAllowance = hmResult.CorrelationAllowance[speedIdx],
                    AirResistance = hmResult.AirResistance[speedIdx],
                    TotalResistance = hmResult.TotalResistance[speedIdx],
                    EffectivePower = hmResult.EffectivePower[speedIdx],
                    ReynoldsNumber = hmResult.ReynoldsNumbers[speedIdx],
                    FroudeNumber = hmResult.FroudeNumbers[speedIdx],
                    FrictionCoefficient = hmResult.FrictionCoefficients[speedIdx],
                    CB = inputs.CB ?? 0.65m,
                    CP = inputs.CP ?? 0.70m,
                    CM = inputs.CM ?? 0.98m
                };
                result.PointDetails.Add(pointDetail);
            }

            _logger.LogDebug(
                "Completed draft {DraftIdx}/{DraftTotal} ({Draft}m): Power range {MinPower}-{MaxPower} kW",
                draftIdx + 1, draftGrid.Count, draft,
                hmResult.EffectivePower.Min(), hmResult.EffectivePower.Max());
        }

        _logger.LogInformation(
            "Speed-draft matrix calculation complete: {TotalPoints} points, Power range {MinPower}-{MaxPower} kW",
            result.TotalPoints,
            result.PowerMatrix.SelectMany(row => row).Min(),
            result.PowerMatrix.SelectMany(row => row).Max());

        return result;
    }

    /// <summary>
    /// Generates a linear grid of values between min and max
    /// </summary>
    private List<decimal> GenerateLinearGrid(decimal min, decimal max, int steps)
    {
        if (steps < 2)
        {
            throw new ArgumentException("Grid must have at least 2 steps", nameof(steps));
        }

        if (max <= min)
        {
            throw new ArgumentException("Max must be greater than min");
        }

        var grid = new List<decimal>();
        decimal stepSize = (max - min) / (steps - 1);

        for (int i = 0; i < steps; i++)
        {
            grid.Add(min + i * stepSize);
        }

        return grid;
    }
}
