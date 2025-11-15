using DataService.Data;
using Microsoft.EntityFrameworkCore;
using NavArch.UnitConversion.Services;
using Shared.DTOs;
using Shared.Models;
using Shared.Services;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Implementation of vessel management service
/// </summary>
public class VesselService : IVesselService
{
    private readonly DataDbContext _context;
    private readonly IValidationService _validationService;
    private readonly IUnitConverter _converter;
    private readonly ILogger<VesselService> _logger;

    public VesselService(
        DataDbContext context,
        IValidationService validationService,
        IUnitConverter converter,
        ILogger<VesselService> logger)
    {
        _context = context;
        _validationService = validationService;
        _converter = converter;
        _logger = logger;
    }

    public async Task<Vessel> CreateVesselAsync(VesselDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        // Convert from user's units to SI for storage
        UnitConversionHelper.ConvertToSI(dto, _converter);

        // Validate
        var validationResult = _validationService.ValidateVessel(dto);
        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join(", ", validationResult.Errors.Select(e => e.Message));
            throw new ArgumentException($"Vessel validation failed: {errorMessages}");
        }

        var vessel = new Vessel
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name,
            Description = dto.Description,
            Lpp = dto.Lpp,  // Now in SI (meters)
            Beam = dto.Beam,  // Now in SI (meters)
            DesignDraft = dto.DesignDraft,  // Now in SI (meters)
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        ApplyShipdMetadata(vessel, dto);

        _context.Vessels.Add(vessel);
        await _context.SaveChangesAsync(cancellationToken);

        // Create optional metadata if provided
        if (dto.Metadata != null)
        {
            var metadata = new VesselMetadata
            {
                VesselId = vessel.Id,
                VesselType = dto.Metadata.VesselType,
                Size = dto.Metadata.Size,
                BlockCoefficient = dto.Metadata.BlockCoefficient,
                HullFamily = dto.Metadata.HullFamily,
                ShipdCategory = dto.Metadata.ShipdCategory,
                ShipdType = dto.Metadata.ShipdType,
                ShipdMaskVersion = dto.Metadata.ShipdMaskVersion,

                // Form coefficients (from hull sizing)
                PrismaticCoefficient = dto.Metadata.PrismaticCoefficient,
                MidshipCoefficient = dto.Metadata.MidshipCoefficient,
                WaterplaneCoefficient = dto.Metadata.WaterplaneCoefficient,

                // Additional dimensions (from hull sizing)
                Lwl = dto.Metadata.Lwl,
                Loa = dto.Metadata.Loa,
                Depth = dto.Metadata.Depth,

                // Stability parameters (from hull sizing)
                KbInitial = dto.Metadata.KbInitial,
                LcbPctLpp = dto.Metadata.LcbPctLpp,
                GmInitial = dto.Metadata.GmInitial,

                // Propulsion data (from hull sizing)
                EhpKw = dto.Metadata.EhpKw,
                ShpKw = dto.Metadata.ShpKw,
                FroudeNumber = dto.Metadata.FroudeNumber,

                CreatedAt = DateTime.UtcNow
            };
            _context.VesselMetadata.Add(metadata);
        }

        if (dto.Materials != null)
        {
            var materials = new MaterialsConfig
            {
                VesselId = vessel.Id,
                HullMaterial = dto.Materials.HullMaterial,
                SuperstructureMaterial = dto.Materials.SuperstructureMaterial,
                CreatedAt = DateTime.UtcNow
            };
            _context.MaterialsConfigs.Add(materials);
        }

        if (dto.Loading != null)
        {
            var loading = new LoadingConditions
            {
                VesselId = vessel.Id,
                LightshipTonnes = dto.Loading.LightshipTonnes,
                DeadweightTonnes = dto.Loading.DeadweightTonnes,
                CreatedAt = DateTime.UtcNow
            };
            _context.LoadingConditions.Add(loading);
        }

