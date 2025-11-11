using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for analyzing hull fairing quality
/// Uses curvature analysis to identify potential fairing issues
/// </summary>
public class FairingQualityService : IFairingQualityService
{
    private readonly DataDbContext _context;
    private readonly ILogger<FairingQualityService> _logger;

    // Thresholds for quality classification
    private const decimal CURVATURE_THRESHOLD_GOOD = 0.1m;
    private const decimal CURVATURE_THRESHOLD_CAUTION = 0.5m;
    private const decimal CURVATURE_CHANGE_LOW = 0.05m;
    private const decimal CURVATURE_CHANGE_MEDIUM = 0.15m;

    public FairingQualityService(
        DataDbContext context,
        ILogger<FairingQualityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes fairing quality for all stations
    /// Computes curvature using second derivative (finite differences)
    /// </summary>
    public async Task<FairingQualityDto> AnalyzeFairingQualityAsync(
        Guid vesselId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Analyzing fairing quality for vessel {VesselId}", vesselId);

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
            return new FairingQualityDto
            {
                StationQualities = new List<StationQualityDto>(),
                OverallScore = 0m
            };
        }

        var stationQualities = new List<StationQualityDto>();

        // Analyze each station curve
        foreach (var station in stations)
        {
            var quality = AnalyzeStationCurve(station, waterlines, offsets);
            stationQualities.Add(quality);
        }

        // Compute overall score (weighted average)
        decimal overallScore = stationQualities.Count > 0
            ? stationQualities.Average(sq => sq.Score)
            : 0m;

        _logger.LogInformation(
            "Fairing analysis complete for vessel {VesselId}: Overall score = {Score:F1}/100",
            vesselId,
            overallScore);

        return new FairingQualityDto
        {
            StationQualities = stationQualities,
            OverallScore = Math.Round(overallScore, 1)
        };
    }

    /// <summary>
    /// Analyzes a single station curve for fairing quality
    /// </summary>
    private StationQualityDto AnalyzeStationCurve(
        Shared.Models.Station station,
        List<Shared.Models.Waterline> waterlines,
        List<Shared.Models.Offset> offsets)
    {
        // Get offsets for this station
        var stationOffsets = offsets
            .Where(o => o.StationIndex == station.StationIndex)
            .OrderBy(o => o.WaterlineIndex)
            .ToList();

        if (stationOffsets.Count < 3)
        {
            // Need at least 3 points for curvature analysis
            return new StationQualityDto
            {
                StationIndex = station.StationIndex,
                Score = 100m, // Too few points to analyze
                QualityLevel = "Good",
                FlaggedRegions = new List<FlaggedRegionDto>()
            };
        }

        // Build (Z, Y) pairs for this station
        var points = new List<(decimal Z, decimal Y)>();
        foreach (var offset in stationOffsets)
        {
            var waterline = waterlines.FirstOrDefault(w => w.WaterlineIndex == offset.WaterlineIndex);
            if (waterline != null)
            {
                points.Add((waterline.Z, offset.HalfBreadthY));
            }
        }

        // Compute curvature at each point using finite differences
        var curvatures = new List<decimal>();
        for (int i = 1; i < points.Count - 1; i++)
        {
            var (z0, y0) = points[i - 1];
            var (z1, y1) = points[i];
            var (z2, y2) = points[i + 1];

            // First derivative (forward and backward)
            decimal dz1 = z1 - z0;
            decimal dy1 = y1 - y0;
            decimal dz2 = z2 - z1;
            decimal dy2 = y2 - y1;

            if (dz1 == 0 || dz2 == 0)
                continue;

            decimal slope1 = dy1 / dz1;
            decimal slope2 = dy2 / dz2;

            // Second derivative (curvature approximation)
            decimal curvature = Math.Abs(slope2 - slope1) / ((dz1 + dz2) / 2m);
            curvatures.Add(curvature);
        }

        if (curvatures.Count == 0)
        {
            return new StationQualityDto
            {
                StationIndex = station.StationIndex,
                Score = 100m,
                QualityLevel = "Good",
                FlaggedRegions = new List<FlaggedRegionDto>()
            };
        }

        // Analyze curvature changes to find flagged regions
        var flaggedRegions = new List<FlaggedRegionDto>();
        for (int i = 0; i < curvatures.Count - 1; i++)
        {
            decimal curvatureChange = Math.Abs(curvatures[i + 1] - curvatures[i]);

            if (curvatureChange > CURVATURE_CHANGE_LOW)
            {
                string severity = curvatureChange > CURVATURE_CHANGE_MEDIUM ? "High" :
                                 curvatureChange > CURVATURE_CHANGE_LOW * 2 ? "Medium" : "Low";

                // Get Z range for this flagged region
                decimal startZ = points[i + 1].Z;
                decimal endZ = points[i + 2].Z;

                flaggedRegions.Add(new FlaggedRegionDto
                {
                    StartZ = startZ,
                    EndZ = endZ,
                    MaxCurvatureChange = curvatureChange,
                    Severity = severity
                });
            }
        }

        // Compute quality score based on curvature statistics
        decimal maxCurvature = curvatures.Max();
        decimal avgCurvature = curvatures.Average();

        // Score calculation (100 = perfect smoothness)
        // Penalize high curvature and curvature changes
        decimal score = 100m;

        if (maxCurvature > CURVATURE_THRESHOLD_CAUTION)
        {
            score -= 40m;
        }
        else if (maxCurvature > CURVATURE_THRESHOLD_GOOD)
        {
            score -= 20m;
        }

        if (avgCurvature > CURVATURE_THRESHOLD_GOOD)
        {
            score -= 10m;
        }

        // Penalize for flagged regions
        score -= flaggedRegions.Count * 5m;

        score = Math.Max(0m, Math.Min(100m, score));

        // Determine quality level
        string qualityLevel = score >= 80m ? "Good" :
                            score >= 60m ? "Caution" : "Issue";

        return new StationQualityDto
        {
            StationIndex = station.StationIndex,
            Score = Math.Round(score, 1),
            QualityLevel = qualityLevel,
            FlaggedRegions = flaggedRegions
        };
    }
}
