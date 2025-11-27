using Shared.Models.Sizing;

namespace Shared.DTOs.Sizing;

/// <summary>
/// DTO for candidate design response
/// </summary>
public record CandidateDesignDto
{
    public Guid Id { get; init; }
    public Guid SizingRunId { get; init; }
    public string HullFamily { get; init; } = string.Empty;
    public string? VesselCategory { get; init; }
    public string? VesselType { get; init; }
    public string? BowFamily { get; init; }
    public string? MidshipFamily { get; init; }
    public string? SternFamily { get; init; }
    public int? FamilyMaskVersion { get; init; }
    public string? ShipdParametersJson { get; init; }

    // Principal dimensions
    public decimal LppM { get; init; }
    public decimal LwlM { get; init; }
    public decimal LoaM { get; init; }
    public decimal BeamM { get; init; }
    public decimal DraftM { get; init; }
    public decimal DepthM { get; init; }

    // Coefficients
    public decimal Cb { get; init; }
    public decimal Cp { get; init; }
    public decimal Cwp { get; init; }

    // ShipD Parameters (extracted from ShipdParametersJson vector)
    // Longitudinal Proportions
    public decimal? BowLengthRatio { get; init; }      // Lb - vector[1]
    public decimal? SternLengthRatio { get; init; }    // Ls - vector[2]

    // Bow Shape
    public decimal? BowFlareAngle { get; init; }       // Beta - vector[8]
    public decimal? BowCurvature { get; init; }        // Rc - vector[9]
    public decimal? BowKnuckle { get; init; }          // Rk - vector[10]
    public decimal? DeadriseAngle { get; init; }       // Cdrft - vector[19]

    // Stern Shape
    public decimal? SternRakeAngle { get; init; }      // Beta_trans - vector[27]
    public decimal? SternCurvature { get; init; }      // Rc_trans - vector[29]
    public decimal? SternKnuckle { get; init; }        // Rk_trans - vector[30]
    public decimal? TransomArea { get; init; }         // Atrans - vector[22]
    public decimal? TransomWidth { get; init; }        // Bc_trans - vector[28]

    // Midship Features
    public bool? HasSheer { get; init; }               // bit_EP_S - vector[20]
    public bool? HasTumblehome { get; init; }          // bit_EP_T - vector[21]

    // Bulbous Bow
    public bool? HasBulb { get; init; }                // bit_BB - vector[31]
    public decimal? BulbLengthRatio { get; init; }     // Lbb - vector[33]
    public decimal? BulbHeightRatio { get; init; }     // Hbb - vector[34]
    public decimal? BulbWidthRatio { get; init; }      // Bbb - vector[35]
    public decimal? BulbAsymmetry { get; init; }       // Lbbm - vector[36]
    public decimal? BulbFilletRadius { get; init; }    // Rbb - vector[37]

    // Derived
    public decimal DispM3 { get; init; }
    public decimal DispT { get; init; }
    public decimal Fn { get; init; }
    public decimal? LwlOverLambda { get; init; }

    // Stability estimates
    public decimal? KbM { get; init; }
    public decimal? LcbPctLpp { get; init; }
    public decimal? KgEstM { get; init; }
    public decimal? GmEstM { get; init; }

    // Resistance
    public decimal? EhpKw { get; init; }
    public decimal? ShpKw { get; init; }

    // Scoring
    public string? FlagsJson { get; init; }
    public decimal Score { get; init; }
    public int Rank { get; init; }
    public bool IsSelected { get; init; }

    // Geometry (optional, large)
    public string? GeometryJson { get; init; }

    /// <summary>
    /// Status of geometry generation
    /// </summary>
    public GeometryGenerationStatus GeometryGenerationStatus { get; init; } = GeometryGenerationStatus.Success;

    /// <summary>
    /// Error message if geometry generation failed
    /// </summary>
    public string? GeometryGenerationError { get; init; }

    /// <summary>
    /// Whether the candidate has valid geometry
    /// </summary>
    public bool HasValidGeometry => GeometryGenerationStatus == GeometryGenerationStatus.Success && !string.IsNullOrEmpty(GeometryJson);

    // Engineering Details
    /// <summary>
    /// Weight breakdown estimate (JSON serialized)
    /// </summary>
    public string? WeightBreakdownJson { get; init; }

    public DateTime CreatedAt { get; init; }

    // Provenance (Data-Driven Mode)
    public string? ReferenceVesselId { get; init; }
    public string? ReferenceVesselName { get; init; }
    public decimal? SimilarityScore { get; init; }
    public string? SolverMode { get; init; }
}

/// <summary>
/// DTO for updating a candidate design
/// </summary>
public record UpdateCandidateDesignDto
{
    public bool? IsSelected { get; init; }
}

/// <summary>
/// DTO for adjusting a parameter and recomputing candidate
/// </summary>
public record AdjustParameterDto
{
    public string Parameter { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public string RecomputeMode { get; init; } = "fast"; // "fast" or "full"
}
