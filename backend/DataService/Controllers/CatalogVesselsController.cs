using Asp.Versioning;
using DataService.Services.Catalog;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Catalog;

namespace DataService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/catalog/vessels")]
public class CatalogVesselsController : ControllerBase
{
    private readonly RealWorldKnnService _knnService;
    private readonly ILogger<CatalogVesselsController> _logger;

    public CatalogVesselsController(
        RealWorldKnnService knnService,
        ILogger<CatalogVesselsController> logger)
    {
        _knnService = knnService;
        _logger = logger;
    }

    /// <summary>
    /// Search for similar vessels using K-Nearest Neighbors algorithm
    /// </summary>
    /// <param name="request">Search criteria (vessel type, displacement, speed, constraints)</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations</param>
    /// <returns>List of K most similar vessels with similarity scores</returns>
    [HttpPost("search-similar")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(KnnSearchResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<KnnSearchResponse>> SearchSimilarVessels(
        [FromBody] KnnSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "KNN search request: Type={Type}, Displacement={Disp}t, Speed={Speed}m/s, K={K}",
                request.VesselType, request.TargetDisplacement, request.ServiceSpeed, request.K);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.VesselType))
            {
                return BadRequest("VesselType is required");
            }

            if (request.TargetDisplacement <= 0)
            {
                return BadRequest("TargetDisplacement must be positive");
            }

            if (request.K < 1 || request.K > 20)
            {
                return BadRequest("K must be between 1 and 20");
            }

            // Convert to mission search criteria
            var criteria = new MissionSearchCriteria
            {
                VesselType = request.VesselType,
                TargetDisplacement = request.TargetDisplacement,
                ServiceSpeed = request.ServiceSpeed,
                MaxBeam = request.MaxBeam,
                MaxDraft = request.MaxDraft
            };

            // Perform KNN search
            var similarVessels = await _knnService.FindSimilarVesselsAsync(
                criteria,
                request.K,
                cancellationToken);

            // Convert to DTOs
            var dtos = similarVessels.Select(v => new SimilarVesselDto
            {
                VesselId = v.VesselId,
                VesselName = v.VesselName,
                VesselType = v.VesselType,
                LppM = v.Vessel.LppM,
                BeamM = v.Vessel.BeamM,
                DraftM = v.Vessel.DraftM,
                DepthM = v.Vessel.DepthM,
                DisplacementT = v.Vessel.DisplacementT,
                Cb = v.Vessel.Cb,
                Cp = v.Vessel.Cp,
                Cm = v.Vessel.Cm,
                Cw = v.Vessel.Cw,
                ServiceSpeedMs = v.Vessel.ServiceSpeedMs,
                SimilarityScore = v.SimilarityScore
            }).ToList();

            var response = new KnnSearchResponse
            {
                SimilarVessels = dtos,
                TotalCatalogSize = 600,  // TODO: Get actual count from service
                CatalogSource = "RealWorld"
            };

            _logger.LogInformation(
                "KNN search completed. Returned {Count} similar vessels. " +
                "Avg similarity: {AvgSimilarity:P2}",
                dtos.Count, dtos.Any() ? dtos.Average(d => d.SimilarityScore) : 0);

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("KNN search cancelled by client");
            return StatusCode(499, "Request cancelled");  // Client Closed Request
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KNN search failed");
            return StatusCode(500, "Internal server error during KNN search");
        }
    }

    /// <summary>
    /// Clear the in-memory catalog cache (for testing or after updates)
    /// </summary>
    [HttpPost("clear-cache")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult ClearCache()
    {
        _knnService.ClearCache();
        _logger.LogInformation("Vessel catalog cache cleared");
        return Ok(new { message = "Cache cleared successfully" });
    }
}

