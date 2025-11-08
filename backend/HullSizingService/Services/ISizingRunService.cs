using Shared.DTOs.Sizing;

namespace HullSizingService.Services;

public interface ISizingRunService
{
    Task<List<SizingRunDto>> GetByMissionCaseIdAsync(Guid missionCaseId, string tenantId, CancellationToken cancellationToken = default);
    Task<SizingRunDto?> GetByIdAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
    Task<SizingRunDto> CreateAsync(CreateSizingRunDto dto, Guid userId, string tenantId, CancellationToken cancellationToken = default);
    Task<List<CandidateDesignDto>> GetCandidatesAsync(Guid runId, string tenantId, CancellationToken cancellationToken = default);
}






