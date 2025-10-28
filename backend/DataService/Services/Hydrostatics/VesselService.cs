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

        _logger.LogInformation("Created vessel {VesselId} '{VesselName}' for user {UserId} (stored in SI units)",
            vessel.Id, vessel.Name, userId);

        return vessel;
    }

    public async Task<Vessel?> GetVesselAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Vessels
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<VesselDetailsDto?> GetVesselDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // First, get the vessel with basic properties
        var vessel = await _context.Vessels
            .Where(v => v.Id == id)
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
            UpdatedAt = vessel.UpdatedAt
        };
    }

    public async Task<List<Vessel>> ListVesselsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Vessels
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Vessel?> UpdateVesselAsync(Guid id, VesselDto dto, CancellationToken cancellationToken = default)
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
}

