namespace Shared.HullGenerators.Models;

/// <summary>
/// Generated hull geometry (stations, waterlines, offsets)
/// Compatible with existing GeometryService format
/// </summary>
public class GeneratedHullGeometry
{
    /// <summary>
    /// Station positions along length (m from aft perpendicular)
    /// </summary>
    public List<decimal> Stations { get; set; } = new();

    /// <summary>
    /// Waterline heights above baseline (m)
    /// </summary>
    public List<decimal> Waterlines { get; set; } = new();

    /// <summary>
    /// Half-breadths (m) - 2D grid: offsets[stationIndex][waterlineIndex]
    /// </summary>
    public List<List<decimal>> Offsets { get; set; } = new();

    /// <summary>
    /// Computed form coefficients (for validation)
    /// </summary>
    public FormCoefficients? ComputedCoefficients { get; set; }
}

/// <summary>
/// Form coefficients for validation
/// </summary>
public record FormCoefficients(
    decimal Cb,
    decimal Cp,
    decimal Cm,
    decimal Cwp,
    decimal LcbPercent,
    decimal Volume
);
