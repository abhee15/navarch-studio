using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Catalog;
using Shared.Models;
using System.Text.Json;

namespace DataService.Services.Catalog;

/// <summary>
/// K-Nearest Neighbors search for parametric hull catalog
/// Uses weighted Euclidean distance on normalized geometric features
/// Includes Redis distributed caching for performance
/// </summary>
public class ParametricKnnService
{
    private readonly DataDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ParametricKnnService> _logger;

    // Feature weights for distance calculation
    private readonly Dictionary<string, double> _featureWeights = new()
    {
        ["VolumeNorm"] = 0.25,      // Most important - overall size
        ["LcbNorm"] = 0.15,          // Critical for resistance
        ["BdRatio"] = 0.15,          // Beam proportion
        ["DdRatio"] = 0.10,          // Depth proportion
        ["CwCoeff"] = 0.10,          // Waterplane shape
        ["LbRatio"] = 0.10,          // Bow fineness
        ["LsRatio"] = 0.10,          // Stern shape
        ["AreaWpNorm"] = 0.05        // Redundant with Cw but useful
    };

    public ParametricKnnService(
        DataDbContext context,
        IDistributedCache cache,
        ILogger<ParametricKnnService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Find K most similar parametric hulls based on geometric features
    /// Uses Redis cache for sub-millisecond repeat queries
    /// </summary>
    public async Task<List<SimilarParametricHull>> FindSimilarHullsAsync(
        ParametricSearchCriteria criteria,
        int K = 5,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Step 1: Try Redis cache
            var cacheKey = GenerateCacheKey(criteria, K);
            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
            
            if (cached != null)
            {
                _logger.LogDebug("Cache HIT for parametric KNN query: {CacheKey}", cacheKey);
                var cachedResults = JsonSerializer.Deserialize<List<SimilarParametricHull>>(cached);
                
                if (cachedResults != null)
                {
                    var cacheElapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    _logger.LogInformation(
                        "Parametric KNN from cache in {ElapsedMs}ms. Returned {Count} hulls",
                        cacheElapsedMs, cachedResults.Count);
                    return cachedResults;
                }
            }

            _logger.LogDebug("Cache MISS for parametric KNN query: {CacheKey}", cacheKey);

            // Step 2: Load all active parametric hulls (for Phase 2A: 5K hulls, fast enough for in-memory)
            // Phase 2B will use ANN index instead
            var catalog = await _context.ParametricHulls
                .Where(h => h.IsActive && h.HasValidCoefficients)
                .ToListAsync(cancellationToken);

            if (!catalog.Any())
            {
                _logger.LogWarning("Parametric catalog is empty!");
                return new List<SimilarParametricHull>();
            }

            _logger.LogInformation(
                "Loaded {Count} parametric hulls for KNN search",
                catalog.Count);

            // Step 2: Extract target features from search criteria
            var targetFeatures = ExtractTargetFeatures(criteria);

            // Step 3: Calculate feature statistics for normalization
            var stats = CalculateFeatureStatistics(catalog);

            // Step 4: Normalize target features
            var normalizedTarget = NormalizeFeatures(targetFeatures, stats);

            // Step 5: Calculate distances for all hulls
            var distances = catalog.Select(hull =>
            {
                var hullFeatures = ExtractHullFeatures(hull);
                var normalizedHull = NormalizeFeatures(hullFeatures, stats);
                var distance = CalculateWeightedDistance(normalizedTarget, normalizedHull);

                return new
                {
                    Hull = hull,
                    Distance = distance
                };
            })
            .OrderBy(x => x.Distance)
            .Take(K)
            .ToList();

            // Step 6: Convert to DTOs with similarity scores
            var results = distances.Select(d => new SimilarParametricHull
            {
                HullId = d.Hull.Id,
                HullIdString = d.Hull.HullId,
                DatasetSource = d.Hull.DatasetSource,

                // Principal dimensions
                LppM = d.Hull.LppMDerived,
                BeamM = d.Hull.BeamMDerived,
                DraftM = d.Hull.DraftMDerived,
                DepthM = d.Hull.DepthMDerived,

                // Form coefficients
                Cb = d.Hull.CbDerived,
                Cp = d.Hull.CpDerived,
                Cm = d.Hull.CmDerived,
                Cw = d.Hull.CwCoeff,

                // Geometric features
                VolumeNorm = d.Hull.VolumeNorm,
                LcbNorm = d.Hull.LcbNorm,

                // Similarity (convert distance to similarity: 1 = identical, 0 = very different)
                GeometricDistance = d.Distance,
                SimilarityScore = CalculateSimilarityScore(d.Distance, stats),

                // Provenance
                ConversionQuality = d.Hull.ConversionQuality ?? "Good"
            }).ToList();

            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation(
                "KNN search completed in {ElapsedMs}ms. Found {Count} similar hulls. Avg similarity: {AvgSim:P0}",
                elapsedMs, results.Count, results.Any() ? results.Average(r => r.SimilarityScore) : 0);

            // Step 7: Cache results for 1 hour
            var serialized = JsonSerializer.Serialize(results);
            await _cache.SetStringAsync(
                cacheKey,
                serialized,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                },
                cancellationToken);

            _logger.LogDebug("Cached parametric KNN results: {CacheKey}", cacheKey);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during parametric KNN search");
            throw;
        }
    }

    /// <summary>
    /// Extract target features from search criteria
    /// Estimate missing features from available data
    /// </summary>
    private Dictionary<string, double> ExtractTargetFeatures(ParametricSearchCriteria criteria)
    {
        // Normalize volume to LOA=10m basis (same as catalog)
        var LOA_target = (double)criteria.TargetLOA;
        var Volume_target = (double)criteria.TargetVolume;
        var Volume_norm = Volume_target / Math.Pow(LOA_target, 3) * Math.Pow(10.0, 3);

        // LCB as fraction of LOA (default: 0.5 for symmetric, adjust by vessel type)
        var LCB_norm = criteria.TargetLCB.HasValue
            ? (double)criteria.TargetLCB.Value
            : 0.50;  // Default midship

        // Estimate Bd_ratio (half-beam / LOA) from typical L/B ratios
        var Bd_ratio = criteria.TargetBeamRatio.HasValue
            ? (double)criteria.TargetBeamRatio.Value / 2.0  // Convert full beam to half-beam
            : 0.20;  // Default: L/B ≈ 10 → B/L = 0.1 → Bd = 0.05*2 = 0.10... but use 0.20 as typical

        // Estimate Dd_ratio (depth / LOA) from typical proportions
        var Dd_ratio = criteria.TargetDraftRatio.HasValue
            ? (double)criteria.TargetDraftRatio.Value * 2.0  // Depth ≈ 2×Draft typically
            : 0.16;  // Default

        // Estimate Cw from Cb (waterplane coefficient typically 0.80-0.95)
        var Cw = criteria.TargetCb.HasValue
            ? (double)criteria.TargetCb.Value + 0.15  // Cw usually 0.10-0.20 higher than Cb
            : 0.85;  // Default

        // Estimate bow and stern length ratios (typical: 0.15-0.25 each)
        var Lb_ratio = 0.20;  // Bow length / LOA
        var Ls_ratio = 0.20;  // Stern length / LOA

        // Estimate Area_WP_norm from Cw and dimensions
        var Area_WP_norm = Cw * (0.96) * (Bd_ratio * 2.0);  // Cw × Lwl/LOA × B/LOA

        return new Dictionary<string, double>
        {
            ["VolumeNorm"] = Volume_norm,
            ["LcbNorm"] = LCB_norm,
            ["BdRatio"] = Bd_ratio,
            ["DdRatio"] = Dd_ratio,
            ["CwCoeff"] = Cw,
            ["LbRatio"] = Lb_ratio,
            ["LsRatio"] = Ls_ratio,
            ["AreaWpNorm"] = Area_WP_norm
        };
    }

    /// <summary>
    /// Extract geometric features from a parametric hull
    /// </summary>
    private Dictionary<string, double> ExtractHullFeatures(ParametricHull hull)
    {
        return new Dictionary<string, double>
        {
            ["VolumeNorm"] = (double)hull.VolumeNorm,
            ["LcbNorm"] = (double)hull.LcbNorm,
            ["BdRatio"] = (double)hull.BdRatio,
            ["DdRatio"] = (double)hull.DdRatio,
            ["CwCoeff"] = (double)hull.CwCoeff,
            ["LbRatio"] = (double)hull.LbRatio,
            ["LsRatio"] = (double)hull.LsRatio,
            ["AreaWpNorm"] = (double)hull.AreaWpNorm
        };
    }

    /// <summary>
    /// Calculate feature statistics (mean, stddev) for z-score normalization
    /// </summary>
    private Dictionary<string, (double Mean, double StdDev)> CalculateFeatureStatistics(List<ParametricHull> catalog)
    {
        var stats = new Dictionary<string, (double, double)>();

        var features = new[] { "VolumeNorm", "LcbNorm", "BdRatio", "DdRatio", "CwCoeff", "LbRatio", "LsRatio", "AreaWpNorm" };

        foreach (var feature in features)
        {
            var values = catalog.Select(h => (double)GetFeatureValue(h, feature)).ToList();
            var mean = values.Average();
            var variance = values.Select(v => Math.Pow(v - mean, 2)).Average();
            var stddev = Math.Sqrt(variance);

            stats[feature] = (mean, stddev > 0 ? stddev : 1.0);  // Avoid div by zero
        }

        return stats;
    }

    private decimal GetFeatureValue(ParametricHull hull, string feature)
    {
        return feature switch
        {
            "VolumeNorm" => hull.VolumeNorm,
            "LcbNorm" => hull.LcbNorm,
            "BdRatio" => hull.BdRatio,
            "DdRatio" => hull.DdRatio,
            "CwCoeff" => hull.CwCoeff,
            "LbRatio" => hull.LbRatio,
            "LsRatio" => hull.LsRatio,
            "AreaWpNorm" => hull.AreaWpNorm,
            _ => 0
        };
    }

    /// <summary>
    /// Normalize features using z-score: (x - mean) / stddev
    /// </summary>
    private Dictionary<string, double> NormalizeFeatures(
        Dictionary<string, double> features,
        Dictionary<string, (double Mean, double StdDev)> stats)
    {
        var normalized = new Dictionary<string, double>();

        foreach (var kvp in features)
        {
            var (mean, stddev) = stats[kvp.Key];
            normalized[kvp.Key] = (kvp.Value - mean) / stddev;
        }

        return normalized;
    }

    /// <summary>
    /// Calculate weighted Euclidean distance between normalized feature vectors
    /// </summary>
    private double CalculateWeightedDistance(
        Dictionary<string, double> target,
        Dictionary<string, double> hull)
    {
        double sumSquares = 0;

        foreach (var kvp in target)
        {
            var feature = kvp.Key;
            var weight = _featureWeights.GetValueOrDefault(feature, 1.0);
            var diff = target[feature] - hull[feature];
            sumSquares += weight * diff * diff;
        }

        return Math.Sqrt(sumSquares);
    }

    /// <summary>
    /// Convert geometric distance to similarity score [0, 1]
    /// Use exponential decay: similarity = exp(-distance)
    /// </summary>
    private double CalculateSimilarityScore(
        double distance,
        Dictionary<string, (double Mean, double StdDev)> stats)
    {
        // Normalize distance by typical scale (use weighted sum of feature variances)
        var avgStdDev = stats.Values.Average(s => s.StdDev);
        var normalizedDistance = distance / (avgStdDev * Math.Sqrt(_featureWeights.Count));

        // Exponential decay: similarity = exp(-k * distance)
        // k=2 gives: distance=0 → sim=1.0, distance=0.5 → sim=0.37, distance=1.0 → sim=0.14
        var similarity = Math.Exp(-2.0 * normalizedDistance);

        return Math.Clamp(similarity, 0.0, 1.0);
    }

    /// <summary>
    /// Generate deterministic cache key from search criteria
    /// </summary>
    private string GenerateCacheKey(ParametricSearchCriteria criteria, int K)
    {
        // Create fingerprint of search parameters
        var parts = new[]
        {
            $"LOA:{criteria.TargetLOA:F2}",
            $"Vol:{criteria.TargetVolume:F2}",
            $"LCB:{criteria.TargetLCB?.ToString("F3") ?? "null"}",
            $"BeamR:{criteria.TargetBeamRatio?.ToString("F3") ?? "null"}",
            $"DraftR:{criteria.TargetDraftRatio?.ToString("F3") ?? "null"}",
            $"Cb:{criteria.TargetCb?.ToString("F3") ?? "null"}",
            $"K:{K}"
        };

        return $"ml_knn:{string.Join(":", parts)}";
    }
}

