using Microsoft.Extensions.Logging;
using Shared.DTOs.Catalog;

namespace HullSizingService.Services.DataDriven;

/// <summary>
/// Converts and scales parametric hulls from LOA=10m baseline to target displacement
/// Uses cube-root scaling law while preserving form coefficients
/// </summary>
public class ParametricConverter
{
    private readonly ILogger<ParametricConverter> _logger;

    public ParametricConverter(ILogger<ParametricConverter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Convert parametric hull (LOA=10m) to target displacement
    /// Apply cube-root scaling and constraints
    /// </summary>
    public ConvertedParametricHull ConvertToTargetDisplacement(
        SimilarParametricHullDto sourceHull,
        decimal targetDisplacement,
        ParametricScalingConstraints? constraints = null)
    {
        try
        {
            // Step 1: Calculate source displacement @ LOA=10m
            var sourceVolume = sourceHull.LppM * sourceHull.BeamM * sourceHull.DraftM * sourceHull.Cb;
            var sourceDisplacement = sourceVolume * 1.025m;  // Seawater density t/m³

            _logger.LogDebug(
                "Converting {HullId}: Source disp={SourceDisp:F1}t → Target disp={TargetDisp:F1}t",
                sourceHull.HullIdString, sourceDisplacement, targetDisplacement);

            // Step 2: Calculate uniform scale factor (cube-root law)
            // Displacement ∝ L³ → k = (Δ_target / Δ_source)^(1/3)
            var scaleFactor = (decimal)Math.Pow(
                (double)(targetDisplacement / sourceDisplacement),
                1.0 / 3.0);

            // Step 3: Scale dimensions uniformly
            var converted = new ConvertedParametricHull
            {
                SourceHullId = sourceHull.HullIdString,
                SourceDataset = sourceHull.DatasetSource,
                ScaleFactor = scaleFactor,

                // Scale dimensions
                Lpp = sourceHull.LppM * scaleFactor,
                Beam = sourceHull.BeamM * scaleFactor,
                Draft = sourceHull.DraftM * scaleFactor,
                Depth = sourceHull.DepthM * scaleFactor,

                // Form coefficients preserved (dimensionless ratios)
                Cb = sourceHull.Cb,
                Cp = sourceHull.Cp,
                Cm = sourceHull.Cm,
                Cw = sourceHull.Cw,

                // Geometric features scaled
                Volume = sourceVolume * (decimal)Math.Pow((double)scaleFactor, 3),
                LCB = (decimal)sourceHull.LcbNorm * sourceHull.LppM * scaleFactor,

                // Provenance
                SimilarityScore = sourceHull.SimilarityScore,
                ConversionQuality = sourceHull.ConversionQuality
            };

            // Step 4: Apply constraints if provided
            if (constraints != null)
            {
                ApplyConstraintsAndCompensate(converted, constraints);
            }

            // Step 5: Validate result
            ValidateConversion(converted, targetDisplacement);

            _logger.LogDebug(
                "Converted hull: Lpp={Lpp:F2}m, B={B:F2}m, T={T:F2}m, Cb={Cb:F3}, Scale={Scale:F3}",
                converted.Lpp, converted.Beam, converted.Draft, converted.Cb, scaleFactor);

            return converted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting parametric hull {HullId}", sourceHull.HullIdString);
            throw;
        }
    }

    /// <summary>
    /// Apply dimensional constraints and compensate to maintain displacement
    /// </summary>
    private void ApplyConstraintsAndCompensate(
        ConvertedParametricHull hull,
        ParametricScalingConstraints constraints)
    {
        var originalVolume = hull.Volume;
        var wasConstrained = false;

        // Apply beam constraint
        if (constraints.MaxBeam.HasValue && hull.Beam > constraints.MaxBeam.Value)
        {
            _logger.LogDebug("Beam constrained: {Original:F2}m → {Max:F2}m",
                hull.Beam, constraints.MaxBeam.Value);
            hull.Beam = constraints.MaxBeam.Value;
            wasConstrained = true;
        }

        // Apply draft constraint
        if (constraints.MaxDraft.HasValue && hull.Draft > constraints.MaxDraft.Value)
        {
            _logger.LogDebug("Draft constrained: {Original:F2}m → {Max:F2}m",
                hull.Draft, constraints.MaxDraft.Value);
            hull.Draft = constraints.MaxDraft.Value;
            wasConstrained = true;
        }

        // Compensate for constraints by adjusting Lpp to restore volume
        if (wasConstrained)
        {
            // V = Lpp × B × T × Cb
            // Lpp_new = V_original / (B_new × T_new × Cb)
            var requiredLpp = originalVolume / (hull.Beam * hull.Draft * hull.Cb);

            _logger.LogDebug("Compensating Lpp: {Original:F2}m → {New:F2}m to restore volume",
                hull.Lpp, requiredLpp);

            hull.Lpp = requiredLpp;

            // Update depth proportionally (maintain D/T ratio)
            var depthDraftRatio = hull.Depth / hull.Draft;
            hull.Depth = hull.Draft * depthDraftRatio;
        }
    }

    /// <summary>
    /// Validate conversion result
    /// </summary>
    private void ValidateConversion(
        ConvertedParametricHull hull,
        decimal targetDisplacement)
    {
        // Check 1: Dimensions positive
        if (hull.Lpp <= 0 || hull.Beam <= 0 || hull.Draft <= 0)
        {
            hull.IsValid = false;
            hull.ValidationErrors.Add("Invalid dimensions (<=0)");
            return;
        }

        // Check 2: Coefficients in range
        if (hull.Cb < 0.25m || hull.Cb > 0.98m)
        {
            hull.IsValid = false;
            hull.ValidationErrors.Add($"Cb out of range: {hull.Cb:F3}");
            return;
        }

        // Check 3: Displacement balance
        var calculatedVolume = hull.Lpp * hull.Beam * hull.Draft * hull.Cb;
        var calculatedDisplacement = calculatedVolume * 1.025m;
        var displacementError = Math.Abs(calculatedDisplacement - targetDisplacement) / targetDisplacement;

        hull.DisplacementError = displacementError;

        if (displacementError > 0.10m)  // >10% error
        {
            hull.IsValid = false;
            hull.ValidationErrors.Add($"Displacement error: {displacementError:P2}");
        }
        else if (displacementError > 0.05m)  // 5-10% error
        {
            hull.ValidationWarnings.Add($"Displacement error: {displacementError:P2}");
        }

        // Check 4: Reasonable ratios
        var LB_ratio = hull.Lpp / hull.Beam;
        var BT_ratio = hull.Beam / hull.Draft;

        if (LB_ratio < 3.0m || LB_ratio > 12.0m)
        {
            hull.ValidationWarnings.Add($"Unusual L/B ratio: {LB_ratio:F2}");
        }

        if (BT_ratio < 1.5m || BT_ratio > 5.0m)
        {
            hull.ValidationWarnings.Add($"Unusual B/T ratio: {BT_ratio:F2}");
        }

        // Check 5: Scale factor reasonable
        if (hull.ScaleFactor < 0.5m || hull.ScaleFactor > 5.0m)
        {
            hull.ValidationWarnings.Add($"Large scale factor: {hull.ScaleFactor:F2}x");
        }

        // Overall validity
        hull.IsValid = !hull.ValidationErrors.Any();
    }
}

/// <summary>
/// Scaling constraints for parametric conversion (beam/draft limits)
/// </summary>
public class ParametricScalingConstraints
{
    public decimal? MaxBeam { get; set; }
    public decimal? MaxDraft { get; set; }
}

/// <summary>
/// Result of parametric hull conversion and scaling
/// </summary>
public class ConvertedParametricHull
{
    // Source provenance
    public string SourceHullId { get; set; } = string.Empty;
    public string SourceDataset { get; set; } = string.Empty;
    public decimal ScaleFactor { get; set; }

    // Scaled dimensions
    public decimal Lpp { get; set; }
    public decimal Beam { get; set; }
    public decimal Draft { get; set; }
    public decimal Depth { get; set; }

    // Form coefficients (preserved)
    public decimal Cb { get; set; }
    public decimal? Cp { get; set; }
    public decimal? Cm { get; set; }
    public decimal Cw { get; set; }

    // Geometric features
    public decimal Volume { get; set; }
    public decimal LCB { get; set; }

    // Similarity
    public double SimilarityScore { get; set; }

    // Quality
    public string ConversionQuality { get; set; } = string.Empty;

    // Validation
    public bool IsValid { get; set; } = true;
    public decimal DisplacementError { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> ValidationWarnings { get; set; } = new();
}
