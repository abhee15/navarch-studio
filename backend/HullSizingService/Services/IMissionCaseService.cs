using Shared.DTOs.Sizing;

namespace HullSizingService.Services;

/// <summary>
/// Service for managing mission cases
/// </summary>
public interface IMissionCaseService
{
    /// <summary>
    /// Get all mission cases for the current tenant
    /// </summary>
    Task<List<MissionCaseDto>> GetAllAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific mission case by ID (with tenant check)
    /// </summary>
    Task<MissionCaseDto?> GetByIdAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new mission case
    /// </summary>
    Task<MissionCaseDto> CreateAsync(CreateMissionCaseDto dto, Guid userId, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing mission case
    /// </summary>
    Task<MissionCaseDto?> UpdateAsync(Guid id, UpdateMissionCaseDto dto, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete a mission case
    /// </summary>
    Task<bool> DeleteAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
}

