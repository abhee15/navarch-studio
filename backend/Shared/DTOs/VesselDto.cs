using Shared.Attributes;

namespace Shared.DTOs;

/// <summary>
/// DTO for creating/updating a vessel
/// NOTE: All values are in user's preferred units (from User.PreferredUnits)
/// Backend will convert to SI for storage
/// </summary>
public class VesselDto : UnitAwareDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    [Convertible("Length")]
    public decimal Lpp { get; set; }

    [Convertible("Length")]
    public decimal Beam { get; set; }

    [Convertible("Length")]
    public decimal DesignDraft { get; set; }

    // Optional metadata
    public VesselMetadataDto? Metadata { get; set; }
    public MaterialsConfigDto? Materials { get; set; }
    public LoadingConditionsDto? Loading { get; set; }
}

/// <summary>
/// DTO for vessel details with geometry counts
/// NOTE: All values are converted to user's preferred units for display
/// </summary>
public class VesselDetailsDto : UnitAwareDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    [Convertible("Length")]
    public decimal Lpp { get; set; }

    [Convertible("Length")]
    public decimal Beam { get; set; }

    [Convertible("Length")]
    public decimal DesignDraft { get; set; }

    public int StationsCount { get; set; }
    public int WaterlinesCount { get; set; }
    public int OffsetsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Whether this is a template vessel (read-only system template)
    /// </summary>
    public bool IsTemplate { get; set; }

    // Optional metadata
    public VesselMetadataDto? Metadata { get; set; }
    public MaterialsConfigDto? Materials { get; set; }
    public LoadingConditionsDto? Loading { get; set; }
}
