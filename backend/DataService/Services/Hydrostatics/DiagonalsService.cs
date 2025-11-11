using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for computing diagonal curves (45° lines from baseline) for hull fairing validation
/// Diagonals are traditional naval architecture lines at 45° from the baseline that help validate smooth fairing
/// </summary>
public class DiagonalsService : IDigonalsService
{
    private readonly DataDbContext _context;
    private readonly ILogger<DiagonalsService> _logger;

    public DiagonalsService(
        DataDbContext context,
        ILogger<DiagonalsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Computes diagonal curves for a vessel
    /// Diagonals are lines at 45° from baseline, intersecting the hull surface
    /// </summary>
    public async Task<DiagonalsDto> GetDiagonalsAsync(
        Guid vesselId,
        int numDiagonals,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Computing {NumDiagonals} diagonals for vessel {VesselId}", numDiagonals, vesselId);

        // Validate input
        if (numDiagonals < 1 || numDiagonals > 10)
        {
            throw new ArgumentException("Number of diagonals must be between 1 and 10", nameof(numDiagonals));
        }

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
            return new DiagonalsDto { Diagonals = new List<DiagonalDto>() };
        }

        // Find maximum half-breadth and height for diagonal spacing
        var maxHalfBreadth = offsets.Max(o => o.HalfBreadthY);
        var maxZ = waterlines.Max(w => w.Z);

        var diagonals = new List<DiagonalDto>();

        // For each diagonal, compute the intersection with the hull surface
        // Diagonals are typically at 45° from baseline
        const decimal diagonalAngle = 45m;

        for (int diagIdx = 0; diagIdx < numDiagonals; diagIdx++)
        {
            // Space diagonals evenly based on their intercept on the centerline
            // Diagonal equation: Z = Y + intercept (for 45° line)
            // Intercept ranges from 0 to maxZ
            decimal intercept = (diagIdx + 1m) * maxZ / (numDiagonals + 1m);

            var points = new List<PointDto>();

            // For each station, find where this diagonal intersects the hull
            foreach (var station in stations)
            {
                // Get offsets at this station for all waterlines
                var stationOffsets = offsets
                    .Where(o => o.StationIndex == station.StationIndex)
                    .OrderBy(o => o.WaterlineIndex)
                    .ToList();

                if (stationOffsets.Count == 0)
                    continue;

                // For this diagonal (Z = Y + intercept), find intersection with hull surface
                // We need to find Y and Z such that:
                // 1. Z = Y + intercept (diagonal equation)
                // 2. Y = hull_surface(X, Z) (hull surface)

                // Iterate through waterlines to find intersection
                for (int wlIdx = 0; wlIdx < waterlines.Count - 1; wlIdx++)
                {
                    var wl1 = waterlines[wlIdx];
                    var wl2 = waterlines[wlIdx + 1];

                    var offset1 = stationOffsets.FirstOrDefault(o => o.WaterlineIndex == wl1.WaterlineIndex);
                    var offset2 = stationOffsets.FirstOrDefault(o => o.WaterlineIndex == wl2.WaterlineIndex);

                    if (offset1 == null || offset2 == null)
                        continue;

                    // Check if diagonal passes between these two waterlines
                    decimal diagonalY1 = wl1.Z - intercept; // Y at this Z for diagonal
                    decimal diagonalY2 = wl2.Z - intercept; // Y at next Z for diagonal

                    // Check if diagonal intersects hull between these waterlines
                    // Hull Y values at these Z levels
                    decimal hullY1 = offset1.HalfBreadthY;
                    decimal hullY2 = offset2.HalfBreadthY;

                    // Check for intersection (diagonal crosses hull surface)
                    bool intersects = (diagonalY1 <= hullY1 && diagonalY2 >= hullY2) ||
                                     (diagonalY1 >= hullY1 && diagonalY2 <= hullY2);

                    if (intersects && diagonalY1 >= 0 && diagonalY2 >= 0)
                    {
                        // Linear interpolation to find exact intersection point
                        // Solve for where diagonal Y equals hull Y
                        decimal t = 0.5m; // Default to midpoint if parallel

                        decimal deltaHull = hullY2 - hullY1;
                        decimal deltaDiag = diagonalY2 - diagonalY1;

                        if (Math.Abs(deltaHull - deltaDiag) > 0.001m)
                        {
                            t = (hullY1 - diagonalY1) / (deltaDiag - deltaHull);
                            t = Math.Max(0m, Math.Min(1m, t)); // Clamp to [0, 1]
                        }

                        decimal intersectionZ = wl1.Z + t * (wl2.Z - wl1.Z);
                        decimal intersectionY = intersectionZ - intercept;

                        // Verify intersection is within hull bounds
                        if (intersectionY >= 0 && intersectionY <= maxHalfBreadth && intersectionZ >= 0 && intersectionZ <= maxZ)
                        {
                            points.Add(new PointDto
                            {
                                X = station.X,
                                Y = intersectionY,
                                Z = intersectionZ
                            });
                        }
                        break; // Found intersection at this station
                    }
                }
            }

            if (points.Count > 0)
            {
                diagonals.Add(new DiagonalDto
                {
                    DiagonalIndex = diagIdx,
                    Angle = diagonalAngle,
                    Points = points
                });

                _logger.LogDebug("Diagonal {Index} computed with {PointCount} points", diagIdx, points.Count);
            }
        }

        _logger.LogInformation("Computed {DiagonalCount} diagonals for vessel {VesselId}", diagonals.Count, vesselId);

        return new DiagonalsDto { Diagonals = diagonals };
    }
}
