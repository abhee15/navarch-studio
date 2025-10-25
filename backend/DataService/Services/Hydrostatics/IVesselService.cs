using Shared.DTOs;
using Shared.Models;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for managing vessels
/// </summary>
public interface IVesselService
{
    /// <summary>
    /// Creates a new vessel
    /// </summary>
    Task<Vessel> CreateVesselAsync(VesselDto dto, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a vessel by ID
    /// </summary>
    Task<Vessel?> GetVesselAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets vessel details including geometry counts
    /// </summary>
    Task<VesselDetailsDto?> GetVesselDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists vessels for a user
    /// </summary>
    Task<List<Vessel>> ListVesselsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a vessel
    /// </summary>
    Task<Vessel?> UpdateVesselAsync(Guid id, VesselDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a vessel
    /// </summary>
    Task<bool> DeleteVesselAsync(Guid id, CancellationToken cancellationToken = default);
}

