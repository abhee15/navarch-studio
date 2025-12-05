namespace HullSizingService.Services.ShipD;

/// <summary>
/// Default hull families and parameters for a vessel type.
/// Used to automatically select appropriate hull shapes based on vessel type.
/// </summary>
public class VesselHullDefaults
{
    /// <summary>
    /// Default bow family (e.g., "bulbous_bow", "straight_raked")
    /// </summary>
    public string? BowFamily { get; set; }

    /// <summary>
    /// Default midship family (e.g., "barge_type", "fine_midship", "deep_v")
    /// </summary>
    public string? MidshipFamily { get; set; }

    /// <summary>
    /// Default stern family (e.g., "transom_stern", "cruiser_stern", "canoe_stern")
    /// </summary>
    public string? SternFamily { get; set; }

    /// <summary>
    /// Default chine type ("hard" or "soft")
    /// </summary>
    public string? ChineType { get; set; }

    /// <summary>
    /// Default curvature type ("convex" or "concave")
    /// </summary>
    public string? CurvatureType { get; set; }

    /// <summary>
    /// Default deadrise angle in degrees (typically 5-30°)
    /// </summary>
    public decimal? DeadriseAngleDeg { get; set; }

    /// <summary>
    /// Default flare angle in degrees (typically 0-45°)
    /// Negative values indicate tumblehome (inward-sloping sides)
    /// </summary>
    public decimal? FlareAngleDeg { get; set; }

    /// <summary>
    /// Whether tumblehome is enabled (for fine_midship with inward-sloping sides)
    /// </summary>
    public bool? TumblehomeEnabled { get; set; }
}

