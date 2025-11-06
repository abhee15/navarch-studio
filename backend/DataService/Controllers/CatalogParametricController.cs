using Asp.Versioning;
using DataService.Services.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Catalog;
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
    private readonly ILogger<CatalogParametricController> _logger;

    public CatalogParametricController(
        ParametricKnnService knnService,
        ILogger<CatalogParametricController> logger)
    {
        _knnService = knnService;
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

