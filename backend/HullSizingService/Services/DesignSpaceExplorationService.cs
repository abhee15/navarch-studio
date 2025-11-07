using System.Diagnostics;
using System.Text.Json;
using HullSizingService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Sizing;
using Shared.Models.Sizing;

namespace HullSizingService.Services;

/// <summary>
/// Implementation of design space exploration service
/// Generates multiple design variants by sweeping parameter ranges
/// </summary>
public class DesignSpaceExplorationService : IDesignSpaceExplorationService
{
    private readonly SizingDbContext _context;
    private readonly Solver.IFirstPrinciplesSolver _solver;
    private readonly ILogger<DesignSpaceExplorationService> _logger;

    public DesignSpaceExplorationService(
        SizingDbContext context,
        Solver.IFirstPrinciplesSolver solver,
        ILogger<DesignSpaceExplorationService> logger)
    {
        _context = context;
        _solver = solver;
        _logger = logger;
    }

    public async Task<DesignSpaceExplorationResponse> ExploreAsync(
        DesignSpaceExplorationRequest request,
        Guid userId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "[EXPLORATION] Starting design space exploration for mission {MissionId}",
            request.MissionCaseId);

        // Get base mission case
        var baseMission = await _context.MissionCases
            .Where(mc => mc.Id == request.MissionCaseId && mc.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (baseMission == null)
        {
            throw new InvalidOperationException($"Mission case {request.MissionCaseId} not found");
        }

        // Generate parameter combinations
        var combinations = GenerateParameterCombinations(request.Ranges);

        if (combinations.Count > request.MaxVariants)
        {
            throw new InvalidOperationException(
                $"Too many variants requested: {combinations.Count}. Maximum allowed: {request.MaxVariants}");
        }

        _logger.LogInformation(
            "[EXPLORATION] Generated {Count} parameter combinations",
            combinations.Count);

        // Create batch ID for tracking
        var batchId = Guid.NewGuid();
        var runIds = new List<Guid>();

        // Generate variants (process in parallel batches to avoid overwhelming the system)
        const int batchSize = 5;
        var failed = 0;

        for (int i = 0; i < combinations.Count; i += batchSize)
        {
            var batch = combinations.Skip(i).Take(batchSize).ToList();
            var batchTasks = batch.Select(combo =>
                GenerateVariantAsync(
                    baseMission,
                    combo,
                    batchId,
                    request.Mode,
                    request.HullFamily,
                    cancellationToken));

            var batchResults = await Task.WhenAll(batchTasks);

            foreach (var result in batchResults)
            {
                if (result != null)
                {
                    runIds.Add(result.Value);
                }
                else
                {
                    failed++;
                }
            }
        }

        sw.Stop();

        _logger.LogInformation(
            "[EXPLORATION] Completed exploration batch {BatchId}: {Success} successful, {Failed} failed, {Time}ms",
            batchId, runIds.Count, failed, sw.ElapsedMilliseconds);

        return new DesignSpaceExplorationResponse
        {
            BatchId = batchId,
            SizingRunIds = runIds,
            TotalVariants = combinations.Count,
            Status = failed > 0 ? "completed_with_errors" : "completed",
            ComputeTimeMs = (int)sw.ElapsedMilliseconds
        };
    }

