namespace Shared.Models;

/// <summary>
/// Represents a hydrostatic curve (e.g., displacement vs draft)
/// </summary>
public class Curve
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VesselId { get; set; }
    public Vessel Vessel { get; set; } = null!;

    /// <summary>
    /// Curve type: 'displacement', 'kb', 'lcb', 'gm', 'awp', 'bonjean', etc.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// X-axis label (e.g., "Draft (m)")
    /// </summary>
    public string? XLabel { get; set; }

    /// <summary>
    /// Y-axis label (e.g., "Displacement (kg)")
    /// </summary>
    public string? YLabel { get; set; }

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public string? Meta { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<CurvePoint> Points { get; set; } = new List<CurvePoint>();
}

