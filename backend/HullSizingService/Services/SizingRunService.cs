using System.Diagnostics;
using System.Text.Json;
using HullSizingService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Sizing;
using Shared.Models.Sizing;

namespace HullSizingService.Services;

public class SizingRunService : ISizingRunService
{
    private readonly SizingDbContext _context;
    private readonly Solver.IFirstPrinciplesSolver _solver;
    private readonly ILogger<SizingRunService> _logger;

    public SizingRunService(
        SizingDbContext context,
        Solver.IFirstPrinciplesSolver solver,
        ILogger<SizingRunService> logger)
    {
        _context = context;
        _solver = solver;
        _logger = logger;
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

        var run = new SizingRun
        {
            Id = Guid.NewGuid(),
            MissionCaseId = dto.MissionCaseId,
            Mode = dto.Mode,
            LocksJson = dto.Locks != null ? JsonSerializer.Serialize(dto.Locks) : null,
            OptionsJson = dto.Options != null ? JsonSerializer.Serialize(dto.Options) : null,
            Status = "running",
            CreatedAt = DateTime.UtcNow
        };

        _context.SizingRuns.Add(run);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            // Call first-principles solver
            var solverRequest = new Solver.SolverRequest(
                MissionCase: missionCase,
                Locks: dto.Locks != null ? new Solver.SizingLocksDto(
                    dto.Locks.KeepFn,
                    dto.Locks.KeepLOverB,
                    dto.Locks.KeepBOverT,
                    dto.Locks.KeepDOverT,
                    dto.Locks.KeepCbBand
                ) : null,
                Options: dto.Options != null ? new Solver.SizingOptionsDto(
                    dto.Options.FamilyHints,
                    dto.Options.MaxCandidates,
                    dto.Options.MinFn,
                    dto.Options.MaxFn
                ) : null
            );

            var solverCandidates = await _solver.SolveAsync(solverRequest, cancellationToken);

            // Convert solver candidates to database entities
            var candidateEntities = new List<CandidateDesign>();
            for (int i = 0; i < solverCandidates.Count; i++)
            {
                var sc = solverCandidates[i];
                var entity = new CandidateDesign
                {
                    Id = Guid.NewGuid(),
                    SizingRunId = run.Id,
                    HullFamily = sc.HullFamily,
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
                    GeometryJson = null, // Geometry generated on demand
                    CreatedAt = DateTime.UtcNow
                };

                candidateEntities.Add(entity);
            }

            _context.CandidateDesigns.AddRange(candidateEntities);

            run.Status = "completed";
            run.ComputeTimeMs = (int)sw.ElapsedMilliseconds;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("[SIZING_RUN] Completed run {RunId} in {ElapsedMs}ms, generated {Count} candidates",
                run.Id, sw.ElapsedMilliseconds, candidateEntities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SIZING_RUN] Failed to generate candidates for run {RunId}", run.Id);
            run.Status = "failed";
            run.ErrorMessage = ex.Message;
            await _context.SaveChangesAsync(cancellationToken);
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

        if (run == null) return new List<CandidateDesignDto>();

        var candidates = await _context.CandidateDesigns
            .Where(cd => cd.SizingRunId == runId)
            .OrderBy(cd => cd.Rank)
            .ToListAsync(cancellationToken);

        return candidates.Select(MapCandidateToDto).ToList();
    }

    private static SizingRunDto MapToDto(SizingRun entity, int candidateCount)
    {
        return new SizingRunDto
        {
            Id = entity.Id,
            MissionCaseId = entity.MissionCaseId,
            Mode = entity.Mode,
            LocksJson = entity.LocksJson,
            OptionsJson = entity.OptionsJson,
            RunStatus = entity.Status,
            ComputeTimeMs = entity.ComputeTimeMs,
            ErrorMessage = entity.ErrorMessage,
            CreatedAt = entity.CreatedAt,
            CandidateCount = candidateCount
        };
    }

    private static CandidateDesignDto MapCandidateToDto(CandidateDesign entity)
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
            GeomParamsJson = entity.GeometryJson,
            CreatedAt = entity.CreatedAt
        };
    }
}

