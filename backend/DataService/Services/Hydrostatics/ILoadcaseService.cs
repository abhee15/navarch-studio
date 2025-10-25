using Shared.DTOs;
using Shared.Models;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for managing load cases
/// </summary>
public interface ILoadcaseService
{
    /// <summary>
    /// Creates a new loadcase for a vessel
    /// </summary>
    Task<Loadcase> CreateLoadcaseAsync(Guid vesselId, LoadcaseDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a loadcase by ID
    /// </summary>
    Task<Loadcase?> GetLoadcaseAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists loadcases for a vessel
    /// </summary>
    Task<List<Loadcase>> ListLoadcasesAsync(Guid vesselId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a loadcase
    /// </summary>
    Task<Loadcase?> UpdateLoadcaseAsync(Guid id, LoadcaseDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a loadcase
    /// </summary>
    Task<bool> DeleteLoadcaseAsync(Guid id, CancellationToken cancellationToken = default);
}

