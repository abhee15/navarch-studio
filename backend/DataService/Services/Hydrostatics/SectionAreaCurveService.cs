using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for computing section area curve (SAC)
/// Extracts sectional area calculation logic from HydroCalculator
/// </summary>
public class SectionAreaCurveService : ISectionAreaCurveService
{
    private readonly DataDbContext _context;
    private readonly IIntegrationEngine _integrationEngine;
    private readonly ILogger<SectionAreaCurveService> _logger;

    public SectionAreaCurveService(
        DataDbContext context,
        IIntegrationEngine integrationEngine,
        ILogger<SectionAreaCurveService> logger)
    {
        _context = context;
        _integrationEngine = integrationEngine;
        _logger = logger;
    }

    /// <summary>
    /// Computes the section area curve for a vessel
    /// Shows sectional area at each station (volume distribution along length)
    /// </summary>
    public async Task<SectionAreaCurveDto> GetSectionAreaCurveAsync(
        Guid vesselId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Computing section area curve for vessel {VesselId}", vesselId);

        // Load geometry data
        var stations = await _context.Stations
            .Where(s => s.VesselId == vesselId)
            .OrderBy(s => s.StationIndex)
            .ToListAsync(cancellationToken);

        var waterlines = await _context.Waterlines
            .Where(w => w.VesselId == vesselId)
            .OrderBy(w => w.WaterlineIndex)
            .ToListAsync(cancellationToken);

        var offsets = await _context.Offsets
            .Where(o => o.VesselId == vesselId)
            .ToListAsync(cancellationToken);

        if (stations.Count == 0 || waterlines.Count == 0 || offsets.Count == 0)
        {
            _logger.LogWarning("No geometry data found for vessel {VesselId}", vesselId);
            return new SectionAreaCurveDto
            {
                StationPositions = new List<decimal>(),
                SectionalAreas = new List<decimal>(),
                Units = "SI"
            };
        }

        var stationPositions = new List<decimal>();
        var sectionalAreas = new List<decimal>();

        // For each station, compute the sectional area by integrating half-breadths
        foreach (var station in stations)
        {
            // Get offsets at this station for all waterlines
            var stationOffsets = offsets
                .Where(o => o.StationIndex == station.StationIndex)
                .OrderBy(o => o.WaterlineIndex)
                .ToList();

            if (stationOffsets.Count == 0)
                continue;

            // Get waterline Z values and half-breadths for integration
            var waterlineZs = new List<decimal>();
            var halfBreadths = new List<decimal>();

            foreach (var offset in stationOffsets)
            {
                var waterline = waterlines.FirstOrDefault(w => w.WaterlineIndex == offset.WaterlineIndex);
                if (waterline != null)
                {
                    waterlineZs.Add(waterline.Z);
                    halfBreadths.Add(offset.HalfBreadthY);
                }
            }

            if (waterlineZs.Count >= 2)
            {
                // Compute half-section area by integrating half-breadths over Z
                var halfSectionArea = _integrationEngine.Integrate(waterlineZs, halfBreadths);

                // Full section area (mirror to port side)
                var fullSectionArea = 2m * halfSectionArea;

                stationPositions.Add(station.X);
                sectionalAreas.Add(fullSectionArea);

                _logger.LogDebug(
                    "Station {Index} at X={X}: Section area = {Area} m²",
                    station.StationIndex,
                    station.X,
                    fullSectionArea);
            }
        }

        _logger.LogInformation(
            "Computed section area curve with {Count} points for vessel {VesselId}",
            sectionalAreas.Count,
            vesselId);

        return new SectionAreaCurveDto
        {
            StationPositions = stationPositions,
            SectionalAreas = sectionalAreas,
            Units = "SI" // Always return SI units internally
        };
    }
}
