using Shared.DTOs.ShipD;
using Shared.DTOs.Sizing;
using Shared.Models.Sizing;

namespace HullSizingService.Services.ShipD;

/// <summary>
/// Builds ShipD-ready parameter vectors from mission cases and user selections.
/// Also provides intelligent vector adjustment methods for interactive parameter exploration.
/// </summary>
public interface IShipDParameterAdapter
{
    Task<ShipDParameterizationResult> BuildAsync(
        MissionCase missionCase,
        CreateSizingRunDto runRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Intelligently adjusts a ShipD parameter vector when principal dimensions change.
    /// Scales longitudinal proportions, beam-related angles, and draft-related parameters
    /// to maintain hull form character.
    /// </summary>
    decimal[] AdjustVectorForDimensionChange(
        decimal[] originalVector,
        decimal oldLpp,
        decimal newLpp,
        decimal oldBeam,
        decimal newBeam,
        decimal oldDraft,
        decimal newDraft,
        IReadOnlyList<ShipDParameterMetadataDto> metadata);

    /// <summary>
    /// Intelligently adjusts a ShipD parameter vector when form coefficients change.
    /// Adjusts section fullness, longitudinal distribution, and waterplane shape.
    /// </summary>
    decimal[] AdjustVectorForCoefficientChange(
        decimal[] originalVector,
        string coefficient,
        decimal oldValue,
        decimal newValue,
        IReadOnlyList<ShipDParameterMetadataDto> metadata);

    /// <summary>
    /// Updates a single parameter in the ShipD vector with validation and clamping.
    /// </summary>
    decimal[] UpdateShipDParameter(
        decimal[] originalVector,
        int parameterIndex,
        decimal newValue,
        IReadOnlyList<ShipDParameterMetadataDto> metadata);
}
