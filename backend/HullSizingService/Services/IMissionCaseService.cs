using Shared.DTOs.Sizing;

namespace HullSizingService.Services;

public interface IMissionCaseService
{
    Task<List<MissionCaseDto>> GetAllAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<MissionCaseDto?> GetByIdAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
    Task<MissionCaseDto> CreateAsync(CreateMissionCaseDto dto, Guid userId, string tenantId, CancellationToken cancellationToken = default);
    Task<MissionCaseDto?> UpdateAsync(Guid id, UpdateMissionCaseDto dto, string tenantId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
}
