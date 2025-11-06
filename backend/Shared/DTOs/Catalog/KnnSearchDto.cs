namespace Shared.DTOs.Catalog;

/// <summary>
/// Request for KNN search on vessel catalog
/// </summary>
public class KnnSearchRequest
{
    public string VesselType { get; set; } = string.Empty;
    public decimal TargetDisplacement { get; set; }
    public decimal ServiceSpeed { get; set; }  // m/s
    public decimal? MaxBeam { get; set; }
    public decimal? MaxDraft { get; set; }
    public int K { get; set; } = 5;
}

/// <summary>
/// Similar vessel returned from KNN search
/// </summary>
public class SimilarVesselDto
{
    public Guid VesselId { get; set; }
    public string VesselName { get; set; } = string.Empty;
    public string VesselType { get; set; } = string.Empty;
    public decimal LppM { get; set; }
    public decimal BeamM { get; set; }
    public decimal DraftM { get; set; }
    public decimal? DepthM { get; set; }
    public decimal DisplacementT { get; set; }
    public decimal Cb { get; set; }
    public decimal? Cp { get; set; }
    public decimal? Cm { get; set; }
    public decimal? Cw { get; set; }
    public decimal? ServiceSpeedMs { get; set; }
    public double SimilarityScore { get; set; }  // 0-1
}

/// <summary>
/// Response from KNN search
/// </summary>
public class KnnSearchResponse
{
    public List<SimilarVesselDto> SimilarVessels { get; set; } = new();
    public int TotalCatalogSize { get; set; }
    public string CatalogSource { get; set; } = "RealWorld";
}

