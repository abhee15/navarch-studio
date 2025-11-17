namespace Shared.Models.Sizing;

/// <summary>
/// Generated hull candidate from sizing solver
/// </summary>
public class CandidateDesign
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Sizing run this candidate belongs to
    /// </summary>
    public Guid SizingRunId { get; set; }

    /// <summary>
    /// Hull family/type: container, tanker, bulk, fishing, yacht_disp, hsc_planing, etc.
    /// </summary>
    public string HullFamily { get; set; } = null!;

    /// <summary>
    /// Vessel category captured at generation time.
    /// </summary>
    public string? VesselCategory { get; set; }

    /// <summary>
    /// Vessel type slug captured at generation time.
    /// </summary>
    public string? VesselType { get; set; }

    /// <summary>
    /// Bow family used for this candidate.
    /// </summary>
    public string? BowFamily { get; set; }

    /// <summary>
    /// Midship family used for this candidate.
    /// </summary>
    public string? MidshipFamily { get; set; }

    /// <summary>
    /// Stern family used for this candidate.
    /// </summary>
    public string? SternFamily { get; set; }

    /// <summary>
    /// Version of the family mask / metadata applied.
    /// </summary>
    public int? FamilyMaskVersion { get; set; }

    /// <summary>
    /// ShipD canonical parameter vector (JSON) that produced this candidate.
    /// </summary>
    public string? ShipdParametersJson { get; set; }

    /// <summary>
    /// Ranking within sizing run (1 = best by score)
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// User-selected candidate for further analysis
    /// </summary>
    public bool IsSelected { get; set; }

    // Principal Dimensions

    /// <summary>
    /// Length between perpendiculars (m)
    /// </summary>
    public decimal LppM { get; set; }

    /// <summary>
    /// Waterline length (m)
    /// </summary>
    public decimal LwlM { get; set; }

    /// <summary>
    /// Length overall (m)
    /// </summary>
    public decimal LoaM { get; set; }

    /// <summary>
    /// Beam (m)
    /// </summary>
    public decimal BM { get; set; }

    /// <summary>
    /// Draft (m)
    /// </summary>
    public decimal TM { get; set; }

    /// <summary>
    /// Depth (m)
    /// </summary>
    public decimal DM { get; set; }

    // Form Coefficients

    /// <summary>
    /// Block coefficient: Δ/(L×B×T×ρ)
    /// </summary>
    public decimal Cb { get; set; }

    /// <summary>
    /// Prismatic coefficient: Δ/(Am×L×ρ) where Am = midship area
    /// </summary>
    public decimal Cp { get; set; }

    /// <summary>
    /// Waterplane area coefficient: Awp/(L×B)
    /// </summary>
    public decimal Cwp { get; set; }

    /// <summary>
    /// Midship section coefficient: Am/(B×T)
    /// </summary>
    public decimal? Cm { get; set; }

    // Mass & Displacement

    /// <summary>
    /// Displacement (tonnes)
    /// </summary>
    public decimal DisplacementT { get; set; }

    // Speed Characteristics

    /// <summary>
    /// Froude number: V/√(g×L)
    /// </summary>
    public decimal Fn { get; set; }

    /// <summary>
    /// LWL/λ ratio (seakeeping screen)
    /// </summary>
    public decimal? LwlOverLambda { get; set; }

    // Resistance & Power

    /// <summary>
    /// Effective horsepower (kW)
    /// </summary>
    public decimal? EhpKw { get; set; }

    /// <summary>
    /// Shaft horsepower (kW)
    /// </summary>
    public decimal? ShpKw { get; set; }

    // Stability Estimates

    /// <summary>
    /// Estimated transverse metacentric height (m)
    /// </summary>
    public decimal? GmEstM { get; set; }

    /// <summary>
    /// Vertical center of buoyancy (m)
    /// </summary>
    public decimal? KbM { get; set; }

    /// <summary>
    /// Longitudinal center of buoyancy (% of Lpp from AP)
    /// </summary>
    public decimal? LcbPctLpp { get; set; }

    // Scoring & Validation

    /// <summary>
    /// Individual KPI scores (JSON)
    /// Example: {"deltaBalance": 0.98, "installedPower": 0.85, "constraintsOk": 1.0}
    /// </summary>
    public string? ScoresJson { get; set; }

    /// <summary>
    /// Constraint violation flags (JSON)
    /// Example: {"draftExceeded": false, "beamExceeded": false, "lowFreeboard": true}
    /// </summary>
    public string? FlagsJson { get; set; }

    /// <summary>
    /// Weighted composite score (0-1, higher is better)
    /// </summary>
    public decimal Score { get; set; }

    // Geometry

    /// <summary>
    /// Parametric hull geometry (offsets grid as JSON)
    /// Example: {"stations": [{"x": 0, "waterlines": [{"z": 0, "y": 0}, ...]}]}
    /// </summary>
    public string? GeometryJson { get; set; }

    /// <summary>
    /// Status of geometry generation
    /// </summary>
    public GeometryGenerationStatus GeometryGenerationStatus { get; set; } = GeometryGenerationStatus.Success;

    /// <summary>
    /// Error message if geometry generation failed
    /// </summary>
    public string? GeometryGenerationError { get; set; }

    // Metadata

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Provenance (Data-Driven Mode)

    /// <summary>
    /// Reference vessel ID from catalog (if Data-Driven mode)
    /// </summary>
    public string? ReferenceVesselId { get; set; }

    /// <summary>
    /// Reference vessel name (e.g., "KCS", "KVLCC2")
    /// </summary>
    public string? ReferenceVesselName { get; set; }

    /// <summary>
    /// Similarity score from KNN search (0-1, higher is better)
    /// Null for First-Principles mode
    /// </summary>
    public decimal? SimilarityScore { get; set; }

    /// <summary>
    /// Solver mode used: FirstPrinciples, DataDrivenRealWorld, DataDrivenParametric
    /// </summary>
    public string? SolverMode { get; set; }

    // Navigation Properties

    /// <summary>
    /// Parent sizing run
    /// </summary>
    public SizingRun SizingRun { get; set; } = null!;
}
