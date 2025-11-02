namespace Shared.DTOs;

/// <summary>
/// Buttocks projection data: XZ projections at specific Y-offsets
/// </summary>
public record ButtocksDto
{
    /// <summary>
    /// List of buttock curves, one for each Y-offset
    /// </summary>
    public List<ButtockCurve> Buttocks { get; init; } = new();
}

/// <summary>
/// Single buttock curve at a specific Y-offset
/// </summary>
public record ButtockCurve
{
    /// <summary>
    /// Index of this buttock line
    /// </summary>
    public int ButtockIndex { get; init; }

    /// <summary>
    /// Y-coordinate of this buttock (offset from centerline)
    /// </summary>
    public decimal Y { get; init; }

    /// <summary>
    /// Points along the buttock line (X, Z coordinates)
    /// Represents the curve at this Y-offset across all stations and waterlines
    /// </summary>
    public List<PointXZ> Points { get; init; } = new();
}

/// <summary>
/// 2D point in XZ plane for buttock curves
/// </summary>
public record PointXZ
{
    /// <summary>
    /// X coordinate (longitudinal position)
    /// </summary>
    public decimal X { get; init; }

    /// <summary>
    /// Z coordinate (vertical position - height above keel)
    /// </summary>
    public decimal Z { get; init; }
}
