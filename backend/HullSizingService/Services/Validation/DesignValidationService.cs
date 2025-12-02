// ValidationTestCases is in test project, not in service project
// This file should not depend on test data
using Microsoft.Extensions.Logging;
using Shared.Models.Sizing;
using Shared.TestData;

namespace HullSizingService.Services.Validation;

/// <summary>
/// Implementation of design validation service
/// </summary>
public class DesignValidationService : IDesignValidationService
{
    private readonly ILogger<DesignValidationService> _logger;

    public DesignValidationService(ILogger<DesignValidationService> logger)
    {
        _logger = logger;
    }

    public DesignValidationResult ValidateAgainstExpectedRanges(
        CandidateDesign candidate,
        string vesselType,
        ValidationToleranceConfig? toleranceConfig = null)
    {
        toleranceConfig ??= new ValidationToleranceConfig();

        var result = new DesignValidationResult
        {
            IsValid = true,
            Warnings = new List<ValidationWarning>(),
            Errors = new List<ValidationError>(),
            Comparisons = new Dictionary<string, ComparisonData>()
        };

        // Get expected ranges based on vessel type
        var expectedRanges = GetExpectedRangesForVesselType(vesselType);
        if (expectedRanges == null)
        {
            result.Warnings.Add(new ValidationWarning
            {
                Field = "VesselType",
                Message = $"No expected ranges defined for vessel type '{vesselType}'. Validation skipped.",
                Severity = "Warning"
            });
            return result;
        }

        // Validate Cb
        ValidateCb(candidate, expectedRanges, toleranceConfig, result);

        // Validate Froude Number
        ValidateFroudeNumber(candidate, expectedRanges, toleranceConfig, result);

        // Validate principal dimensions
        ValidateDimensions(candidate, expectedRanges, toleranceConfig, result);

        // Validate form coefficients relationships
        ValidateCoefficientRelationships(candidate, result);

        // Determine overall validity
        result.IsValid = !result.Errors.Any();

        _logger.LogDebug(
            "Design validation for vessel type '{VesselType}': Valid={Valid}, Warnings={WarningCount}, Errors={ErrorCount}",
            vesselType, result.IsValid, result.Warnings.Count, result.Errors.Count);

        return result;
    }

    public AlexanderLimitValidationResult ValidateAlexanderLimit(
        decimal froudeNumber,
        decimal blockCoefficient)
    {
        // Input validation
        if (froudeNumber < 0)
        {
            _logger.LogWarning("ValidateAlexanderLimit called with negative Froude Number: {Fn}", froudeNumber);
            return new AlexanderLimitValidationResult
            {
                ViolatesLimit = true,
                FroudeNumber = froudeNumber,
                BlockCoefficient = blockCoefficient,
                MaxEfficientCb = 0,
                MarginPercent = 0,
                Severity = "Error",
                Message = $"Invalid Froude Number: {froudeNumber} (must be non-negative)"
            };
        }

        if (blockCoefficient < 0 || blockCoefficient > 1.5m)
        {
            _logger.LogWarning("ValidateAlexanderLimit called with invalid Block Coefficient: {Cb}", blockCoefficient);
            return new AlexanderLimitValidationResult
            {
                ViolatesLimit = true,
                FroudeNumber = froudeNumber,
                BlockCoefficient = blockCoefficient,
                MaxEfficientCb = 0,
                MarginPercent = 0,
                Severity = "Error",
                Message = $"Invalid Block Coefficient: {blockCoefficient} (must be in range [0, 1.5])"
            };
        }

        var maxEfficientCb = AlexanderLimitReference.InterpolateMaxEfficientCb(froudeNumber);

        // Validate interpolation result
        if (maxEfficientCb <= 0 || maxEfficientCb > 1.5m)
        {
            _logger.LogError("Alexander Limit interpolation returned invalid result: MaxCb={MaxCb} for Fn={Fn}", maxEfficientCb, froudeNumber);
            return new AlexanderLimitValidationResult
            {
                ViolatesLimit = true,
                FroudeNumber = froudeNumber,
                BlockCoefficient = blockCoefficient,
                MaxEfficientCb = maxEfficientCb,
                MarginPercent = 0,
                Severity = "Error",
                Message = $"Alexander Limit interpolation error: invalid MaxCb={maxEfficientCb:F3} for Fn={froudeNumber:F3}"
            };
        }

        var violatesLimit = AlexanderLimitReference.ViolatesLimit(froudeNumber, blockCoefficient);
        var marginPercent = AlexanderLimitReference.CalculateMarginPercent(froudeNumber, blockCoefficient);
        var severity = AlexanderLimitReference.GetSeverityLevel(froudeNumber, blockCoefficient);

        var message = violatesLimit
            ? $"Block Coefficient {blockCoefficient:F3} exceeds maximum efficient {maxEfficientCb:F3} for Froude Number {froudeNumber:F3}"
            : $"Design is within Alexander Limit (margin: {marginPercent:F1}%)";

        if (severity == "Warning")
        {
            message += $" - Approaching limit (margin < 5%)";
        }

        return new AlexanderLimitValidationResult
        {
            ViolatesLimit = violatesLimit,
            FroudeNumber = froudeNumber,
            BlockCoefficient = blockCoefficient,
            MaxEfficientCb = maxEfficientCb,
            MarginPercent = marginPercent,
            Severity = severity,
            Message = message
        };
    }

