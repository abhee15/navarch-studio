using Shared.Attributes;

namespace Shared.DTOs;

/// <summary>
/// DTO for catalog hull entries
/// Values are converted to user's preferred units for display
/// </summary>
public class CatalogHullDto : UnitAwareDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CanonicalRefs { get; set; }

    // Hull particulars
    public string? HullType { get; set; }  // "Container" | "Tanker" | "Naval" | "Template"

    [Convertible("Length")]
    public decimal? Lpp { get; set; }  // Length between perpendiculars (m)

    [Convertible("Length")]
    public decimal? Beam { get; set; }  // Beam (m)

    [Convertible("Length")]
    public decimal? Draft { get; set; }  // Draft (m)

    public decimal? Cb { get; set; }  // Block coefficient (dimensionless)
    public decimal? Cp { get; set; }  // Prismatic coefficient (dimensionless)

    [Convertible("Length")]
    public decimal? LCB { get; set; }  // Longitudinal center of buoyancy (m)

    [Convertible("Length")]
    public decimal? LCF { get; set; }  // Longitudinal center of flotation (m)

    public bool GeometryMissing { get; set; }  // Whether geometry is available

    public DateTime CreatedAt { get; set; }

    // Geometry counts (if available)
    public int? StationsCount { get; set; }
    public int? WaterlinesCount { get; set; }
    public int? OffsetsCount { get; set; }
}

/// <summary>
/// DTO for listing catalog hulls
/// </summary>
public class CatalogHullListItemDto : UnitAwareDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HullType { get; set; }

    [Convertible("Length")]
    public decimal? Lpp { get; set; }

    [Convertible("Length")]
    public decimal? Beam { get; set; }

    [Convertible("Length")]
    public decimal? Draft { get; set; }

    public decimal? Cb { get; set; }
    public bool GeometryMissing { get; set; }
}
