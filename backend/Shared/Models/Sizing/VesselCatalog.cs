namespace Shared.Models.Sizing;

public class VesselCatalog
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Provenance { get; set; }
    public string? VesselType { get; set; }

    public decimal? LppM { get; set; }
    public decimal? LwlM { get; set; }
    public decimal? BM { get; set; }
    public decimal? TM { get; set; }
    public decimal? DM { get; set; }

    public decimal? Cb { get; set; }
    public decimal? Cp { get; set; }
    public decimal? Cwp { get; set; }
    public decimal? Cm { get; set; }

    public decimal? DwtT { get; set; }
    public decimal? ServiceSpeedKn { get; set; }

    public string? Notes { get; set; }
    public string? SourceUrl { get; set; }
    public string? LicenseInfo { get; set; }
}

