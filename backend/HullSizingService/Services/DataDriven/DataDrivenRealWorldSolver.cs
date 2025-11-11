using HullSizingService.Services.Integration;
using HullSizingService.Services.Solver;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Catalog;
using Shared.Models.Sizing;

namespace HullSizingService.Services.DataDriven;

/// <summary>
/// Data-Driven solver using Real-World vessel catalog (600 vessels)
/// Workflow: KNN search → Scaling → First-Principles refinement → Ranking
/// </summary>
public class DataDrivenRealWorldSolver
{
    private readonly IDataServiceClient _dataServiceClient;
    private readonly VesselScalingService _scalingService;
    private readonly IFirstPrinciplesSolver _firstPrinciplesSolver;
    private readonly ILogger<DataDrivenRealWorldSolver> _logger;

    public DataDrivenRealWorldSolver(
        IDataServiceClient dataServiceClient,
        VesselScalingService scalingService,
        IFirstPrinciplesSolver firstPrinciplesSolver,
        ILogger<DataDrivenRealWorldSolver> logger)
    {
        _dataServiceClient = dataServiceClient;
        _scalingService = scalingService;
        _firstPrinciplesSolver = firstPrinciplesSolver;
        _logger = logger;
    }

    public async Task<List<SolverCandidate>> SolveAsync(
        SolverRequest request,
        CancellationToken cancellationToken = default)
    {
        var mission = request.MissionCase;
        var startTime = DateTime.UtcNow;
        _logger.LogInformation(
            "Starting Data-Driven Real-World solver for mission {MissionId}. " +
            "MissionType: {Type}, Cargo: {Cargo}, Speed: {Speed}kn",
            mission.Id, mission.MissionType, mission.CargoValue, mission.ServiceSpeedKn);

        try
        {
            // Step 1: KNN Search via DataService
            _logger.LogDebug("Step 1/4: Performing KNN search on real-world vessel catalog...");

            var similarVessels = await FindSimilarVesselsAsync(mission, K: 5, cancellationToken);

            if (!similarVessels.Any())
            {
                _logger.LogWarning(
                    "No similar vessels found for type '{Type}'. Falling back to First-Principles solver.",
                    mission.MissionType);

                return await FallbackToFirstPrinciplesAsync(request, cancellationToken);
            }

            _logger.LogInformation(
                "Found {Count} similar vessels. Avg similarity: {AvgSimilarity:P2}",
                similarVessels.Count, similarVessels.Average(v => v.SimilarityScore));

            // Step 2: Scale each vessel to target displacement
            _logger.LogDebug("Step 2/4: Scaling {Count} reference vessels to target displacement...",
                similarVessels.Count);

            var scaledCandidates = ScaleVessels(similarVessels, mission, cancellationToken);

            if (!scaledCandidates.Any())
            {
                _logger.LogWarning(
                    "All scaled vessels invalid (constraint violations or excessive distortion). " +
                    "Falling back to First-Principles solver.");

                return await FallbackToFirstPrinciplesAsync(request, cancellationToken);
            }

            _logger.LogInformation(
                "{ValidCount}/{TotalCount} scaled candidates valid",
                scaledCandidates.Count, similarVessels.Count);

            // Step 3: Refine with First-Principles solver (displacement closure)
            _logger.LogDebug("Step 3/4: Refining candidates with First-Principles solver...");

            var refinedCandidates = await RefineWithPhysicsAsync(
                scaledCandidates, mission, request, cancellationToken);

            if (!refinedCandidates.Any())
            {
                _logger.LogWarning("Refinement failed for all candidates. Falling back.");
                return await FallbackToFirstPrinciplesAsync(request, cancellationToken);
            }

            // Step 4: Rank and return top 5
            _logger.LogDebug("Step 4/4: Ranking candidates by score...");

            var ranked = refinedCandidates
                .OrderByDescending(c => c.Score)
                .Take(5)
                .ToList();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation(
                "✅ Data-Driven solver completed in {Elapsed}ms. Returned {Count} candidates. " +
                "Top score: {TopScore:F2}",
                elapsed, ranked.Count, ranked.FirstOrDefault()?.Score ?? 0);

            return ranked;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Data-Driven solver cancelled by user");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data-Driven solver failed. Falling back to First-Principles.");
            return await FallbackToFirstPrinciplesAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Call DataService to perform KNN search on real-world catalog
    /// </summary>
    private async Task<List<SimilarVesselDto>> FindSimilarVesselsAsync(
        MissionCase mission,
        int K,
        CancellationToken cancellationToken)
    {
        // Calculate target displacement from cargo
        var targetDisplacement = CalculateTargetDisplacement(mission);

        // Build search criteria
        var criteria = new KnnSearchRequest
        {
            VesselType = mission.MissionType,
            TargetDisplacement = targetDisplacement,
            ServiceSpeed = ConvertKnotsToMs(mission.ServiceSpeedKn),
            MaxBeam = mission.CapBeamM,
            MaxDraft = mission.CapDraftM,
            K = K
        };

        // Call DataService KNN endpoint
        var response = await _dataServiceClient.SearchSimilarVesselsAsync(criteria, cancellationToken);

        return response.SimilarVessels ?? new List<SimilarVesselDto>();
    }

    /// <summary>
    /// Scale reference vessels to target displacement
    /// </summary>
    private List<(ScaledCandidate Scaled, SimilarVesselDto Source)> ScaleVessels(
        List<SimilarVesselDto> similarVessels,
        MissionCase mission,
        CancellationToken cancellationToken)
    {
        var targetDisplacement = CalculateTargetDisplacement(mission);
        var constraints = new ScalingConstraints
        {
            MaxBeam = mission.CapBeamM,
            MaxDraft = mission.CapDraftM,
            MaxDisplacement = null  // Not in MissionCase model
        };

        var scaledList = new List<(ScaledCandidate, SimilarVesselDto)>();

        foreach (var similar in similarVessels)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Convert DTO to model for scaling
            var referenceVessel = ConvertToVesselModel(similar);

            var scaled = _scalingService.ScaleToTarget(
                referenceVessel,
                targetDisplacement,
                constraints);

            if (scaled.IsValid)
            {
                // Attach similarity score from KNN
                scaled.SimilarityScore = similar.SimilarityScore;
                scaledList.Add((scaled, similar));
            }
            else
            {
                _logger.LogDebug(
                    "Scaled vessel {VesselId} invalid (Distortion: {Distortion:P2}). Skipping.",
                    similar.VesselName, scaled.Distortion);
            }
        }

        return scaledList;
    }

