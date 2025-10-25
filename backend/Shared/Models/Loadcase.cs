namespace Shared.Models;

/// <summary>
/// Represents a loading condition for a vessel (e.g., design, ballast, full load)
/// </summary>
public class Loadcase
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VesselId { get; set; }
    public Vessel Vessel { get; set; } = null!;

    /// <summary>
    /// Loadcase name (e.g., "Design Condition", "Ballast")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Water density (kg/m³)
    /// Typical: 1025 (saltwater), 1000 (freshwater)
    /// </summary>
    public decimal Rho { get; set; } = 1025m;

    /// <summary>
    /// Vertical center of gravity (m from keel)
    /// </summary>
    public decimal? KG { get; set; }

    /// <summary>
    /// Notes about this loadcase
    /// </summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<HydroResult> HydroResults { get; set; } = new List<HydroResult>();
}

