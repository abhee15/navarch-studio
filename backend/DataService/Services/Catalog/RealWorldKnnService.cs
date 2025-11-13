using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Models;

namespace DataService.Services.Catalog;

/// <summary>
/// K-Nearest Neighbors search for real-world vessel catalog
/// Uses mission-based features: vessel type, payload, speed, constraints
/// </summary>
public class RealWorldKnnService
{
    private readonly DataDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IVesselTypeMapper _vesselTypeMapper;
    private readonly ILogger<RealWorldKnnService> _logger;
    private const string CacheKey = "RealWorldCatalog_All";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public RealWorldKnnService(
        DataDbContext context,
        IMemoryCache cache,
        IVesselTypeMapper vesselTypeMapper,
        ILogger<RealWorldKnnService> logger)
    {
        _context = context;
        _cache = cache;
        _vesselTypeMapper = vesselTypeMapper;
        _logger = logger;
    }

    /// <summary>
    /// Find K most similar vessels based on mission requirements
    /// </summary>
    public async Task<List<SimilarVessel>> FindSimilarVesselsAsync(
        MissionSearchCriteria criteria,
        int K = 5,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        // 1. Load catalog from cache (or DB if cache miss)
        var catalog = await GetCatalogAsync(cancellationToken);

        if (!catalog.Any())
        {
            _logger.LogWarning("Real-world vessel catalog is empty. Cannot perform KNN search.");
            return new List<SimilarVessel>();
        }

        // 2. Filter by vessel type using mapper (handles ShipD taxonomy -> catalog mapping)
        // Map ShipD taxonomy type to catalog types (e.g., "bulk_carrier" -> ["Bulk carrier", "Bulk"])
        var catalogTypes = _vesselTypeMapper.MapToCatalogTypes(criteria.VesselType);

        List<CatalogVesselReal> sameType;
        if (catalogTypes.Any())
        {
            // Normalize catalog types for comparison
            var normalizedCatalogTypes = catalogTypes
                .Select(t => _vesselTypeMapper.NormalizeVesselType(t))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Filter by mapped catalog types (OR logic - match any of the mapped types)
            sameType = catalog
                .Where(v => normalizedCatalogTypes.Contains(
                    _vesselTypeMapper.NormalizeVesselType(v.VesselType)))
                .ToList();

            _logger.LogDebug(
                "Filtered catalog: {Total} vessels, {SameType} matching ShipD type '{ShipDType}' " +
                "(mapped to catalog types: {CatalogTypes})",
                catalog.Count, sameType.Count, criteria.VesselType, string.Join(", ", catalogTypes));
        }
        else
        {
            // Fallback: try direct match (for backward compatibility with old MissionType values)
            sameType = catalog
                .Where(v => v.VesselType.Equals(criteria.VesselType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger.LogDebug(
                "No catalog mapping found for '{Type}'. Using direct match. " +
                "Filtered catalog: {Total} vessels, {SameType} matching type",
                criteria.VesselType, catalog.Count, sameType.Count);
        }

        // 3. Calculate target feature vector
        var targetVector = ExtractFeatures(criteria);

        // 4. Normalize features using catalog statistics
        var stats = CalculateStatistics(catalog);
        var normalizedTarget = NormalizeFeatures(targetVector, stats);

        // 5. Calculate distances for same-type vessels
        var distances = sameType.Select(v =>
        {
            var vesselVector = ExtractVesselFeatures(v);
            var normalizedVessel = NormalizeFeatures(vesselVector, stats);
            var distance = CalculateWeightedDistance(normalizedTarget, normalizedVessel);

            return new
            {
                Vessel = v,
                Distance = distance,
                SimilarityScore = CalculateSimilarityScore(distance, stats.MaxDistance)
            };
        }).OrderBy(x => x.Distance).ToList();

        // 6. If fewer than 3 matches in same type, fallback to all types
        if (distances.Count < 3)
        {
            _logger.LogInformation(
                "Only {Count} vessels of type '{Type}'. Expanding search to all types.",
                distances.Count, criteria.VesselType);

            distances = catalog.Select(v =>
            {
                var vesselVector = ExtractVesselFeatures(v);
                var normalizedVessel = NormalizeFeatures(vesselVector, stats);
                var distance = CalculateWeightedDistance(normalizedTarget, normalizedVessel);

                return new
                {
                    Vessel = v,
                    Distance = distance,
                    SimilarityScore = CalculateSimilarityScore(distance, stats.MaxDistance)
                };
            }).OrderBy(x => x.Distance).ToList();
        }

        // 7. Return top K results
        var results = distances.Take(K).Select(d => new SimilarVessel
        {
            VesselId = d.Vessel.Id,
            VesselName = d.Vessel.VesselId,  // Display name
            VesselType = d.Vessel.VesselType,
            SimilarityScore = d.SimilarityScore,
            Distance = d.Distance,
            Vessel = d.Vessel
        }).ToList();

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger.LogInformation(
            "KNN search completed in {Elapsed}ms. Found {Count} similar vessels. " +
            "Avg similarity: {AvgSimilarity:P2}",
            elapsed, results.Count, results.Average(r => r.SimilarityScore));

        return results;
    }

    /// <summary>
    /// Extract mission features into a vector for KNN search
    /// </summary>
    private double[] ExtractFeatures(MissionSearchCriteria criteria)
    {
        return new double[]
        {
            (double)criteria.TargetDisplacement,  // Feature 0: Displacement (tons)
            (double)criteria.ServiceSpeed,         // Feature 1: Service speed (m/s)
            (double)(criteria.MaxBeam ?? 999.0m),  // Feature 2: Max beam constraint
            (double)(criteria.MaxDraft ?? 999.0m)  // Feature 3: Max draft constraint
        };
    }

    /// <summary>
    /// Extract vessel features into a vector
    /// </summary>
    private double[] ExtractVesselFeatures(CatalogVesselReal vessel)
    {
        return new double[]
        {
            (double)vessel.DisplacementT,
            (double)(vessel.ServiceSpeedMs ?? 10.0m),  // Default if missing
            (double)vessel.BeamM,
            (double)vessel.DraftM
        };
    }

    /// <summary>
    /// Normalize features to [0, 1] range using min-max normalization
    /// </summary>
    private double[] NormalizeFeatures(double[] features, FeatureStatistics stats)
    {
        var normalized = new double[features.Length];

        for (int i = 0; i < features.Length; i++)
        {
            var range = stats.Max[i] - stats.Min[i];
            if (range > 0)
            {
                normalized[i] = (features[i] - stats.Min[i]) / range;
            }
            else
            {
                normalized[i] = 0.5;  // All values same, middle of range
            }
        }

        return normalized;
    }

    /// <summary>
    /// Calculate weighted Euclidean distance
    /// Weights reflect importance of each feature for hull sizing
    /// </summary>
    private double CalculateWeightedDistance(double[] target, double[] candidate)
    {
        // Feature weights (must sum to 1.0)
        var weights = new double[]
        {
            0.40,  // Displacement (most important)
            0.30,  // Service speed (critical for performance)
            0.15,  // Beam constraint
            0.15   // Draft constraint
        };

        double sum = 0;
        for (int i = 0; i < target.Length; i++)
        {
            var diff = target[i] - candidate[i];
            sum += weights[i] * diff * diff;
        }

        return Math.Sqrt(sum);
    }

    /// <summary>
    /// Convert distance to similarity score (0-1, higher is better)
    /// </summary>
    private double CalculateSimilarityScore(double distance, double maxDistance)
    {
        if (maxDistance == 0)
            return 1.0;

        // Normalize distance to [0, 1], then invert
        var normalizedDistance = Math.Min(distance / maxDistance, 1.0);
        return 1.0 - normalizedDistance;
    }

    /// <summary>
    /// Calculate statistics for normalization
    /// </summary>
    private FeatureStatistics CalculateStatistics(List<CatalogVesselReal> catalog)
    {
        var stats = new FeatureStatistics
        {
            Min = new double[4],
            Max = new double[4]
        };

        if (!catalog.Any())
            return stats;

        // Displacement
        stats.Min[0] = (double)catalog.Min(v => v.DisplacementT);
        stats.Max[0] = (double)catalog.Max(v => v.DisplacementT);

        // Service speed
        stats.Min[1] = (double)catalog.Min(v => v.ServiceSpeedMs ?? 5.0m);
        stats.Max[1] = (double)catalog.Max(v => v.ServiceSpeedMs ?? 20.0m);

        // Beam
        stats.Min[2] = (double)catalog.Min(v => v.BeamM);
        stats.Max[2] = (double)catalog.Max(v => v.BeamM);

        // Draft
        stats.Min[3] = (double)catalog.Min(v => v.DraftM);
        stats.Max[3] = (double)catalog.Max(v => v.DraftM);

        // Max distance (for similarity calculation)
        stats.MaxDistance = Math.Sqrt(4.0);  // Max possible distance with weights summing to 1

        return stats;
    }

    /// <summary>
    /// Load catalog from cache or database
    /// </summary>
    private async Task<List<CatalogVesselReal>> GetCatalogAsync(CancellationToken cancellationToken)
    {
        // Try cache first
        if (_cache.TryGetValue(CacheKey, out List<CatalogVesselReal>? cached) && cached != null)
        {
            _logger.LogDebug("Loaded {Count} vessels from cache", cached.Count);
            return cached;
        }

        // Cache miss - load from database
        _logger.LogInformation("Cache miss. Loading real-world vessel catalog from database...");

        var catalog = await _context.CatalogVesselsReal
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Cache for 1 hour
        _cache.Set(CacheKey, catalog, CacheDuration);

        _logger.LogInformation("Loaded {Count} vessels from database and cached", catalog.Count);

        return catalog;
    }

    /// <summary>
    /// Clear cache (for testing or after catalog updates)
    /// </summary>
    public void ClearCache()
    {
        _cache.Remove(CacheKey);
        _logger.LogInformation("Real-world vessel catalog cache cleared");
    }
}

/// <summary>
/// Search criteria for mission-based KNN
/// </summary>
public class MissionSearchCriteria
{
    public string VesselType { get; set; } = string.Empty;
    public decimal TargetDisplacement { get; set; }
    public decimal ServiceSpeed { get; set; }  // m/s
    public decimal? MaxBeam { get; set; }
    public decimal? MaxDraft { get; set; }
}

/// <summary>
/// KNN search result with similarity scoring
/// </summary>
public class SimilarVessel
{
    public Guid VesselId { get; set; }
    public string VesselName { get; set; } = string.Empty;
    public string VesselType { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }  // 0-1, higher is better
    public double Distance { get; set; }  // Raw Euclidean distance
    public CatalogVesselReal Vessel { get; set; } = null!;
}

/// <summary>
/// Feature statistics for normalization
/// </summary>
internal class FeatureStatistics
{
    public double[] Min { get; set; } = Array.Empty<double>();
    public double[] Max { get; set; } = Array.Empty<double>();
    public double MaxDistance { get; set; }
}