        // Save metadata if any was added
        if (dto.Metadata != null || dto.Materials != null || dto.Loading != null)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Created vessel {VesselId} '{VesselName}' for user {UserId} (stored in SI units)",
            vessel.Id, vessel.Name, userId);

        return vessel;
    }

    public async Task<Vessel?> GetVesselAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Vessels
            .AsNoTracking()
            .Select(v => new Vessel
            {
                Id = v.Id,
                UserId = v.UserId,
                Name = v.Name,
                Description = v.Description,
                Lpp = v.Lpp,
                Beam = v.Beam,
                DesignDraft = v.DesignDraft,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt,
                DeletedAt = v.DeletedAt
            })
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<VesselDetailsDto?> GetVesselDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // First, get the vessel with basic properties (no navigation properties)
        var vessel = await _context.Vessels
            .AsNoTracking()
            .Where(v => v.Id == id)
            .Select(v => new
            {
                v.Id,
                v.UserId,
                v.Name,
                v.Description,
                v.Lpp,
                v.Beam,
                v.DesignDraft,
                v.CreatedAt,
                v.UpdatedAt,
                v.ShipdCategory,
                v.ShipdType,
                v.ShipdTypeDisplayName,
                v.ShipdBowFamily,
                v.ShipdMidshipFamily,
                v.ShipdSternFamily,
                v.ShipdMaskVersion,
                v.ShipdParametersJson,
                v.OriginCandidateId,
                v.OriginSizingRunId,
                v.OriginMissionCaseId,
                v.OriginSystem,
                v.OriginDesignName,
                v.OriginMissionName,
                v.OriginRunName,
                v.OriginCreatedAt,
                v.PushedToHydrostaticsAt,
                v.OriginUserId,
                v.OriginUserDisplayName,
                v.OriginIdempotencyKey
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (vessel == null)
        {
            return null;
        }

        // Get counts using separate efficient queries
        // EF Core will translate these to SQL COUNT queries
        var stationsCount = await _context.Stations
            .CountAsync(s => s.VesselId == id, cancellationToken);

        var waterlinesCount = await _context.Waterlines
            .CountAsync(w => w.VesselId == id, cancellationToken);

        var offsetsCount = await _context.Offsets
            .CountAsync(o => o.VesselId == id, cancellationToken);

        // Get optional metadata
        var metadata = await _context.VesselMetadata
            .AsNoTracking()
            .Where(m => m.VesselId == id)
            .FirstOrDefaultAsync(cancellationToken);

        var materials = await _context.MaterialsConfigs
            .AsNoTracking()
            .Where(m => m.VesselId == id)
            .FirstOrDefaultAsync(cancellationToken);

        var loading = await _context.LoadingConditions
            .AsNoTracking()
            .Where(l => l.VesselId == id)
            .FirstOrDefaultAsync(cancellationToken);

        var isTemplate = vessel.UserId == Shared.Constants.TemplateVessels.SystemUserId;

        return new VesselDetailsDto
        {
            Id = vessel.Id,
            Name = vessel.Name,
            Description = vessel.Description,
            ShipdCategory = vessel.ShipdCategory,
            ShipdType = vessel.ShipdType,
            ShipdTypeDisplayName = vessel.ShipdTypeDisplayName,
            ShipdBowFamily = vessel.ShipdBowFamily,
            ShipdMidshipFamily = vessel.ShipdMidshipFamily,
            ShipdSternFamily = vessel.ShipdSternFamily,
            ShipdMaskVersion = vessel.ShipdMaskVersion,
            ShipdParametersJson = vessel.ShipdParametersJson,
            Lpp = vessel.Lpp,  // Stored in SI, filter will convert to user's preference
            Beam = vessel.Beam,
            DesignDraft = vessel.DesignDraft,
            Units = "SI",  // Data comes from DB in SI
            StationsCount = stationsCount,
            WaterlinesCount = waterlinesCount,
            OffsetsCount = offsetsCount,
            CreatedAt = vessel.CreatedAt,
            UpdatedAt = vessel.UpdatedAt,
            IsTemplate = isTemplate,
            Metadata = metadata != null ? new VesselMetadataDto
            {
                VesselType = metadata.VesselType,
                Size = metadata.Size,
                BlockCoefficient = metadata.BlockCoefficient,
                HullFamily = metadata.HullFamily,
                ShipdCategory = metadata.ShipdCategory,
                ShipdType = metadata.ShipdType,
                ShipdMaskVersion = metadata.ShipdMaskVersion,

                // Form coefficients (from hull sizing)
                PrismaticCoefficient = metadata.PrismaticCoefficient,
                MidshipCoefficient = metadata.MidshipCoefficient,
                WaterplaneCoefficient = metadata.WaterplaneCoefficient,

                // Additional dimensions (from hull sizing)
                Lwl = metadata.Lwl,
                Loa = metadata.Loa,
                Depth = metadata.Depth,

                // Stability parameters (from hull sizing)
                KbInitial = metadata.KbInitial,
                LcbPctLpp = metadata.LcbPctLpp,
                GmInitial = metadata.GmInitial,

                // Propulsion data (from hull sizing)
                EhpKw = metadata.EhpKw,
                ShpKw = metadata.ShpKw,
                FroudeNumber = metadata.FroudeNumber
            } : null,
            Materials = materials != null ? new MaterialsConfigDto
            {
                HullMaterial = materials.HullMaterial,
                SuperstructureMaterial = materials.SuperstructureMaterial
            } : null,
            Loading = loading != null ? new LoadingConditionsDto
            {
                LightshipTonnes = loading.LightshipTonnes,
                DeadweightTonnes = loading.DeadweightTonnes
            } : null,
            SourceDesign = vessel.OriginCandidateId.HasValue ||
                           !string.IsNullOrWhiteSpace(vessel.OriginSystem) ||
                           !string.IsNullOrWhiteSpace(vessel.OriginIdempotencyKey)
                ? new SourceDesignDto
                {
                    CandidateId = vessel.OriginCandidateId,
                    SizingRunId = vessel.OriginSizingRunId,
                    MissionCaseId = vessel.OriginMissionCaseId,
                    SourceSystem = vessel.OriginSystem,
                    DesignName = vessel.OriginDesignName,
                    MissionName = vessel.OriginMissionName,
                    RunName = vessel.OriginRunName,
                    OriginCreatedAt = vessel.OriginCreatedAt,
                    PushedAt = vessel.PushedToHydrostaticsAt,
                    UserId = vessel.OriginUserId,
                    UserDisplayName = vessel.OriginUserDisplayName,
                    IdempotencyKey = vessel.OriginIdempotencyKey
                }
                : null
        };
    }

    public async Task<List<Vessel>> ListVesselsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Include both user's vessels and template vessels (system-owned)
        return await _context.Vessels
            .AsNoTracking()  // Don't track for read-only query
            .Where(v => v.UserId == userId || v.UserId == Shared.Constants.TemplateVessels.SystemUserId)
            .OrderByDescending(v => v.UpdatedAt)
            .Select(v => new Vessel
            {
                Id = v.Id,
                UserId = v.UserId,
                Name = v.Name,
                Description = v.Description,
                Lpp = v.Lpp,
                Beam = v.Beam,
                DesignDraft = v.DesignDraft,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt,
                DeletedAt = v.DeletedAt,
                ShipdCategory = v.ShipdCategory,
                ShipdType = v.ShipdType,
                ShipdTypeDisplayName = v.ShipdTypeDisplayName,
                ShipdBowFamily = v.ShipdBowFamily,
                ShipdMidshipFamily = v.ShipdMidshipFamily,
                ShipdSternFamily = v.ShipdSternFamily,
                ShipdMaskVersion = v.ShipdMaskVersion,
                ShipdParametersJson = v.ShipdParametersJson,
                OriginCandidateId = v.OriginCandidateId,
                OriginSizingRunId = v.OriginSizingRunId,
                OriginMissionCaseId = v.OriginMissionCaseId,
                OriginSystem = v.OriginSystem,
                OriginDesignName = v.OriginDesignName,
                OriginMissionName = v.OriginMissionName,
                OriginRunName = v.OriginRunName,
                OriginCreatedAt = v.OriginCreatedAt,
                PushedToHydrostaticsAt = v.PushedToHydrostaticsAt,
                OriginUserId = v.OriginUserId,
                OriginUserDisplayName = v.OriginUserDisplayName,
                OriginIdempotencyKey = v.OriginIdempotencyKey
                // Explicitly exclude navigation properties to avoid lazy loading
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Vessel?> UpdateVesselAsync(Guid id, VesselDto dto, CancellationToken cancellationToken = default)
    {
        // Check if this is a template vessel - template vessels cannot be modified
        if (Shared.Constants.TemplateVessels.IsTemplateVesselId(id))
        {
            throw new InvalidOperationException("Template vessels cannot be modified. Please create your own vessel to customize.");
        }

        // Convert from user's units to SI for storage
        UnitConversionHelper.ConvertToSI(dto, _converter);

        // Validate
        var validationResult = _validationService.ValidateVessel(dto);
        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join(", ", validationResult.Errors.Select(e => e.Message));
            throw new ArgumentException($"Vessel validation failed: {errorMessages}");
        }

        var vessel = await _context.Vessels.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vessel == null)
        {
            return null;
        }

        vessel.Name = dto.Name;
        vessel.Description = dto.Description;
        vessel.Lpp = dto.Lpp;  // Now in SI (meters)
        vessel.Beam = dto.Beam;  // Now in SI (meters)
        vessel.DesignDraft = dto.DesignDraft;  // Now in SI (meters)
        vessel.UpdatedAt = DateTime.UtcNow;
        ApplyShipdMetadata(vessel, dto);

        await UpsertMetadataAsync(vessel.Id, dto.Metadata, cancellationToken);
        await UpsertMaterialsAsync(vessel.Id, dto.Materials, cancellationToken);
        await UpsertLoadingAsync(vessel.Id, dto.Loading, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated vessel {VesselId} '{VesselName}' (stored in SI units)", vessel.Id, vessel.Name);

        return vessel;
    }

    public async Task<bool> DeleteVesselAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Check if this is a template vessel - template vessels cannot be deleted
        if (Shared.Constants.TemplateVessels.IsTemplateVesselId(id))
        {
            throw new InvalidOperationException("Template vessels cannot be deleted. They are system templates available to all users.");
        }

        var vessel = await _context.Vessels.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vessel == null)
        {
            return false;
        }

        // Soft delete
        vessel.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted vessel {VesselId} '{VesselName}'", vessel.Id, vessel.Name);

        return true;
    }

    /// <summary>
    /// Checks if a vessel is a template vessel (read-only system template)
    /// </summary>
    public bool IsTemplateVessel(Guid vesselId)
    {
        return Shared.Constants.TemplateVessels.IsTemplateVesselId(vesselId);
    }

    public Task<List<VesselTemplateDto>> GetTemplatesAsync()
    {
        var templates = new List<VesselTemplateDto>
        {
            new VesselTemplateDto
            {
                Id = "patrol-boat-35m",
                Name = "Patrol Boat (35m)",
                Description = "Medium patrol vessel for coastal operations",
                Preset = new VesselDto
                {
                    Name = "Patrol Boat (35m)",
                    Description = "Medium patrol vessel for coastal operations",
                    Lpp = 35,
                    Beam = 6.2m,
                    DesignDraft = 1.9m,
                    Metadata = new VesselMetadataDto
                    {
                        VesselType = "Ship",
                        Size = "Small",
                        BlockCoefficient = 0.52m,
                        HullFamily = "NPL"
                    }
                }
            },
            new VesselTemplateDto
            {
                Id = "trawler-24m",
                Name = "Trawler (24m)",
                Description = "Commercial fishing trawler",
                Preset = new VesselDto
                {
                    Name = "Trawler (24m)",
                    Description = "Commercial fishing trawler",
                    Lpp = 24,
                    Beam = 7.0m,
                    DesignDraft = 3.1m,
                    Metadata = new VesselMetadataDto
                    {
                        VesselType = "Ship",
                        Size = "Small",
                        BlockCoefficient = 0.62m,
                        HullFamily = "Prismatic"
                    }
                }
            },
            new VesselTemplateDto
            {
                Id = "sailing-yacht-45ft",
                Name = "Sailing Yacht (45ft)",
                Description = "Performance cruising yacht",
                Preset = new VesselDto
                {
                    Name = "Sailing Yacht (45ft)",
                    Description = "Performance cruising yacht",
                    Lpp = 13.7m,
                    Beam = 4.2m,
                    DesignDraft = 2.2m,
                    Metadata = new VesselMetadataDto
                    {
                        VesselType = "Yacht",
                        Size = "Small",
                        BlockCoefficient = 0.44m,
                        HullFamily = "Wigley"
                    }
                }
            },
            new VesselTemplateDto
            {
                Id = "container-feeder-180m",
                Name = "Container Feeder (180m)",
                Description = "Regional container ship",
                Preset = new VesselDto
                {
                    Name = "Container Feeder (180m)",
                    Description = "Regional container ship",
                    Lpp = 180,
                    Beam = 28,
                    DesignDraft = 9.5m,
                    Metadata = new VesselMetadataDto
                    {
                        VesselType = "Ship",
                        Size = "Medium",
                        BlockCoefficient = 0.70m,
                        HullFamily = "Series 60"
                    }
                }
            }
        };

        return Task.FromResult(templates);
    }

    private async Task UpsertMetadataAsync(Guid vesselId, VesselMetadataDto? dto, CancellationToken cancellationToken)
    {
        if (dto == null)
        {
            return;
        }

        var metadata = await _context.VesselMetadata.FirstOrDefaultAsync(m => m.VesselId == vesselId, cancellationToken);
        if (metadata == null)
        {
            metadata = new VesselMetadata
            {
                VesselId = vesselId,
                CreatedAt = DateTime.UtcNow
            };
            _context.VesselMetadata.Add(metadata);
        }

        metadata.VesselType = dto.VesselType;
        metadata.Size = dto.Size;
        metadata.BlockCoefficient = dto.BlockCoefficient;
        metadata.HullFamily = dto.HullFamily;
        metadata.ShipdCategory = dto.ShipdCategory;
        metadata.ShipdType = dto.ShipdType;
        metadata.ShipdMaskVersion = dto.ShipdMaskVersion;

        // Form coefficients (from hull sizing)
        metadata.PrismaticCoefficient = dto.PrismaticCoefficient;
        metadata.MidshipCoefficient = dto.MidshipCoefficient;
        metadata.WaterplaneCoefficient = dto.WaterplaneCoefficient;

        // Additional dimensions (from hull sizing)
        metadata.Lwl = dto.Lwl;
        metadata.Loa = dto.Loa;
        metadata.Depth = dto.Depth;

        // Stability parameters (from hull sizing)
        metadata.KbInitial = dto.KbInitial;
        metadata.LcbPctLpp = dto.LcbPctLpp;
        metadata.GmInitial = dto.GmInitial;

        // Propulsion data (from hull sizing)
        metadata.EhpKw = dto.EhpKw;
        metadata.ShpKw = dto.ShpKw;
        metadata.FroudeNumber = dto.FroudeNumber;
    }

    private async Task UpsertMaterialsAsync(Guid vesselId, MaterialsConfigDto? dto, CancellationToken cancellationToken)
    {
        if (dto == null)
        {
            return;
        }

        var materials = await _context.MaterialsConfigs.FirstOrDefaultAsync(m => m.VesselId == vesselId, cancellationToken);
        if (materials == null)
        {
            materials = new MaterialsConfig
            {
                VesselId = vesselId,
                CreatedAt = DateTime.UtcNow
            };
            _context.MaterialsConfigs.Add(materials);
        }

        materials.HullMaterial = dto.HullMaterial;
        materials.SuperstructureMaterial = dto.SuperstructureMaterial;
    }

    private async Task UpsertLoadingAsync(Guid vesselId, LoadingConditionsDto? dto, CancellationToken cancellationToken)
    {
        if (dto == null)
        {
            return;
        }

        var loading = await _context.LoadingConditions.FirstOrDefaultAsync(l => l.VesselId == vesselId, cancellationToken);
        if (loading == null)
        {
            loading = new LoadingConditions
            {
                VesselId = vesselId,
                CreatedAt = DateTime.UtcNow
            };
            _context.LoadingConditions.Add(loading);
        }

        loading.LightshipTonnes = dto.LightshipTonnes;
        loading.DeadweightTonnes = dto.DeadweightTonnes;
    }

    private static void ApplyShipdMetadata(Vessel vessel, VesselDto dto)
    {
        vessel.ShipdCategory = dto.ShipdCategory ?? vessel.ShipdCategory;
        vessel.ShipdType = dto.ShipdType ?? vessel.ShipdType;
        vessel.ShipdTypeDisplayName = dto.ShipdTypeDisplayName ?? vessel.ShipdTypeDisplayName;
        vessel.ShipdBowFamily = dto.ShipdBowFamily ?? vessel.ShipdBowFamily;
        vessel.ShipdMidshipFamily = dto.ShipdMidshipFamily ?? vessel.ShipdMidshipFamily;
        vessel.ShipdSternFamily = dto.ShipdSternFamily ?? vessel.ShipdSternFamily;
        vessel.ShipdMaskVersion = dto.ShipdMaskVersion ?? vessel.ShipdMaskVersion;
        vessel.ShipdParametersJson = dto.ShipdParametersJson ?? vessel.ShipdParametersJson;

        if (dto.SourceDesign != null)
        {
            vessel.OriginCandidateId = dto.SourceDesign.CandidateId ?? vessel.OriginCandidateId;
            vessel.OriginSizingRunId = dto.SourceDesign.SizingRunId ?? vessel.OriginSizingRunId;
            vessel.OriginMissionCaseId = dto.SourceDesign.MissionCaseId ?? vessel.OriginMissionCaseId;
            vessel.OriginSystem = dto.SourceDesign.SourceSystem ?? vessel.OriginSystem;
            vessel.OriginDesignName = dto.SourceDesign.DesignName ?? vessel.OriginDesignName;
            vessel.OriginMissionName = dto.SourceDesign.MissionName ?? vessel.OriginMissionName;
            vessel.OriginRunName = dto.SourceDesign.RunName ?? vessel.OriginRunName;
            vessel.OriginCreatedAt = dto.SourceDesign.OriginCreatedAt ?? vessel.OriginCreatedAt;
            vessel.PushedToHydrostaticsAt = dto.SourceDesign.PushedAt ?? vessel.PushedToHydrostaticsAt;
            vessel.OriginUserId = dto.SourceDesign.UserId ?? vessel.OriginUserId;
            vessel.OriginUserDisplayName = dto.SourceDesign.UserDisplayName ?? vessel.OriginUserDisplayName;
            vessel.OriginIdempotencyKey = dto.SourceDesign.IdempotencyKey ?? vessel.OriginIdempotencyKey;
        }
    }
}
