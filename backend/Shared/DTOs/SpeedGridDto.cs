namespace Shared.DTOs;

/// <summary>
/// DTO for SpeedGrid with its speed points
/// </summary>
public class SpeedGridDto
{
    public Guid Id { get; set; }
    public Guid VesselId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<SpeedPointDto> SpeedPoints { get; set; } = new();
}

/// <summary>
/// DTO for a single speed point
/// </summary>
public class SpeedPointDto
{
    public Guid Id { get; set; }
    public Guid SpeedGridId { get; set; }
    public decimal Speed { get; set; } // m/s
    public decimal? SpeedKnots { get; set; }
    public decimal? FroudeNumber { get; set; }
    public string? Notes { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for creating/updating a speed grid
/// </summary>
public class CreateSpeedGridRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<CreateSpeedPointRequest> SpeedPoints { get; set; } = new();
}

/// <summary>
/// DTO for creating/updating a speed point
/// </summary>
public class CreateSpeedPointRequest
{
    public decimal Speed { get; set; } // m/s (will be normalized)
    public decimal? SpeedKnots { get; set; } // Optional, for convenience
    public string? Notes { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for updating speed grid points in bulk
/// </summary>
public class UpdateSpeedPointsRequest
{
    public List<CreateSpeedPointRequest> SpeedPoints { get; set; } = new();
}
