namespace Shared.DTOs;

/// <summary>
/// Waterlines projection data: XY projections at specific Z-levels
/// </summary>
public record WaterlinesDto
{
    /// <summary>
    /// List of waterline curves, one for each Z-level
    /// </summary>
    public List<WaterlineCurve> Waterlines { get; init; } = new();
}

/// <summary>
/// Single waterline curve at a specific Z-level
/// </summary>
public record WaterlineCurve
{
    /// <summary>
    /// Waterline index (from vessel geometry)
    /// </summary>
    public int WaterlineIndex { get; init; }

    /// <summary>
    /// Z-coordinate of this waterline (height above keel)
    /// </summary>
    public decimal Z { get; init; }

    /// <summary>
    /// Points along the waterline (X, Y coordinates)
    /// Represents the curve at this Z-level across all stations
    /// </summary>
    public List<Point2D> Points { get; init; } = new();
}

/// <summary>
/// 2D point for waterline/buttock curves
/// </summary>
public record Point2D
{
    /// <summary>
    /// X coordinate (longitudinal position)
    /// </summary>
    public decimal X { get; init; }

    /// <summary>
    /// Y coordinate (transverse position - half-breadth from centerline)
    /// </summary>
    public decimal Y { get; init; }
}
