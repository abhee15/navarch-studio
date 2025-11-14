using System.ComponentModel.DataAnnotations;

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
    [MaxLength(100)]
    public string? VesselType { get; set; }

    /// <summary>
    /// Size classification
    /// </summary>
    [MaxLength(50)]
    public string? Size { get; set; }

    /// <summary>
    /// Block coefficient (Cb)
    /// </summary>
    public decimal? BlockCoefficient { get; set; }

    /// <summary>
    /// Hull family/form (Wigley, Series 60, NPL, Prismatic)
    /// </summary>
    [MaxLength(100)]
    public string? HullFamily { get; set; }

    /// <summary>
    /// ShipD taxonomy category stored alongside metadata for quick analytics (duplicated from vessel for backwards compat)
    /// </summary>
    [MaxLength(100)]
    public string? ShipdCategory { get; set; }

    /// <summary>
    /// ShipD taxonomy type slug
    /// </summary>
    [MaxLength(100)]
    public string? ShipdType { get; set; }

    /// <summary>
    /// ShipD taxonomy family mask version
    /// </summary>
    public int? ShipdMaskVersion { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Vessel Vessel { get; set; } = null!;
}















