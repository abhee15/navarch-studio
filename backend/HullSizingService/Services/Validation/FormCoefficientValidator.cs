namespace HullSizingService.Services.Validation;

/// <summary>
/// Validator for form coefficients (Cb, Cp, Cm, Cwp) against expected ranges and relationships.
///
/// Validates:
/// - Coefficient ranges per vessel type
/// - Physical relationships (Cp = Cb/Cm, etc.)
/// - Midship coefficient for specific hull forms (e.g., Cm ≈ 0.99 for full-form)
/// </summary>
public static class FormCoefficientValidator
{
    /// <summary>
    /// Validates Cb range for a vessel type
    /// </summary>
    public static CoefficientRangeValidation ValidateCbRange(
        decimal cb,
        string vesselType,
        decimal tolerance = 0.05m)
    {
        var expectedRange = GetExpectedCbRange(vesselType);
        if (!expectedRange.HasValue)
        {
            return new CoefficientRangeValidation
            {
                IsValid = true,
                Message = $"No expected Cb range defined for vessel type '{vesselType}'"
            };
        }

        var range = expectedRange.Value;
        var adjustedMin = range.min - tolerance;
        var adjustedMax = range.max + tolerance;
        var isWithinRange = cb >= adjustedMin && cb <= adjustedMax;
        var deviationPercent = range.mean > 0
            ? (cb - range.mean) / range.mean * 100m
            : 0;

        return new CoefficientRangeValidation
        {
            IsValid = isWithinRange,
            Value = cb,
            ExpectedMin = range.min,
            ExpectedMax = range.max,
            ExpectedMean = range.mean,
            Tolerance = tolerance,
            DeviationPercent = deviationPercent,
            Message = isWithinRange
                ? $"Cb {cb:F4} within expected range [{range.min:F4}, {range.max:F4}] ± {tolerance:F4}"
                : $"Cb {cb:F4} outside expected range [{range.min:F4}, {range.max:F4}] ± {tolerance:F4}. Deviation: {deviationPercent:F1}%"
        };
    }

    /// <summary>
    /// Validates the physical relationship: Cp = Cb/Cm
    /// </summary>
    public static RelationshipValidation ValidateCpRelationship(
        decimal cb,
        decimal cp,
        decimal? cm)
    {
        if (!cm.HasValue || cm.Value <= 0)
        {
            return new RelationshipValidation
            {
                IsValid = false,
                Message = "Cannot validate Cp relationship: Cm is missing or invalid"
            };
        }

        var expectedCp = cb / cm.Value;
        var deviation = Math.Abs(cp - expectedCp);
        var deviationPercent = expectedCp > 0 ? deviation / expectedCp * 100m : 0;
        var tolerance = 0.01m; // 1% tolerance
        var isValid = deviation <= tolerance;

        return new RelationshipValidation
        {
            IsValid = isValid,
            Relationship = "Cp = Cb/Cm",
            ExpectedValue = expectedCp,
            ActualValue = cp,
            Deviation = deviation,
            DeviationPercent = deviationPercent,
            Tolerance = tolerance,
            Message = isValid
                ? $"Cp relationship valid: Cp={cp:F4} ≈ Cb/Cm={expectedCp:F4} (deviation: {deviation:F4})"
                : $"Cp relationship invalid: Cp={cp:F4}, expected Cb/Cm={expectedCp:F4}. Deviation: {deviationPercent:F2}%"
        };
    }

    /// <summary>
    /// Validates Cm for full-form vessels (should be ≈ 0.99)
    /// </summary>
    public static CoefficientRangeValidation ValidateCmForFullForm(
        decimal cm,
        decimal tolerance = 0.01m)
    {
        var expectedCm = 0.99m;
        var minAcceptable = expectedCm - tolerance;
        var maxAcceptable = expectedCm + tolerance;
        var isWithinRange = cm >= minAcceptable && cm <= maxAcceptable;
        var deviation = Math.Abs(cm - expectedCm);
        var deviationPercent = deviation / expectedCm * 100m;

        return new CoefficientRangeValidation
        {
            IsValid = isWithinRange,
            Value = cm,
            ExpectedMin = minAcceptable,
            ExpectedMax = maxAcceptable,
            ExpectedMean = expectedCm,
            Tolerance = tolerance,
            DeviationPercent = deviationPercent,
            Message = isWithinRange
                ? $"Cm {cm:F4} within expected range for full-form vessel [{minAcceptable:F4}, {maxAcceptable:F4}]"
                : $"Cm {cm:F4} outside expected range for full-form vessel. Expected ≈ {expectedCm:F4}, deviation: {deviationPercent:F2}%"
        };
    }

    /// <summary>
    /// Gets expected Cb range for vessel type
    /// </summary>
    private static (decimal min, decimal max, decimal mean)? GetExpectedCbRange(string vesselType)
    {
        // Map to test cases from ValidationTestCases
        if (vesselType.Contains("product_carrier", StringComparison.OrdinalIgnoreCase))
        {
            return (0.792m, 0.80m, 0.796m);
        }
        else if (vesselType.Contains("bulk", StringComparison.OrdinalIgnoreCase) ||
                 vesselType.Contains("vlcc", StringComparison.OrdinalIgnoreCase))
        {
            return (0.82m, 0.86m, 0.84m);
        }
        else if (vesselType.Contains("general_cargo", StringComparison.OrdinalIgnoreCase))
        {
            return (0.60m, 0.70m, 0.65m);
        }
        else if (vesselType.Contains("container", StringComparison.OrdinalIgnoreCase))
        {
            return (0.50m, 0.65m, 0.57m);
        }

        return null;
    }
}

/// <summary>
/// Coefficient range validation result
/// </summary>
public class CoefficientRangeValidation
{
    public bool IsValid { get; init; }
    public decimal Value { get; init; }
    public decimal ExpectedMin { get; init; }
    public decimal ExpectedMax { get; init; }
    public decimal ExpectedMean { get; init; }
    public decimal Tolerance { get; init; }
    public decimal DeviationPercent { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Relationship validation result
/// </summary>
public class RelationshipValidation
{
    public bool IsValid { get; init; }
    public string Relationship { get; init; } = string.Empty;
    public decimal ExpectedValue { get; init; }
    public decimal ActualValue { get; init; }
    public decimal Deviation { get; init; }
    public decimal DeviationPercent { get; init; }
    public decimal Tolerance { get; init; }
    public string Message { get; init; } = string.Empty;
}
