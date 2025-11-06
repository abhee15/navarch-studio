namespace Shared.DTOs.Catalog;

/// <summary>
/// Request for parametric KNN search
/// </summary>
public record ParametricSearchRequest
{
    public decimal TargetLOA { get; init; }  // m
    public decimal TargetVolume { get; init; }  // m³
    public decimal? TargetLCB { get; init; }  // Fraction of LOA
    public decimal? TargetBeamRatio { get; init; }  // B/LOA
    public decimal? TargetDraftRatio { get; init; }  // T/LOA
    public decimal? TargetCb { get; init; }  // Block coefficient
    public int K { get; init; } = 5;
    public string? DatasetFilter { get; init; }  // e.g., "Constrained"
}

/// <summary>
/// Similar parametric hull result
/// </summary>
public class SimilarParametricHullDto
{
    public int HullId { get; set; }
    public string HullIdString { get; set; } = string.Empty;  // "CS1_00123"
    public string DatasetSource { get; set; } = string.Empty;

    // Principal dimensions (derived, at LOA=10m baseline, will be scaled)
    public decimal LppM { get; set; }
    public decimal BeamM { get; set; }
    public decimal DraftM { get; set; }
    public decimal DepthM { get; set; }

    // Form coefficients (dimensionless, preserved during scaling)
    public decimal Cb { get; set; }
    public decimal? Cp { get; set; }
    public decimal? Cm { get; set; }
    public decimal Cw { get; set; }

    // Geometric features (normalized to LOA=10m)
    public decimal VolumeNorm { get; set; }
    public decimal LcbNorm { get; set; }

    // Similarity
    public double SimilarityScore { get; set; }  // 0-1, higher = more similar
    public double GeometricDistance { get; set; }

    // Quality
    public string ConversionQuality { get; set; } = string.Empty;
}

/// <summary>
/// Response with similar parametric hulls
/// </summary>
public record ParametricSearchResponse
{
    public List<SimilarParametricHullDto> SimilarHulls { get; init; } = new();
    public int TotalCatalogSize { get; init; }
    public string CatalogSource { get; init; } = "ML_Parametric";
    public string AlgorithmUsed { get; init; } = "Geometric_KNN";  // Later: "ANN_HNSW"
    public int QueryTimeMs { get; init; }
}
