using System.Collections.Generic;

namespace Shared.DTOs.ShipD;

public record ShipDParameterMetadataDto(
    Guid Id,
    int ParameterIndex,
    string Label,
    string? Group,
    string? Description,
    string? Unit,
    decimal? Min,
    decimal? Max,
    decimal? Mean,
    decimal? StdDev,
    string? MetadataJson
);

public record ShipDVesselTaxonomyDto(
    Guid Id,
    string Category,
    string Type,
    string DisplayName,
    string? Description,
    IReadOnlyList<string> BowFamilies,
    IReadOnlyList<string> MidshipFamilies,
    IReadOnlyList<string> SternFamilies,
    int MaskVersion,
    string? AdditionalParametersJson
);
