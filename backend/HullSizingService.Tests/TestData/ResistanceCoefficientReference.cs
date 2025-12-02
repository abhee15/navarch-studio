namespace HullSizingService.Tests.TestData;

/// <summary>
/// Resistance coefficient reference data for Product Carrier validation.
///
/// This reference data validates the resistance calculation component of the solver
/// against known results for the 40,000 DWT Product Carrier calibration case.
///
/// Source: Ship Design Validation Handbook - Chart 2: Resistance Coefficient (Ct vs Speed)
/// </summary>
public static class ResistanceCoefficientReference
{
    /// <summary>
    /// Resistance coefficient data point for a specific speed
    /// </summary>
    public record ResistanceCoefficientPoint
    {
        /// <summary>Speed in knots</summary>
        public decimal SpeedKn { get; init; }

        /// <summary>Froude Number (calculated)</summary>
        public decimal FroudeNumber { get; init; }

        /// <summary>Target total resistance coefficient (Ct × 10^-3)</summary>
        public decimal TargetCt { get; init; }

        /// <summary>Validation tolerance (±range)</summary>
        public decimal Tolerance { get; init; }

        /// <summary>Minimum acceptable Ct value (TargetCt - Tolerance)</summary>
        public decimal MinCt => TargetCt - Tolerance;

        /// <summary>Maximum acceptable Ct value (TargetCt + Tolerance)</summary>
        public decimal MaxCt => TargetCt + Tolerance;
    }

    /// <summary>
    /// Reference resistance coefficient data for Product Carrier (40,000 DWT)
    ///
    /// These values are used to validate that the resistance calculation produces
    /// realistic results. The curve should follow the expected trend and stay within
    /// the validation ranges at each speed point.
    ///
    /// Source: Ship Design Validation Handbook - Chart 2
    ///
    /// Note: If the curve bends upward too sharply after 14 knots, the wave drag
    /// calculation is likely over-predicting resistance.
    /// </summary>
    public static readonly ResistanceCoefficientPoint[] ProductCarrierReferencePoints = new[]
    {
        new ResistanceCoefficientPoint
        {
            SpeedKn = 12.0m,
            FroudeNumber = 0.141m, // Approximate for Lpp=185m
            TargetCt = 12.35m, // × 10^-3
            Tolerance = 0.05m
        },
        new ResistanceCoefficientPoint
        {
            SpeedKn = 14.0m,
            FroudeNumber = 0.165m, // Approximate for Lpp=185m
            TargetCt = 2.48m, // × 10^-3
            Tolerance = 0.05m
        },
        new ResistanceCoefficientPoint
        {
            SpeedKn = 16.0m,
            FroudeNumber = 0.188m, // Approximate for Lpp=185m
            TargetCt = 2.60m, // × 10^-3
            Tolerance = 0.10m
        }
    };

    /// <summary>
    /// Gets the reference point for a specific speed (closest match).
    /// </summary>
    /// <param name="speedKn">Speed in knots</param>
    /// <returns>Closest reference point, or null if no match within reasonable range</returns>
    public static ResistanceCoefficientPoint? GetReferencePoint(decimal speedKn)
    {
        ResistanceCoefficientPoint? closest = null;
        decimal minDifference = decimal.MaxValue;

        foreach (var point in ProductCarrierReferencePoints)
        {
            var difference = Math.Abs(point.SpeedKn - speedKn);
            if (difference < minDifference && difference <= 2.0m) // Within 2 knots
            {
                minDifference = difference;
                closest = point;
            }
        }

        return closest;
    }

    /// <summary>
    /// Validates if a calculated resistance coefficient (Ct) is within acceptable range.
    /// </summary>
    /// <param name="speedKn">Speed in knots</param>
    /// <param name="calculatedCt">Calculated total resistance coefficient (× 10^-3)</param>
    /// <returns>True if within tolerance, false otherwise</returns>
    public static bool IsWithinTolerance(decimal speedKn, decimal calculatedCt)
    {
        var referencePoint = GetReferencePoint(speedKn);
        if (referencePoint == null)
        {
            // No reference point for this speed - cannot validate
            return false;
        }

        return calculatedCt >= referencePoint.MinCt && calculatedCt <= referencePoint.MaxCt;
    }

    /// <summary>
    /// Calculates the deviation from target as a percentage.
    /// </summary>
    /// <param name="speedKn">Speed in knots</param>
    /// <param name="calculatedCt">Calculated total resistance coefficient (× 10^-3)</param>
    /// <returns>Deviation percentage, or null if no reference point available</returns>
    public static decimal? CalculateDeviationPercent(decimal speedKn, decimal calculatedCt)
    {
        var referencePoint = GetReferencePoint(speedKn);
        if (referencePoint == null)
        {
            return null;
        }

        if (referencePoint.TargetCt == 0)
        {
            return null;
        }

        var deviation = (calculatedCt - referencePoint.TargetCt) / referencePoint.TargetCt * 100m;
        return deviation;
    }

    /// <summary>
    /// Gets validation result details for a calculated resistance coefficient.
    /// </summary>
    /// <param name="speedKn">Speed in knots</param>
    /// <param name="calculatedCt">Calculated total resistance coefficient (× 10^-3)</param>
    /// <returns>Validation result with details</returns>
    public static ResistanceValidationResult Validate(decimal speedKn, decimal calculatedCt)
    {
        var referencePoint = GetReferencePoint(speedKn);

        if (referencePoint == null)
        {
            return new ResistanceValidationResult
            {
                IsValid = false,
                HasReferencePoint = false,
                Message = $"No reference point available for speed {speedKn} knots"
            };
        }

        var withinTolerance = IsWithinTolerance(speedKn, calculatedCt);
        var deviationPercent = CalculateDeviationPercent(speedKn, calculatedCt);
        var severity = withinTolerance ? "Info" : (Math.Abs(deviationPercent ?? 0) > 20 ? "Error" : "Warning");

        return new ResistanceValidationResult
        {
            IsValid = withinTolerance,
            HasReferencePoint = true,
            SpeedKn = speedKn,
            CalculatedCt = calculatedCt,
            TargetCt = referencePoint.TargetCt,
            MinCt = referencePoint.MinCt,
            MaxCt = referencePoint.MaxCt,
            DeviationPercent = deviationPercent,
            Severity = severity,
            Message = withinTolerance
                ? $"Resistance coefficient within tolerance (±{referencePoint.Tolerance} × 10^-3)"
                : $"Resistance coefficient deviation: {deviationPercent:F1}% (target: {referencePoint.TargetCt}, calculated: {calculatedCt:F3})"
        };
    }

    /// <summary>
    /// Validation result for resistance coefficient
    /// </summary>
    public class ResistanceValidationResult
    {
        public bool IsValid { get; init; }
        public bool HasReferencePoint { get; init; }
        public decimal? SpeedKn { get; init; }
        public decimal? CalculatedCt { get; init; }
        public decimal? TargetCt { get; init; }
        public decimal? MinCt { get; init; }
        public decimal? MaxCt { get; init; }
        public decimal? DeviationPercent { get; init; }
        public string? Severity { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}

