namespace Shared.DTOs;

/// <summary>
/// Represents the section area curve showing sectional area distribution along the hull length
/// </summary>
public record SectionAreaCurveDto
{
    /// <summary>
    /// Station X positions (longitudinal coordinates)
    /// </summary>
    public List<decimal> StationPositions { get; init; } = new();

    /// <summary>
    /// Sectional areas at each station (m² or ft²)
    /// </summary>
    public List<decimal> SectionalAreas { get; init; } = new();

    /// <summary>
    /// Unit system (SI or Imperial)
    /// </summary>
    public string Units { get; init; } = "SI";
}
