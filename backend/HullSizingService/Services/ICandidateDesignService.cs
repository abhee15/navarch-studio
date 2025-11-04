using Shared.DTOs.Sizing;

namespace HullSizingService.Services;

public interface ICandidateDesignService
{
    Task<CandidateDesignDto?> GetByIdAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
    Task<CandidateDesignDto?> UpdateAsync(Guid id, UpdateCandidateDesignDto dto, string tenantId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
    Task<string?> ExportJsonAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
    Task<string?> ExportCsvAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
    Task<CandidateDesignDto?> AdjustParameterAsync(Guid id, AdjustParameterDto dto, string tenantId, CancellationToken cancellationToken = default);
}