    public ResistanceTrendValidationResult ValidateResistanceTrend(
        decimal ehpKw,
        decimal displacementTonnes,
        string vesselType)
    {
        // Input validation
        if (ehpKw < 0)
        {
            _logger.LogWarning("ValidateResistanceTrend called with negative EHP: {EhpKw}", ehpKw);
            return new ResistanceTrendValidationResult
            {
                TrendCategory = "Unknown",
                EhpKw = ehpKw,
                EhpPerTonne = 0,
                ExpectedTrend = "Unknown",
                MatchesExpected = false,
                Severity = "Error",
                Message = $"Invalid EHP value: {ehpKw} kW (must be non-negative)"
            };
        }

        if (displacementTonnes <= 0)
        {
            _logger.LogWarning("ValidateResistanceTrend called with invalid displacement: {Displacement}", displacementTonnes);
            return new ResistanceTrendValidationResult
            {
                TrendCategory = "Unknown",
                EhpKw = ehpKw,
                EhpPerTonne = 0,
                ExpectedTrend = "Unknown",
                MatchesExpected = false,
                Severity = "Error",
                Message = $"Invalid displacement: {displacementTonnes} tonnes (must be positive)"
            };
        }

        var ehpPerTonne = ehpKw / displacementTonnes;

        // Categorize trend based on EHP per tonne
        string trendCategory;
        if (ehpPerTonne < 0.2m)
        {
            trendCategory = "Low";
        }
        else if (ehpPerTonne < 0.5m)
        {
            trendCategory = "Moderate";
        }
        else
        {
            trendCategory = "High";
        }

        // Get expected trend for vessel type
        var expectedTrend = GetExpectedEhpTrendForVesselType(vesselType);
        var matchesExpected = trendCategory.Equals(expectedTrend, StringComparison.OrdinalIgnoreCase);

        var severity = matchesExpected ? "Info" : "Warning";
        var message = matchesExpected
            ? $"EHP trend '{trendCategory}' matches expected '{expectedTrend}' for {vesselType}"
            : $"EHP trend '{trendCategory}' differs from expected '{expectedTrend}' for {vesselType}";

        return new ResistanceTrendValidationResult
        {
            TrendCategory = trendCategory,
            EhpKw = ehpKw,
            EhpPerTonne = ehpPerTonne,
            ExpectedTrend = expectedTrend,
            MatchesExpected = matchesExpected,
            Severity = severity,
            Message = message
        };
    }

