using System.Collections.Generic;

namespace HullSizingService.Services.ShipD;

/// <summary>
/// Result produced by the ShipD parameter adapter, containing the canonical vector and metadata.
/// </summary>
public record ShipDParameterizationResult(
    IReadOnlyList<decimal> ParameterVector,
    IReadOnlyList<string> ParameterLabels,
    string VesselCategory,
    string VesselType,
    string? BowFamily,
    string? MidshipFamily,
    string? SternFamily,
    int MaskVersion,
    IDictionary<string, object>? AdditionalParameters,
    IReadOnlyList<string> Warnings);

