using Microsoft.Extensions.Logging;
using Shared.Models;

namespace HullSizingService.Services.DataDriven;

/// <summary>
/// Scales reference vessels to target displacement while preserving form ratios
/// Handles constraint validation and distortion checking
/// </summary>
public class VesselScalingService
{
    private readonly ILogger<VesselScalingService> _logger;
    private const decimal MaxAcceptableDistortion = 0.10m;  // 10%

    public VesselScalingService(ILogger<VesselScalingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Scale a reference vessel to target displacement
    /// </summary>
    public ScaledCandidate ScaleToTarget(
        CatalogVesselReal reference,
        decimal targetDisplacement,
        ScalingConstraints? constraints = null)
    {
        _logger.LogDebug(
            "Scaling vessel {VesselId} from {RefDisp}t to {TargetDisp}t",
            reference.VesselId, reference.DisplacementT, targetDisplacement);

        // Calculate scale factor using cube-root law
        // Δ ∝ L³ (for geometrically similar hulls)
        // k = (Δ_target / Δ_ref)^(1/3)
        var scaleFactor = (decimal)Math.Pow(
            (double)(targetDisplacement / reference.DisplacementT),
            1.0 / 3.0
        );

        // Scale dimensions linearly
        var scaled = new ScaledCandidate
        {
            SourceVesselId = reference.Id,
            SourceVesselName = reference.VesselId,
            ScaleFactor = scaleFactor,

            // Principal dimensions
            Lpp = reference.LppM * scaleFactor,
            Beam = reference.BeamM * scaleFactor,
            Draft = reference.DraftM * scaleFactor,
            Depth = (reference.DepthM ?? reference.DraftM * 1.5m) * scaleFactor,

            // Form coefficients (preserved)
            Cb = reference.Cb,
            Cp = reference.Cp ?? EstimateCp(reference.Cb),
            Cm = reference.Cm ?? EstimateCm(reference.Cb, reference.Cp),
            Cw = reference.Cw ?? 0.80m,  // Typical value

            // Performance (scale speed by Froude number preservation)
            ServiceSpeed = reference.ServiceSpeedMs ?? 10.0m,

            // Target values
            TargetDisplacement = targetDisplacement,
            OriginalDisplacement = reference.DisplacementT
        };

        // Validate and enforce constraints
        if (constraints != null)
        {
            ApplyConstraints(scaled, constraints, reference);
        }

        // Calculate displacement from scaled dimensions
        var calculatedDisplacement = CalculateDisplacement(scaled);
        var displacementError = Math.Abs(calculatedDisplacement - targetDisplacement) / targetDisplacement;

        // Check distortion
        var distortion = CalculateDistortion(scaled, reference, scaleFactor);
        scaled.Distortion = distortion;
        scaled.IsValid = distortion < MaxAcceptableDistortion && displacementError < 0.05m;

        if (!scaled.IsValid)
        {
            _logger.LogWarning(
                "Scaled vessel {VesselId} invalid: Distortion={Distortion:P2}, DisplacementError={Error:P2}",
                reference.VesselId, distortion, displacementError);
        }

        _logger.LogDebug(
            "Scaled: Lpp={Lpp:F1}m, B={Beam:F1}m, T={Draft:F1}m, Cb={Cb:F3}, Valid={Valid}",
            scaled.Lpp, scaled.Beam, scaled.Draft, scaled.Cb, scaled.IsValid);

        return scaled;
    }

    /// <summary>
    /// Apply mission constraints, attempting to maintain validity
    /// </summary>
    private void ApplyConstraints(
        ScaledCandidate scaled,
        ScalingConstraints constraints,
        CatalogVesselReal reference)
    {
        bool constraintViolated = false;

        // Check beam constraint
        if (constraints.MaxBeam.HasValue && scaled.Beam > constraints.MaxBeam.Value)
        {
            _logger.LogDebug(
                "Beam constraint violated: {Scaled:F2}m > {Max:F2}m. Attempting to clamp.",
                scaled.Beam, constraints.MaxBeam.Value);

            var originalBeam = scaled.Beam;
            scaled.Beam = constraints.MaxBeam.Value;
            constraintViolated = true;

            // Re-scale other dimensions to maintain displacement
            // Δ ∝ L × B × T × Cb
            // If we reduce B, we need to increase L or T proportionally
            var beamReduction = scaled.Beam / originalBeam;
            
            // Split the compensation between L and T
            var compensationFactor = (decimal)Math.Pow((double)(1.0m / beamReduction), 0.5);
            scaled.Lpp *= compensationFactor;
            scaled.Draft *= compensationFactor;
        }

        // Check draft constraint
        if (constraints.MaxDraft.HasValue && scaled.Draft > constraints.MaxDraft.Value)
        {
            _logger.LogDebug(
                "Draft constraint violated: {Scaled:F2}m > {Max:F2}m. Attempting to clamp.",
                scaled.Draft, constraints.MaxDraft.Value);

            var originalDraft = scaled.Draft;
            scaled.Draft = constraints.MaxDraft.Value;
            constraintViolated = true;

            // Compensate with L and B
            var draftReduction = scaled.Draft / originalDraft;
            var compensationFactor = (decimal)Math.Pow((double)(1.0m / draftReduction), 0.5);
            scaled.Lpp *= compensationFactor;
            scaled.Beam *= compensationFactor;
        }

        // Check displacement constraint
        if (constraints.MaxDisplacement.HasValue && scaled.TargetDisplacement > constraints.MaxDisplacement.Value)
        {
            _logger.LogWarning(
                "Target displacement {Target}t exceeds max {Max}t. Cannot scale this reference.",
                scaled.TargetDisplacement, constraints.MaxDisplacement.Value);
            
            scaled.IsValid = false;
        }

        if (constraintViolated)
        {
            scaled.ConstraintsApplied = true;
        }
    }

    /// <summary>
    /// Calculate displacement from dimensions and Cb
    /// Δ = ρ × L × B × T × Cb
    /// </summary>
    private decimal CalculateDisplacement(ScaledCandidate scaled)
    {
        const decimal seawaterDensity = 1.025m;  // tonnes/m³
        return seawaterDensity * scaled.Lpp * scaled.Beam * scaled.Draft * scaled.Cb;
    }

    /// <summary>
    /// Calculate geometric distortion from ideal scaling
    /// Measures how much constraint enforcement changed the hull form
    /// </summary>
    private decimal CalculateDistortion(
        ScaledCandidate scaled,
        CatalogVesselReal reference,
        decimal idealScaleFactor)
    {
        // Calculate ideal scaled dimensions
        var idealLpp = reference.LppM * idealScaleFactor;
        var idealBeam = reference.BeamM * idealScaleFactor;
        var idealDraft = reference.DraftM * idealScaleFactor;

        // Calculate relative differences
        var lppDiff = Math.Abs(scaled.Lpp - idealLpp) / idealLpp;
        var beamDiff = Math.Abs(scaled.Beam - idealBeam) / idealBeam;
        var draftDiff = Math.Abs(scaled.Draft - idealDraft) / idealDraft;

        // Weighted average (beam and draft more critical)
        var distortion = (lppDiff * 0.3m) + (beamDiff * 0.4m) + (draftDiff * 0.3m);

        return distortion;
    }

    /// <summary>
    /// Estimate prismatic coefficient if missing
    /// </summary>
    private decimal EstimateCp(decimal cb)
    {
        // Typical relationship: CP ≈ CB + 0.05 to 0.10
        // More accurate: CP depends on L/B ratio, but we use simple estimate
        return Math.Min(cb + 0.08m, 1.0m);
    }

    /// <summary>
    /// Estimate midship coefficient if missing
    /// </summary>
    private decimal EstimateCm(decimal cb, decimal? cp)
    {
        if (!cp.HasValue)
            return Math.Min(cb / 0.65m, 1.0m);  // Assume typical CP

        // CM = CB / CP
        if (cp.Value > 0)
            return Math.Min(cb / cp.Value, 1.0m);

        return 0.95m;  // Fallback
    }
}

/// <summary>
/// Constraints for vessel scaling
/// </summary>
public class ScalingConstraints
{
    public decimal? MaxBeam { get; set; }
    public decimal? MaxDraft { get; set; }
    public decimal? MaxDisplacement { get; set; }
}

/// <summary>
/// Result of scaling operation
/// </summary>
public class ScaledCandidate
{
    // Source
    public Guid SourceVesselId { get; set; }
    public string SourceVesselName { get; set; } = string.Empty;
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
    public decimal? Cw { get; set; }

    // Performance
    public decimal ServiceSpeed { get; set; }

    // Target vs Original
    public decimal TargetDisplacement { get; set; }
    public decimal OriginalDisplacement { get; set; }

    // Validation
    public bool IsValid { get; set; }
    public decimal Distortion { get; set; }
    public bool ConstraintsApplied { get; set; }

    // From KNN (set by caller)
    public double SimilarityScore { get; set; }
}

