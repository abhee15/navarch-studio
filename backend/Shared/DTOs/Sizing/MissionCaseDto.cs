namespace Shared.DTOs.Sizing;

/// <summary>
/// DTO for mission case response
/// </summary>
public record MissionCaseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string MissionType { get; init; } = string.Empty;

    // Cargo basis
    public string CargoBasis { get; init; } = string.Empty; // "volume", "weight", "teu"
    public decimal CargoValue { get; init; }
    public decimal? CargoDensityTPerM3 { get; init; }
    public decimal? CargoVolumeM3 { get; init; }
    public int? TeuCount { get; init; }

    // Speed & margins
    public decimal ServiceSpeedKn { get; init; }
    public decimal SeaMarginPct { get; init; }

    // Environment
    public decimal? EnvHsM { get; init; }
    public decimal? EnvTzS { get; init; }

    // Constraints
    public decimal? CapLoaM { get; init; }
    public decimal? CapBeamM { get; init; }
    public decimal? CapDraftM { get; init; }
    public decimal? CapAirdraftM { get; init; }

    // Additional
    public decimal? EnduranceNm { get; init; }
    public string? Notes { get; init; }

    // Metadata
    public Guid UserId { get; init; }
    public string? TenantId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating a new mission case
/// </summary>
public record CreateMissionCaseDto
{
    public string Name { get; init; } = string.Empty;
    public string MissionType { get; init; } = string.Empty;

    // Cargo basis (ONE of these must be provided based on CargoBasis)
    public string CargoBasis { get; init; } = string.Empty; // "volume", "weight", "teu"
    public decimal CargoValue { get; init; }
    public decimal? CargoDensityTPerM3 { get; init; }
    public decimal? CargoVolumeM3 { get; init; }
    public int? TeuCount { get; init; }

    // Speed & margins
    public decimal ServiceSpeedKn { get; init; }
    public decimal SeaMarginPct { get; init; } = 15.0m; // Default 15%

    // Environment
    public decimal? EnvHsM { get; init; }
    public decimal? EnvTzS { get; init; }

    // Constraints (optional)
    public decimal? CapLoaM { get; init; }
    public decimal? CapBeamM { get; init; }
    public decimal? CapDraftM { get; init; }
    public decimal? CapAirdraftM { get; init; }

    // Additional
    public decimal? EnduranceNm { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// DTO for updating an existing mission case
/// </summary>
public record UpdateMissionCaseDto
{
    public string? Name { get; init; }
    public string? MissionType { get; init; }

    // Cargo basis
    public string? CargoBasis { get; init; }
    public decimal? CargoValue { get; init; }
    public decimal? CargoDensityTPerM3 { get; init; }
    public decimal? CargoVolumeM3 { get; init; }
    public int? TeuCount { get; init; }

    // Speed & margins
    public decimal? ServiceSpeedKn { get; init; }
    public decimal? SeaMarginPct { get; init; }

    // Environment
    public decimal? EnvHsM { get; init; }
    public decimal? EnvTzS { get; init; }

    // Constraints
    public decimal? CapLoaM { get; init; }
    public decimal? CapBeamM { get; init; }
    public decimal? CapDraftM { get; init; }
    public decimal? CapAirdraftM { get; init; }

    // Additional
    public decimal? EnduranceNm { get; init; }
    public string? Notes { get; init; }
}

