namespace Shared.Models;

/// <summary>
/// Stores material selections for vessel hull and superstructure
/// </summary>
public class MaterialsConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the parent vessel
    /// </summary>
    public Guid VesselId { get; set; }

    /// <summary>
    /// Hull material (Steel, Aluminium, FRP, Wood)
    /// </summary>
    public string? HullMaterial { get; set; }

    /// <summary>
    /// Superstructure material (Aluminium, Composite, Steel)
    /// </summary>
    public string? SuperstructureMaterial { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Vessel Vessel { get; set; } = null!;
}