    /// <summary>
    /// Refine scaled candidates using First-Principles solver
    /// Uses scaled dimensions as initial guess for faster convergence
    /// </summary>
    private async Task<List<SolverCandidate>> RefineWithPhysicsAsync(
        List<(ScaledCandidate Scaled, SimilarVesselDto Source)> scaledCandidates,
        MissionCase mission,
        SolverRequest originalRequest,
        CancellationToken cancellationToken)
    {
        var refined = new List<SolverCandidate>();

        foreach (var (scaled, source) in scaledCandidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Create solver request (reuse original locks/options)
                var solverRequest = new SolverRequest(
                    MissionCase: mission,
                    Locks: originalRequest.Locks,
                    Options: originalRequest.Options
                );

                // Run first-principles solver
                // NOTE: In future, pass scaled dimensions as initial guess to speed up convergence
                var (solverCandidates, _) = await _firstPrinciplesSolver.SolveAsync(
                    solverRequest,
                    cancellationToken);

                // Take the best candidate from refinement
                var best = solverCandidates.OrderByDescending(c => c.Score).FirstOrDefault();

                if (best != null)
                {
                    // Attach provenance metadata via flags
                    var flags = best.Flags.ToList();
                    flags.Add($"ReferenceVessel:{source.VesselName}");
                    flags.Add($"Similarity:{scaled.SimilarityScore:P0}");
                    flags.Add("DataDrivenRealWorld");

                    var enhanced = best with { Flags = flags };
                    refined.Add(enhanced);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to refine candidate from {VesselName}. Skipping.",
                    source.VesselName);
            }
        }

        return refined;
    }

    /// <summary>
    /// Fallback to standard First-Principles solver
    /// </summary>
    private async Task<List<SolverCandidate>> FallbackToFirstPrinciplesAsync(
        SolverRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing First-Principles fallback...");

        var (solverCandidates, _) = await _firstPrinciplesSolver.SolveAsync(request, cancellationToken);

        // Mark as fallback in flags
        var marked = solverCandidates.Select(c =>
        {
            var flags = c.Flags.ToList();
            flags.Add("FirstPrinciples_Fallback");
            return c with { Flags = flags };
        }).ToList();

        return marked;
    }

    /// <summary>
    /// Calculate target displacement from mission requirements
    /// </summary>
    private decimal CalculateTargetDisplacement(MissionCase mission)
    {
        // Convert cargo to displacement
        // This logic should match FirstPrinciplesSolver's approach

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

        return (cargoMass / (1.0m - lightshipRatio)) * margin;
    }

    /// <summary>
    /// Convert knots to m/s
    /// </summary>
    private decimal ConvertKnotsToMs(decimal knots)
    {
        return knots * 0.514444m;
    }

    /// <summary>
    /// Convert DTO to model for scaling service
    /// </summary>
    private Shared.Models.CatalogVesselReal ConvertToVesselModel(SimilarVesselDto dto)
    {
        return new Shared.Models.CatalogVesselReal
        {
            Id = dto.VesselId,
            VesselId = dto.VesselName,
            VesselType = dto.VesselType,
            LppM = dto.LppM,
            BeamM = dto.BeamM,
            DraftM = dto.DraftM,
            DepthM = dto.DepthM,
            DisplacementT = dto.DisplacementT,
            Cb = dto.Cb,
            Cp = dto.Cp,
            Cm = dto.Cm,
            Cw = dto.Cw,
            ServiceSpeedMs = dto.ServiceSpeedMs
        };
    }
}