    public async Task<ExplorationResultsSummary> GetExplorationResultsAsync(
        Guid batchId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[EXPLORATION] Fetching results for batch {BatchId}", batchId);

        // Find all sizing runs with this batch ID in their options
        var runs = await _context.SizingRuns
            .Include(sr => sr.MissionCase)
            .Where(sr =>
                sr.MissionCase.TenantId == tenantId &&
                sr.OptionsJson != null &&
                sr.OptionsJson.Contains(batchId.ToString()))
            .ToListAsync(cancellationToken);

        if (runs.Count == 0)
        {
            _logger.LogWarning("[EXPLORATION] No runs found for batch {BatchId}", batchId);
            return new ExplorationResultsSummary
            {
                BatchId = batchId,
                Status = "not_found"
            };
        }

        // Get all candidates from these runs
        var runIds = runs.Select(r => r.Id).ToList();
        var candidates = await _context.CandidateDesigns
            .Where(cd => runIds.Contains(cd.SizingRunId))
            .OrderBy(cd => cd.Rank)
            .ToListAsync(cancellationToken);

        var completed = runs.Count(r => r.Status == "completed");
        var failed = runs.Count(r => r.Status == "failed");

        // Convert to DTOs
        var candidateDtos = candidates.Select(MapToDto).ToList();

        // Calculate Pareto front if we have enough data
        ParetoAnalysis? paretoAnalysis = null;
        if (candidateDtos.Count > 1)
        {
            paretoAnalysis = CalculateParetoFront(candidateDtos);
        }

        return new ExplorationResultsSummary
        {
            BatchId = batchId,
            TotalVariants = runs.Count,
            CompletedVariants = completed,
            FailedVariants = failed,
            Status = completed == runs.Count ? "completed" : "running",
            Candidates = candidateDtos,
            ParetoAnalysis = paretoAnalysis
        };
    }

    /// <summary>
    /// Generates all parameter combinations from ranges
    /// </summary>
    private List<ParameterCombination> GenerateParameterCombinations(ParameterRanges ranges)
    {
        var combinations = new List<ParameterCombination>();

        // Generate Lpp values
        var lppValues = GenerateRange(
            ranges.LppMinM,
            ranges.LppMaxM,
            ranges.LppSteps);

        // Generate Beam values
        var beamValues = GenerateRange(
            ranges.BeamMinM,
            ranges.BeamMaxM,
            ranges.BeamSteps);

        // Generate Draft values
        var draftValues = GenerateRange(
            ranges.DraftMinM,
            ranges.DraftMaxM,
            ranges.DraftSteps);

        // Generate Speed values
        var speedValues = GenerateRange(
            ranges.SpeedMinKn,
            ranges.SpeedMaxKn,
            ranges.SpeedSteps);

        // Generate Cb values
        var cbValues = GenerateRange(
            ranges.CbMin,
            ranges.CbMax,
            ranges.CbSteps);

        // Create all combinations
        foreach (var lpp in lppValues)
        {
            foreach (var beam in beamValues)
            {
                foreach (var draft in draftValues)
                {
                    foreach (var speed in speedValues)
                    {
                        foreach (var cb in cbValues)
                        {
                            combinations.Add(new ParameterCombination
                            {
                                LppM = lpp,
                                BeamM = beam,
                                DraftM = draft,
                                SpeedKn = speed,
                                Cb = cb
                            });
                        }
                    }
                }
            }
        }

        return combinations;
    }

    /// <summary>
    /// Generates evenly spaced values in a range
    /// </summary>
    private List<decimal?> GenerateRange(decimal? min, decimal? max, int steps)
    {
        if (min == null || max == null || steps <= 0)
        {
            return new List<decimal?> { null };
        }

        if (steps == 1)
        {
            return new List<decimal?> { (min + max) / 2 };
        }

        var values = new List<decimal?>();
        var step = (max.Value - min.Value) / (steps - 1);

        for (int i = 0; i < steps; i++)
        {
            values.Add(min.Value + (step * i));
        }

        return values;
    }

