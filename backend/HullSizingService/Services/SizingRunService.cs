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
        DataDriven.DataDrivenRealWorldSolver? dataDrivenSolver = null,
        DataDriven.DataDrivenParametricSolver? parametricSolver = null,
        IShipDHullGeometryService? shipdGeometryService = null,
        IDataServiceClient? dataServiceClient = null)
    {
        _context = context;
        _firstPrinciplesSolver = firstPrinciplesSolver;
        _logger = logger;
        _configuration = configuration;
        _shipdAdapter = shipdAdapter;
        _shipdValidator = shipdValidator;
        _dataDrivenSolver = dataDrivenSolver;
        _parametricSolver = parametricSolver;
        _shipdGeometryService = shipdGeometryService;
        _dataServiceClient = dataServiceClient;
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

        // Build ShipD parameterization
        var shipdResult = await _shipdAdapter.BuildAsync(missionCase, dto, cancellationToken);
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

        var familyHints = BuildFamilyHints(dto, shipdResult);
        var mergedAdditionalParameters = MergeAdditionalParameters(dto.Options?.AdditionalParameters, shipdResult.AdditionalParameters);

        var maxCandidates = dto.Options?.MaxCandidates ?? 5;
        if (maxCandidates < 1)
        {
            maxCandidates = 1;
        }

        Dictionary<string, object>? effectiveAdditionalParameters = null;
        if (mergedAdditionalParameters != null)
        {
            effectiveAdditionalParameters = mergedAdditionalParameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        else if (dto.Options?.AdditionalParameters != null)
        {
            effectiveAdditionalParameters = new Dictionary<string, object>(dto.Options.AdditionalParameters, StringComparer.OrdinalIgnoreCase);
        }

        var effectiveRunOptionsDto = new Shared.DTOs.Sizing.SizingOptionsDto
        {
            FamilyHints = familyHints.Count > 0 ? new List<string>(familyHints) : dto.Options?.FamilyHints,
            MaxCandidates = maxCandidates,
            MinFn = dto.Options?.MinFn,
            MaxFn = dto.Options?.MaxFn,
            IncludeGeometry = dto.Options?.IncludeGeometry ?? false,
            AdditionalParameters = effectiveAdditionalParameters
        };

        var runOptionsJson = JsonSerializer.Serialize(effectiveRunOptionsDto);

        var solverOptions = new Solver.SizingOptionsDto(
            FamilyHints: familyHints.Count > 0 ? familyHints : null,
            MaxCandidates: maxCandidates,
            MinFn: dto.Options?.MinFn,
            MaxFn: dto.Options?.MaxFn,
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

            for (int i = 0; i < solverCandidates.Count; i++)
            {
                var sc = solverCandidates[i];

                // Generate ShipD geometry if parameters are available
                string? geometryJson = null;
                if (_shipdGeometryService != null && shipdMetadata != null && !string.IsNullOrEmpty(shipdVectorJson))
                {
                    try
                    {
                        var shipdVector = JsonSerializer.Deserialize<decimal[]>(shipdVectorJson);
                        if (shipdVector != null && shipdVector.Length == 45)
                        {
                            // Generate hull sections
                            var sections = await _shipdGeometryService.GenerateSectionsAsync(
                                shipdVector,
                                sc.LppM,
                                sc.BeamM,
                                sc.DraftM,
                                shipdMetadata,
                                stationCount: 20,
                                cancellationToken);

                            // Serialize sections to JSON
                            geometryJson = JsonSerializer.Serialize(sections);
                            _logger.LogDebug("[SIZING_RUN] Generated ShipD geometry for candidate {Rank}", i + 1);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[SIZING_RUN] Failed to generate ShipD geometry for candidate {Rank}. Continuing without geometry.", i + 1);
                    }
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
                    ShipdParametersJson = shipdVectorJson,
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
                    GeometryJson = geometryJson, // ShipD geometry if available
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
            DispM3 = entity.DisplacementT / 1.025m, // Convert tonnes to m3
            DispT = entity.DisplacementT,
            Fn = entity.Fn,
            LwlOverLambda = entity.LwlOverLambda,
            KbM = entity.KbM,
            LcbPctLpp = entity.LcbPctLpp,
            KgEstM = null, // Not in model
            GmEstM = entity.GmEstM,
            EhpKw = entity.EhpKw,
            ShpKw = entity.ShpKw,
            FlagsJson = entity.FlagsJson,
            Score = entity.Score,
            Rank = entity.Rank,
            IsSelected = entity.IsSelected,
            GeometryJson = entity.GeometryJson,
            CreatedAt = entity.CreatedAt,

            // Provenance (Data-Driven Mode)
            ReferenceVesselId = entity.ReferenceVesselId,
            ReferenceVesselName = entity.ReferenceVesselName,
            SimilarityScore = entity.SimilarityScore,
            SolverMode = entity.SolverMode
        };
    }

    private static List<string> BuildFamilyHints(CreateSizingRunDto dto, ShipDParameterizationResult shipdResult)
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

        if (dto.Options?.FamilyHints != null)
        {
            foreach (var hint in dto.Options.FamilyHints)
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
}
