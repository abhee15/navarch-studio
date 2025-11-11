namespace Shared.DTOs;

/// <summary>
/// Represents a single diagonal curve (45° line from baseline)
/// </summary>
public record DiagonalDto
{
    /// <summary>
    /// Index of this diagonal (0-based)
    /// </summary>
    public int DiagonalIndex { get; init; }

    /// <summary>
    /// Angle of diagonal from baseline (typically 45°)
    /// </summary>
    public decimal Angle { get; init; }

    /// <summary>
    /// Points along the diagonal curve (X, Y, Z coordinates)
    /// </summary>
    public List<PointDto> Points { get; init; } = new();
}

/// <summary>
/// Contains multiple diagonal curves for a vessel
/// </summary>
public record DiagonalsDto
{
    /// <summary>
    /// Collection of diagonal curves
    /// </summary>
    public List<DiagonalDto> Diagonals { get; init; } = new();
}

/// <summary>
/// Represents a 3D point
/// </summary>
public record PointDto
{
    /// <summary>
    /// X coordinate (longitudinal)
    /// </summary>
    public decimal X { get; init; }

    /// <summary>
    /// Y coordinate (transverse)
    /// </summary>
    public decimal Y { get; init; }

    /// <summary>
    /// Z coordinate (vertical)
    /// </summary>
    public decimal Z { get; init; }
}
