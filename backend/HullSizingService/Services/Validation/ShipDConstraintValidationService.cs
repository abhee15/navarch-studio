using Microsoft.Extensions.Logging;
using Shared.DTOs.ShipD;

namespace HullSizingService.Services.Validation;

/// <summary>
/// Implementation of ShipD constraint validation service
/// </summary>
public class ShipDConstraintValidationService : IShipDConstraintValidationService
{
    private readonly ILogger<ShipDConstraintValidationService> _logger;

    public ShipDConstraintValidationService(ILogger<ShipDConstraintValidationService> logger)
    {
        _logger = logger;
    }

    public ConstraintValidationResult ValidateLongitudinalProportions(
        decimal bowLengthRatio,
        decimal sternLengthRatio)
    {
        var sum = bowLengthRatio + sternLengthRatio;
        var isValid = sum < 1.0m;
        var margin = 1.0m - sum;

        var message = isValid
            ? $"Longitudinal proportions valid: Lb={bowLengthRatio:F3}, Ls={sternLengthRatio:F3}, Lm={margin:F3}"
            : $"Constraint violation: Lb + Ls = {sum:F3} >= 1.0. Midship length would be {margin:F3} (must be > 0)";

        var fixSuggestions = new List<string>();
        if (!isValid)
        {
            var excess = sum - 0.99m; // Target 0.99 to leave some margin
            fixSuggestions.Add($"Reduce Lb by {(excess / 2):F4} (new Lb: {Math.Max(0.05m, bowLengthRatio - excess / 2):F3})");
            fixSuggestions.Add($"Reduce Ls by {(excess / 2):F4} (new Ls: {Math.Max(0.05m, sternLengthRatio - excess / 2):F3})");
        }

        return new ConstraintValidationResult
        {
            IsValid = isValid,
            ConstraintName = "Constraint 0: Lb + Ls < 1.0",
            Message = message,
            FixSuggestions = fixSuggestions,
            Severity = isValid ? "Info" : "Error"
        };
    }

    public ConstraintValidationResult ValidateChineIntersection(
        decimal deadriseAngleDeg,
        decimal beamAtChine)
    {
        // Simplified validation: deadrise should be positive for inboard chine
        // More complex validation would require actual geometry intersection check
        var isValid = deadriseAngleDeg >= 0 && beamAtChine <= 1.0m;

        var message = isValid
            ? $"Chine intersection valid: Deadrise={deadriseAngleDeg:F1}°, BeamAtChine={beamAtChine:F3}"
            : $"Constraint violation: Chine intersection may be outboard. Deadrise={deadriseAngleDeg:F1}°, BeamAtChine={beamAtChine:F3}";

        var fixSuggestions = new List<string>();
        if (!isValid)
        {
            if (deadriseAngleDeg < 0)
            {
                fixSuggestions.Add("Increase Deadrise Angle to positive value");
            }
            if (beamAtChine > 1.0m)
            {
                fixSuggestions.Add($"Reduce Beam at Chine (Bc) to <= 1.0 (current: {beamAtChine:F3})");
            }
        }

        return new ConstraintValidationResult
        {
            IsValid = isValid,
            ConstraintName = "Constraint 6: Chine intersection inboard",
            Message = message,
            FixSuggestions = fixSuggestions,
            Severity = isValid ? "Info" : "Warning"
        };
    }

    public ConstraintValidationResult ValidateKeelChineRadii(
        decimal keelRadius,
        decimal chineRadius,
        decimal deadriseAngleDeg)
    {
        // Simplified validation: keel radius should be reasonable relative to chine
        // For full-form hulls, both radii should be small (tight corners)
        // This is a simplified check - full validation would require geometry analysis
        var isValid = keelRadius >= 0 && chineRadius >= 0 && keelRadius <= 0.5m && chineRadius <= 0.5m;

        var message = isValid
            ? $"Keel/Chine radii valid: Keel={keelRadius:F3}, Chine={chineRadius:F3}"
            : $"Constraint violation: Keel/Chine radii may cause intersection issues. Keel={keelRadius:F3}, Chine={chineRadius:F3}";

        var fixSuggestions = new List<string>();
        if (!isValid)
        {
            if (keelRadius > 0.5m || chineRadius > 0.5m)
            {
                fixSuggestions.Add("Reduce keel and chine radii (should be < 0.5 for full-form hulls)");
            }
            if (keelRadius < 0 || chineRadius < 0)
            {
                fixSuggestions.Add("Keel and chine radii must be non-negative");
            }
        }

        return new ConstraintValidationResult
        {
            IsValid = isValid,
            ConstraintName = "Constraint 7: Keel/Chine radii intersection",
            Message = message,
            FixSuggestions = fixSuggestions,
            Severity = isValid ? "Info" : "Warning"
        };
    }

