using DataService.Data;
using Microsoft.EntityFrameworkCore;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Implementation of curves generator service
/// </summary>
public class CurvesGenerator : ICurvesGenerator
{
    private readonly IHydroCalculator _hydroCalculator;
    private readonly IIntegrationEngine _integrationEngine;
    private readonly DataDbContext _context;
    private readonly ILogger<CurvesGenerator> _logger;

    public CurvesGenerator(
        IHydroCalculator hydroCalculator,
        IIntegrationEngine integrationEngine,
        DataDbContext context,
        ILogger<CurvesGenerator> logger)
    {
        _hydroCalculator = hydroCalculator;
        _integrationEngine = integrationEngine;
        _context = context;
        _logger = logger;
    }

    public async Task<CurveDataDto> GenerateDisplacementCurveAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default)
    {
        var drafts = GenerateDraftRange(minDraft, maxDraft, points);
        var results = await _hydroCalculator.ComputeTableAsync(vesselId, loadcaseId, drafts, cancellationToken);

        var curvePoints = results.Select(r => new CurvePointDto
        {
            X = r.Draft,
            Y = r.DispWeight
        }).ToList();

        return new CurveDataDto
        {
            Type = "displacement",
            XLabel = "Draft (m)",
            YLabel = "Displacement (kg)",
            Points = curvePoints
        };
    }

    public async Task<CurveDataDto> GenerateKBCurveAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default)
    {
        var drafts = GenerateDraftRange(minDraft, maxDraft, points);
        var results = await _hydroCalculator.ComputeTableAsync(vesselId, loadcaseId, drafts, cancellationToken);

        var curvePoints = results.Select(r => new CurvePointDto
        {
            X = r.Draft,
            Y = r.KBz
        }).ToList();

        return new CurveDataDto
        {
            Type = "kb",
            XLabel = "Draft (m)",
            YLabel = "KB (m)",
            Points = curvePoints
        };
    }

    public async Task<CurveDataDto> GenerateLCBCurveAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default)
    {
        var drafts = GenerateDraftRange(minDraft, maxDraft, points);
        var results = await _hydroCalculator.ComputeTableAsync(vesselId, loadcaseId, drafts, cancellationToken);

        var curvePoints = results.Select(r => new CurvePointDto
        {
            X = r.Draft,
            Y = r.LCBx
        }).ToList();

        return new CurveDataDto
        {
            Type = "lcb",
            XLabel = "Draft (m)",
            YLabel = "LCB (m)",
            Points = curvePoints
        };
    }

    public async Task<CurveDataDto> GenerateGMtCurveAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default)
    {
        if (!loadcaseId.HasValue)
        {
            throw new ArgumentException("Loadcase ID is required to compute GM");
        }

        var drafts = GenerateDraftRange(minDraft, maxDraft, points);
        var results = await _hydroCalculator.ComputeTableAsync(vesselId, loadcaseId, drafts, cancellationToken);

        var curvePoints = results
            .Where(r => r.GMt.HasValue)
            .Select(r => new CurvePointDto
            {
                X = r.Draft,
                Y = r.GMt!.Value
            }).ToList();

        return new CurveDataDto
        {
            Type = "gmt",
            XLabel = "Draft (m)",
            YLabel = "GMt (m)",
            Points = curvePoints
        };
    }

    public async Task<CurveDataDto> GenerateAwpCurveAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default)
    {
        var drafts = GenerateDraftRange(minDraft, maxDraft, points);
        var results = await _hydroCalculator.ComputeTableAsync(vesselId, loadcaseId, drafts, cancellationToken);

        var curvePoints = results.Select(r => new CurvePointDto
        {
            X = r.Draft,
            Y = r.Awp
        }).ToList();

        return new CurveDataDto
        {
            Type = "awp",
            XLabel = "Draft (m)",
            YLabel = "Waterplane Area (m²)",
            Points = curvePoints
        };
    }

    public async Task<List<BonjeanCurveDto>> GenerateBonjeanCurvesAsync(
        Guid vesselId,
        CancellationToken cancellationToken = default)
    {
        // Get vessel geometry
        var vessel = await _context.Vessels
            .Include(v => v.Stations)
            .Include(v => v.Waterlines)
            .Include(v => v.Offsets)
            .FirstOrDefaultAsync(v => v.Id == vesselId, cancellationToken);

        if (vessel == null)
        {
            throw new ArgumentException($"Vessel with ID {vesselId} not found");
        }

        var stations = vessel.Stations.OrderBy(s => s.StationIndex).ToList();
        var waterlines = vessel.Waterlines.OrderBy(w => w.WaterlineIndex).ToList();
        var offsets = vessel.Offsets.ToList();

        if (stations.Count == 0 || waterlines.Count == 0 || offsets.Count == 0)
        {
            throw new InvalidOperationException("Vessel geometry is incomplete");
        }

        var bonjeanCurves = new List<BonjeanCurveDto>();

        // For each station, compute sectional area at each waterline
        foreach (var station in stations)
        {
            var points = new List<CurvePointDto>();

            foreach (var waterline in waterlines)
            {
                // Get all offsets for this station up to current waterline
                var activeWaterlines = waterlines.Where(w => w.Z <= waterline.Z).ToList();

                if (activeWaterlines.Count < 2)
                {
                    points.Add(new CurvePointDto { X = waterline.Z, Y = 0 });
                    continue;
                }

                // Compute sectional area by integrating half-breadths
                var halfBreadths = new List<decimal>();
                var zCoords = new List<decimal>();

                foreach (var wl in activeWaterlines)
                {
                    var offset = offsets.FirstOrDefault(o =>
                        o.StationIndex == station.StationIndex &&
                        o.WaterlineIndex == wl.WaterlineIndex);

                    zCoords.Add(wl.Z);
                    halfBreadths.Add(offset?.HalfBreadthY ?? 0m);
                }

                // Integrate to get half-section area, then mirror
                var halfSectionArea = _integrationEngine.Integrate(zCoords, halfBreadths);
                var fullSectionArea = 2 * halfSectionArea;

                points.Add(new CurvePointDto
                {
                    X = waterline.Z, // Draft
                    Y = fullSectionArea // Sectional area
                });
            }

            bonjeanCurves.Add(new BonjeanCurveDto
            {
                StationIndex = station.StationIndex,
                StationX = station.X,
                Points = points
            });
        }

        _logger.LogInformation("Generated Bonjean curves for vessel {VesselId}: {Count} stations",
            vesselId, bonjeanCurves.Count);

        return bonjeanCurves;
    }

    public async Task<Dictionary<string, CurveDataDto>> GenerateMultipleCurvesAsync(
        Guid vesselId,
        Guid? loadcaseId,
        List<string> curveTypes,
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default)
    {
        var curves = new Dictionary<string, CurveDataDto>();

        foreach (var type in curveTypes)
        {
            CurveDataDto? curve = type.ToLower() switch
            {
                "displacement" => await GenerateDisplacementCurveAsync(vesselId, loadcaseId, minDraft, maxDraft, points, cancellationToken),
                "kb" => await GenerateKBCurveAsync(vesselId, loadcaseId, minDraft, maxDraft, points, cancellationToken),
                "lcb" => await GenerateLCBCurveAsync(vesselId, loadcaseId, minDraft, maxDraft, points, cancellationToken),
                "gmt" => await GenerateGMtCurveAsync(vesselId, loadcaseId, minDraft, maxDraft, points, cancellationToken),
                "awp" => await GenerateAwpCurveAsync(vesselId, loadcaseId, minDraft, maxDraft, points, cancellationToken),
                _ => null
            };

            if (curve != null)
            {
                curves[type] = curve;
            }
        }

        _logger.LogInformation("Generated {Count} curves for vessel {VesselId}",
            curves.Count, vesselId);

        return curves;
    }

    private static List<decimal> GenerateDraftRange(decimal minDraft, decimal maxDraft, int points)
    {
        if (points < 2)
        {
            throw new ArgumentException("At least 2 points required");
        }

        if (maxDraft <= minDraft)
        {
            throw new ArgumentException("Max draft must be greater than min draft");
        }

        var step = (maxDraft - minDraft) / (points - 1);
        var drafts = new List<decimal>();

        for (int i = 0; i < points; i++)
        {
            drafts.Add(minDraft + i * step);
        }

        return drafts;
    }
}

