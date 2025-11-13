using HullSizingService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Models.Sizing;
using System.Linq;

namespace HullSizingService.Services.Solver;

/// <summary>
/// First-principles hull sizing solver - orchestrates all solver components
/// Generates multiple candidate hulls and ranks them by multi-objective scoring
/// </summary>
public class FirstPrinciplesSolver : IFirstPrinciplesSolver
{
    private readonly IHullFamilyService _familyService;
    private readonly IDisplacementClosureService _closureService;
    private readonly IResistanceService _resistanceService;
    private readonly IStabilityScreenService _stabilityService;
    private readonly IWaterPropertiesService _waterService;
    private readonly SizingDbContext _context;
    private readonly ILogger<FirstPrinciplesSolver> _logger;

    private const decimal KNOTS_TO_MS = 0.5144m;
    private const decimal G = 9.81m;

    public FirstPrinciplesSolver(
        IHullFamilyService familyService,
        IDisplacementClosureService closureService,
        IResistanceService resistanceService,
        IStabilityScreenService stabilityService,
        IWaterPropertiesService waterService,
        SizingDbContext context,
        ILogger<FirstPrinciplesSolver> logger)
    {
        _familyService = familyService;
        _closureService = closureService;
        _resistanceService = resistanceService;
        _stabilityService = stabilityService;
        _waterService = waterService;
        _context = context;
        _logger = logger;
    }