    public FormCoefficientValidationResult ValidateFormCoefficients(
        FormCoefficients coefficients,
        string vesselType)
    {
        var result = new FormCoefficientValidationResult
        {
            IsValid = true,
            Checks = new List<CoefficientCheck>(),
            Warnings = new List<ValidationWarning>()
        };

        // Input validation
        if (coefficients.Cb < 0 || coefficients.Cb > 1.5m)
        {
            result.Warnings.Add(new ValidationWarning
            {
                Field = "Cb",
                Message = $"Block Coefficient {coefficients.Cb:F4} is outside reasonable range [0, 1.5]",
                Severity = "Error"
            });
            result.IsValid = false;
        }

        if (coefficients.Cp < 0 || coefficients.Cp > 1.5m)
        {
            result.Warnings.Add(new ValidationWarning
            {
                Field = "Cp",
                Message = $"Prismatic Coefficient {coefficients.Cp:F4} is outside reasonable range [0, 1.5]",
                Severity = "Error"
            });
            result.IsValid = false;
        }

        if (coefficients.Cm <= 0 || coefficients.Cm > 1.5m)
        {
            result.Warnings.Add(new ValidationWarning
            {
                Field = "Cm",
                Message = $"Midship Coefficient {coefficients.Cm:F4} is invalid (must be positive and <= 1.5)",
                Severity = "Error"
            });
            result.IsValid = false;
            return result; // Cannot validate relationships if Cm is invalid
        }

        // Validate Cp = Cb/Cm relationship
        if (coefficients.Cm > 0)
        {
            var expectedCp = coefficients.Cb / coefficients.Cm;

            // Additional check: Cb should be <= Cm (since Cp = Cb/Cm and Cp <= 1 typically)
            if (coefficients.Cb > coefficients.Cm)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Field = "CoefficientRelationship",
                    Message = $"Block Coefficient ({coefficients.Cb:F4}) exceeds Midship Coefficient ({coefficients.Cm:F4}), which is unusual (typically Cb <= Cm)",
                    Severity = "Warning"
                });
            }

            var cpDeviation = Math.Abs(coefficients.Cp - expectedCp);
            var relationshipValid = cpDeviation < 0.01m; // Allow 1% tolerance

            result.Checks.Add(new CoefficientCheck
            {
                Coefficient = "Cp",
                Value = coefficients.Cp,
                RelationshipValid = relationshipValid,
                RelationshipError = relationshipValid
                    ? null
                    : $"Cp ({coefficients.Cp:F4}) should equal Cb/Cm ({expectedCp:F4}). Deviation: {cpDeviation:F4}"
            });

            if (!relationshipValid)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Field = "Cp",
                    Message = $"Prismatic coefficient relationship invalid: Cp = {coefficients.Cp:F4}, expected Cb/Cm = {expectedCp:F4}",
                    Severity = "Warning"
                });
            }
        }

        // Validate Cb range for vessel type
        var expectedCbRange = GetExpectedCbRangeForVesselType(vesselType);
        if (expectedCbRange.HasValue)
        {
            var withinRange = coefficients.Cb >= expectedCbRange.Value.min && coefficients.Cb <= expectedCbRange.Value.max;

            result.Checks.Add(new CoefficientCheck
            {
                Coefficient = "Cb",
                Value = coefficients.Cb,
                ExpectedMin = expectedCbRange.Value.min,
                ExpectedMax = expectedCbRange.Value.max,
                IsWithinRange = withinRange
            });

            if (!withinRange)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Field = "Cb",
                    Message = $"Block coefficient {coefficients.Cb:F4} outside expected range [{expectedCbRange.Value.min:F4}, {expectedCbRange.Value.max:F4}] for {vesselType}",
                    Severity = "Warning"
                });
            }
        }

        // Validate Cm for full-form vessels (should be ≈ 0.99)
        if (vesselType.Contains("tanker", StringComparison.OrdinalIgnoreCase) ||
            vesselType.Contains("bulk", StringComparison.OrdinalIgnoreCase) ||
            vesselType.Contains("product_carrier", StringComparison.OrdinalIgnoreCase))
        {
            if (coefficients.Cm < 0.98m || coefficients.Cm > 1.0m)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Field = "Cm",
                    Message = $"Midship coefficient {coefficients.Cm:F4} outside typical range [0.98, 1.00] for full-form vessel. Expected ≈ 0.99.",
                    Severity = "Warning"
                });
            }
        }

        result.IsValid = !result.Warnings.Any(w => w.Severity == "Error");

        return result;
    }

    #region Private Helper Methods

    private ExpectedRanges? GetExpectedRangesForVesselType(string vesselType)
    {
        // Map vessel types to expected ranges based on naval architecture standards
        // These ranges are industry-standard values for different vessel types
        if (vesselType.Contains("product_carrier", StringComparison.OrdinalIgnoreCase))
        {
            // Product Carrier: 40,000 DWT typical (Lpp~185m, B~28m, Cb 0.79-0.80)
            return new ExpectedRanges
            {
                CbMin = 0.792m,
                CbMax = 0.80m,
                CbMean = 0.796m,
                FnMin = null, // Calculate from speed
                FnMax = null,
                LppMin = 175m,
                LppMax = 195m,
                BeamMin = 26m,
                BeamMax = 30m
            };
        }
        else if (vesselType.Contains("bulk", StringComparison.OrdinalIgnoreCase) || vesselType.Contains("vlcc", StringComparison.OrdinalIgnoreCase))
        {
            // Bulk Carrier/VLCC: Large, slow, full-form (Cb 0.82-0.86, Fn 0.13-0.15)
            return new ExpectedRanges
            {
                CbMin = 0.82m,
                CbMax = 0.86m,
                CbMean = 0.84m,
                FnMin = 0.13m,
                FnMax = 0.15m,
                LppMin = 300m,
                LppMax = 350m,
                BeamMin = 55m,
                BeamMax = 62m
            };
        }
        else if (vesselType.Contains("general_cargo", StringComparison.OrdinalIgnoreCase))
        {
            // General Cargo: Moderate speed, moderate fullness (Cb 0.60-0.70, Fn 0.20-0.25)
            return new ExpectedRanges
            {
                CbMin = 0.60m,
                CbMax = 0.70m,
                CbMean = 0.65m,
                FnMin = 0.20m,
                FnMax = 0.25m,
                LppMin = 180m,
                LppMax = 220m,
                BeamMin = 28m,
                BeamMax = 35m
            };
        }
        else if (vesselType.Contains("container", StringComparison.OrdinalIgnoreCase))
        {
            // Fast Container Ship: High speed, fine form (Cb 0.50-0.65, Fn 0.30+)
            return new ExpectedRanges
            {
                CbMin = 0.50m,
                CbMax = 0.65m,
                CbMean = 0.57m,
                FnMin = 0.30m,
                FnMax = 0.35m,
                LppMin = 240m,
                LppMax = 300m,
                BeamMin = 38m,
                BeamMax = 48m
            };
        }

        return null;
    }

    private void ValidateCb(
        CandidateDesign candidate,
        ExpectedRanges expectedRanges,
        ValidationToleranceConfig toleranceConfig,
        DesignValidationResult result)
    {
        var tolerance = toleranceConfig.CbTolerance;
        var minAcceptable = expectedRanges.CbMin - tolerance;
        var maxAcceptable = expectedRanges.CbMax + tolerance;

        var isWithinRange = candidate.Cb >= minAcceptable && candidate.Cb <= maxAcceptable;
        var deviationPercent = expectedRanges.CbMean > 0
            ? (candidate.Cb - expectedRanges.CbMean) / expectedRanges.CbMean * 100m
            : 0;

        result.Comparisons["Cb"] = new ComparisonData
        {
            Field = "Cb",
            ExpectedMin = expectedRanges.CbMin,
            ExpectedMax = expectedRanges.CbMax,
            ExpectedMean = expectedRanges.CbMean,
            Actual = candidate.Cb,
            IsWithinRange = isWithinRange,
            DeviationPercent = deviationPercent
        };

        if (!isWithinRange)
        {
            result.Warnings.Add(new ValidationWarning
            {
                Field = "Cb",
                Message = $"Block Coefficient {candidate.Cb:F4} outside expected range [{expectedRanges.CbMin:F4}, {expectedRanges.CbMax:F4}] ± {tolerance:F4}",
                Severity = "Warning"
            });
        }
    }

    private void ValidateFroudeNumber(
        CandidateDesign candidate,
        ExpectedRanges expectedRanges,
        ValidationToleranceConfig toleranceConfig,
        DesignValidationResult result)
    {
        if (!expectedRanges.FnMin.HasValue || !expectedRanges.FnMax.HasValue)
        {
            return; // No Fn range defined
        }

        var tolerance = toleranceConfig.FnTolerance;
        var minAcceptable = expectedRanges.FnMin.Value - tolerance;
        var maxAcceptable = expectedRanges.FnMax.Value + tolerance;

        var isWithinRange = candidate.Fn >= minAcceptable && candidate.Fn <= maxAcceptable;
        var expectedMean = (expectedRanges.FnMin.Value + expectedRanges.FnMax.Value) / 2m;
        var deviationPercent = expectedMean > 0
            ? (candidate.Fn - expectedMean) / expectedMean * 100m
            : 0;

        result.Comparisons["Fn"] = new ComparisonData
        {
            Field = "Fn",
            ExpectedMin = expectedRanges.FnMin,
            ExpectedMax = expectedRanges.FnMax,
            ExpectedMean = expectedMean,
            Actual = candidate.Fn,
            IsWithinRange = isWithinRange,
            DeviationPercent = deviationPercent
        };

        if (!isWithinRange)
        {
            result.Warnings.Add(new ValidationWarning
            {
                Field = "Fn",
                Message = $"Froude Number {candidate.Fn:F4} outside expected range [{expectedRanges.FnMin:F4}, {expectedRanges.FnMax:F4}] ± {tolerance:F4}",
                Severity = "Warning"
            });
        }
    }

    private void ValidateDimensions(
        CandidateDesign candidate,
        ExpectedRanges expectedRanges,
        ValidationToleranceConfig toleranceConfig,
        DesignValidationResult result)
    {
        var tolerancePercent = toleranceConfig.DimensionTolerancePercent / 100m;

        // Validate Lpp
        if (expectedRanges.LppMin.HasValue && expectedRanges.LppMax.HasValue)
        {
            var isWithinRange = candidate.LppM >= expectedRanges.LppMin.Value && candidate.LppM <= expectedRanges.LppMax.Value;
            var expectedMean = (expectedRanges.LppMin.Value + expectedRanges.LppMax.Value) / 2m;
            var deviationPercent = expectedMean > 0
                ? (candidate.LppM - expectedMean) / expectedMean * 100m
                : 0;

            result.Comparisons["Lpp"] = new ComparisonData
            {
                Field = "Lpp",
                ExpectedMin = expectedRanges.LppMin,
                ExpectedMax = expectedRanges.LppMax,
                ExpectedMean = expectedMean,
                Actual = candidate.LppM,
                IsWithinRange = isWithinRange,
                DeviationPercent = deviationPercent
            };

            if (!isWithinRange)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Field = "Lpp",
                    Message = $"Length {candidate.LppM:F2}m outside expected range [{expectedRanges.LppMin:F2}, {expectedRanges.LppMax:F2}]m",
                    Severity = "Warning"
                });
            }
        }

        // Validate Beam
        if (expectedRanges.BeamMin.HasValue && expectedRanges.BeamMax.HasValue)
        {
            var isWithinRange = candidate.BM >= expectedRanges.BeamMin.Value && candidate.BM <= expectedRanges.BeamMax.Value;
            var expectedMean = (expectedRanges.BeamMin.Value + expectedRanges.BeamMax.Value) / 2m;
            var deviationPercent = expectedMean > 0
                ? (candidate.BM - expectedMean) / expectedMean * 100m
                : 0;

            result.Comparisons["Beam"] = new ComparisonData
            {
                Field = "Beam",
                ExpectedMin = expectedRanges.BeamMin,
                ExpectedMax = expectedRanges.BeamMax,
                ExpectedMean = expectedMean,
                Actual = candidate.BM,
                IsWithinRange = isWithinRange,
                DeviationPercent = deviationPercent
            };

            if (!isWithinRange)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Field = "Beam",
                    Message = $"Beam {candidate.BM:F2}m outside expected range [{expectedRanges.BeamMin:F2}, {expectedRanges.BeamMax:F2}]m",
                    Severity = "Warning"
                });
            }
        }
    }

    private void ValidateCoefficientRelationships(
        CandidateDesign candidate,
        DesignValidationResult result)
    {
        // Validate Cp = Cb/Cm relationship
        if (candidate.Cm.HasValue && candidate.Cm.Value > 0)
        {
            var expectedCp = candidate.Cb / candidate.Cm.Value;
            var cpDeviation = Math.Abs(candidate.Cp - expectedCp);

            if (cpDeviation > 0.01m) // More than 1% deviation
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Field = "CoefficientRelationship",
                    Message = $"Prismatic coefficient relationship: Cp ({candidate.Cp:F4}) should equal Cb/Cm ({expectedCp:F4}). Deviation: {cpDeviation:F4}",
                    Severity = "Warning"
                });
            }
        }
    }

    private string GetExpectedEhpTrendForVesselType(string vesselType)
    {
        if (vesselType.Contains("bulk", StringComparison.OrdinalIgnoreCase) ||
            vesselType.Contains("tanker", StringComparison.OrdinalIgnoreCase) ||
            vesselType.Contains("vlcc", StringComparison.OrdinalIgnoreCase) ||
            vesselType.Contains("product_carrier", StringComparison.OrdinalIgnoreCase))
        {
            return "Low";
        }
        else if (vesselType.Contains("general_cargo", StringComparison.OrdinalIgnoreCase))
        {
            return "Moderate";
        }
        else if (vesselType.Contains("container", StringComparison.OrdinalIgnoreCase))
        {
            return "High";
        }

        return "Moderate"; // Default
    }

    private (decimal min, decimal max)? GetExpectedCbRangeForVesselType(string vesselType)
    {
        var ranges = GetExpectedRangesForVesselType(vesselType);
        if (ranges == null)
        {
            return null;
        }

        return (ranges.CbMin, ranges.CbMax);
    }

    private class ExpectedRanges
    {
        public decimal CbMin { get; init; }
        public decimal CbMax { get; init; }
        public decimal CbMean { get; init; }
        public decimal? FnMin { get; init; }
        public decimal? FnMax { get; init; }
        public decimal? LppMin { get; init; }
        public decimal? LppMax { get; init; }
        public decimal? BeamMin { get; init; }
        public decimal? BeamMax { get; init; }
    }

    #endregion
}
