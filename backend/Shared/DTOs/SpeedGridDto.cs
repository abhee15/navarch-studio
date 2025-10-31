using Shared.Attributes;

namespace Shared.DTOs;

/// <summary>
/// DTO for creating/updating a speed grid
/// </summary>
public class SpeedGridDto
{
    public Guid? Id { get; set; }
    public Guid VesselId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<SpeedPointDto> SpeedPoints { get; set; } = new();
}

/// <summary>
/// DTO for a speed point in a speed grid
/// </summary>
public class SpeedPointDto
{
    public Guid? Id { get; set; }
    public Guid SpeedGridId { get; set; }

    [Convertible("Velocity")]
    public decimal Speed { get; set; } // m/s internally

    [Convertible("Velocity")]
    public decimal? SpeedKnots { get; set; } // For convenience/display

    public decimal? FroudeNumber { get; set; }
    public string? Notes { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for speed grid details with metadata
/// </summary>
public class SpeedGridDetailsDto : UnitAwareDto
{
    public Guid Id { get; set; }
    public Guid VesselId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<SpeedPointDto> SpeedPoints { get; set; } = new();
    public int SpeedPointsCount { get; set; }
}

