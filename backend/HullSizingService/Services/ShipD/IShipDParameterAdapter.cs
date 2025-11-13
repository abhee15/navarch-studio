using Shared.DTOs.Sizing;
using Shared.Models.Sizing;

namespace HullSizingService.Services.ShipD;

/// <summary>
/// Builds ShipD-ready parameter vectors from mission cases and user selections.
/// </summary>
public interface IShipDParameterAdapter
{
    Task<ShipDParameterizationResult> BuildAsync(
        MissionCase missionCase,
        CreateSizingRunDto runRequest,
        CancellationToken cancellationToken = default);
}