/// <summary>
/// Search criteria for parametric KNN
/// </summary>
public class ParametricSearchCriteria
{
    public decimal TargetLOA { get; set; }  // Target length overall (m)
    public decimal TargetVolume { get; set; }  // Target underwater volume (m³)
    public decimal? TargetLCB { get; set; }  // Target LCB as fraction of LOA (optional)
    public decimal? TargetBeamRatio { get; set; }  // Target B/LOA (optional)
    public decimal? TargetDraftRatio { get; set; }  // Target T/LOA (optional)
    public decimal? TargetCb { get; set; }  // Target block coefficient (optional)
}

/// <summary>
/// Similar parametric hull result
/// </summary>
public class SimilarParametricHull
{
    public int HullId { get; set; }
    public string HullIdString { get; set; } = string.Empty;
    public string DatasetSource { get; set; } = string.Empty;

    // Principal dimensions (derived, at LOA=10m baseline)
    public decimal LppM { get; set; }
    public decimal BeamM { get; set; }
    public decimal DraftM { get; set; }
    public decimal DepthM { get; set; }

    // Form coefficients
    public decimal Cb { get; set; }
    public decimal? Cp { get; set; }
    public decimal? Cm { get; set; }
    public decimal Cw { get; set; }

    // Geometric features (normalized)
    public decimal VolumeNorm { get; set; }
    public decimal LcbNorm { get; set; }

    // Similarity
    public double GeometricDistance { get; set; }
    public double SimilarityScore { get; set; }  // 0-1, higher = more similar

    // Quality
    public string ConversionQuality { get; set; } = string.Empty;
}
