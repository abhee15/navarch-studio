namespace Shared.Models;

/// <summary>
/// Stores design-phase metadata for a vessel
/// </summary>
public class VesselMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the parent vessel
    /// </summary>
    public Guid VesselId { get; set; }

    /// <summary>
    /// Vessel type classification
    /// </summary>
    public string? VesselType { get; set; }

    /// <summary>
    /// Size classification
    /// </summary>
    public string? Size { get; set; }

    /// <summary>
    /// Block coefficient (Cb)
    /// </summary>
    public decimal? BlockCoefficient { get; set; }

    /// <summary>
    /// Hull family/form (Wigley, Series 60, NPL, Prismatic)
    /// </summary>
    public string? HullFamily { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Vessel Vessel { get; set; } = null!;
}
