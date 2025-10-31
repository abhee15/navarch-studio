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
                v.UpdatedAt
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
                HullFamily = metadata.HullFamily
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
            } : null
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
                DeletedAt = v.DeletedAt
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
}
