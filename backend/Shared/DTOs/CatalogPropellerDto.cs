namespace Shared.DTOs;

/// <summary>
/// DTO for catalog propeller series
/// </summary>
public class CatalogPropellerSeriesDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BladeCount { get; set; }  // Z: 3-7
    public decimal ExpandedAreaRatio { get; set; }  // AE/A0
    public decimal? PitchDiameterRatio { get; set; }  // P/D (optional)
    public string? SourceUrl { get; set; }
    public string? License { get; set; }
    public bool IsDemo { get; set; }

    public int PointsCount { get; set; }  // Number of open-water points
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for propeller open-water points
/// </summary>
public class CatalogPropellerPointDto
{
    public Guid Id { get; set; }
    public Guid SeriesId { get; set; }

    public decimal J { get; set; }  // Advance coefficient (dimensionless)
    public decimal Kt { get; set; }  // Thrust coefficient (dimensionless)
    public decimal Kq { get; set; }  // Torque coefficient (dimensionless)
    public decimal Eta0 { get; set; }  // Open-water efficiency (dimensionless)
    public decimal? ReynoldsNumber { get; set; }  // Reynolds number (dimensionless)
}

/// <summary>
/// DTO for detailed propeller series with points
/// </summary>
public class CatalogPropellerSeriesDetailsDto : CatalogPropellerSeriesDto
{
    public List<CatalogPropellerPointDto> Points { get; set; } = new();
}
