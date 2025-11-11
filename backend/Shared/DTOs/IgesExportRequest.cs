namespace Shared.DTOs;

/// <summary>
/// Request for exporting hull geometry as IGES file
/// </summary>
public class IgesExportRequest : UnitAwareDto
{
    /// <summary>
    /// Include station curves
    /// </summary>
    public bool IncludeStations { get; set; } = true;

    /// <summary>
    /// Include waterline curves
    /// </summary>
    public bool IncludeWaterlines { get; set; } = true;

    /// <summary>
    /// Include buttock curves
    /// </summary>
    public bool IncludeButtocks { get; set; } = true;

    /// <summary>
    /// Include diagonal curves
    /// </summary>
    public bool IncludeDiagonals { get; set; } = true;

    /// <summary>
    /// IGES format version
    /// </summary>
    public string Version { get; set; } = "5.3"; // IGES 5.3
}
