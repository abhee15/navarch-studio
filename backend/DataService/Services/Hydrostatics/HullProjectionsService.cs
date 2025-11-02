using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for generating hull projection views (waterlines, buttocks)
/// </summary>
public class HullProjectionsService : IHullProjectionsService
{
    private readonly DataDbContext _context;
    private readonly ILogger<HullProjectionsService> _logger;

    public HullProjectionsService(
        DataDbContext context,
        ILogger<HullProjectionsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Generate waterline projections (XY views at specific Z-levels)
    /// Each waterline shows the hull outline at a constant height
    /// </summary>
    public async Task<WaterlinesDto> GetWaterlinesAsync(
        Guid vesselId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating waterlines for vessel {VesselId}", vesselId);

        // Get stations ordered by X position
        var stations = await _context.Stations
            .Where(s => s.VesselId == vesselId)
            .OrderBy(s => s.StationIndex)
            .ToListAsync(cancellationToken);

        if (stations.Count == 0)
        {
            _logger.LogWarning("No stations found for vessel {VesselId}", vesselId);
            return new WaterlinesDto { Waterlines = new List<WaterlineCurve>() };
        }

        // Get waterlines ordered by Z position
        var waterlines = await _context.Waterlines
            .Where(w => w.VesselId == vesselId)
            .OrderBy(w => w.WaterlineIndex)
            .ToListAsync(cancellationToken);

        if (waterlines.Count == 0)
        {
            _logger.LogWarning("No waterlines found for vessel {VesselId}", vesselId);
            return new WaterlinesDto { Waterlines = new List<WaterlineCurve>() };
        }

        // Get all offsets
        var offsets = await _context.Offsets
            .Where(o => o.VesselId == vesselId)
            .ToListAsync(cancellationToken);

        var waterlineCurves = new List<WaterlineCurve>();

        // Generate a curve for each waterline
        foreach (var waterline in waterlines)
        {
            var points = new List<Point2D>();

            // For each station, get the half-breadth at this waterline
            foreach (var station in stations)
            {
                var offset = offsets.FirstOrDefault(o =>
                    o.StationIndex == station.StationIndex &&
                    o.WaterlineIndex == waterline.WaterlineIndex);

                if (offset != null)
                {
                    // Add point (X = station position, Y = half-breadth)
                    points.Add(new Point2D
                    {
                        X = station.X,
                        Y = offset.HalfBreadthY
                    });
                }
            }

            waterlineCurves.Add(new WaterlineCurve
            {
                WaterlineIndex = waterline.WaterlineIndex,
                Z = waterline.Z,
                Points = points
            });

            _logger.LogDebug(
                "Generated waterline {Index} at Z={Z} with {Count} points",
                waterline.WaterlineIndex, waterline.Z, points.Count);
        }

        _logger.LogInformation(
            "Generated {Count} waterlines for vessel {VesselId}",
            waterlineCurves.Count, vesselId);

        return new WaterlinesDto { Waterlines = waterlineCurves };
    }

    /// <summary>
    /// Generate buttock projections (XZ views at specific Y-offsets)
    /// Each buttock shows the hull profile at a constant offset from centerline
    /// </summary>
    public async Task<ButtocksDto> GetButtocksAsync(
        Guid vesselId,
        int? numButtocks = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating buttocks for vessel {VesselId}", vesselId);

        // Get stations ordered by X position
        var stations = await _context.Stations
            .Where(s => s.VesselId == vesselId)
            .OrderBy(s => s.StationIndex)
            .ToListAsync(cancellationToken);

        if (stations.Count == 0)
        {
            _logger.LogWarning("No stations found for vessel {VesselId}", vesselId);
            return new ButtocksDto { Buttocks = new List<ButtockCurve>() };
        }

        // Get waterlines ordered by Z position
        var waterlines = await _context.Waterlines
            .Where(w => w.VesselId == vesselId)
            .OrderBy(w => w.WaterlineIndex)
            .ToListAsync(cancellationToken);

        if (waterlines.Count == 0)
        {
            _logger.LogWarning("No waterlines found for vessel {VesselId}", vesselId);
            return new ButtocksDto { Buttocks = new List<ButtockCurve>() };
        }

        // Get all offsets
        var offsets = await _context.Offsets
            .Where(o => o.VesselId == vesselId)
            .ToListAsync(cancellationToken);

        // Determine buttock Y-offsets to generate
        // Find max half-breadth across all offsets
        var maxHalfBreadth = offsets.Any() ? offsets.Max(o => o.HalfBreadthY) : 0m;

        if (maxHalfBreadth == 0)
        {
            _logger.LogWarning("Max half-breadth is zero for vessel {VesselId}", vesselId);
            return new ButtocksDto { Buttocks = new List<ButtockCurve>() };
        }

        // Default to 5 buttocks if not specified
        int buttockCount = numButtocks ?? 5;
        var buttockYOffsets = new List<decimal>();

        // Generate evenly spaced Y-offsets from centerline to max half-breadth
        for (int i = 0; i < buttockCount; i++)
        {
            decimal yOffset = maxHalfBreadth * (i + 1) / (buttockCount + 1);
            buttockYOffsets.Add(yOffset);
        }

        var buttockCurves = new List<ButtockCurve>();
        int buttockIndex = 0;

        // Generate a curve for each Y-offset
        foreach (var yOffset in buttockYOffsets)
        {
            var points = new List<PointXZ>();

            // For each station
            foreach (var station in stations)
            {
                // Find the waterlines where hull crosses this Y-offset at this station
                // by interpolating between adjacent offsets

                var stationOffsets = offsets
                    .Where(o => o.StationIndex == station.StationIndex)
                    .OrderBy(o => o.WaterlineIndex)
                    .ToList();

                // Build list of (Z, Y) pairs for this station
                var stationPoints = new List<(decimal Z, decimal Y)>();
                foreach (var offset in stationOffsets)
                {
                    var wl = waterlines.FirstOrDefault(w => w.WaterlineIndex == offset.WaterlineIndex);
                    if (wl != null)
                    {
                        stationPoints.Add((wl.Z, offset.HalfBreadthY));
                    }
                }

                // Interpolate to find Z at the target yOffset
                decimal? interpolatedZ = InterpolateZ(stationPoints, yOffset);

                if (interpolatedZ.HasValue)
                {
                    points.Add(new PointXZ
                    {
                        X = station.X,
                        Z = interpolatedZ.Value
                    });
                }
            }

            buttockCurves.Add(new ButtockCurve
            {
                ButtockIndex = buttockIndex,
                Y = yOffset,
                Points = points
            });

            _logger.LogDebug(
                "Generated buttock {Index} at Y={Y} with {Count} points",
                buttockIndex, yOffset, points.Count);

            buttockIndex++;
        }

        _logger.LogInformation(
            "Generated {Count} buttocks for vessel {VesselId}",
            buttockCurves.Count, vesselId);

        return new ButtocksDto { Buttocks = buttockCurves };
    }

    /// <summary>
    /// Linear interpolation to find Z given Y-offset
    /// </summary>
    /// <param name="points">List of (Z, Y) pairs, ordered by Z</param>
    /// <param name="targetY">Target Y-offset</param>
    /// <returns>Interpolated Z value, or null if Y is outside range</returns>
    private decimal? InterpolateZ(List<(decimal Z, decimal Y)> points, decimal targetY)
    {
        if (points.Count == 0)
            return null;

        // Find the two points that bracket targetY
        for (int i = 0; i < points.Count - 1; i++)
        {
            var p1 = points[i];
            var p2 = points[i + 1];

            // Check if targetY is between p1.Y and p2.Y
            if ((p1.Y <= targetY && targetY <= p2.Y) || (p2.Y <= targetY && targetY <= p1.Y))
            {
                // Linear interpolation
                if (Math.Abs(p2.Y - p1.Y) < 0.0001m)
                {
                    // Points have essentially the same Y, return average Z
                    return (p1.Z + p2.Z) / 2m;
                }

                decimal t = (targetY - p1.Y) / (p2.Y - p1.Y);
                return p1.Z + t * (p2.Z - p1.Z);
            }
        }

        // If targetY is outside the range, check if it's at a boundary
        if (points.Any(p => Math.Abs(p.Y - targetY) < 0.0001m))
        {
            return points.First(p => Math.Abs(p.Y - targetY) < 0.0001m).Z;
        }

        return null;
    }
}
