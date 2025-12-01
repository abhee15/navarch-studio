using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using HullSizingService.Data;
using HullSizingService.Services.Geometry;
using HullSizingService.Services.Integration;
using HullSizingService.Services.ShipD;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Sizing;
using Shared.Models.Sizing;

namespace HullSizingService.Services;

public class SizingRunService : ISizingRunService
{
    private readonly SizingDbContext _context;
    private readonly Solver.IFirstPrinciplesSolver _firstPrinciplesSolver;
    private readonly DataDriven.DataDrivenRealWorldSolver? _dataDrivenSolver;
    private readonly DataDriven.DataDrivenParametricSolver? _parametricSolver;
    private readonly ILogger<SizingRunService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IShipDParameterAdapter _shipdAdapter;
    private readonly IShipDConstraintValidator _shipdValidator;
    private readonly IShipDHullGeometryService? _shipdGeometryService;
    private readonly IDataServiceClient? _dataServiceClient;
    private readonly IHullGeometryGeneratorService? _hullGeometryGenerator;
    private readonly Engineering.IWeightEstimationService _weightService;
    private readonly Geometry.IHullOptimizationService? _hullOptimizationService;

    // JSON serializer options with camelCase naming (matches API response format)
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly Dictionary<string, List<string>> VesselTypeFamilyHints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["general_cargo"] = new() { "cargo", "container", "bulk", "roro", "osv" },
        ["bulk_carrier"] = new() { "bulk", "tanker", "cargo", "lng", "barge" },
        ["container"] = new() { "container", "roro", "cargo", "ferry_conv", "ferry_fast" },
        ["fishing"] = new() { "fishing", "cargo", "osv", "patrol", "yacht_disp" },
        ["tanker"] = new() { "tanker", "bulk", "lng", "cargo", "barge" },
        ["lng_carrier"] = new() { "lng", "tanker", "cargo", "bulk", "roro" },
        ["cruise_vessel"] = new() { "ferry_conv", "ferry_fast", "roro", "container", "cargo" },
        ["passenger_vessel"] = new() { "ferry_conv", "ferry_fast", "roro", "container", "yacht_disp" },
        ["cutters"] = new() { "patrol", "ferry_fast", "osv", "cargo", "yacht_disp" },
        ["medical_ship"] = new() { "osv", "cargo", "ferry_conv", "research", "patrol" },
        ["general_military"] = new() { "patrol", "ferry_fast", "osv", "cargo", "yacht_disp" },
        ["yacht"] = new() { "yacht_disp", "ferry_fast", "patrol", "fishing", "research" },
        ["fishing_recreational"] = new() { "fishing", "yacht_disp", "patrol", "cargo", "ferry_fast" },
        ["high_speed_craft"] = new() { "ferry_fast", "patrol", "yacht_disp", "osv", "container" },
        ["research_vessel"] = new() { "research", "cargo", "osv", "patrol", "ferry_conv" }
    };

    public SizingRunService(
        SizingDbContext context,
        Solver.IFirstPrinciplesSolver firstPrinciplesSolver,
        ILogger<SizingRunService> logger,
        IConfiguration configuration,
        IShipDParameterAdapter shipdAdapter,
        IShipDConstraintValidator shipdValidator,
        Engineering.IWeightEstimationService weightService,
        DataDriven.DataDrivenRealWorldSolver? dataDrivenSolver = null,
        DataDriven.DataDrivenParametricSolver? parametricSolver = null,
        IShipDHullGeometryService? shipdGeometryService = null,
        IDataServiceClient? dataServiceClient = null,
        IHullGeometryGeneratorService? hullGeometryGenerator = null,
        Geometry.IHullOptimizationService? hullOptimizationService = null)
    {
        _context = context;
        _firstPrinciplesSolver = firstPrinciplesSolver;
        _logger = logger;
        _configuration = configuration;
        _shipdAdapter = shipdAdapter;
        _shipdValidator = shipdValidator;
        _weightService = weightService;
        _dataDrivenSolver = dataDrivenSolver;
        _parametricSolver = parametricSolver;
        _shipdGeometryService = shipdGeometryService;
        _dataServiceClient = dataServiceClient;
        _hullGeometryGenerator = hullGeometryGenerator;
        _hullOptimizationService = hullOptimizationService;
    }

    public async Task<List<SizingRunDto>> GetByMissionCaseIdAsync(Guid missionCaseId, string tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SIZING_RUN] Getting runs for mission case {MissionCaseId}", missionCaseId);

        // Verify mission case belongs to tenant
        var missionCase = await _context.MissionCases
            .Where(mc => mc.Id == missionCaseId && mc.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (missionCase == null)
        {
            return new List<SizingRunDto>();
        }

        var runs = await _context.SizingRuns
            .Where(sr => sr.MissionCaseId == missionCaseId)
            .OrderByDescending(sr => sr.CreatedAt)
            .ToListAsync(cancellationToken);

        // Get candidate counts for each run
        var runIds = runs.Select(r => r.Id).ToList();
        var candidateCounts = await _context.CandidateDesigns
            .Where(cd => runIds.Contains(cd.SizingRunId))
            .GroupBy(cd => cd.SizingRunId)
            .Select(g => new { RunId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RunId, x => x.Count, cancellationToken);

        return runs.Select(r => MapToDto(r, candidateCounts.GetValueOrDefault(r.Id, 0))).ToList();
    }

    public async Task<SizingRunDto?> GetByIdAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
    {
        var run = await _context.SizingRuns
            .Include(sr => sr.MissionCase)
            .Where(sr => sr.Id == id && sr.MissionCase.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (run == null) return null;

        var candidateCount = await _context.CandidateDesigns
            .Where(cd => cd.SizingRunId == id)
            .CountAsync(cancellationToken);

        return MapToDto(run, candidateCount);
    }

    public async Task<SizingRunDto> CreateAsync(CreateSizingRunDto dto, Guid userId, string tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SIZING_RUN] Creating sizing run for mission case {MissionCaseId}, mode={Mode}", dto.MissionCaseId, dto.Mode);

        // Verify mission case exists and belongs to tenant
        var missionCase = await _context.MissionCases
            .Where(mc => mc.Id == dto.MissionCaseId && mc.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (missionCase == null)
        {
            throw new InvalidOperationException($"Mission case {dto.MissionCaseId} not found for tenant {tenantId}");
        }

        var sw = Stopwatch.StartNew();

        // Extract additionalParameters from MissionCase.ShipdInputsJson if not already in request
        // This ensures wizard-set geometry parameters are used even when not explicitly passed in the request
        var effectiveDto = dto;
        if (dto.Options?.AdditionalParameters == null || dto.Options.AdditionalParameters.Count == 0)
        {
            var missionStoredAdditionalParameters = ExtractAdditionalParametersFromMissionCase(missionCase);
            if (missionStoredAdditionalParameters != null && missionStoredAdditionalParameters.Count > 0)
            {
                _logger.LogInformation("[SIZING_RUN] Extracted {Count} additional parameters from MissionCase.ShipdInputsJson before BuildAsync",
                    missionStoredAdditionalParameters.Count);

                // Inject into dto.Options so they're available to BuildAsync
                if (effectiveDto.Options == null)
                {
                    // Create new options with extracted parameters
                    effectiveDto = effectiveDto with
                    {
                        Options = new SizingOptionsDto
                        {
                            AdditionalParameters = missionStoredAdditionalParameters
                        }
                    };
                }
                else
                {
                    // Merge with existing options - request parameters take precedence
                    var mergedParams = new Dictionary<string, object>(missionStoredAdditionalParameters, StringComparer.OrdinalIgnoreCase);
                    if (effectiveDto.Options.AdditionalParameters != null)
                    {
                        foreach (var kvp in effectiveDto.Options.AdditionalParameters)
                        {
                            mergedParams[kvp.Key] = kvp.Value;
                        }
                    }
                    effectiveDto = effectiveDto with
                    {
                        Options = effectiveDto.Options with
                        {
                            AdditionalParameters = mergedParams
                        }
                    };
                }
            }
        }

        // Build ShipD parameterization
        var shipdResult = await _shipdAdapter.BuildAsync(missionCase, effectiveDto, cancellationToken);
        var validationResult = _shipdValidator.Validate(shipdResult);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"ShipD constraint validation failed: {string.Join("; ", validationResult.Errors)}");
        }

        var shipdPayloadJson = JsonSerializer.Serialize(shipdResult);
        var shipdVectorJson = JsonSerializer.Serialize(shipdResult.ParameterVector);

        missionCase.MissionCategory = shipdResult.VesselCategory;
        missionCase.MissionType = shipdResult.VesselType;
        missionCase.BowFamily = shipdResult.BowFamily;
        missionCase.MidshipFamily = shipdResult.MidshipFamily;
        missionCase.SternFamily = shipdResult.SternFamily;
        missionCase.FamilyMaskVersion = shipdResult.MaskVersion;
        missionCase.ShipdInputsJson = shipdPayloadJson;
        missionCase.UpdatedAt = DateTime.UtcNow;

        var familyHints = BuildFamilyHints(effectiveDto, shipdResult);

        // Merge additionalParameters: request parameters override ShipD result parameters
        var mergedAdditionalParameters = MergeAdditionalParameters(
            effectiveDto.Options?.AdditionalParameters,
            shipdResult.AdditionalParameters);

        var maxCandidates = effectiveDto.Options?.MaxCandidates ?? 5;
        if (maxCandidates < 1)
        {
            maxCandidates = 1;
        }

        Dictionary<string, object>? effectiveAdditionalParameters = null;
        if (mergedAdditionalParameters != null)
        {
            effectiveAdditionalParameters = mergedAdditionalParameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        else if (effectiveDto.Options?.AdditionalParameters != null)
        {
            effectiveAdditionalParameters = new Dictionary<string, object>(effectiveDto.Options.AdditionalParameters, StringComparer.OrdinalIgnoreCase);
        }

        var effectiveRunOptionsDto = new Shared.DTOs.Sizing.SizingOptionsDto
        {
            FamilyHints = familyHints.Count > 0 ? new List<string>(familyHints) : effectiveDto.Options?.FamilyHints,
            MaxCandidates = maxCandidates,
            MinFn = effectiveDto.Options?.MinFn,
            MaxFn = effectiveDto.Options?.MaxFn,
            // Persist geometry by default to ensure offsets are available to the viewer
            IncludeGeometry = effectiveDto.Options?.IncludeGeometry ?? true,
            AdditionalParameters = effectiveAdditionalParameters
        };

        var runOptionsJson = JsonSerializer.Serialize(effectiveRunOptionsDto);

        var solverOptions = new Solver.SizingOptionsDto(
            FamilyHints: familyHints.Count > 0 ? familyHints : null,
            MaxCandidates: maxCandidates,
            MinFn: effectiveDto.Options?.MinFn,
            MaxFn: effectiveDto.Options?.MaxFn,
            AdditionalParameters: mergedAdditionalParameters
        );

        var run = new SizingRun
        {
            Id = Guid.NewGuid(),
            MissionCaseId = dto.MissionCaseId,
            Mode = dto.Mode,
            LocksJson = dto.Locks != null ? JsonSerializer.Serialize(dto.Locks) : null,
            OptionsJson = runOptionsJson,
            VesselCategory = shipdResult.VesselCategory,
            VesselType = shipdResult.VesselType,
            BowFamily = shipdResult.BowFamily,
            MidshipFamily = shipdResult.MidshipFamily,
            SternFamily = shipdResult.SternFamily,
            FamilyMaskVersion = shipdResult.MaskVersion,
            ShipdInputVectorJson = shipdVectorJson,
            Status = "running",
            CreatedAt = DateTime.UtcNow
        };

        _context.SizingRuns.Add(run);

        bool runPersisted = false;

        try
        {
            // Persist the run entity first
            await _context.SaveChangesAsync(cancellationToken);
            runPersisted = true;

            // Build solver request
            var solverRequest = new Solver.SolverRequest(
                MissionCase: missionCase,
                Locks: dto.Locks != null ? new Solver.SizingLocksDto(
                    dto.Locks.KeepFn,
                    dto.Locks.KeepLOverB,
                    dto.Locks.KeepBOverT,
                    dto.Locks.KeepDOverT,
                    dto.Locks.KeepCbBand
                ) : null,
                Options: solverOptions
            );

            // Route to appropriate solver based on mode
            List<Solver.SolverCandidate> solverCandidates;
            Solver.SolverDiagnostics? diagnostics = null;

            if (dto.Mode == "data_driven_real" && _dataDrivenSolver != null)
            {
                // Check feature flag
                var featureEnabled = _configuration.GetValue<bool>("FeatureFlags:DataDrivenReal", false);
                if (!featureEnabled)
                {
                    _logger.LogWarning("Data-Driven Real mode requested but feature flag disabled. Falling back to First-Principles.");
                    (solverCandidates, diagnostics) = await _firstPrinciplesSolver.SolveAsync(solverRequest, cancellationToken);
                }
                else
                {
                    _logger.LogInformation("Using Data-Driven Real-World solver");
                    solverCandidates = await _dataDrivenSolver.SolveAsync(solverRequest, cancellationToken);
                    // Note: Data-driven solver doesn't return diagnostics yet
                }
            }
            else if (dto.Mode == "data_driven_ml" && _parametricSolver != null)
            {
                // Check feature flag
                var featureEnabled = _configuration.GetValue<bool>("FeatureFlags:DataDrivenML", false);
                if (!featureEnabled)
                {
                    _logger.LogWarning("Data-Driven ML mode requested but feature flag disabled. Falling back to First-Principles.");
                    (solverCandidates, diagnostics) = await _firstPrinciplesSolver.SolveAsync(solverRequest, cancellationToken);
                }
                else
                {
                    _logger.LogInformation("Using Data-Driven ML/Parametric solver (ShipD dataset)");
                    solverCandidates = await _parametricSolver.SolveAsync(solverRequest, cancellationToken);
                    // Note: Data-driven solver doesn't return diagnostics yet
                }
            }
            else
            {
                // Default: First-Principles mode
                _logger.LogInformation("Using First-Principles solver");
                (solverCandidates, diagnostics) = await _firstPrinciplesSolver.SolveAsync(solverRequest, cancellationToken);
            }

            // Convert solver candidates to database entities
            var candidateEntities = new List<CandidateDesign>();

            // Get ShipD metadata if geometry service is available
            IReadOnlyList<Shared.DTOs.ShipD.ShipDParameterMetadataDto>? shipdMetadata = null;
            if (_shipdGeometryService != null && _dataServiceClient != null && !string.IsNullOrEmpty(shipdVectorJson))
            {
                try
                {
                    shipdMetadata = await _dataServiceClient.GetShipDParameterMetadataAsync(cancellationToken);
                    _logger.LogDebug("[SIZING_RUN] Retrieved ShipD metadata for geometry generation");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[SIZING_RUN] Failed to retrieve ShipD metadata for geometry generation");
                }
            }

            // Get base dimensions from ShipD result for vector adjustment
            // Use the first candidate's dimensions as reference, or mission case defaults
            decimal baseLpp = solverCandidates.Count > 0 ? solverCandidates[0].LppM : missionCase.CapLoaM ?? 100m;
            decimal baseBeam = solverCandidates.Count > 0 ? solverCandidates[0].BeamM : missionCase.CapBeamM ?? 20m;
            decimal baseDraft = solverCandidates.Count > 0 ? solverCandidates[0].DraftM : missionCase.CapDraftM ?? 8m;
            decimal baseCb = solverCandidates.Count > 0 ? solverCandidates[0].Cb : 0.65m;

            for (int i = 0; i < solverCandidates.Count; i++)
            {
                var sc = solverCandidates[i];

                // CRITICAL FIX: Adjust ShipD vector for each candidate's unique dimensions and coefficients
                // This ensures each candidate has a properly parameterized ShipD vector
                decimal[] candidateShipdVector = shipdResult.ParameterVector.ToArray(); // Convert IReadOnlyList to array
                string? candidateShipdVectorJson = shipdVectorJson;

                // CRITICAL: Calculate unique longitudinal ratios for each candidate based on variant index
                // This ensures each candidate has a distinct hull shape, not just scaled versions of the same shape
                // Higher Cb (fuller hull) → longer midship section → lower bow/stern ratios
                // Lower Cb (finer hull) → shorter midship section → higher bow/stern ratios
                bool vectorUpdated = false;
                if (candidateShipdVector.Length == 45)
                {
                    // Calculate variant fraction (same logic as solver)
                    decimal variantFraction = solverCandidates.Count <= 1
                        ? 0.5m
                        : (i + 1m) / (solverCandidates.Count + 1m);

                    // Calculate unique longitudinal ratios based on variant fraction
                    // As Cb increases (variantFraction increases), midship section gets longer
                    // Typical range: Lb=25-35%, Lm=35-50%, Ls=25-35%
                    decimal baseBowRatio = 0.30m;   // Base bow ratio
                    decimal baseSternRatio = 0.30m; // Base stern ratio

                    // Adjust ratios based on Cb: higher Cb → longer midship, shorter bow/stern
                    // Cb range: typically 0.60-0.72 for container ships
                    // Map variantFraction (0.167-0.833) to ratio adjustments
                    decimal cbVariation = (variantFraction - 0.5m) * 0.10m; // ±0.05 variation
                    decimal midshipAdjustment = cbVariation; // Positive for higher Cb
                    decimal bowSternAdjustment = -cbVariation * 0.5m; // Negative for higher Cb (half the magnitude)

                    decimal targetBowRatio = Math.Clamp(baseBowRatio + bowSternAdjustment, 0.25m, 0.35m);
                    decimal targetSternRatio = Math.Clamp(baseSternRatio + bowSternAdjustment, 0.25m, 0.35m);
                    decimal targetMidshipRatio = 1.0m - targetBowRatio - targetSternRatio;

                    // Always calculate and set unique ratios for each candidate to ensure distinct hull shapes
                    // This overrides any existing ratios to ensure progression from candidate 1 to N
                    candidateShipdVector[1] = targetBowRatio;
                    candidateShipdVector[2] = targetSternRatio;
                    vectorUpdated = true;
                    _logger.LogInformation(
                        "[SIZING_RUN] ✅ Calculated unique longitudinal ratios for candidate {Rank}: Lb={BowRatio:P0}, Lm={MidshipRatio:P0}, Ls={SternRatio:P0} (variantFraction={VariantFraction:F3}, Cb={Cb:F3})",
                        i + 1, targetBowRatio, targetMidshipRatio, targetSternRatio, variantFraction, sc.Cb);

                    // Apply family-specific defaults (e.g., bit_BB for bulbous bow)
                    // This ensures ALL candidates get family defaults, including Candidate 1
                    string? bowFamily = run.BowFamily ?? missionCase.BowFamily;
                    if (!string.IsNullOrEmpty(bowFamily))
                    {
                        var bowFamilyLower = bowFamily.ToLowerInvariant();
                        if (bowFamilyLower == "bulbous" || bowFamilyLower == "bulbous_bow")
                        {
                            if (candidateShipdVector[31] == 0m) // bit_BB
                            {
                                candidateShipdVector[31] = 1.0m; // Enable bulbous bow
                                vectorUpdated = true;
                                _logger.LogWarning("[SIZING_RUN] ✅ Applied bulbous bow flag (bit_BB=1) for candidate {Rank} (BowFamily={BowFamily})", i + 1, bowFamily);
                            }
                        }
                    }

                    // Ensure bulb dimensions are sensible if bulb is enabled
                    // This prevents the issue where bulb is enabled but dimensions are zero or minimal
                    if (candidateShipdVector[31] > 0.5m && shipdMetadata != null) // bit_BB
                    {
                        EnsureSensibleBulbDimensions(candidateShipdVector, shipdMetadata, i + 1, ref vectorUpdated);
                    }

                    // CRITICAL: Serialize the updated vector immediately after populating defaults
                    // This ensures the JSON has the correct values even if metadata adjustment fails
                    if (vectorUpdated)
                    {
                        candidateShipdVectorJson = JsonSerializer.Serialize(candidateShipdVector);
                        _logger.LogWarning(
                            "[SIZING_RUN] ✅ Serialized ShipD vector with defaults for candidate {Rank}: Vector[1]={Bow}, Vector[2]={Stern}, Vector[31]={BitBB}",
                            i + 1, candidateShipdVector[1], candidateShipdVector[2], candidateShipdVector[31]);
                    }
                }

                if (_shipdAdapter != null && shipdMetadata != null && candidateShipdVector.Length == 45)
                {
                    try
                    {

                        // Adjust for dimension changes (Lpp, Beam, Draft)
                        if (Math.Abs(sc.LppM - baseLpp) > 0.1m ||
                            Math.Abs(sc.BeamM - baseBeam) > 0.1m ||
                            Math.Abs(sc.DraftM - baseDraft) > 0.1m)
                        {
                            candidateShipdVector = _shipdAdapter.AdjustVectorForDimensionChange(
                                candidateShipdVector,
                                baseLpp, sc.LppM,
                                baseBeam, sc.BeamM,
                                baseDraft, sc.DraftM,
                                shipdMetadata);
                            _logger.LogDebug(
                                "[SIZING_RUN] Adjusted ShipD vector for candidate {Rank} dimensions: Lpp={Lpp}m, Beam={Beam}m, Draft={Draft}m",
                                i + 1, sc.LppM, sc.BeamM, sc.DraftM);
                        }

                        // Adjust for Cb change (affects hull fullness)
                        if (Math.Abs(sc.Cb - baseCb) > 0.01m)
                        {
                            candidateShipdVector = _shipdAdapter.AdjustVectorForCoefficientChange(
                                candidateShipdVector,
                                "Cb",
                                baseCb,
                                sc.Cb,
                                shipdMetadata);
                            _logger.LogDebug(
                                "[SIZING_RUN] Adjusted ShipD vector for candidate {Rank} Cb change: {OldCb} → {NewCb}",
                                i + 1, baseCb, sc.Cb);
                        }

                        // Serialize adjusted vector
                        candidateShipdVectorJson = JsonSerializer.Serialize(candidateShipdVector);
                        _logger.LogWarning(
                            "[SIZING_RUN] ✅ Generated unique ShipD vector for candidate {Rank}: Lpp={Lpp}m, Beam={Beam}m, Draft={Draft}m, Cb={Cb}, Vector[1]={Bow}, Vector[2]={Stern}",
                            i + 1, sc.LppM, sc.BeamM, sc.DraftM, sc.Cb, candidateShipdVector[1], candidateShipdVector[2]);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "[SIZING_RUN] ❌ Failed to adjust ShipD vector for candidate {Rank}. Using base vector. ShipdAdapter={HasAdapter}, ShipdMetadata={HasMetadata}, VectorLength={Length}",
                            i + 1, _shipdAdapter != null, shipdMetadata != null, candidateShipdVector?.Length ?? 0);
                        // Continue with base vector if adjustment fails
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "[SIZING_RUN] ⚠️ Skipping ShipD vector adjustment for candidate {Rank}: ShipdAdapter={HasAdapter}, ShipdMetadata={HasMetadata}, VectorLength={Length}",
                        i + 1, _shipdAdapter != null, shipdMetadata != null, candidateShipdVector?.Length ?? 0);
                }

                // Generate geometry (OffsetsGrid is primary, ShipD is secondary)
                string? geometryJson = null;
                var geometryStatus = GeometryGenerationStatus.Success;
                string? geometryError = null;

                // Priority 1: Always generate form-coefficient-based OffsetsGrid for ALL candidates
                // This ensures solver-generated geometry is available for all candidates
                if (_hullGeometryGenerator != null)
                {
                    try
                    {
                        // Extract vessel type and ShipD families from sizing run (from ShipD result) or mission case
                        // This allows the generator to use parent hull if available and apply ShipD family adjustments
                        string? vesselType = run.VesselType ?? missionCase.MissionType;
                        string? bowFamily = run.BowFamily ?? missionCase.BowFamily;
                        string? midshipFamily = run.MidshipFamily ?? missionCase.MidshipFamily;
                        string? sternFamily = run.SternFamily ?? missionCase.SternFamily;

                        var offsetsGrid = await _hullGeometryGenerator.GenerateOffsetsFromCandidateAsync(
                            sc,
                            vesselType: vesselType,
                            numStations: 60, // Increased for smooth 3D rendering (was 23 for BSRA-compatible)
                            numWaterlines: 13,
                            bowFamily: bowFamily,
                            midshipFamily: midshipFamily,
                            sternFamily: sternFamily,
                            cancellationToken);

                        if (offsetsGrid != null)
                        {
                            // Serialize offsets grid to JSON (primary geometry format)
                            // Use camelCase naming to match API response format and frontend expectations
                            geometryJson = JsonSerializer.Serialize(offsetsGrid, JsonOptions);
                            _logger.LogInformation("[SIZING_RUN] Generated form-coefficient-based OffsetsGrid for candidate {Rank}", i + 1);

                            // Validate form coefficients (log warnings if mismatch)
                            var validation = await _hullGeometryGenerator.ValidateFormCoefficientsAsync(
                                sc,
                                offsetsGrid,
                                tolerance: 0.10m, // 10% tolerance for initial implementation
                                cancellationToken);

                            if (!validation.IsValid && validation.Warnings.Any())
                            {
                                _logger.LogWarning(
                                    "[SIZING_RUN] Form coefficient validation warnings for candidate {Rank}: {Warnings}",
                                    i + 1, string.Join("; ", validation.Warnings));
                            }
                            else
                            {
                                _logger.LogDebug(
                                    "[SIZING_RUN] Form coefficient validation passed for candidate {Rank}",
                                    i + 1);
                            }
                        }
                        else
                        {
                            // OffsetsGrid generation failed
                            geometryStatus = GeometryGenerationStatus.FormCoefficientFailed;
                            geometryError = "Form-coefficient generation returned null";
                            _logger.LogWarning("[SIZING_RUN] OffsetsGrid generation returned null for candidate {Rank}. Will try ShipD as fallback.", i + 1);
                        }
                    }
                    catch (Exception ex)
                    {
                        geometryStatus = GeometryGenerationStatus.FormCoefficientFailed;
                        geometryError = $"Form-coefficient generation failed: {ex.Message}";
                        _logger.LogWarning(ex, "[SIZING_RUN] Failed to generate OffsetsGrid for candidate {Rank}. Will try ShipD as fallback.", i + 1);
                    }
                }

                // Optional: Apply NURBS optimization to refine geometry to match target CB/CP/LCB
                // This is enabled via feature flag or options
                var enableOptimization = _configuration.GetValue<bool>("FeatureFlags:NurbsOptimization", false) ||
                                       effectiveDto.Options?.AdditionalParameters?.ContainsKey("enableOptimization") == true;

                if (enableOptimization && _hullOptimizationService != null)
                {
                    try
                    {
                        _logger.LogInformation(
                            "[SIZING_RUN] Applying NURBS optimization to candidate {Rank} to refine CB={Cb}, CP={Cp}, LCB={Lcb}%",
                            i + 1, sc.Cb, sc.Cp, sc.LcbPctLpp ?? 0m);

                        var optimizationOptions = new Geometry.HullOptimizationService.OptimizationOptions
                        {
                            PopulationSize = 15, // Smaller for faster optimization
                            MaxIterations = 50,  // Fewer iterations for initial integration
                            Tolerance = 0.005m,  // 0.5% tolerance
                            LogInterval = 10
                        };

                        var optimizationResult = await _hullOptimizationService.OptimizeAsync(
                            targetCb: sc.Cb,
                            targetCp: sc.Cp,
                            targetLcbPercent: sc.LcbPctLpp ?? 0m,
                            lppM: sc.LppM,
                            beamM: sc.BeamM,
                            draftM: sc.DraftM,
                            initialGuess: null,
                            options: optimizationOptions,
                            cancellationToken: cancellationToken);

                        if (optimizationResult.Converged || optimizationResult.FinalError < 0.01m)
                        {
                            // Generate optimized geometry
                            var optimizedSections = await _hullOptimizationService.GenerateSectionsFromOptimizedGridAsync(
                                optimizationResult.OptimalControlPoints,
                                sc.LppM,
                                sc.BeamM,
                                sc.DraftM,
                                numStations: 60,
                                numWaterlines: 13,
                                cancellationToken);

                            // Use optimized geometry
                            geometryJson = JsonSerializer.Serialize(optimizedSections, JsonOptions);
                            geometryStatus = GeometryGenerationStatus.Success;
                            geometryError = null;

                            _logger.LogInformation(
                                "[SIZING_RUN] ✅ NURBS optimization completed for candidate {Rank}: Final CB={Cb}, CP={Cp}, LCB={Lcb}% (Error={Error})",
                                i + 1, optimizationResult.FinalCb, optimizationResult.FinalCp,
                                optimizationResult.FinalLcbPercent, optimizationResult.FinalError);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[SIZING_RUN] ⚠️ NURBS optimization did not converge for candidate {Rank} (Error={Error}). Using original geometry.",
                                i + 1, optimizationResult.FinalError);
                        }
                    }
                    catch (Exception optEx)
                    {
                        _logger.LogWarning(optEx,
                            "[SIZING_RUN] NURBS optimization failed for candidate {Rank}. Using original geometry.",
                            i + 1);
                        // Continue with original geometry if optimization fails
                    }
                }

                // Priority 2: Try ShipD geometry as fallback if OffsetsGrid generation failed
                // ShipD geometry is stored separately and can be used for 3D visualization
                if (string.IsNullOrEmpty(geometryJson) && _shipdGeometryService != null && shipdMetadata != null && candidateShipdVector != null && candidateShipdVector.Length == 45)
                {
                    try
                    {
                        // Use candidate-specific adjusted vector directly
                        if (candidateShipdVector != null && candidateShipdVector.Length == 45)
                        {
                            // Generate hull sections
                            var sections = await _shipdGeometryService.GenerateSectionsAsync(
                                candidateShipdVector,
                                sc.LppM,
                                sc.BeamM,
                                sc.DraftM,
                                shipdMetadata,
                                stationCount: 60, // Increased for smooth 3D rendering (was 20)
                                cancellationToken);

                            // Serialize sections to JSON (fallback geometry format)
                            // Use camelCase naming to match API response format and frontend expectations
                            geometryJson = JsonSerializer.Serialize(sections, JsonOptions);
                            _logger.LogInformation("[SIZING_RUN] Generated ShipD geometry as fallback for candidate {Rank}", i + 1);

                            // Update status to indicate ShipD was used as fallback
                            if (geometryStatus == GeometryGenerationStatus.FormCoefficientFailed)
                            {
                                geometryStatus = GeometryGenerationStatus.ShipDFailed; // ShipD succeeded but OffsetsGrid failed
                                geometryError = $"{geometryError}; Using ShipD geometry as fallback";
                            }
                            else
                            {
                                geometryStatus = GeometryGenerationStatus.Success;
                                geometryError = null;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Both OffsetsGrid and ShipD failed
                        if (geometryStatus == GeometryGenerationStatus.FormCoefficientFailed)
                        {
                            geometryStatus = GeometryGenerationStatus.BothFailed;
                            geometryError = $"{geometryError}; ShipD geometry generation also failed: {ex.Message}";
                        }
                        else
                        {
                            geometryStatus = GeometryGenerationStatus.ShipDFailed;
                            geometryError = $"ShipD geometry generation failed: {ex.Message}";
                        }
                        _logger.LogWarning(ex, "[SIZING_RUN] Failed to generate ShipD geometry as fallback for candidate {Rank}. Candidate will have no geometry.", i + 1);
                    }
                }

                // Calculate weight breakdown
                string? weightBreakdownJson = null;
                try
                {
                    var weightBreakdown = _weightService.EstimateWeights(
                        lppM: sc.LppM,
                        beamM: sc.BeamM,
                        depthM: sc.DepthM,
                        draftM: sc.DraftM,
                        cb: sc.Cb,
                        displacementT: sc.DisplacementT,
                        shpKw: sc.ShpKw,
                        missionType: missionCase.MissionType,
                        enduranceNm: missionCase.EnduranceNm,
                        serviceSpeedKn: missionCase.ServiceSpeedKn
                    );
                    weightBreakdownJson = JsonSerializer.Serialize(weightBreakdown, JsonOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[SIZING_RUN] Failed to calculate weight breakdown for candidate {Rank}", i + 1);
                    // Continue without weight breakdown - not critical for candidate generation
                }

                var entity = new CandidateDesign
                {
                    Id = Guid.NewGuid(),
                    SizingRunId = run.Id,
                    HullFamily = sc.HullFamily,
                    VesselCategory = run.VesselCategory,
                    VesselType = run.VesselType,
                    BowFamily = run.BowFamily ?? sc.HullFamily,
                    MidshipFamily = run.MidshipFamily,
                    SternFamily = run.SternFamily,
                    FamilyMaskVersion = run.FamilyMaskVersion,
                    ShipdParametersJson = candidateShipdVectorJson, // Use candidate-specific adjusted vector
                    LppM = sc.LppM,
                    LwlM = sc.LwlM,
                    LoaM = sc.LoaM,
                    BM = sc.BeamM,
                    TM = sc.DraftM,
                    DM = sc.DepthM,
                    Cb = sc.Cb,
                    Cp = sc.Cp,
                    Cwp = sc.Cwp,
                    Cm = sc.Cm,
                    DisplacementT = sc.DisplacementT,
                    Fn = sc.Fn,
                    LwlOverLambda = sc.LwlOverLambda,
                    KbM = sc.KbM,
                    LcbPctLpp = sc.LcbPctLpp,
                    GmEstM = sc.GmEstM,
                    EhpKw = sc.EhpKw,
                    ShpKw = sc.ShpKw,
                    FlagsJson = JsonSerializer.Serialize(sc.Flags),
                    Score = sc.Score,
                    Rank = i + 1,
                    IsSelected = i == 0, // First candidate is selected by default
                    GeometryJson = geometryJson, // ShipD or OffsetsGrid geometry if available
                    GeometryGenerationStatus = geometryStatus,
                    GeometryGenerationError = geometryError,
                    WeightBreakdownJson = weightBreakdownJson,
                    CreatedAt = DateTime.UtcNow
                };

                candidateEntities.Add(entity);
            }

            _context.CandidateDesigns.AddRange(candidateEntities);

            run.Status = "completed";
            run.ComputeTimeMs = (int)sw.ElapsedMilliseconds;

            // Store diagnostics if 0 candidates generated
            if (candidateEntities.Count == 0 && diagnostics != null)
            {
                run.DiagnosticsJson = JsonSerializer.Serialize(diagnostics);
                _logger.LogWarning("[SIZING_RUN] 0 candidates generated. Diagnostics: {Summary}", diagnostics.GetSummary());
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("[SIZING_RUN] Completed run {RunId} in {ElapsedMs}ms, generated {Count} candidates",
                run.Id, sw.ElapsedMilliseconds, candidateEntities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SIZING_RUN] Error during sizing run {RunId}. Run persisted: {Persisted}", run.Id, runPersisted);

            // Only update run status if it was successfully persisted to the database
            if (runPersisted)
            {
                try
                {
                    run.Status = "failed";
                    run.ErrorMessage = ex.Message;
                    run.ComputeTimeMs = (int)sw.ElapsedMilliseconds;
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "[SIZING_RUN] Failed to update run status to 'failed' for run {RunId}", run.Id);
                    // Don't throw - we already have the original exception to propagate
                }
            }

            // Re-throw the exception so the controller can handle it and return proper HTTP status
            throw;
        }

        return MapToDto(run, await _context.CandidateDesigns.Where(cd => cd.SizingRunId == run.Id).CountAsync(cancellationToken));
    }

    public async Task<List<CandidateDesignDto>> GetCandidatesAsync(Guid runId, string tenantId, CancellationToken cancellationToken = default)
    {
        // Verify run belongs to tenant via mission case
        var run = await _context.SizingRuns
            .Include(sr => sr.MissionCase)
            .Where(sr => sr.Id == runId && sr.MissionCase.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (run == null)
        {
            _logger.LogWarning("[SIZING_RUN] No run found with ID={RunId} and TenantId={TenantId}", runId, tenantId);

            // Check if run exists with ANY tenantId (for debugging)
            var anyRun = await _context.SizingRuns.FirstOrDefaultAsync(sr => sr.Id == runId, cancellationToken);
            if (anyRun != null)
            {
                var actualTenant = await _context.MissionCases
                    .Where(mc => mc.Id == anyRun.MissionCaseId)
                    .Select(mc => mc.TenantId)
                    .FirstOrDefaultAsync(cancellationToken);
                _logger.LogError("[SIZING_RUN] ⚠️ TENANT MISMATCH! Run {RunId} exists but with different tenantId! Expected={Expected}, Actual={Actual}",
                    runId, tenantId, actualTenant);
                Console.WriteLine($"[SIZING_RUN] ⚠️ TENANT MISMATCH! Run {runId} exists but tenantId mismatch: expected '{tenantId}', actual '{actualTenant}'");
            }
            else
            {
                _logger.LogWarning("[SIZING_RUN] Run {RunId} does not exist at all (not just tenant mismatch)", runId);
                Console.WriteLine($"[SIZING_RUN] Run {runId} does not exist in database");
            }

            return new List<CandidateDesignDto>();
        }

        var candidates = await _context.CandidateDesigns
            .Where(cd => cd.SizingRunId == runId)
            .OrderBy(cd => cd.Rank)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("[SIZING_RUN] Found {Count} candidates for run {RunId}", candidates.Count, runId);
        return candidates.Select(MapCandidateToDto).ToList();
    }

    private static SizingRunDto MapToDto(SizingRun entity, int candidateCount)
    {
        // Deserialize diagnostics if present
        SolverDiagnosticsDto? diagnosticsDto = null;
        if (!string.IsNullOrEmpty(entity.DiagnosticsJson))
        {
            try
            {
                var diagnostics = JsonSerializer.Deserialize<Solver.SolverDiagnostics>(entity.DiagnosticsJson);
                if (diagnostics != null)
                {
                    diagnosticsDto = new SolverDiagnosticsDto
                    {
                        TotalFamiliesConsidered = diagnostics.TotalFamiliesConsidered,
                        FamiliesAfterFnFiltering = diagnostics.FamiliesAfterFnFiltering,
                        FamiliesAfterHintsFiltering = diagnostics.FamiliesAfterHintsFiltering,
                        FamiliesFailedClosure = diagnostics.FamiliesFailedClosure,
                        FailureReasons = diagnostics.FailureReasons,
                        Suggestions = diagnostics.Suggestions,
                        TargetDisplacementT = diagnostics.TargetDisplacementT,
                        EstimatedFroudeNumber = diagnostics.EstimatedFroudeNumber,
                        MissionType = diagnostics.MissionType,
                        FailedFamilies = diagnostics.FailedFamilies,
                        Summary = diagnostics.GetSummary()
                    };
                }
            }
            catch (Exception)
            {
                // Ignore deserialization errors - diagnostics is optional
            }
        }

        return new SizingRunDto
        {
            Id = entity.Id,
            MissionCaseId = entity.MissionCaseId,
            Mode = entity.Mode,
            LocksJson = entity.LocksJson,
            OptionsJson = entity.OptionsJson,
            VesselCategory = entity.VesselCategory,
            VesselType = entity.VesselType,
            BowFamily = entity.BowFamily,
            MidshipFamily = entity.MidshipFamily,
            SternFamily = entity.SternFamily,
            FamilyMaskVersion = entity.FamilyMaskVersion,
            ShipdInputVectorJson = entity.ShipdInputVectorJson,
            RunStatus = entity.Status,
            ComputeTimeMs = entity.ComputeTimeMs,
            ErrorMessage = entity.ErrorMessage,
            CreatedAt = entity.CreatedAt,
            CandidateCount = candidateCount,
            Diagnostics = diagnosticsDto
        };
    }

    private static CandidateDesignDto MapCandidateToDto(CandidateDesign entity)
    {
        // Extract ShipD parameters from vector JSON
        decimal[]? shipdVector = null;
        if (!string.IsNullOrWhiteSpace(entity.ShipdParametersJson))
        {
            try
            {
                shipdVector = JsonSerializer.Deserialize<decimal[]>(entity.ShipdParametersJson);
            }
            catch (JsonException)
            {
                // If parsing fails, shipdVector remains null and parameters will be null
            }
        }

        return new CandidateDesignDto
        {
            Id = entity.Id,
            SizingRunId = entity.SizingRunId,
            HullFamily = entity.HullFamily,
            VesselCategory = entity.VesselCategory,
            VesselType = entity.VesselType,
            BowFamily = entity.BowFamily,
            MidshipFamily = entity.MidshipFamily,
            SternFamily = entity.SternFamily,
            FamilyMaskVersion = entity.FamilyMaskVersion,
            ShipdParametersJson = entity.ShipdParametersJson,
            LppM = entity.LppM,
            LwlM = entity.LwlM,
            LoaM = entity.LoaM,
            BeamM = entity.BM,
            DraftM = entity.TM,
            DepthM = entity.DM,
            Cb = entity.Cb,
            Cp = entity.Cp,
            Cwp = entity.Cwp,

            // ShipD Parameters (extracted from vector)
            BowLengthRatio = GetShipDParam(shipdVector, 1),
            SternLengthRatio = GetShipDParam(shipdVector, 2),
            BowFlareAngle = GetShipDParam(shipdVector, 8),
            BowCurvature = GetShipDParam(shipdVector, 9),
            BowKnuckle = GetShipDParam(shipdVector, 10),
            DeadriseAngle = GetShipDParam(shipdVector, 19),
            SternRakeAngle = GetShipDParam(shipdVector, 27),
            SternCurvature = GetShipDParam(shipdVector, 29),
            SternKnuckle = GetShipDParam(shipdVector, 30),
            TransomArea = GetShipDParam(shipdVector, 22),
            TransomWidth = GetShipDParam(shipdVector, 28),
            HasSheer = GetShipDParam(shipdVector, 20) > 0.5m,
            HasTumblehome = GetShipDParam(shipdVector, 21) > 0.5m,
            HasBulb = GetShipDParam(shipdVector, 31) > 0.5m,
            BulbLengthRatio = GetShipDParam(shipdVector, 33),
            BulbHeightRatio = GetShipDParam(shipdVector, 34),
            BulbWidthRatio = GetShipDParam(shipdVector, 35),
            BulbAsymmetry = GetShipDParam(shipdVector, 36),
            BulbFilletRadius = GetShipDParam(shipdVector, 37),

            DispM3 = entity.DisplacementT / 1.025m, // Convert tonnes to m3
            DispT = entity.DisplacementT,
            Fn = entity.Fn,
            LwlOverLambda = entity.LwlOverLambda,
            KbM = entity.KbM,
            LcbPctLpp = entity.LcbPctLpp,
            WeightBreakdownJson = entity.WeightBreakdownJson,
            KgEstM = null, // Not in model
            GmEstM = entity.GmEstM,
            EhpKw = entity.EhpKw,
            ShpKw = entity.ShpKw,
            FlagsJson = entity.FlagsJson,
            Score = entity.Score,
            Rank = entity.Rank,
            IsSelected = entity.IsSelected,
            GeometryJson = entity.GeometryJson,
            GeometryGenerationStatus = entity.GeometryGenerationStatus,
            GeometryGenerationError = entity.GeometryGenerationError,
            CreatedAt = entity.CreatedAt,

            // Provenance (Data-Driven Mode)
            ReferenceVesselId = entity.ReferenceVesselId,
            ReferenceVesselName = entity.ReferenceVesselName,
            SimilarityScore = entity.SimilarityScore,
            SolverMode = entity.SolverMode
        };
    }

    /// <summary>
    /// Extracts a ShipD parameter from the vector at the specified index.
    /// Returns null if vector is null or index is out of range.
    /// </summary>
    private static decimal? GetShipDParam(decimal[]? vector, int index)
    {
        if (vector == null || index < 0 || index >= vector.Length)
        {
            return null;
        }
        return vector[index];
    }

    private static List<string> BuildFamilyHints(CreateSizingRunDto effectiveDto, ShipDParameterizationResult shipdResult)
    {
        var orderedHints = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddHint(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var normalized = value.Trim();
            if (seen.Add(normalized))
            {
                orderedHints.Add(normalized);
            }
        }

        if (effectiveDto.Options?.FamilyHints != null)
        {
            foreach (var hint in effectiveDto.Options.FamilyHints)
            {
                AddHint(hint);
            }
        }

        if (!string.IsNullOrWhiteSpace(shipdResult.VesselType) &&
            VesselTypeFamilyHints.TryGetValue(shipdResult.VesselType, out var mappedFamilies))
        {
            foreach (var family in mappedFamilies)
            {
                AddHint(family);
            }
        }

        return orderedHints;
    }

    private static IDictionary<string, object>? MergeAdditionalParameters(
        Dictionary<string, object>? requestParameters,
        IDictionary<string, object>? shipdParameters)
    {
        if ((requestParameters == null || requestParameters.Count == 0) &&
            (shipdParameters == null || shipdParameters.Count == 0))
        {
            return null;
        }

        var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (shipdParameters != null)
        {
            foreach (var kvp in shipdParameters)
            {
                merged[kvp.Key] = kvp.Value;
            }
        }

        if (requestParameters != null)
        {
            foreach (var kvp in requestParameters)
            {
                merged[kvp.Key] = kvp.Value;
            }
        }

        return merged;
    }

    /// <summary>
    /// Extracts additionalParameters from MissionCase.ShipdInputsJson if present.
    /// Returns null if not found or on parse error.
    /// </summary>
    private static Dictionary<string, object>? ExtractAdditionalParametersFromMissionCase(MissionCase missionCase)
    {
        if (string.IsNullOrWhiteSpace(missionCase.ShipdInputsJson))
        {
            return null;
        }

        try
        {
            var shipdInputs = JsonSerializer.Deserialize<Dictionary<string, object>>(missionCase.ShipdInputsJson);
            if (shipdInputs != null && shipdInputs.TryGetValue("additionalParameters", out var additionalParamsObj))
            {
                if (additionalParamsObj is JsonElement jsonElement)
                {
                    var additionalParams = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText());
                    return additionalParams;
                }
                else if (additionalParamsObj is Dictionary<string, object> dict)
                {
                    return dict;
                }
                else
                {
                    // Try to serialize and deserialize to convert to Dictionary
                    var json = JsonSerializer.Serialize(additionalParamsObj);
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    return result;
                }
            }
        }
        catch (JsonException)
        {
            // Log but don't throw - mission case may have old format or invalid JSON
            // This is a non-critical error - solver will use defaults
        }
        catch (Exception)
        {
            // Ignore other errors - mission case may have unexpected structure
        }

        return null;
    }

    /// <summary>
    /// Ensures bulb dimensions (Lbb, Hbb, Bbb) are set to sensible defaults when bulb is enabled.
    /// This prevents the issue where bulb is enabled but dimensions are zero or minimal, resulting in no effective bulb geometry.
    /// </summary>
    /// <param name="vector">ShipD parameter vector</param>
    /// <param name="metadata">Parameter metadata</param>
    /// <param name="candidateRank">Candidate rank (for logging)</param>
    /// <param name="vectorUpdated">Reference flag to track if vector was updated</param>
    private void EnsureSensibleBulbDimensions(
        decimal[] vector,
        IReadOnlyList<Shared.DTOs.ShipD.ShipDParameterMetadataDto> metadata,
        int candidateRank,
        ref bool vectorUpdated)
    {
        // Only apply if bulb is enabled
        if (vector[31] <= 0.5m) // bit_BB
        {
            return;
        }

        const decimal minLbbThreshold = 0.01m; // 1% of Lpp (very small, should be at least 15%)
        const decimal minHbbThreshold = 0.05m; // 5% of draft (very small, should be at least 20%)
        const decimal minBbbThreshold = 0.05m; // 5% of beam (very small, should be at least 20%)

        // Sensible defaults based on recommendations:
        // - Lbb: 15% of Lpp (normalized: 0.15)
        // - Hbb: 20% of draft (normalized: 0.2)
        // - Bbb: 25% of beam (normalized: 0.25)
        const decimal defaultLbb = 0.15m; // 15% of Lpp
        const decimal defaultHbb = 0.2m;  // 20% of draft
        const decimal defaultBbb = 0.25m; // 25% of beam

        bool updated = false;

        // Check and set Lbb (Bulb length ratio, index 33)
        if (vector[33] < minLbbThreshold)
        {
            var param = metadata.FirstOrDefault(m => m.ParameterIndex == 33);
            if (param != null)
            {
                var oldValue = vector[33];
                var clampedValue = Math.Clamp(defaultLbb, param.Min ?? 0m, param.Max ?? 0.2m);
                if (oldValue != clampedValue)
                {
                    vector[33] = clampedValue;
                    updated = true;
                    _logger.LogInformation(
                        "[SIZING_RUN] Set Lbb to sensible default for candidate {Rank}: {Value} (was {OldValue}, recommended: 15% of Lpp)",
                        candidateRank, clampedValue, oldValue);
                }
            }
        }

        // Check and set Hbb (Bulb height ratio, index 34)
        if (vector[34] < minHbbThreshold)
        {
            var param = metadata.FirstOrDefault(m => m.ParameterIndex == 34);
            if (param != null)
            {
                var oldValue = vector[34];
                var clampedValue = Math.Clamp(defaultHbb, param.Min ?? 0m, param.Max ?? 1m);
                if (oldValue != clampedValue)
                {
                    vector[34] = clampedValue;
                    updated = true;
                    _logger.LogInformation(
                        "[SIZING_RUN] Set Hbb to sensible default for candidate {Rank}: {Value} (was {OldValue}, recommended: 20% of draft)",
                        candidateRank, clampedValue, oldValue);
                }
            }
        }

        // Check and set Bbb (Bulb width ratio, index 35)
        if (vector[35] < minBbbThreshold)
        {
            var param = metadata.FirstOrDefault(m => m.ParameterIndex == 35);
            if (param != null)
            {
                var oldValue = vector[35];
                var clampedValue = Math.Clamp(defaultBbb, param.Min ?? 0m, param.Max ?? 1m);
                if (oldValue != clampedValue)
                {
                    vector[35] = clampedValue;
                    updated = true;
                    _logger.LogInformation(
                        "[SIZING_RUN] Set Bbb to sensible default for candidate {Rank}: {Value} (was {OldValue}, recommended: 25% of beam)",
                        candidateRank, clampedValue, oldValue);
                }
            }
        }

        if (updated)
        {
            vectorUpdated = true;
            _logger.LogWarning(
                "[SIZING_RUN] ✅ Applied sensible bulb dimension defaults for candidate {Rank}: Lbb={Lbb}, Hbb={Hbb}, Bbb={Bbb}",
                candidateRank, vector[33], vector[34], vector[35]);
        }
    }
}