    public async Task<(List<SolverCandidate> Candidates, SolverDiagnostics Diagnostics)> SolveAsync(SolverRequest request, CancellationToken cancellationToken = default)
    {
        var mission = request.MissionCase;
        var locks = request.Locks ?? new SizingLocksDto(false, false, false, false, false);
        var options = request.Options ?? new SizingOptionsDto(null, 5, null, null, null);

        // Initialize diagnostics
        var diagnostics = new SolverDiagnostics
        {
            MissionType = mission.MissionType
        };

        _logger.LogInformation("[SOLVER] Starting first-principles solve for mission {MissionId}, cargo={Cargo}, speed={Speed}kn",
            mission.Id, mission.CargoValue, mission.ServiceSpeedKn);

        // Step 1: Convert payload to target displacement
        var targetDisplacementT = await EstimateTargetDisplacementAsync(mission);
        diagnostics.TargetDisplacementT = targetDisplacementT;
        _logger.LogDebug("[SOLVER] Target displacement: {Disp:F1} tonnes", targetDisplacementT);

        // Step 2: Get water properties (cached)
        var waterProps = await _waterService.GetWaterPropertiesAsync(15.0m, 35.0m, cancellationToken);

        var families = await _familyService.GetApplicableFamiliesAsync(
            mission.MissionType,
            null,
            mission.CapDraftM,
            mission.CapBeamM,
            cancellationToken);

        diagnostics.TotalFamiliesConsidered = families.Count;
        diagnostics.FamiliesAfterFnFiltering = families.Count;

        if (families.Count == 0)
        {
            _logger.LogWarning("[SOLVER] No applicable families found for mission type '{Type}'", mission.MissionType);
            diagnostics.FailureReasons.Add("no_applicable_families");
            families = await _context.HullFamilyPresets.Where(f => f.IsActive).Take(3).ToListAsync(cancellationToken);
            diagnostics.TotalFamiliesConsidered = families.Count;
            diagnostics.FamiliesAfterFnFiltering = families.Count;
        }

        if (options.FamilyHints != null && options.FamilyHints.Any())
        {
            var hintOrder = options.FamilyHints
                .Select((hint, index) => new { hint, index })
                .ToDictionary(x => x.hint, x => x.index, StringComparer.OrdinalIgnoreCase);

            var beforeHints = families.Count;
            families = families
                .Where(f => hintOrder.ContainsKey(f.Family))
                .OrderBy(f => hintOrder[f.Family])
                .ToList();
            if (families.Count == 0)
            {
                diagnostics.FailureReasons.Add($"family_hints_filtered_all_{beforeHints}_families");
            }
        }
        else
        {
            families = families.OrderBy(f => f.Family).ToList();
        }

        diagnostics.FamiliesAfterHintsFiltering = families.Count;

        if (families.Count == 0)
        {
            _logger.LogError("[SOLVER] ⚠️ NO HULL FAMILIES AVAILABLE! Check database seeding.");
            diagnostics.GenerateSuggestions(mission);
            return (new List<SolverCandidate>(), diagnostics);
        }

        var primaryFamily = families.First();
        if (families.Count > 1)
        {
            _logger.LogInformation("[SOLVER] Multiple families matched filters; using primary family '{Family}' for variant sweep.", primaryFamily.Family);
        }

        var variantCount = Math.Max(1, options.MaxCandidates);
        diagnostics.FamiliesAfterHintsFiltering = 1;
        var variantPlan = Enumerable.Range(0, variantCount)
            .Select(index => (family: primaryFamily, variantIndex: index, variantCount: variantCount))
            .ToList();

        _logger.LogInformation("[SOLVER] Generating {VariantCount} candidate variants for family '{Family}'", variantCount, primaryFamily.Family);

        // Step 4: Generate candidates for each variant (in parallel)
        var candidateTasks = variantPlan.Select(plan => GenerateCandidateAsync(
            plan.family,
            mission,
            targetDisplacementT,
            waterProps,
            locks,
            options,
            plan.variantIndex,
            plan.variantCount,
            cancellationToken));

        var allCandidates = await Task.WhenAll(candidateTasks);
        var nullCount = allCandidates.Count(c => c == null);

        diagnostics.FamiliesFailedClosure = nullCount;

        if (nullCount > 0)
        {
            _logger.LogWarning("[SOLVER] {NullCount} of {TotalCount} candidates failed to generate (displacement closure failed)",
                nullCount, allCandidates.Length);
        }

        var candidates = allCandidates
            .Where(c => c != null)
            .Cast<SolverCandidate>()
            .ToList();

        // Collect failure flags from successful candidates to understand constraints
        foreach (var candidate in candidates)
        {
            foreach (var flag in candidate.Flags)
            {
                if (!diagnostics.FailureReasons.Contains(flag))
                {
                    diagnostics.FailureReasons.Add(flag);
                }
            }
        }

        // Step 5: Score and rank candidates
        var scoredCandidates = await ScoreAndRankCandidatesAsync(candidates, mission, cancellationToken);

        _logger.LogInformation("[SOLVER] Generated {Count} valid candidates out of {TotalFamilies} families",
            scoredCandidates.Count, families.Count);

        // Generate diagnostics and suggestions if no candidates
        if (scoredCandidates.Count == 0)
        {
            // Estimate Froude number for diagnostics
            if (targetDisplacementT > 0)
            {
                var estimatedLength = (decimal)Math.Pow((double)targetDisplacementT * 0.65, 1.0 / 3.0) * 7.0m; // Rough estimate
                var speedMs = mission.ServiceSpeedKn * 0.5144m;
                diagnostics.EstimatedFroudeNumber = speedMs / (decimal)Math.Sqrt((double)estimatedLength * 9.81);
            }

            diagnostics.GenerateSuggestions(mission);
            _logger.LogWarning("[SOLVER] No candidates generated. Diagnostics: {Summary}", diagnostics.GetSummary());
        }

        return (scoredCandidates, diagnostics);
    }

