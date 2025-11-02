namespace Shared.Models.Sizing;

/// <summary>
/// Generated hull candidate from sizing solver
/// </summary>
public class CandidateDesign
{
    public Guid Id { get; set; }
    public Guid SizingRunId { get; set; }
    public string HullFamily { get; set; } = null!;
    public int Rank { get; set; }
    public bool IsSelected { get; set; }

    // Principal Dimensions
    public decimal LppM { get; set; }
    public decimal LwlM { get; set; }
    public decimal LoaM { get; set; }
    public decimal BM { get; set; }
    public decimal TM { get; set; }
    public decimal DM { get; set; }

    // Form Coefficients
    public decimal Cb { get; set; }
    public decimal Cp { get; set; }
    public decimal Cwp { get; set; }
    public decimal? Cm { get; set; }

    // Displacement
    public decimal DisplacementT { get; set; }

    // Speed
    public decimal Fn { get; set; }
    public decimal? LwlOverLambda { get; set; }

    // Resistance & Power
    public decimal? EhpKw { get; set; }
    public decimal? ShpKw { get; set; }

    // Stability
    public decimal? GmEstM { get; set; }
    public decimal? KbM { get; set; }
    public decimal? LcbPctLpp { get; set; }

    // Scoring
    public string? ScoresJson { get; set; }
    public string? FlagsJson { get; set; }
    public decimal Score { get; set; }

    // Geometry
    public string? GeometryJson { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public SizingRun SizingRun { get; set; } = null!;
}

