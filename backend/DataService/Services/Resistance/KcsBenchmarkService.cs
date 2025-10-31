using Shared.DTOs;

namespace DataService.Services.Resistance;

/// <summary>
/// Service for KCS benchmark validation
/// </summary>
public class KcsBenchmarkService
{
    private readonly HoltropMennenService _hmService;
    private readonly ILogger<KcsBenchmarkService> _logger;

    public KcsBenchmarkService(
        HoltropMennenService hmService,
        ILogger<KcsBenchmarkService> logger)
    {
        _hmService = hmService;
        _logger = logger;
    }

    /// <summary>
    /// Validates resistance calculations against KCS reference data
    /// </summary>
    public KcsBenchmarkResult ValidateBenchmark(
        KcsBenchmarkRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Running KCS benchmark for vessel {VesselId} with {Count} reference points",
            request.VesselId,
            request.ReferenceData.Count);

        if (request.ReferenceData.Count == 0)
        {
            throw new ArgumentException("Reference data cannot be empty", nameof(request));
        }

        // Extract speed grid from reference data
        var speedGrid = request.ReferenceData.Select(rp => rp.Speed).ToList();
        var referenceRt = request.ReferenceData.Select(rp => rp.RtReference).ToList();

        // Prepare HM inputs
        var hmInputs = new HoltropMennenInputs
        {
            LWL = request.LWL,
            B = request.B,
            T = request.T,
            CB = request.CB,
            CP = request.CP,
            CM = request.CM,
            LCB_pct = request.LCB_pct,
            S = request.S,
            TempC = request.TempC,
            SalinityPpt = request.SalinityPpt,
            ApplyFormFactor = true,
        };

        // Calculate resistance using Holtrop-Mennen
        var hmResult = _hmService.CalculateResistance(hmInputs, speedGrid, cancellationToken);

        // Map HM result to benchmark result
        var result = new KcsBenchmarkResult
        {
            SpeedGrid = speedGrid,
            CalculatedResistance = hmResult.TotalResistance.ToList(),
            ReferenceResistance = referenceRt,
            ErrorPercent = new List<decimal>(),
            MaeTolerance = request.MaeTolerancePercent,
            MaxTolerance = request.MaxTolerancePercent,
            CalculationDetails = new HoltropMennenCalculationResult
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
                EffectivePower = hmResult.EffectivePower,
            }
        };

        // Calculate errors
        decimal sumAbsoluteError = 0m;
        decimal maxError = 0m;

        for (int i = 0; i < speedGrid.Count; i++)
        {
            decimal rtCalc = hmResult.TotalResistance[i];
            decimal rtRef = referenceRt[i];

            if (rtRef > 0)
            {
                // Error % = |(RT_calc - RT_ref) / RT_ref| * 100
                decimal error = Math.Abs((rtCalc - rtRef) / rtRef) * 100m;
                result.ErrorPercent.Add(error);
                sumAbsoluteError += error;
                maxError = Math.Max(maxError, error);
            }
            else
            {
                // If reference is zero, set error to 0 (edge case)
                result.ErrorPercent.Add(0m);
            }
        }

        // Calculate MAE
        result.MeanAbsoluteError = sumAbsoluteError / speedGrid.Count;
        result.MaxError = maxError;

        // Check if benchmark passes
        result.Pass = result.MeanAbsoluteError <= request.MaeTolerancePercent &&
                      result.MaxError <= request.MaxTolerancePercent;

        _logger.LogInformation(
            "KCS benchmark complete: MAE={MAE}%, Max={Max}%, Pass={Pass}",
            result.MeanAbsoluteError,
            result.MaxError,
            result.Pass);

        return result;
    }
}
