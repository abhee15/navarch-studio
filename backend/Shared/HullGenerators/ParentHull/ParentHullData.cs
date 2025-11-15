namespace Shared.HullGenerators.ParentHull;

/// <summary>
/// Parent hull data loaded from registry and offset tables
/// </summary>
public class ParentHullData
{
    /// <summary>
    /// Vessel type (e.g., "product_carrier")
    /// </summary>
    public string VesselType { get; set; } = string.Empty;

    /// <summary>
    /// Block coefficient
    /// </summary>
    public decimal Cb { get; set; }

    /// <summary>
    /// Length between perpendiculars (m)
    /// </summary>
    public decimal Lbp { get; set; }

    /// <summary>
    /// Breadth (m)
    /// </summary>
    public decimal B { get; set; }

    /// <summary>
    /// Depth (m)
    /// </summary>
    public decimal D { get; set; }

    /// <summary>
    /// Draft (m)
    /// </summary>
    public decimal T { get; set; }

    /// <summary>
    /// Midship coefficient
    /// </summary>
    public decimal Cm { get; set; }

    /// <summary>
    /// Waterplane coefficient
    /// </summary>
    public decimal Cw { get; set; }

    /// <summary>
    /// LCB position (% Lbp forward of midship)
    /// </summary>
    public decimal LcbPercent { get; set; }

    /// <summary>
    /// Source of parent hull data
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Notes about this parent hull
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Station positions (normalized 0-10, or actual meters)
    /// </summary>
    public List<decimal> Stations { get; set; } = new();

    /// <summary>
    /// Waterline heights (m above baseline)
    /// </summary>
    public List<decimal> Waterlines { get; set; } = new();

    /// <summary>
    /// Half-breadths (m) - 2D grid: offsets[stationIndex][waterlineIndex]
    /// </summary>
    public List<List<decimal>> Offsets { get; set; } = new();
}