    /// <summary>
    /// Generates a single variant by running the solver with modified mission parameters
    /// </summary>
    private async Task<Guid?> GenerateVariantAsync(
        MissionCase baseMission,
        ParameterCombination combo,
        Guid batchId,
        string mode,
        string? hullFamily,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create a temporary mission case with modified constraints
            var variantMission = new MissionCase
            {
                Id = baseMission.Id, // Keep same ID to reference base mission
                UserId = baseMission.UserId,
                TenantId = baseMission.TenantId,
                Name = baseMission.Name,
                MissionCategory = baseMission.MissionCategory,
                MissionType = baseMission.MissionType,
                CargoBasis = baseMission.CargoBasis,
                CargoValue = baseMission.CargoValue,
                CargoVolumeM3 = baseMission.CargoVolumeM3,
                CargoDensityTPerM3 = baseMission.CargoDensityTPerM3,
                TeuCount = baseMission.TeuCount,
                ServiceSpeedKn = combo.SpeedKn ?? baseMission.ServiceSpeedKn,
                SeaMarginPct = baseMission.SeaMarginPct,
                ServiceMarginPct = baseMission.ServiceMarginPct,
                EnvHsM = baseMission.EnvHsM,
                EnvTzS = baseMission.EnvTzS,

                // Set tight constraints to force specific dimensions
                CapLoaM = combo.LppM != null ? combo.LppM * 1.05m : baseMission.CapLoaM,
                CapBeamM = combo.BeamM ?? baseMission.CapBeamM,
                CapDraftM = combo.DraftM ?? baseMission.CapDraftM,

                CapAirdraftM = baseMission.CapAirdraftM,
                EnduranceNm = baseMission.EnduranceNm,
                Notes = baseMission.Notes,
                CreatedAt = baseMission.CreatedAt,
                UpdatedAt = baseMission.UpdatedAt,
                DeletedAt = baseMission.DeletedAt
            };

            // Create solver request with exploration metadata
            var options = new Solver.SizingOptionsDto(
                FamilyHints: hullFamily != null ? new List<string> { hullFamily } : null,
                MaxCandidates: 1, // Only need the best candidate
                MinFn: null,
                MaxFn: null
            );

            var solverRequest = new Solver.SolverRequest(
                MissionCase: variantMission,
                Locks: null,
                Options: options
            );

            // Run solver
            var candidates = await _solver.SolveAsync(solverRequest, cancellationToken);

            if (candidates.Count == 0)
            {
                _logger.LogWarning(
                    "[EXPLORATION] No candidates generated for Lpp={Lpp}, Beam={Beam}",
                    combo.LppM, combo.BeamM);
                return null;
            }

            // Create sizing run record
            var run = new SizingRun
            {
                Id = Guid.NewGuid(),
                MissionCaseId = baseMission.Id,
                Mode = mode,
                LocksJson = null,
                OptionsJson = JsonSerializer.Serialize(new
                {
                    batchId = batchId,
                    exploration = true,
                    parameters = combo
                }),
                Status = "completed",
                ComputeTimeMs = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.SizingRuns.Add(run);

            // Add candidate
            var candidate = candidates[0];
            var candidateEntity = new CandidateDesign
            {
                Id = Guid.NewGuid(),
                SizingRunId = run.Id,
                HullFamily = candidate.HullFamily,
                LppM = candidate.LppM,
                LwlM = candidate.LwlM,
                LoaM = candidate.LoaM,
                BM = candidate.BeamM,
                TM = candidate.DraftM,
                DM = candidate.DepthM,
                Cb = candidate.Cb,
                Cp = candidate.Cp,
                Cwp = candidate.Cwp,
                Cm = candidate.Cm,
                DisplacementT = candidate.DisplacementT,
                Fn = candidate.Fn,
                LwlOverLambda = candidate.LwlOverLambda,
                KbM = candidate.KbM,
                LcbPctLpp = candidate.LcbPctLpp,
                GmEstM = candidate.GmEstM,
                EhpKw = candidate.EhpKw,
                ShpKw = candidate.ShpKw,
                FlagsJson = JsonSerializer.Serialize(candidate.Flags),
                Score = candidate.Score,
                Rank = 1,
                IsSelected = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.CandidateDesigns.Add(candidateEntity);
            await _context.SaveChangesAsync(cancellationToken);

            return run.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[EXPLORATION] Failed to generate variant for Lpp={Lpp}, Beam={Beam}",
                combo.LppM, combo.BeamM);
            return null;
        }
    }

