using System.Linq;
using HullSizingService.Data;
using HullSizingService.Services.Validation;
using Microsoft.EntityFrameworkCore;
using Shared.Models.Sizing;

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
    private readonly IConstraintFeasibilityValidator? _feasibilityValidator;
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
        ILogger<FirstPrinciplesSolver> logger,
        IConstraintFeasibilityValidator? feasibilityValidator = null)
    {
        _familyService = familyService;
        _closureService = closureService;
        _resistanceService = resistanceService;
        _stabilityService = stabilityService;
        _waterService = waterService;
        _feasibilityValidator = feasibilityValidator;
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

        // Step 0: Pre-flight constraint feasibility check
        if (_feasibilityValidator != null && (mission.CapLoaM.HasValue || mission.CapBeamM.HasValue || mission.CapDraftM.HasValue))
        {
            var feasibilityResult = await _feasibilityValidator.CheckAsync(mission, cancellationToken);

            if (!feasibilityResult.IsFeasible)
            {
                _logger.LogWarning("[SOLVER] ⚠️ Pre-flight check FAILED for mission {MissionId}. Errors: {Errors}",
                    mission.Id, string.Join("; ", feasibilityResult.Errors));

                diagnostics.FailureReasons.AddRange(feasibilityResult.Errors);

                // Return empty results with helpful error messages
                return (new List<SolverCandidate>(), diagnostics);
            }

            if (feasibilityResult.Warnings.Count > 0)
            {
                _logger.LogInformation("[SOLVER] ℹ️ Pre-flight check passed with warnings for mission {MissionId}. Warnings: {Warnings}",
                    mission.Id, string.Join("; ", feasibilityResult.Warnings));
            }
        }

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
            // Use deterministic ordering to ensure consistent results across runs
            families = await _context.HullFamilyPresets
                .Where(f => f.IsActive)
                .OrderBy(f => f.Family)
                .Take(3)
                .ToListAsync(cancellationToken);
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

            // Smart diagnostics: Detect failure patterns to help user understand why
            if (nullCount >= 3 && allCandidates.Length >= 3)
            {
                // Check if failures are concentrated in high/low Cb range
                // This helps diagnose "constraints too tight" vs "physics impossible"
                _logger.LogWarning(
                    "[SOLVER] ⚠️ SMART DIAGNOSTIC: {FailurePercent:F0}% of variants failed. Possible causes: constraints too restrictive (check beam/draft), insufficient displacement for cargo, or extreme Froude number range.",
                    (decimal)nullCount / allCandidates.Length * 100m);

                diagnostics.FailureReasons.Add($"high_failure_rate_{nullCount}_of_{allCandidates.Length}");
            }
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

            // Validate Cp is valid (should be > 0 and typically < 1.0 for most vessels)
            if (cp <= 0 || cp > 1.5m)
            {
                _logger.LogWarning(
                    "[SOLVER] Invalid Cp={Cp:F4} calculated for family '{Family}'. Clamping to reasonable bounds.",
                    cp, family.Family);
                cp = Math.Clamp(cp, 0.4m, 1.0m); // Typical range for commercial vessels
            }

            var cwp = family.CwpMin.HasValue && family.CwpMax.HasValue
                ? (family.CwpMin.Value + family.CwpMax.Value) / 2.0m
                : 0.85m; // Default waterplane coefficient

            // Validate Cwp
            if (cwp <= 0 || cwp > 1.0m)
            {
                _logger.LogWarning(
                    "[SOLVER] Invalid Cwp={Cwp:F4} calculated for family '{Family}'. Using default 0.85.",
                    cwp, family.Family);
                cwp = 0.85m;
            }

            // Calculate Cm with division-by-zero protection
            var cm = cp > 0 ? closure.Cb / cp : closure.Cb; // Midship coefficient (fallback if Cp invalid)

            // Validate Cm is in reasonable range (typically 0.9-1.0)
            if (cm < 0.8m || cm > 1.1m)
            {
                _logger.LogWarning(
                    "[SOLVER] Unusual Cm={Cm:F4} calculated for family '{Family}'. Expected range: 0.9-1.0. This may indicate invalid Cp or Cb.",
                    cm, family.Family);
            }

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
                // Default density: 0.5 t/m³ is typical for light cargo, but should be validated
                var density = mission.CargoDensityTPerM3 ?? 0.5m;

                // Validate density is reasonable (typical range: 0.1-2.5 t/m³ for most cargo)
                if (density < 0.1m || density > 2.5m)
                {
                    _logger.LogWarning(
                        "[SOLVER] Cargo density {Density:F2} t/m³ is outside typical range [0.1, 2.5] t/m³. " +
                        "This may lead to inaccurate displacement estimates.",
                        density);

                    // Clamp to reasonable bounds
                    density = Math.Clamp(density, 0.1m, 2.5m);
                    _logger.LogInformation("[SOLVER] Clamped cargo density to {Density:F2} t/m³", density);
                }

                // Warn if using default density
                if (!mission.CargoDensityTPerM3.HasValue)
                {
                    _logger.LogInformation(
                        "[SOLVER] Using default cargo density {Density:F2} t/m³ for volume-based cargo. " +
                        "Consider specifying CargoDensityTPerM3 for more accurate results.",
                        density);
                }

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
        // Typical DWT/Δ ratios (from prefinal_1 and industry standards):
        // - Container: 0.70
        // - Product Carrier: 0.78 (from prefinal_1 document)
        // - Tanker: 0.85
        // - Bulker: 0.80
        // - General cargo: 0.65
        // - Fishing: 0.50

        // DWT/Displacement ratios are critical for accurate sizing
        // These values are based on typical vessel statistics but should be validated
        var dwtToDispRatio = mission.MissionType.ToLower() switch
        {
            "commercial" when mission.CargoBasis == "teu" => 0.70m, // Container (typical: 0.65-0.75)
            "product_carrier" => 0.78m, // Product Carrier (from prefinal_1: finalized DWT/Δ = 0.78)
            "tanker" => 0.85m, // Tanker (typical: 0.80-0.87)
            "commercial" when payloadT > 100000 => 0.85m, // Large bulk/tanker (typical: 0.80-0.87)
            "commercial" => 0.75m, // General commercial (typical: 0.70-0.80)
            _ => 0.65m // Default conservative value (typical: 0.60-0.70)
        };

        // Log the ratio used for traceability
        _logger.LogDebug(
            "[SOLVER] Using DWT/Δ ratio {Ratio:F2} for mission type '{Type}', cargo basis '{Basis}', payload {Payload:F0}t",
            dwtToDispRatio, mission.MissionType, mission.CargoBasis, payloadT);

        // Validate ratio is in reasonable range
        if (dwtToDispRatio < 0.40m || dwtToDispRatio > 0.90m)
        {
            _logger.LogWarning(
                "[SOLVER] DWT/Δ ratio {Ratio:F2} is outside typical range [0.40, 0.90]. This may indicate incorrect mission type classification.",
                dwtToDispRatio);
        }

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
        // Adjusted to include mission-type match bonus (0.12 = 12%)
        var deltaWeight = weights.GetValueOrDefault("delta_balance", 0.33m);
        var powerWeight = weights.GetValueOrDefault("installed_power", 0.23m);
        var constraintsWeight = weights.GetValueOrDefault("constraints_ok", 0.18m);
        var stabilityWeight = weights.GetValueOrDefault("stability_screen", 0.09m);
        var volumeWeight = weights.GetValueOrDefault("teu_or_volume_fit", 0.05m);
        var missionMatchWeight = weights.GetValueOrDefault("mission_type_match", 0.12m);

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

            // 6. Mission-type match bonus (NEW)
            // Give bonus for hull families that match the vessel's mission type
            var missionMatchScore = CalculateMissionTypeMatchScore(candidate.HullFamily, mission.MissionType);
            score += missionMatchScore * missionMatchWeight;

            // Create new candidate with score
            scoredCandidates.Add(candidate with { Score = score });
        }

        // Rank by score (descending)
        var ranked = scoredCandidates
            .OrderByDescending(c => c.Score)
            .ToList();

        return ranked;
    }

    /// <summary>
    /// Calculate mission-type match score for hull family selection
    /// Returns 1.0 for perfect match, 0.5 for compatible, 0.0 for mismatch
    /// </summary>
    private static decimal CalculateMissionTypeMatchScore(string hullFamily, string missionType)
    {
        // Normalize to lowercase for comparison
        var family = hullFamily.ToLowerInvariant();
        var type = missionType.ToLowerInvariant();

        // Perfect match: hull family name matches mission type
        if (family == type) return 1.0m;

        // Compatible matches based on vessel characteristics
        var compatibilityMap = new Dictionary<string, List<string>>
        {
            // Container ships work best with container family
            { "container", new List<string> { "container", "cargo", "roro" } },

            // Tankers work best with tanker family
            { "tanker", new List<string> { "tanker", "lng", "bulk" } },

            // Bulk carriers work best with bulk family
            { "bulk", new List<string> { "bulk", "cargo", "tanker" } },

            // General cargo is versatile
            { "cargo", new List<string> { "cargo", "bulk", "container", "roro" } },

            // RoRo / Car carriers
            { "roro", new List<string> { "roro", "cargo", "container" } },

            // LNG carriers
            { "lng", new List<string> { "lng", "tanker" } },

            // Offshore vessels
            { "osv", new List<string> { "osv", "fishing", "tugboat" } },
            { "offshore", new List<string> { "osv", "fishing", "tugboat" } },

            // Fishing vessels
            { "fishing", new List<string> { "fishing", "osv", "cargo" } },

            // Tugboats
            { "tugboat", new List<string> { "tugboat", "osv", "fishing" } },

            // Yachts (displacement)
            { "yacht_disp", new List<string> { "yacht_disp", "yacht_semi", "fishing" } },
            { "yacht", new List<string> { "yacht_disp", "yacht_semi", "fishing" } },

            // High-speed craft
            { "hsc", new List<string> { "hsc_planing", "hsc_semi", "ferry" } },
            { "ferry", new List<string> { "hsc_planing", "hsc_semi", "ferry", "roro" } },
            { "fast_ferry", new List<string> { "hsc_planing", "hsc_semi", "ferry" } },

            // Naval vessels
            { "naval", new List<string> { "patrol", "osv", "cargo" } },
            { "patrol", new List<string> { "patrol", "hsc_planing", "osv" } },
        };

        // Check if mission type has compatible families
        if (compatibilityMap.TryGetValue(type, out var compatibleFamilies))
        {
            // Perfect match (already checked above, but for clarity)
            if (compatibleFamilies.Count > 0 && compatibleFamilies[0] == family)
                return 1.0m;

            // Compatible match
            if (compatibleFamilies.Contains(family))
                return 0.5m;
        }

        // No match or compatibility
        return 0.0m;
    }
}
