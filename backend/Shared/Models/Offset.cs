namespace Shared.Models;

/// <summary>
/// Represents a hull offset (half-breadth) at a specific station and waterline intersection
/// </summary>
public class Offset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid VesselId { get; set; }
    public Vessel Vessel { get; set; } = null!;
    
    /// <summary>
    /// Station index
    /// </summary>
    public int StationIndex { get; set; }
    
    /// <summary>
    /// Waterline index
    /// </summary>
    public int WaterlineIndex { get; set; }
    
    /// <summary>
    /// Half-breadth from centerline (m)
    /// Port/starboard symmetry assumed
    /// </summary>
    public decimal HalfBreadthY { get; set; }
}