    public ConstraintValidationResult ValidateBulbDimensions(
        decimal bulbWidthRatio,
        decimal bulbHeightRatio,
        decimal bowSectionWidthAtBulb)
    {
        // Bulb width must be less than bow section width at the bulb height
        var isValid = bulbWidthRatio < bowSectionWidthAtBulb && bulbWidthRatio > 0 && bulbHeightRatio > 0;

        var message = isValid
            ? $"Bulb dimensions valid: BulbWidth={bulbWidthRatio:F3}, BowSectionWidth={bowSectionWidthAtBulb:F3}"
            : $"Constraint violation: Bulb is wider ({bulbWidthRatio:F3}) than bow section ({bowSectionWidthAtBulb:F3}) at bulb height";

        var fixSuggestions = new List<string>();
        if (!isValid)
        {
            if (bulbWidthRatio >= bowSectionWidthAtBulb)
            {
                var maxWidth = bowSectionWidthAtBulb * 0.95m; // 95% of bow section width
                fixSuggestions.Add($"Reduce Bulb Width (Bbb) to < {maxWidth:F3} (current: {bulbWidthRatio:F3})");
            }
            if (bulbWidthRatio <= 0 || bulbHeightRatio <= 0)
            {
                fixSuggestions.Add("Bulb dimensions must be positive");
            }
        }

        return new ConstraintValidationResult
        {
            IsValid = isValid,
            ConstraintName = "Constraint 38: Bulb beam < hull beam at Z-height",
            Message = message,
            FixSuggestions = fixSuggestions,
            Severity = isValid ? "Info" : "Error"
        };
    }

    public AggregateConstraintValidationResult ValidateAllConstraints(
        decimal[] parameters,
        IReadOnlyList<ShipDParameterMetadataDto> metadata)
    {
        var results = new List<ConstraintValidationResult>();

        // Extract parameters using metadata lookup
        var lb = GetParameterValue(parameters, metadata, 1, "Lb");
        var ls = GetParameterValue(parameters, metadata, 2, "Ls");
        var deadrise = GetParameterValue(parameters, metadata, 19, "Cdrft");
        var bbb = GetParameterValue(parameters, metadata, 35, "Bbb");
        var hbb = GetParameterValue(parameters, metadata, 34, "Hbb");

        // Validate Constraint 0: Lb + Ls < 1.0
        if (lb.HasValue && ls.HasValue)
        {
            results.Add(ValidateLongitudinalProportions(lb.Value, ls.Value));
        }

        // Validate Constraint 6: Chine intersection (simplified)
        if (deadrise.HasValue)
        {
            // Approximate beam at chine (would need actual geometry for precise check)
            var beamAtChine = 0.8m; // Placeholder - should be calculated from geometry
            results.Add(ValidateChineIntersection(deadrise.Value * 180m / (decimal)Math.PI, beamAtChine)); // Convert to degrees
        }

        // Validate Constraint 38: Bulb dimensions (if bulb is enabled)
        var bitBb = GetParameterValue(parameters, metadata, 31, "bit_BB");
        if (bitBb.HasValue && bitBb.Value > 0.5m && bbb.HasValue && hbb.HasValue)
        {
            // Approximate bow section width at bulb height
            var bowSectionWidthAtBulb = 0.3m; // Placeholder - should be calculated from geometry
            results.Add(ValidateBulbDimensions(bbb.Value, hbb.Value, bowSectionWidthAtBulb));
        }

        var allValid = results.All(r => r.IsValid);
        var errorCount = results.Count(r => r.Severity == "Error" && !r.IsValid);
        var warningCount = results.Count(r => r.Severity == "Warning" && !r.IsValid);

        if (errorCount > 0)
        {
            _logger.LogWarning(
                "Constraint validation found {ErrorCount} errors and {WarningCount} warnings",
                errorCount, warningCount);
        }

        return new AggregateConstraintValidationResult
        {
            AllConstraintsValid = allValid,
            Results = results
        };
    }

    private decimal? GetParameterValue(
        decimal[] parameters,
        IReadOnlyList<ShipDParameterMetadataDto> metadata,
        int parameterIndex,
        string? label = null)
    {
        if (parameterIndex < 0 || parameterIndex >= parameters.Length)
        {
            return null;
        }

        // Try to find by index first
        var param = metadata.FirstOrDefault(m => m.ParameterIndex == parameterIndex);

        // If not found and label provided, try by label
        if (param == null && !string.IsNullOrEmpty(label))
        {
            param = metadata.FirstOrDefault(m =>
                m.Label != null && m.Label.Contains(label, StringComparison.OrdinalIgnoreCase));
        }

        return parameters[parameterIndex];
    }
}
