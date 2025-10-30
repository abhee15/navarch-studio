namespace Shared.Models;

/// <summary>
/// Represents a speed grid for resistance/power calculations
/// </summary>
public class SpeedGrid
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VesselId { get; set; }
    public Vessel Vessel { get; set; } = null!;

    /// <summary>
    /// Grid name (e.g., "Service Speed Grid", "Test Matrix")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the speed grid
    /// </summary>
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<SpeedPoint> SpeedPoints { get; set; } = new List<SpeedPoint>();
}

/// <summary>
/// Represents a speed point in the grid
/// </summary>
public class SpeedPoint
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SpeedGridId { get; set; }
    public SpeedGrid SpeedGrid { get; set; } = null!;

    /// <summary>
    /// Speed in m/s
    /// </summary>
    public decimal Speed { get; set; }

    /// <summary>
    /// Speed in knots (for convenience)
    /// </summary>
    public decimal? SpeedKnots { get; set; }

    /// <summary>
    /// Froude number (calculated)
    /// </summary>
    public decimal? FroudeNumber { get; set; }

    /// <summary>
    /// Notes for this speed point
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }
}
