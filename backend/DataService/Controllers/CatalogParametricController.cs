using Asp.Versioning;
using DataService.Data;
using DataService.Services.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Catalog;
using Shared.Models;
using System.Diagnostics;

namespace DataService.Controllers;

/// <summary>
/// API for ML/Parametric hull catalog (MIT ShipD dataset)
/// KNN search and catalog management
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/catalog/parametric")]
public class CatalogParametricController : ControllerBase
{
    private readonly ParametricKnnService _knnService;
    private readonly DataDbContext _context;
    private readonly ILogger<CatalogParametricController> _logger;

    public CatalogParametricController(
        ParametricKnnService knnService,
        DataDbContext context,
        ILogger<CatalogParametricController> logger)
    {
        _knnService = knnService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Search for similar parametric hulls using KNN
    /// </summary>
    [HttpPost("search-similar")]
    public async Task<ActionResult<ParametricSearchResponse>> SearchSimilar(
        [FromBody] ParametricSearchRequest request,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Parametric KNN search: LOA={LOA}m, Volume={Vol}m³, K={K}",
                request.TargetLOA, request.TargetVolume, request.K);

            // Validate
            if (request.K < 1 || request.K > 20)
            {
                return BadRequest(new { error = "K must be between 1 and 20" });
            }

            if (request.TargetLOA <= 0 || request.TargetVolume <= 0)
            {
                return BadRequest(new { error = "TargetLOA and TargetVolume must be positive" });
            }

            // Build search criteria
            var criteria = new ParametricSearchCriteria
            {
                TargetLOA = request.TargetLOA,
                TargetVolume = request.TargetVolume,
                TargetLCB = request.TargetLCB,
                TargetBeamRatio = request.TargetBeamRatio,
                TargetDraftRatio = request.TargetDraftRatio,
                TargetCb = request.TargetCb
            };

            // Execute KNN search
            var similarHulls = await _knnService.FindSimilarHullsAsync(criteria, request.K, cancellationToken);

            sw.Stop();

            var response = new ParametricSearchResponse
            {
                SimilarHulls = similarHulls.Select(MapToDto).ToList(),
                TotalCatalogSize = 5000,  // Phase 2A: 5K, will be dynamic later
                CatalogSource = "ML_Parametric",
                AlgorithmUsed = "Geometric_KNN",  // Phase 2B will upgrade to ANN_HNSW
                QueryTimeMs = (int)sw.ElapsedMilliseconds
            };

            _logger.LogInformation(
                "Parametric KNN completed in {ElapsedMs}ms. Returned {Count} hulls. Avg similarity: {AvgSim:P0}",
                sw.ElapsedMilliseconds, similarHulls.Count, similarHulls.Any() ? similarHulls.Average(h => h.SimilarityScore) : 0);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in parametric KNN search");
            return StatusCode(500, new { error = "Internal server error during KNN search", details = ex.Message });
        }
    }

    /// <summary>
    /// Browse parametric catalog with pagination and filters
    /// </summary>
    [HttpGet("browse")]
    public async Task<ActionResult<PagedParametricResponse>> Browse(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? dataset = null,
        [FromQuery] decimal? minCb = null,
        [FromQuery] decimal? maxCb = null,
        [FromQuery] decimal? minVolume = null,
        [FromQuery] decimal? maxVolume = null,
        [FromQuery] string? sortBy = "hull_id",
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate pagination
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _context.ParametricHulls.Where(h => h.IsActive);

            // Apply filters
            if (!string.IsNullOrEmpty(dataset))
                query = query.Where(h => h.DatasetSource.Contains(dataset));

            if (minCb.HasValue)
                query = query.Where(h => h.CbDerived >= minCb.Value);

            if (maxCb.HasValue)
                query = query.Where(h => h.CbDerived <= maxCb.Value);

            if (minVolume.HasValue)
                query = query.Where(h => h.VolumeNorm >= minVolume.Value);

            if (maxVolume.HasValue)
                query = query.Where(h => h.VolumeNorm <= maxVolume.Value);

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "cb" => query.OrderBy(h => h.CbDerived),
                "volume" => query.OrderBy(h => h.VolumeNorm),
                "lcb" => query.OrderBy(h => h.LcbNorm),
                "lpp" => query.OrderBy(h => h.LppMDerived),
                _ => query.OrderBy(h => h.HullId)
            };

            // Count total
            var total = await query.CountAsync(cancellationToken);

            // Paginate
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new ParametricHullBriefDto
                {
                    HullId = h.HullId,
                    DatasetSource = h.DatasetSource,
                    LppM = h.LppMDerived,
                    BeamM = h.BeamMDerived,
                    DraftM = h.DraftMDerived,
                    Cb = h.CbDerived,
                    VolumeNorm = h.VolumeNorm,
                    LcbNorm = h.LcbNorm,
                    ConversionQuality = h.ConversionQuality ?? "Good"
                })
                .ToListAsync(cancellationToken);

            return Ok(new PagedParametricResponse
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing parametric catalog");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get catalog statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<CatalogStatsDto>> GetStats(CancellationToken cancellationToken)
    {
        try
        {
            var total = await _context.ParametricHulls.CountAsync(cancellationToken);

            if (total == 0)
            {
                return Ok(new CatalogStatsDto
                {
                    TotalHulls = 0,
                    ByDataset = new Dictionary<string, int>(),
                    AvgCb = 0,
                    CbRange = new { Min = 0m, Max = 0m }
                });
            }

            var byDataset = await _context.ParametricHulls
                .GroupBy(h => h.DatasetSource)
                .Select(g => new { Source = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Source, x => x.Count, cancellationToken);

            var avgCb = await _context.ParametricHulls.AverageAsync(h => (double)h.CbDerived, cancellationToken);
            var minCb = await _context.ParametricHulls.MinAsync(h => h.CbDerived, cancellationToken);
            var maxCb = await _context.ParametricHulls.MaxAsync(h => h.CbDerived, cancellationToken);

            return Ok(new CatalogStatsDto
            {
                TotalHulls = total,
                ByDataset = byDataset,
                AvgCb = avgCb,
                CbRange = new { Min = minCb, Max = maxCb }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting catalog stats");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Map internal model to API DTO
    /// </summary>
    private SimilarParametricHullDto MapToDto(SimilarParametricHull hull)
    {
        return new SimilarParametricHullDto
        {
            HullId = hull.HullId,
            HullIdString = hull.HullIdString,
            DatasetSource = hull.DatasetSource,
            LppM = hull.LppM,
            BeamM = hull.BeamM,
            DraftM = hull.DraftM,
            DepthM = hull.DepthM,
            Cb = hull.Cb,
            Cp = hull.Cp,
            Cm = hull.Cm,
            Cw = hull.Cw,
            VolumeNorm = hull.VolumeNorm,
            LcbNorm = hull.LcbNorm,
            SimilarityScore = hull.SimilarityScore,
            GeometricDistance = hull.GeometricDistance,
            ConversionQuality = hull.ConversionQuality
        };
    }
}

/// <summary>
/// Brief DTO for catalog browsing
/// </summary>
public class ParametricHullBriefDto
{
    public string HullId { get; set; } = string.Empty;
    public string DatasetSource { get; set; } = string.Empty;
    public decimal LppM { get; set; }
    public decimal BeamM { get; set; }
    public decimal DraftM { get; set; }
    public decimal Cb { get; set; }
    public decimal VolumeNorm { get; set; }
    public decimal LcbNorm { get; set; }
    public string ConversionQuality { get; set; } = string.Empty;
}

/// <summary>
/// Paged response for catalog browsing
/// </summary>
public class PagedParametricResponse
{
    public List<ParametricHullBriefDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Catalog statistics DTO
/// </summary>
public class CatalogStatsDto
{
    public int TotalHulls { get; set; }
    public Dictionary<string, int> ByDataset { get; set; } = new();
    public double AvgCb { get; set; }
    public object? CbRange { get; set; }
}
