using System.Text.Json;
using DataService.Data;
using Microsoft.Extensions.Logging;
using Shared.Models;

namespace DataService.Services.Catalog;

/// <summary>
/// Generates synthetic demo parametric hulls when ShipD dataset is not available
/// Creates realistic-looking hulls for testing and demos
/// </summary>
public class ParametricDemoDataGenerator
{
    private readonly DataDbContext _context;
    private readonly ILogger<ParametricDemoDataGenerator> _logger;

    public ParametricDemoDataGenerator(
        DataDbContext context,
        ILogger<ParametricDemoDataGenerator> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Generate demo parametric hulls for testing/demos
    /// Creates 100 synthetic hulls with realistic parameters
    /// </summary>
    public async Task<int> GenerateDemoDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[DEMO] Generating synthetic parametric hull data for testing...");

            var hulls = new List<ParametricHull>();
            var random = new Random(42); // Fixed seed for reproducibility

            // Generate 100 demo hulls with varied parameters
            for (int i = 0; i < 100; i++)
            {
                var hull = GenerateDemoHull(i + 1, random);
                hulls.Add(hull);
            }

            await _context.ParametricHulls.AddRangeAsync(hulls, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("[DEMO] ✅ Generated {Count} demo parametric hulls", hulls.Count);
            return hulls.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DEMO] Failed to generate demo data");
            throw;
        }
    }

    private ParametricHull GenerateDemoHull(int index, Random random)
    {
        // Generate realistic hull parameters
        var loaM = 10.0m; // ShipD standard
        var lbRatio = (decimal)(0.10 + random.NextDouble() * 0.15); // 0.10-0.25
        var lsRatio = (decimal)(0.10 + random.NextDouble() * 0.15); // 0.10-0.25
        var bdRatio = (decimal)(0.06 + random.NextDouble() * 0.08); // 0.06-0.14
        var ddRatio = (decimal)(0.04 + random.NextDouble() * 0.06); // 0.04-0.10
        var bsRatio = (decimal)(0.80 + random.NextDouble() * 0.40); // 0.80-1.20

        // Derived dimensions
        var beamM = bdRatio * 2 * loaM;
        var depthM = ddRatio * loaM;
        var draftM = depthM * 0.5m; // T/D = 0.5 (design draft)

        // Form coefficients
        var cb = (decimal)(0.45 + random.NextDouble() * 0.35); // 0.45-0.80
        var cp = cb + (decimal)(random.NextDouble() * 0.10); // Cp > Cb
        var cm = cp + (decimal)(random.NextDouble() * 0.10); // Cm > Cp

        // Geometric measures
        var volumeNorm = cb * bdRatio * ddRatio * 0.5m; // Approximate
        var lcbNorm = (decimal)(0.45 + random.NextDouble() * 0.10); // -5% to +5% from midship
        var cwCoeff = (decimal)(0.70 + random.NextDouble() * 0.20); // 0.70-0.90
        var areaWpNorm = (decimal)(cwCoeff * bdRatio * 2);

        // Create parametric vector (simplified 45-parameter vector)
        var parametricVector = new
        {
            loaM = 10.0,
            lbRatio,
            lsRatio,
            bdRatio,
            ddRatio,
            bsRatio,
            // Additional 39 parameters would go here in real dataset
            note = "Demo data - simplified parameters"
        };

        // Create geometric measures (simplified)
        var geometricMeasures = new
        {
            volume = new[] { volumeNorm },
            lcb = new[] { lcbNorm },
            vcb = new[] { ddRatio * 0.33m },
            areaWp = new[] { areaWpNorm },
            cw = new[] { cwCoeff },
            note = "Demo data - single draft point"
        };

        return new ParametricHull
        {
            HullId = $"DEMO_{index:D5}",
            DatasetSource = "Demo_Synthetic",
            RowIndex = index,
            ParametricVector = JsonSerializer.Serialize(parametricVector),
            GeometricMeasures = JsonSerializer.Serialize(geometricMeasures),

            // Key parameters
            LoaM = loaM,
            LbRatio = lbRatio,
            LsRatio = lsRatio,
            BdRatio = bdRatio,
            DdRatio = ddRatio,
            BsRatio = bsRatio,

            // Geometric measures @ design draft
            VolumeNorm = volumeNorm,
            LcbNorm = lcbNorm,
            VcbNorm = ddRatio * 0.33m,
            AreaWpNorm = areaWpNorm,
            CwCoeff = cwCoeff,

            // Derived dimensions
            LppMDerived = loaM * 0.97m, // LPP ≈ 97% of LOA
            BeamMDerived = beamM,
            DraftMDerived = draftM,
            DepthMDerived = depthM,

            // Form coefficients
            CbDerived = cb,
            CpDerived = cp,
            CmDerived = cm,

            // Quality
            ConversionQuality = "Demo",
            HasValidCoefficients = true,
            DistortionScore = null,

            // Metadata
            ImportedAt = DateTime.UtcNow,
            DataVersion = 1,
            IsActive = true
        };
    }
}

