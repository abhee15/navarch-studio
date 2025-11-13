using Shared.DTOs.ShipD;

namespace DataService.Services.ShipD;

public interface IShipDMetadataService
{
    Task<IReadOnlyList<ShipDParameterMetadataDto>> GetParameterMetadataAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShipDVesselTaxonomyDto>> GetVesselTaxonomyAsync(CancellationToken cancellationToken = default);
}

