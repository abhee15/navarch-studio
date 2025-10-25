namespace Shared.Models;

/// <summary>
/// Represents a horizontal waterline at a specific height
/// </summary>
public class Waterline
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid VesselId { get; set; }
    public Vessel Vessel { get; set; } = null!;
    
    /// <summary>
    /// Waterline index (0, 1, 2, ...) from keel upward
    /// </summary>
    public int WaterlineIndex { get; set; }
    
    /// <summary>
    /// Vertical position from keel (m)
    /// </summary>
    public decimal Z { get; set; }
}

