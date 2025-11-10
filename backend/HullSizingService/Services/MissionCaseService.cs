using HullSizingService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Sizing;
using Shared.Models.Sizing;

namespace HullSizingService.Services;

public class MissionCaseService : IMissionCaseService
{
    private readonly SizingDbContext _context;
    private readonly ILogger<MissionCaseService> _logger;

    public MissionCaseService(SizingDbContext context, ILogger<MissionCaseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<MissionCaseDto>> GetAllAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MISSION_SERVICE] Getting all mission cases for tenant {TenantId}", tenantId);

        var cases = await _context.MissionCases
            .Where(mc => mc.TenantId == tenantId)
            .OrderByDescending(mc => mc.CreatedAt)
            .ToListAsync(cancellationToken);

        return cases.Select(MapToDto).ToList();
    }

    public async Task<MissionCaseDto?> GetByIdAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
    {
        var missionCase = await _context.MissionCases
            .Where(mc => mc.Id == id && mc.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        return missionCase == null ? null : MapToDto(missionCase);
    }

    public async Task<MissionCaseDto> CreateAsync(CreateMissionCaseDto dto, Guid userId, string tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MISSION_SERVICE] Creating mission case '{Name}' for tenant {TenantId}", dto.Name, tenantId);

        var missionCase = new MissionCase
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Name = dto.Name,
            MissionType = dto.MissionType.ToLower(),
            CargoBasis = dto.CargoBasis.ToLower(),
            CargoValue = dto.CargoValue,
            CargoDensityTPerM3 = dto.CargoDensityTPerM3,
            CargoVolumeM3 = dto.CargoVolumeM3,
            TeuCount = dto.TeuCount,
            ServiceSpeedKn = dto.ServiceSpeedKn,
            SeaMarginPct = dto.SeaMarginPct,
            EnvHsM = dto.EnvHsM,
            EnvTzS = dto.EnvTzS,
            CapLoaM = dto.CapLoaM,
            CapBeamM = dto.CapBeamM,
            CapDraftM = dto.CapDraftM,
            CapAirdraftM = dto.CapAirdraftM,
            EnduranceNm = dto.EnduranceNm,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MissionCases.Add(missionCase);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[MISSION_SERVICE] Created mission case {Id}", missionCase.Id);

        return MapToDto(missionCase);
    }

    public async Task<MissionCaseDto?> UpdateAsync(Guid id, UpdateMissionCaseDto dto, string tenantId, CancellationToken cancellationToken = default)
    {
        var missionCase = await _context.MissionCases
            .Where(mc => mc.Id == id && mc.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (missionCase == null) return null;

        if (dto.Name != null) missionCase.Name = dto.Name;
        if (dto.MissionType != null) missionCase.MissionType = dto.MissionType.ToLower();
        if (dto.CargoBasis != null) missionCase.CargoBasis = dto.CargoBasis.ToLower();
        if (dto.CargoValue.HasValue) missionCase.CargoValue = dto.CargoValue;
        if (dto.CargoDensityTPerM3.HasValue) missionCase.CargoDensityTPerM3 = dto.CargoDensityTPerM3;
        if (dto.CargoVolumeM3.HasValue) missionCase.CargoVolumeM3 = dto.CargoVolumeM3;
        if (dto.TeuCount.HasValue) missionCase.TeuCount = dto.TeuCount;
        if (dto.ServiceSpeedKn.HasValue) missionCase.ServiceSpeedKn = dto.ServiceSpeedKn.Value;
        if (dto.SeaMarginPct.HasValue) missionCase.SeaMarginPct = dto.SeaMarginPct.Value;
        if (dto.EnvHsM.HasValue) missionCase.EnvHsM = dto.EnvHsM;
        if (dto.EnvTzS.HasValue) missionCase.EnvTzS = dto.EnvTzS;
        if (dto.CapLoaM.HasValue) missionCase.CapLoaM = dto.CapLoaM;
        if (dto.CapBeamM.HasValue) missionCase.CapBeamM = dto.CapBeamM;
        if (dto.CapDraftM.HasValue) missionCase.CapDraftM = dto.CapDraftM;
        if (dto.CapAirdraftM.HasValue) missionCase.CapAirdraftM = dto.CapAirdraftM;
        if (dto.EnduranceNm.HasValue) missionCase.EnduranceNm = dto.EnduranceNm;
        if (dto.Notes != null) missionCase.Notes = dto.Notes;

        missionCase.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(missionCase);
    }

    public async Task<MissionCaseDto?> CloneAsync(Guid id, string newName, Guid userId, string tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MISSION_SERVICE] Cloning mission case {Id} with new name '{Name}'", id, newName);

        // Validate name
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("Brief name cannot be empty", nameof(newName));
        }

        var trimmedName = newName.Trim();
        if (trimmedName.Length > 255)
        {
            throw new ArgumentException("Brief name cannot exceed 255 characters", nameof(newName));
        }

        // Check if name already exists for this tenant
        var nameExists = await _context.MissionCases
            .AnyAsync(mc => mc.Name == trimmedName && mc.TenantId == tenantId, cancellationToken);

        if (nameExists)
        {
            throw new InvalidOperationException($"A brief with the name '{trimmedName}' already exists");
        }

        var original = await _context.MissionCases
            .Where(mc => mc.Id == id && mc.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (original == null) return null;

        var clone = new MissionCase
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Name = trimmedName,
            MissionType = original.MissionType,
            CargoBasis = original.CargoBasis,
            CargoValue = original.CargoValue,
            CargoDensityTPerM3 = original.CargoDensityTPerM3,
            CargoVolumeM3 = original.CargoVolumeM3,
            TeuCount = original.TeuCount,
            ServiceSpeedKn = original.ServiceSpeedKn,
            SeaMarginPct = original.SeaMarginPct,
            EnvHsM = original.EnvHsM,
            EnvTzS = original.EnvTzS,
            CapLoaM = original.CapLoaM,
            CapBeamM = original.CapBeamM,
            CapDraftM = original.CapDraftM,
            CapAirdraftM = original.CapAirdraftM,
            EnduranceNm = original.EnduranceNm,
            Notes = original.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MissionCases.Add(clone);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[MISSION_SERVICE] Cloned mission case {OriginalId} to {CloneId} with name '{Name}'", id, clone.Id, trimmedName);

        return MapToDto(clone);
    }

    public async Task<bool> DeleteAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
    {
        var missionCase = await _context.MissionCases
            .Where(mc => mc.Id == id && mc.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (missionCase == null) return false;

        missionCase.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static MissionCaseDto MapToDto(MissionCase entity)
    {
        return new MissionCaseDto
        {
            Id = entity.Id,
            Name = entity.Name,
            MissionType = entity.MissionType,
            CargoBasis = entity.CargoBasis,
            CargoValue = entity.CargoValue ?? 0m,
            CargoDensityTPerM3 = entity.CargoDensityTPerM3,
            CargoVolumeM3 = entity.CargoVolumeM3,
            TeuCount = entity.TeuCount,
            ServiceSpeedKn = entity.ServiceSpeedKn,
            SeaMarginPct = entity.SeaMarginPct,
            EnvHsM = entity.EnvHsM,
            EnvTzS = entity.EnvTzS,
            CapLoaM = entity.CapLoaM,
            CapBeamM = entity.CapBeamM,
            CapDraftM = entity.CapDraftM,
            CapAirdraftM = entity.CapAirdraftM,
            EnduranceNm = entity.EnduranceNm,
            Notes = entity.Notes,
            UserId = entity.UserId,
            TenantId = entity.TenantId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
