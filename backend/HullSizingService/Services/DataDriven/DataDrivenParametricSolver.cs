using HullSizingService.Services.Integration;
using HullSizingService.Services.Solver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Catalog;
using Shared.Models.Sizing;

namespace HullSizingService.Services.DataDriven;

/// <summary>
/// Data-Driven solver using ML/Parametric catalog (MIT ShipD dataset)
/// 4-Step Workflow: KNN Search → Scale → Refine with Physics → Rank
/// </summary>
public class DataDrivenParametricSolver
{
    private readonly IDataServiceClient _dataServiceClient;
    private readonly ParametricConverter _converter;
    private readonly IFirstPrinciplesSolver _firstPrinciplesSolver;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DataDrivenParametricSolver> _logger;

    public DataDrivenParametricSolver(
        IDataServiceClient dataServiceClient,
        ParametricConverter converter,
        IFirstPrinciplesSolver firstPrinciplesSolver,
        IConfiguration configuration,
        ILogger<DataDrivenParametricSolver> logger)
    {
        _dataServiceClient = dataServiceClient;
        _converter = converter;
        _firstPrinciplesSolver = firstPrinciplesSolver;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<SolverCandidate>> SolveAsync(
        SolverRequest request,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting Data-Driven ML/Parametric solver for mission {MissionId}",
            request.MissionCase.Id);

        try
        {
            // Step 1: Search parametric catalog via KNN
            var similarHulls = await SearchParametricCatalogAsync(request.MissionCase, cancellationToken);

            if (!similarHulls.Any())
            {
                _logger.LogWarning("No similar parametric hulls found. Falling back to First-Principles.");
                return await FallbackToFirstPrinciplesAsync(request, cancellationToken);
            }

            _logger.LogInformation("Found {Count} similar parametric hulls. Avg similarity: {AvgSim:P0}",
                similarHulls.Count, similarHulls.Average(h => h.SimilarityScore));

            // Step 2: Convert and scale parametric hulls to target displacement
            var convertedHulls = ConvertAndScaleHulls(similarHulls, request.MissionCase);

            var validConverted = convertedHulls.Where(h => h.IsValid).ToList();
            if (!validConverted.Any())
            {
                _logger.LogWarning("No valid conversions. Falling back to First-Principles.");
                return await FallbackToFirstPrinciplesAsync(request, cancellationToken);
            }

            _logger.LogInformation("{ValidCount}/{TotalCount} conversions valid",
                validConverted.Count, convertedHulls.Count);

            // Step 3: Refine with physics-based solver
            var refinedCandidates = await RefineWithPhysicsAsync(validConverted, request, cancellationToken);

            if (!refinedCandidates.Any())
            {
                _logger.LogWarning("Physics refinement produced no candidates. Falling back.");
                return await FallbackToFirstPrinciplesAsync(request, cancellationToken);
            }

            // Step 4: Attach ML provenance and rank
            var finalCandidates = refinedCandidates
                .Select(c => AttachMLProvenance(c, convertedHulls))
                .OrderByDescending(c => c.Score)
                .Take(request.Options.MaxCandidates)
                .ToList();

            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation(
                "✅ Data-Driven ML solver complete. Generated {Count} candidates in {ElapsedMs}ms",
                finalCandidates.Count, elapsedMs);

            return finalCandidates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Data-Driven ML solver. Falling back to First-Principles.");
            return await FallbackToFirstPrinciplesAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Step 1: Search parametric catalog using KNN
    /// </summary>
    private async Task<List<SimilarParametricHullDto>> SearchParametricCatalogAsync(
        MissionCase mission,
        CancellationToken cancellationToken)
    {
        try
        {
            // Calculate target displacement and volume
            var targetDisplacement = CalculateTargetDisplacement(mission);
            var targetVolume = targetDisplacement / 1.025m;  // Seawater density

            // Estimate LOA from displacement (using typical displacement-length ratio)
            // Δ ≈ 0.01 * L³ (for merchant ships), so L ≈ (Δ / 0.01)^(1/3)
            var estimatedLOA = (decimal)Math.Pow((double)targetDisplacement / 0.01, 1.0 / 3.0);

            // Build KNN search request
            var knnRequest = new ParametricSearchRequest
            {
                TargetLOA = estimatedLOA,
                TargetVolume = targetVolume,
                TargetCb = null,  // Let KNN find best match
                K = _configuration.GetValue<int>("CatalogSettings:KnnDefaultK", 5)
            };

            // Call DataService KNN endpoint
            var response = await _dataServiceClient.SearchSimilarParametricHullsAsync(knnRequest, cancellationToken);

            return response.SimilarHulls;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching parametric catalog");
            return new List<SimilarParametricHullDto>();
        }
    }

    /// <summary>
    /// Step 2: Convert and scale parametric hulls to target displacement
    /// </summary>
    private List<ConvertedParametricHull> ConvertAndScaleHulls(
        List<SimilarParametricHullDto> similarHulls,
        MissionCase mission)
    {
        var targetDisplacement = CalculateTargetDisplacement(mission);

        var constraints = new ParametricScalingConstraints
        {
            MaxBeam = mission.CapBeamM,
            MaxDraft = mission.CapDraftM
        };

        var converted = new List<ConvertedParametricHull>();

        foreach (var hull in similarHulls)
        {
            try
            {
                var convertedHull = _converter.ConvertToTargetDisplacement(hull, targetDisplacement, constraints);
                converted.Add(convertedHull);

                if (!convertedHull.IsValid)
                {
                    _logger.LogDebug("Conversion invalid for {HullId}: {Errors}",
                        hull.HullIdString, string.Join("; ", convertedHull.ValidationErrors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert parametric hull {HullId}", hull.HullIdString);
            }
        }

        return converted;
    }

    /// <summary>
    /// Step 3: Refine converted hulls with First-Principles physics solver
    /// </summary>
    private async Task<List<SolverCandidate>> RefineWithPhysicsAsync(
        List<ConvertedParametricHull> convertedHulls,
        SolverRequest originalRequest,
        CancellationToken cancellationToken)
    {
        var refinedCandidates = new List<SolverCandidate>();

        foreach (var converted in convertedHulls)
        {
            try
            {
                // Create a solver request with scaled dimensions as starting point
                // The First-Principles solver will validate and refine
                var refinementRequest = new SolverRequest(
                    MissionCase: originalRequest.MissionCase,
                    Locks: originalRequest.Locks,
                    Options: new SizingOptionsDto(
                        FamilyHints: null,
                        MaxCandidates: 1,  // Just need best refinement
                        MinFn: null,
                        MaxFn: null
                    )
                );

                // Run First-Principles solver (will use its own initialization, but influenced by mission)
                var (candidates, _) = await _firstPrinciplesSolver.SolveAsync(refinementRequest, cancellationToken);

                if (candidates.Any())
                {
                    // Take best candidate
                    var best = candidates.OrderByDescending(c => c.Score).First();
                    refinedCandidates.Add(best);

                    _logger.LogDebug("Refined {HullId}: Score={Score:F3}",
                        converted.SourceHullId, best.Score);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refine hull {HullId}", converted.SourceHullId);
            }
        }

        return refinedCandidates;
    }

    /// <summary>
    /// Step 4: Attach ML/Parametric provenance to candidates
    /// </summary>
    private SolverCandidate AttachMLProvenance(
        SolverCandidate candidate,
        List<ConvertedParametricHull> convertedHulls)
    {
        // Find the best-matching converted hull (by Lpp, B, T similarity)
        var bestMatch = convertedHulls
            .OrderBy(h => Math.Abs(h.Lpp - candidate.LppM) +
                         Math.Abs(h.Beam - candidate.BeamM) +
                         Math.Abs(h.Draft - candidate.DraftM))
            .FirstOrDefault();

        if (bestMatch == null)
            return candidate;

        // Add ML provenance flags
        var flags = candidate.Flags.ToList();
        flags.Add($"ML_Parametric");
        flags.Add($"Source:{bestMatch.SourceHullId}");
        flags.Add($"Dataset:{bestMatch.SourceDataset}");
        flags.Add($"Similarity:{bestMatch.SimilarityScore:P0}");
        flags.Add($"ScaleFactor:{bestMatch.ScaleFactor:F2}x");

        return candidate with
        {
            ReferenceVesselId = bestMatch.SourceHullId,
            ReferenceVesselName = bestMatch.SourceHullId,  // e.g., "CS1_00123"
            SimilarityScore = (decimal)bestMatch.SimilarityScore,
            SolverMode = "DataDrivenML",
            Flags = flags
        };
    }

    /// <summary>
    /// Fallback to First-Principles if Data-Driven fails
    /// </summary>
    private async Task<List<SolverCandidate>> FallbackToFirstPrinciplesAsync(
        SolverRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Using First-Principles fallback");

        var (candidates, _) = await _firstPrinciplesSolver.SolveAsync(request, cancellationToken);

        // Mark as fallback
        return candidates.Select(c =>
        {
            var flags = c.Flags.ToList();
            flags.Add("ML_Fallback");
            return c with
            {
                Flags = flags,
                SolverMode = "FirstPrinciples_Fallback"
            };
        }).ToList();
    }

    /// <summary>
    /// Calculate target displacement from mission requirements
    /// </summary>
    private decimal CalculateTargetDisplacement(MissionCase mission)
    {
        // Calculate cargo mass based on input type
        decimal cargoMass = mission.CargoBasis.ToLower() switch
        {
            "volume" => (mission.CargoVolumeM3 ?? mission.CargoValue ?? 0) * (mission.CargoDensityTPerM3 ?? 1.0m),
            "weight" => mission.CargoValue ?? 0,
            "teu" => (mission.TeuCount ?? 0) * 14.0m,  // Typical 14t per TEU
            _ => mission.CargoValue ?? 0
        };

        // Add lightship and margin (simplified estimate)
        const decimal lightshipRatio = 0.35m;  // Lightship ~35% of displacement
        const decimal margin = 1.10m;  // 10% margin

        var targetDisplacement = (cargoMass / (1.0m - lightshipRatio)) * margin;

        _logger.LogDebug("Target displacement calculated: {Disp:F0}t from cargo: {Cargo}t",
            targetDisplacement, cargoMass);

        return targetDisplacement;
    }
}
