using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;
using Shared.Models;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Implementation of geometry management service
/// </summary>
public class GeometryService : IGeometryService
{
    private readonly DataDbContext _context;
    private readonly IValidationService _validationService;
    private readonly ILogger<GeometryService> _logger;

    public GeometryService(
        DataDbContext context,
        IValidationService validationService,
        ILogger<GeometryService> logger)
    {
        _context = context;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<List<Station>> ImportStationsAsync(Guid vesselId, List<StationDto> stations, CancellationToken cancellationToken = default)
    {
        // Validate vessel exists
        var vessel = await _context.Vessels.FindAsync(new object[] { vesselId }, cancellationToken);
        if (vessel == null)
        {
            throw new ArgumentException($"Vessel with ID {vesselId} not found");
        }

        // Validate stations
        var validationResult = _validationService.ValidateStations(stations);
        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join(", ", validationResult.Errors.Select(e => e.Message));
            throw new ArgumentException($"Stations validation failed: {errorMessages}");
        }

        // Remove existing stations for this vessel
        var existingStations = await _context.Stations
            .Where(s => s.VesselId == vesselId)
            .ToListAsync(cancellationToken);
        _context.Stations.RemoveRange(existingStations);

        // Add new stations
        var newStations = stations.Select(dto => new Station
        {
            Id = Guid.NewGuid(),
            VesselId = vesselId,
            StationIndex = dto.StationIndex,
            X = dto.X
        }).ToList();

        _context.Stations.AddRange(newStations);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Imported {Count} stations for vessel {VesselId}",
            newStations.Count, vesselId);

        return newStations;
    }

    public async Task<List<Waterline>> ImportWaterlinesAsync(Guid vesselId, List<WaterlineDto> waterlines, CancellationToken cancellationToken = default)
    {
        // Validate vessel exists
        var vessel = await _context.Vessels.FindAsync(new object[] { vesselId }, cancellationToken);
        if (vessel == null)
        {
            throw new ArgumentException($"Vessel with ID {vesselId} not found");
        }

        // Validate waterlines
        var validationResult = _validationService.ValidateWaterlines(waterlines);
        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join(", ", validationResult.Errors.Select(e => e.Message));
            throw new ArgumentException($"Waterlines validation failed: {errorMessages}");
        }

        // Remove existing waterlines for this vessel
        var existingWaterlines = await _context.Waterlines
            .Where(w => w.VesselId == vesselId)
            .ToListAsync(cancellationToken);
        _context.Waterlines.RemoveRange(existingWaterlines);

        // Add new waterlines
        var newWaterlines = waterlines.Select(dto => new Waterline
        {
            Id = Guid.NewGuid(),
            VesselId = vesselId,
            WaterlineIndex = dto.WaterlineIndex,
            Z = dto.Z
        }).ToList();

        _context.Waterlines.AddRange(newWaterlines);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Imported {Count} waterlines for vessel {VesselId}",
            newWaterlines.Count, vesselId);

        return newWaterlines;
    }

    public async Task<List<Offset>> ImportOffsetsAsync(Guid vesselId, List<OffsetDto> offsets, CancellationToken cancellationToken = default)
    {
        // Validate vessel exists
        var vessel = await _context.Vessels.FindAsync(new object[] { vesselId }, cancellationToken);
        if (vessel == null)
        {
            throw new ArgumentException($"Vessel with ID {vesselId} not found");
        }

        // Get station and waterline counts for validation
        var stationCount = await _context.Stations.CountAsync(s => s.VesselId == vesselId, cancellationToken);
        var waterlineCount = await _context.Waterlines.CountAsync(w => w.VesselId == vesselId, cancellationToken);

        if (stationCount == 0)
        {
            throw new ArgumentException("Stations must be imported before offsets");
        }

        if (waterlineCount == 0)
        {
            throw new ArgumentException("Waterlines must be imported before offsets");
        }

        // Validate offsets
        var validationResult = _validationService.ValidateOffsets(offsets, stationCount, waterlineCount);
        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join(", ", validationResult.Errors.Select(e => e.Message));
            throw new ArgumentException($"Offsets validation failed: {errorMessages}");
        }

        // Remove existing offsets for this vessel
        var existingOffsets = await _context.Offsets
            .Where(o => o.VesselId == vesselId)
            .ToListAsync(cancellationToken);
        _context.Offsets.RemoveRange(existingOffsets);

        // Add new offsets
        var newOffsets = offsets.Select(dto => new Offset
        {
            Id = Guid.NewGuid(),
            VesselId = vesselId,
            StationIndex = dto.StationIndex,
            WaterlineIndex = dto.WaterlineIndex,
            HalfBreadthY = dto.HalfBreadthY
        }).ToList();

        _context.Offsets.AddRange(newOffsets);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Imported {Count} offsets for vessel {VesselId}",
            newOffsets.Count, vesselId);

        return newOffsets;
    }

    public async Task<OffsetsGridDto?> GetOffsetsGridAsync(Guid vesselId, CancellationToken cancellationToken = default)
    {
        // Get stations ordered by index
        var stations = await _context.Stations
            .Where(s => s.VesselId == vesselId)
            .OrderBy(s => s.StationIndex)
            .ToListAsync(cancellationToken);

        if (stations.Count == 0)
        {
            return null;
        }

        // Get waterlines ordered by index
        var waterlines = await _context.Waterlines
            .Where(w => w.VesselId == vesselId)
            .OrderBy(w => w.WaterlineIndex)
            .ToListAsync(cancellationToken);

        if (waterlines.Count == 0)
        {
            return null;
        }

        // Get all offsets
        var offsets = await _context.Offsets
            .Where(o => o.VesselId == vesselId)
            .ToListAsync(cancellationToken);

        // Build 2D grid: offsets[stationIndex][waterlineIndex]
        var grid = new List<List<decimal>>();

        for (int s = 0; s < stations.Count; s++)
        {
            var row = new List<decimal>();
            for (int w = 0; w < waterlines.Count; w++)
            {
                var offset = offsets.FirstOrDefault(o =>
                    o.StationIndex == stations[s].StationIndex &&
                    o.WaterlineIndex == waterlines[w].WaterlineIndex);

                row.Add(offset?.HalfBreadthY ?? 0m);
            }
            grid.Add(row);
        }

        return new OffsetsGridDto
        {
            Stations = stations.Select(s => s.X).ToList(),
            Waterlines = waterlines.Select(w => w.Z).ToList(),
            Offsets = grid
        };
    }

    public async Task<(int stationsCount, int waterlinesCount, int offsetsCount)> ImportCombinedGeometryAsync(
        Guid vesselId,
        List<StationDto> stations,
        List<WaterlineDto> waterlines,
        List<OffsetDto> offsets,
        CancellationToken cancellationToken = default)
    {
        // Use a transaction for atomicity
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Import in order: stations, waterlines, offsets
            var importedStations = await ImportStationsAsync(vesselId, stations, cancellationToken);
            var importedWaterlines = await ImportWaterlinesAsync(vesselId, waterlines, cancellationToken);
            var importedOffsets = await ImportOffsetsAsync(vesselId, offsets, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully imported combined geometry for vessel {VesselId}: {StationsCount} stations, {WaterlinesCount} waterlines, {OffsetsCount} offsets",
                vesselId, importedStations.Count, importedWaterlines.Count, importedOffsets.Count);

            return (importedStations.Count, importedWaterlines.Count, importedOffsets.Count);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

