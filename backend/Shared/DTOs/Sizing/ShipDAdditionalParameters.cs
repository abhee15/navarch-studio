namespace Shared.DTOs.Sizing;

/// <summary>
/// Additional ShipD parameters that can be specified conditionally based on selected hull families.
/// These parameters provide fine-grained control over hull geometry beyond family selection.
/// </summary>
public record ShipDAdditionalParameters
{
    // Section Geometry (Image 1: Section Geometry Parameters)
    /// <summary>
    /// Bow flare angle in degrees (Beta, index 8)
    /// </summary>
    public decimal? FlareAngleDeg { get; init; }

    /// <summary>
    /// Forward deadrise/flare control angle in degrees (Cdrft, index 19)
    /// </summary>
    public decimal? DeadriseAngleDeg { get; init; }

    /// <summary>
    /// Chine type: "hard" for sharp corner, "soft" for rounded transition
    /// Affects Rc (index 9) and Rk (index 10) curvature coefficients
    /// </summary>
    public string? ChineType { get; init; }

    /// <summary>
    /// Curvature type: "convex" for rounded bottom, "concave" for inward curving sides
    /// Affects Kappa_bow (index 14) and Kappa_stern (index 24)
    /// </summary>
    public string? CurvatureType { get; init; }

    /// <summary>
    /// Tumblehome toggle - inward curving upper sides (bit_EP_T, index 21)
    /// Only applicable for fine_midship family
    /// </summary>
    public bool? TumblehomeEnabled { get; init; }

    // Longitudinal Segmentation (Image 2: Longitudinal Segmentation)
    /// <summary>
    /// Bow length ratio relative to LOA (Lb, index 1) - override default
    /// </summary>
    public decimal? BowLengthRatio { get; init; }

    /// <summary>
    /// Mid-body length ratio relative to LOA (Lm, derived: 1 - Lb - Ls)
    /// </summary>
    public decimal? MidBodyLengthRatio { get; init; }

    /// <summary>
    /// Stern length ratio relative to LOA (Ls, index 2) - override default
    /// </summary>
    public decimal? SternLengthRatio { get; init; }

    /// <summary>
    /// Bow rake angle in degrees (Beta, index 8) - separate from flare
    /// </summary>
    public decimal? BowRakeAngleDeg { get; init; }

    /// <summary>
    /// Stern rake angle in degrees (Beta_trans, index 27)
    /// </summary>
    public decimal? SternRakeAngleDeg { get; init; }

    // Bulb Geometry (Image 3: Bulb Geometry) - only when bulbous_bow selected
    /// <summary>
    /// Bulb length ratio (Lbb, index 33)
    /// </summary>
    public decimal? BulbLengthRatio { get; init; }

    /// <summary>
    /// Bulb width ratio (Bbb, index 35)
    /// </summary>
    public decimal? BulbWidthRatio { get; init; }

    /// <summary>
    /// Bulb height ratio (Hbb, index 34)
    /// </summary>
    public decimal? BulbHeightRatio { get; init; }

    /// <summary>
    /// Bulb longitudinal moment coefficient / asymmetry factor (Lbbm, index 36)
    /// </summary>
    public decimal? BulbAsymmetryFactor { get; init; }

    /// <summary>
    /// Bulb radius coefficient / fillet radius (Rbb, index 37)
    /// </summary>
    public decimal? BulbFilletRadius { get; init; }
}