    private async Task<SolverCandidate?> GenerateCandidateAsync(
        HullFamilyPreset family,
        MissionCase mission,
        decimal targetDisplacementT,
        Services.Integration.WaterPropertiesResponse waterProps,
        SizingLocksDto locks,
        SizingOptionsDto options,
        int variantIndex,
        int variantCount,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("[SOLVER] Generating candidate variant {Variant}/{VariantCount} for family '{Family}'", variantIndex + 1, variantCount, family.Family);

            // Step 1: Pick Froude number from family band, biasing by variant index
            var variantFraction = variantCount <= 1
                ? 0.5m
                : (variantIndex + 1m) / (variantCount + 1m); // Distribute variants through range

            var fnTarget = PickFroudeNumber(family, mission.ServiceSpeedKn, options, variantFraction);

            // Step 2: Interpolate geometric ratios within family bounds
            static decimal Interpolate(decimal min, decimal max, decimal fraction)
            {
                if (max <= min)
                {
                    return min;
                }
                return min + (max - min) * fraction;
            }

            var lOverBFraction = variantFraction;
            var bOverTFraction = variantCount <= 1 ? 0.5m : Math.Clamp(variantFraction + 0.15m, 0m, 1m);
            var dOverTFraction = variantCount <= 1 ? 0.5m : Math.Clamp(variantFraction - 0.15m, 0m, 1m);
            var cbFraction = variantFraction;

            var lOverB = Interpolate(family.LOverBMin, family.LOverBMax, lOverBFraction);
            var bOverT = Interpolate(family.BOverTMin, family.BOverTMax, bOverTFraction);
            var dOverT = Interpolate(family.DOverTMin, family.DOverTMax, dOverTFraction);
            var cb = Interpolate(family.CbMin, family.CbMax, cbFraction);

            // Step 3: Displacement closure
            var closureRequest = new ClosureRequest(
                TargetDisplacementT: targetDisplacementT,
                WaterDensityKgM3: waterProps.DensityKgM3,
                FroudeNumber: fnTarget,
                LOverB: lOverB,
                BOverT: bOverT,
                DOverT: dOverT,
                Cb: cb,
                MaxBeamM: mission.CapBeamM,
                MaxDraftM: mission.CapDraftM,
                MaxLoaM: mission.CapLoaM,
                KeepLOverB: locks.KeepLOverB,
                KeepBOverT: locks.KeepBOverT,
                KeepCb: locks.KeepCbBand,
                LOverBMin: family.LOverBMin,
                LOverBMax: family.LOverBMax,
                BOverTMin: family.BOverTMin,
                BOverTMax: family.BOverTMax,
                CbMin: family.CbMin,
                CbMax: family.CbMax
            );

            var closure = await _closureService.SolveAsync(closureRequest, cancellationToken);

            if (!closure.Converged)
            {
                var failureReason = closure.Flags.Any() ? string.Join(", ", closure.Flags) : "no_convergence";
                _logger.LogWarning("[SOLVER] Family '{Family}' failed to converge. Reason: {Reason}. Target disp={TargetDisp:F1}t, Fn={Fn:F3}, L/B={LOverB:F2}, B/T={BOverT:F2}, Cb={Cb:F3}",
                    family.Family, failureReason, targetDisplacementT, fnTarget, lOverB, bOverT, cb);
                return null; // Skip this family (diagnostics tracked at caller)
            }

            // Step 4: Calculate derived dimensions
            var lwl = closure.LppM * 1.02m; // Lwl ≈ 1.02 * Lpp for typical ships
            var loa = closure.LppM * 1.05m; // LOA ≈ 1.05 * Lpp (bow/stern overhangs)
            var cp = family.CpMin.HasValue && family.CpMax.HasValue
                ? (family.CpMin.Value + family.CpMax.Value) / 2.0m
                : closure.Cb * 1.02m; // Cp ≈ Cb * 1.02
            var cwp = family.CwpMin.HasValue && family.CwpMax.HasValue
                ? (family.CwpMin.Value + family.CwpMax.Value) / 2.0m
                : 0.85m; // Default waterplane coefficient
            var cm = closure.Cb / cp; // Midship coefficient

            // Step 5: Stability screening
            var stabilityRequest = new StabilityRequest(
                LwlM: lwl,
                BeamM: closure.BeamM,
                DraftM: closure.DraftM,
                DepthM: closure.DepthM,
                Cb: closure.Cb,
                Cwp: cwp,
                VesselType: family.Family,
                DisplacementT: closure.DisplacementT
            );

            var stability = await _stabilityService.ScreenAsync(stabilityRequest, cancellationToken);

            // Step 6: Resistance calculation
            var resistanceRequest = new ResistanceRequest(
                LppM: closure.LppM,
                LwlM: lwl,
                BeamM: closure.BeamM,
                DraftM: closure.DraftM,
                Cb: closure.Cb,
                Cp: cp,
                Cwp: cwp,
                Cm: cm,
                SpeedKn: mission.ServiceSpeedKn,
                WaterDensityKgM3: waterProps.DensityKgM3,
                KinematicViscosityM2S: waterProps.KinematicViscosityM2S
            );

            var resistance = await _resistanceService.CalculateAsync(resistanceRequest, cancellationToken);

            // Step 7: Calculate wavelength and Lwl/λ ratio
            var speedMs = mission.ServiceSpeedKn * KNOTS_TO_MS;
            var wavelength = 2.0m * (decimal)Math.PI * speedMs * speedMs / G;
            var lwlOverLambda = lwl / wavelength;

            // Step 8: Combine all flags
            var allFlags = new List<string>();
            allFlags.AddRange(closure.Flags);
            allFlags.AddRange(stability.Flags);

            // Step 9: Create candidate
            var candidate = new SolverCandidate(
                HullFamily: family.Family,
                LppM: closure.LppM,
                LwlM: lwl,
                LoaM: loa,
                BeamM: closure.BeamM,
                DraftM: closure.DraftM,
                DepthM: closure.DepthM,
                Cb: closure.Cb,
                Cp: cp,
                Cwp: cwp,
                Cm: cm,
                DisplacementT: closure.DisplacementT,
                Fn: resistance.Fn,
                LwlOverLambda: lwlOverLambda,
                KbM: stability.KbM,
                LcbPctLpp: -2.5m, // Simplified: assume LCB at -2.5% Lpp (typical for displacement hulls)
                GmEstM: stability.GmEstM,
                EhpKw: resistance.EhpKw,
                ShpKw: resistance.ShpKw,
                Score: 0, // Will be calculated in scoring phase
                Flags: allFlags
            );

            return candidate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SOLVER] Failed to generate candidate for family '{Family}'", family.Family);
            return null;
        }
    }

    private decimal PickFroudeNumber(
        HullFamilyPreset family,
        decimal serviceSpeedKn,
        SizingOptionsDto options,
        decimal variantFraction)
    {
        var fnMin = options.MinFn ?? family.FnMin ?? 0.15m;
        var fnMax = options.MaxFn ?? family.FnMax ?? 0.30m;

        var speedFactor = Math.Clamp(serviceSpeedKn / 25.0m, 0.0m, 1.0m);
        var fnBlend = Math.Clamp((speedFactor + variantFraction) / 2.0m, 0.0m, 1.0m);

        return fnMin + (fnMax - fnMin) * fnBlend;
    }

    private async Task<decimal> EstimateTargetDisplacementAsync(MissionCase mission)
    {
        decimal payloadT = 0;

        // Convert cargo to mass (tonnes)
        switch (mission.CargoBasis.ToLower())
        {
            case "weight":
                payloadT = mission.CargoValue ?? 0;
                break;

            case "volume":
                var density = mission.CargoDensityTPerM3 ?? 0.5m; // Default to 0.5 t/m³
                var volume = mission.CargoVolumeM3 ?? mission.CargoValue ?? 0;
                payloadT = volume * density;
                break;

            case "teu":
                // TEU to weight estimation
                // Typical laden container: ~14 tonnes average
                var teuCount = mission.TeuCount ?? (int)(mission.CargoValue ?? 0);
                payloadT = teuCount * 14.0m;
                break;
        }

        // Estimate total displacement from DWT
        // DWT ≈ payload + stores + fuel
        // Δ ≈ DWT / (DWT/Δ ratio)
        // Typical DWT/Δ ratios:
        // - Container: 0.70
        // - Tanker: 0.85
        // - Bulker: 0.80
        // - General cargo: 0.65
        // - Fishing: 0.50

        var dwtToDispRatio = mission.MissionType.ToLower() switch
        {
            "commercial" when mission.CargoBasis == "teu" => 0.70m, // Container
            "commercial" when payloadT > 100000 => 0.85m, // Tanker (large cargo)
            "commercial" => 0.75m, // General commercial
            _ => 0.65m // Default
        };

        var dwt = payloadT * 1.15m; // Add 15% for stores, fuel, provisions
        var targetDisplacement = dwt / dwtToDispRatio;

        _logger.LogDebug("[SOLVER] Payload={Payload:F1}t, DWT={Dwt:F1}t, Target Δ={Disp:F1}t (ratio={Ratio:F2})",
            payloadT, dwt, targetDisplacement, dwtToDispRatio);

        return await Task.FromResult(targetDisplacement);
    }

    private async Task<List<SolverCandidate>> ScoreAndRankCandidatesAsync(
        List<SolverCandidate> candidates,
        MissionCase mission,
        CancellationToken cancellationToken)
    {
        // Get KPI weights from database (system defaults). The table may contain multiple rows per metric if
        // we have reseeded defaults in development. Collapse duplicates by taking the most recently inserted value.
        var weightRecords = await _context.KpiWeights
            .Where(w => w.UserId == null) // System defaults
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var weights = weightRecords
            .GroupBy(w => w.Metric.ToLowerInvariant())
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(w => w.Id).First().Weight);

        // Default weights if not found
        var deltaWeight = weights.GetValueOrDefault("delta_balance", 0.35m);
        var powerWeight = weights.GetValueOrDefault("installed_power", 0.25m);
        var constraintsWeight = weights.GetValueOrDefault("constraints_ok", 0.20m);
        var stabilityWeight = weights.GetValueOrDefault("stability_screen", 0.10m);
        var volumeWeight = weights.GetValueOrDefault("teu_or_volume_fit", 0.10m);

        var maxShp = candidates.Max(c => c.ShpKw) ?? 1.0m;
        var minShp = candidates.Min(c => c.ShpKw) ?? 0.0m;

        var scoredCandidates = new List<SolverCandidate>();

        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            var score = 0.0m;

            // 1. Displacement accuracy (no error = 1.0, ±10% error = 0.0)
            var cargoValue = mission.CargoValue ?? 1.0m;
            var deltaError = Math.Abs(candidate.DisplacementT - cargoValue) / cargoValue;
            var deltaScore = Math.Max(0, 1.0m - deltaError * 10.0m);
            score += deltaScore * deltaWeight;

            // 2. Installed power (lower is better, normalized)
            var powerScore = maxShp > minShp
                ? 1.0m - (candidate.ShpKw.GetValueOrDefault(maxShp) - minShp) / (maxShp - minShp)
                : 1.0m;
            score += powerScore * powerWeight;

            // 3. Constraints (no flags = 1.0, each flag = -0.2)
            var constraintFlags = candidate.Flags.Count(f => f.Contains("constrained") || f.Contains("exceeded"));
            var constraintScore = Math.Max(0, 1.0m - constraintFlags * 0.2m);
            score += constraintScore * constraintsWeight;

            // 4. Stability (GMt in range 1-3m = 1.0, outside = reduced)
            var gm = candidate.GmEstM ?? 1.5m;
            var stabilityScore = gm >= 1.0m && gm <= 3.0m ? 1.0m : 0.5m;
            score += stabilityScore * stabilityWeight;

            // 5. Volume/TEU fit (placeholder for MVP)
            score += 0.8m * volumeWeight;

            // Create new candidate with score
            scoredCandidates.Add(candidate with { Score = score });
        }

        // Rank by score (descending)
        var ranked = scoredCandidates
            .OrderByDescending(c => c.Score)
            .ToList();

        return ranked;
    }
}
