namespace Shared.Models.Sizing;

/// <summary>
/// User-defined mission requirements for hull sizing
/// </summary>
public class MissionCase
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TenantId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? MissionCategory { get; set; }
    public string MissionType { get; set; } = null!;

    // Cargo
    public string CargoBasis { get; set; } = null!;
    public decimal? CargoValue { get; set; }
    public decimal? CargoVolumeM3 { get; set; }
    public decimal? CargoDensityTPerM3 { get; set; }
    public int? TeuCount { get; set; }

    // Speed & Margins
    public decimal ServiceSpeedKn { get; set; }
    public decimal SeaMarginPct { get; set; } = 0.15m;
    public decimal ServiceMarginPct { get; set; } = 0.15m;

    // Environment
    public decimal? EnvHsM { get; set; }
    public decimal? EnvTzS { get; set; }

    // Constraints
    public decimal? CapLoaM { get; set; }
    public decimal? CapBeamM { get; set; }
    public decimal? CapDraftM { get; set; }
    public decimal? CapAirdraftM { get; set; }
    public decimal? EnduranceNm { get; set; }

    public string? Notes { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public ICollection<SizingRun> SizingRuns { get; set; } = new List<SizingRun>();
}

