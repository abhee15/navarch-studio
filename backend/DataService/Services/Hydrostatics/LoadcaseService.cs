using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;
using Shared.Models;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Implementation of loadcase management service
/// </summary>
public class LoadcaseService : ILoadcaseService
{
    private readonly DataDbContext _context;
    private readonly ILogger<LoadcaseService> _logger;

    public LoadcaseService(
        DataDbContext context,
        ILogger<LoadcaseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Loadcase> CreateLoadcaseAsync(Guid vesselId, LoadcaseDto dto, CancellationToken cancellationToken = default)
    {
        // Validate vessel exists
        var vessel = await _context.Vessels.FindAsync(new object[] { vesselId }, cancellationToken);
        if (vessel == null)
        {
            throw new ArgumentException($"Vessel with ID {vesselId} not found");
        }

        // Validate loadcase data
        ValidateLoadcase(dto);

        var loadcase = new Loadcase
        {
            Id = Guid.NewGuid(),
            VesselId = vesselId,
            Name = dto.Name,
            Rho = dto.Rho,
            KG = dto.KG,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Loadcases.Add(loadcase);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created loadcase {LoadcaseId} '{LoadcaseName}' for vessel {VesselId}",
            loadcase.Id, loadcase.Name, vesselId);

        return loadcase;
    }

    public async Task<Loadcase?> GetLoadcaseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Loadcases
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<List<Loadcase>> ListLoadcasesAsync(Guid vesselId, CancellationToken cancellationToken = default)
    {
        return await _context.Loadcases
            .Where(l => l.VesselId == vesselId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Loadcase?> UpdateLoadcaseAsync(Guid id, LoadcaseDto dto, CancellationToken cancellationToken = default)
    {
        var loadcase = await _context.Loadcases.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (loadcase == null)
        {
            return null;
        }

        // Validate loadcase data
        ValidateLoadcase(dto);

        loadcase.Name = dto.Name;
        loadcase.Rho = dto.Rho;
        loadcase.KG = dto.KG;
        loadcase.Notes = dto.Notes;
        loadcase.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated loadcase {LoadcaseId} '{LoadcaseName}'",
            loadcase.Id, loadcase.Name);

        return loadcase;
    }

    public async Task<bool> DeleteLoadcaseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var loadcase = await _context.Loadcases.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (loadcase == null)
        {
            return false;
        }

        _context.Loadcases.Remove(loadcase);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted loadcase {LoadcaseId} '{LoadcaseName}'",
            loadcase.Id, loadcase.Name);

        return true;
    }

    private static void ValidateLoadcase(LoadcaseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Loadcase name is required");
        }

        if (dto.Rho <= 0)
        {
            throw new ArgumentException("Water density (rho) must be positive");
        }

        // Reasonable range for water density: 900-1100 kg/m³
        if (dto.Rho < 900 || dto.Rho > 1100)
        {
            throw new ArgumentException("Water density (rho) must be between 900 and 1100 kg/m³");
        }

        if (dto.KG.HasValue && dto.KG < 0)
        {
            throw new ArgumentException("Vertical center of gravity (KG) cannot be negative");
        }
    }
}

