namespace Shared.DTOs;

/// <summary>
/// DTO for vessel design-phase metadata
/// </summary>
public class VesselMetadataDto
{
    public string? VesselType { get; set; }
    public string? Size { get; set; }
    public decimal? BlockCoefficient { get; set; }
    public string? HullFamily { get; set; }
    public string? ShipdCategory { get; set; }
    public string? ShipdType { get; set; }
    public int? ShipdMaskVersion { get; set; }
}















