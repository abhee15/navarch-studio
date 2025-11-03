namespace Shared.DTOs.Sizing;

/// <summary>
/// DTO for candidate design response
/// </summary>
public record CandidateDesignDto
{
    public Guid Id { get; init; }
    public Guid SizingRunId { get; init; }
    public string HullFamily { get; init; } = string.Empty;

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
    public string? GeomParamsJson { get; init; }

    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for updating a candidate design
/// </summary>
public record UpdateCandidateDesignDto
{
    public bool? IsSelected { get; init; }
}

