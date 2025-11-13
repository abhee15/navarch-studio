namespace Shared.DTOs.Sizing;

/// <summary>
/// DTO for mission case response
/// </summary>
public record MissionCaseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string MissionType { get; init; } = string.Empty;
    public string? MissionCategory { get; init; }
    public string? BowFamily { get; init; }
    public string? MidshipFamily { get; init; }
    public string? SternFamily { get; init; }
    public int? FamilyMaskVersion { get; init; }
    public string? ShipdInputsJson { get; init; }

    // Cargo basis
    public string CargoBasis { get; init; } = string.Empty;
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
    public string MissionCategory { get; init; } = string.Empty;
    public string? BowFamily { get; init; }
    public string? MidshipFamily { get; init; }
    public string? SternFamily { get; init; }
    public int? FamilyMaskVersion { get; init; }
    public string? ShipdInputsJson { get; init; }

    public string CargoBasis { get; init; } = string.Empty;
    public decimal CargoValue { get; init; }
    public decimal? CargoDensityTPerM3 { get; init; }
    public decimal? CargoVolumeM3 { get; init; }
    public int? TeuCount { get; init; }

    public decimal ServiceSpeedKn { get; init; }
    public decimal SeaMarginPct { get; init; } = 15.0m;

    public decimal? EnvHsM { get; init; }
    public decimal? EnvTzS { get; init; }

    public decimal? CapLoaM { get; init; }
    public decimal? CapBeamM { get; init; }
    public decimal? CapDraftM { get; init; }
    public decimal? CapAirdraftM { get; init; }

    public decimal? EnduranceNm { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// DTO for updating a mission case
/// </summary>
public record UpdateMissionCaseDto
{
    public string? Name { get; init; }
    public string? MissionType { get; init; }
    public string? MissionCategory { get; init; }
    public string? BowFamily { get; init; }
    public string? MidshipFamily { get; init; }
    public string? SternFamily { get; init; }
    public int? FamilyMaskVersion { get; init; }
    public string? ShipdInputsJson { get; init; }

    public string? CargoBasis { get; init; }
    public decimal? CargoValue { get; init; }
    public decimal? CargoDensityTPerM3 { get; init; }
    public decimal? CargoVolumeM3 { get; init; }
    public int? TeuCount { get; init; }

    public decimal? ServiceSpeedKn { get; init; }
    public decimal? SeaMarginPct { get; init; }

    public decimal? EnvHsM { get; init; }
    public decimal? EnvTzS { get; init; }

    public decimal? CapLoaM { get; init; }
    public decimal? CapBeamM { get; init; }
    public decimal? CapDraftM { get; init; }
    public decimal? CapAirdraftM { get; init; }

    public decimal? EnduranceNm { get; init; }
    public string? Notes { get; init; }
}
