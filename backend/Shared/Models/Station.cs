namespace Shared.Models;

/// <summary>
/// Represents a transverse section (station) along the vessel's length
/// </summary>
public class Station
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VesselId { get; set; }
    public Vessel Vessel { get; set; } = null!;

    /// <summary>
    /// Station index (0, 1, 2, ...) from aft to forward
    /// </summary>
    public int StationIndex { get; set; }

    /// <summary>
    /// Longitudinal position from aft perpendicular (m)
    /// </summary>
    public decimal X { get; set; }
}

