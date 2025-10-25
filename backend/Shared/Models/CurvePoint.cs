namespace Shared.Models;

/// <summary>
/// Represents a single point on a curve
/// </summary>
public class CurvePoint
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CurveId { get; set; }
    public Curve Curve { get; set; } = null!;

    /// <summary>
    /// X-coordinate
    /// </summary>
    public decimal X { get; set; }

    /// <summary>
    /// Y-coordinate
    /// </summary>
    public decimal Y { get; set; }

    /// <summary>
    /// Sequence number for ordering points
    /// </summary>
    public int Sequence { get; set; }
}

