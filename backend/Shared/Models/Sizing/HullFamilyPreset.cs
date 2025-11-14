namespace Shared.Models.Sizing;

/// <summary>
/// Hull type presets with geometric ranges (seeded from hull_family_presets_extended.csv)
/// </summary>
public class HullFamilyPreset
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Hull family identifier: container, tanker, bulk, fishing, yacht_disp, hsc_planing, etc.
    /// </summary>
    public string Family { get; set; } = null!;

    /// <summary>
    /// Display name for UI
    /// </summary>
    public string? DisplayName { get; set; }

    // Geometric Ratio Ranges

    /// <summary>
    /// Minimum L/B ratio
    /// </summary>
    public decimal LOverBMin { get; set; }

    /// <summary>
    /// Maximum L/B ratio
    /// </summary>
    public decimal LOverBMax { get; set; }

    /// <summary>
    /// Minimum B/T ratio
    /// </summary>
    public decimal BOverTMin { get; set; }

    /// <summary>
    /// Maximum B/T ratio
    /// </summary>
    public decimal BOverTMax { get; set; }

    /// <summary>
    /// Minimum D/T ratio
    /// </summary>
    public decimal DOverTMin { get; set; }

    /// <summary>
    /// Maximum D/T ratio
    /// </summary>
    public decimal DOverTMax { get; set; }

    // Form Coefficient Ranges

    /// <summary>
    /// Minimum block coefficient
    /// </summary>
    public decimal CbMin { get; set; }

    /// <summary>
    /// Maximum block coefficient
    /// </summary>
    public decimal CbMax { get; set; }

    /// <summary>
    /// Minimum prismatic coefficient
    /// </summary>
    public decimal? CpMin { get; set; }

    /// <summary>
    /// Maximum prismatic coefficient
    /// </summary>
    public decimal? CpMax { get; set; }

    /// <summary>
    /// Minimum waterplane coefficient
    /// </summary>
    public decimal? CwpMin { get; set; }

    /// <summary>
    /// Maximum waterplane coefficient
    /// </summary>
    public decimal? CwpMax { get; set; }

    // Froude Number Band

    /// <summary>
    /// Minimum Froude number for this family
    /// </summary>
    public decimal? FnMin { get; set; }

    /// <summary>
    /// Maximum Froude number for this family
    /// </summary>
    public decimal? FnMax { get; set; }

    // Geometry Generator

    /// <summary>
    /// Parametric hull generator type: wigley, series60, kcs_like, kvlcc2_like, planing
    /// </summary>
    public string? GeneratorType { get; set; }

    // Metadata

    /// <summary>
    /// Whether this family is active for selection
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }
}