    /// <summary>
    /// Calculates Pareto front for multi-objective optimization
    /// Currently uses Displacement and Power as objectives (minimize both)
    /// </summary>
    private ParetoAnalysis CalculateParetoFront(List<CandidateDesignDto> candidates)
    {
        var paretoFront = new List<Guid>();

        // For each candidate, check if it's dominated by any other
        foreach (var candidate in candidates)
        {
            bool isDominated = false;

            foreach (var other in candidates)
            {
                if (other.Id == candidate.Id) continue;

                // Check if 'other' dominates 'candidate'
                // Domination: other is better or equal in all objectives, and strictly better in at least one
                bool betterOrEqualDisp = other.DispT <= candidate.DispT;
                bool betterOrEqualPower = other.EhpKw <= candidate.EhpKw;
                bool strictlyBetterInOne = (other.DispT < candidate.DispT) || (other.EhpKw < candidate.EhpKw);

                if (betterOrEqualDisp && betterOrEqualPower && strictlyBetterInOne)
                {
                    isDominated = true;
                    break;
                }
            }

            if (!isDominated)
            {
                paretoFront.Add(candidate.Id);
            }
        }

        // Calculate statistics
        var stats = new Dictionary<string, ObjectiveStats>
        {
            ["displacement"] = CalculateStats(candidates.Select(c => c.DispT).ToList()),
            ["power"] = CalculateStats(candidates.Select(c => c.EhpKw ?? 0).ToList()),
            ["length"] = CalculateStats(candidates.Select(c => c.LppM).ToList()),
            ["beam"] = CalculateStats(candidates.Select(c => c.BeamM).ToList())
        };

        return new ParetoAnalysis
        {
            ParetoFrontIds = paretoFront,
            ObjectiveStatistics = stats
        };
    }

    /// <summary>
    /// Calculates statistics for a set of values
    /// </summary>
    private ObjectiveStats CalculateStats(List<decimal> values)
    {
        if (values.Count == 0)
        {
            return new ObjectiveStats();
        }

        var mean = values.Average();
        var variance = values.Sum(v => (v - mean) * (v - mean)) / values.Count;
        var stdDev = (decimal)Math.Sqrt((double)variance);

        return new ObjectiveStats
        {
            Min = values.Min(),
            Max = values.Max(),
            Mean = mean,
            StdDev = stdDev
        };
    }

    /// <summary>
    /// Maps CandidateDesign entity to DTO
    /// </summary>
    private static CandidateDesignDto MapToDto(CandidateDesign entity)
    {
        return new CandidateDesignDto
        {
            Id = entity.Id,
            SizingRunId = entity.SizingRunId,
            HullFamily = entity.HullFamily,
            LppM = entity.LppM,
            LwlM = entity.LwlM,
            LoaM = entity.LoaM,
            BeamM = entity.BM,
            DraftM = entity.TM,
            DepthM = entity.DM,
            Cb = entity.Cb,
            Cp = entity.Cp,
            Cwp = entity.Cwp,
            DispM3 = entity.DisplacementT / 1.025m,
            DispT = entity.DisplacementT,
            Fn = entity.Fn,
            LwlOverLambda = entity.LwlOverLambda,
            KbM = entity.KbM,
            LcbPctLpp = entity.LcbPctLpp,
            GmEstM = entity.GmEstM,
            EhpKw = entity.EhpKw,
            ShpKw = entity.ShpKw,
            FlagsJson = entity.FlagsJson,
            Score = entity.Score,
            Rank = entity.Rank,
            IsSelected = entity.IsSelected,
            CreatedAt = entity.CreatedAt,
            ReferenceVesselId = entity.ReferenceVesselId,
            ReferenceVesselName = entity.ReferenceVesselName,
            SimilarityScore = entity.SimilarityScore,
            SolverMode = entity.SolverMode
        };
    }
}

/// <summary>
/// Internal record for parameter combinations
/// </summary>
internal record ParameterCombination
{
    public decimal? LppM { get; init; }
    public decimal? BeamM { get; init; }
    public decimal? DraftM { get; init; }
    public decimal? SpeedKn { get; init; }
    public decimal? Cb { get; init; }
}
