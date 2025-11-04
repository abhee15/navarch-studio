using System.Text;
using System.Text.Json;
using HullSizingService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Sizing;
using Shared.Models.Sizing;

namespace HullSizingService.Services;

public class CandidateDesignService : ICandidateDesignService
{
    private readonly SizingDbContext _context;
    private readonly ILogger<CandidateDesignService> _logger;

    public CandidateDesignService(
        SizingDbContext context,
        ILogger<CandidateDesignService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CandidateDesignDto?> GetByIdAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
    {
        var candidate = await _context.CandidateDesigns
            .Include(cd => cd.SizingRun)
                .ThenInclude(sr => sr.MissionCase)
            .Where(cd => cd.Id == id && cd.SizingRun.MissionCase.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        return candidate == null ? null : MapToDto(candidate);
    }

    public async Task<CandidateDesignDto?> UpdateAsync(Guid id, UpdateCandidateDesignDto dto, string tenantId, CancellationToken cancellationToken = default)
    {
        var candidate = await _context.CandidateDesigns
            .Include(cd => cd.SizingRun)
                .ThenInclude(sr => sr.MissionCase)
            .Where(cd => cd.Id == id && cd.SizingRun.MissionCase.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate == null) return null;

        if (dto.IsSelected.HasValue)
        {
            candidate.IsSelected = dto.IsSelected.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(candidate);
    }

    public async Task<bool> DeleteAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
    {
        var candidate = await _context.CandidateDesigns
            .Include(cd => cd.SizingRun)
                .ThenInclude(sr => sr.MissionCase)
            .Where(cd => cd.Id == id && cd.SizingRun.MissionCase.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate == null) return false;

        _context.CandidateDesigns.Remove(candidate);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<string?> ExportJsonAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
    {
        var candidate = await _context.CandidateDesigns
            .Include(cd => cd.SizingRun)
                .ThenInclude(sr => sr.MissionCase)
            .Where(cd => cd.Id == id && cd.SizingRun.MissionCase.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate == null) return null;

        var export = new
        {
            candidate.Id,
            candidate.HullFamily,
            PrincipalDimensions = new
            {
                LppM = candidate.LppM,
                LwlM = candidate.LwlM,
                LoaM = candidate.LoaM,
                BeamM = candidate.BM,
                DraftM = candidate.TM,
                DepthM = candidate.DM
            },
            Coefficients = new
            {
                Cb = candidate.Cb,
                Cp = candidate.Cp,
                Cwp = candidate.Cwp
            },
            Derived = new
            {
                DispM3 = candidate.DisplacementT / 1.025m,
                DispT = candidate.DisplacementT,
                Fn = candidate.Fn
            },
            Stability = new
            {
                KbM = candidate.KbM,
                LcbPctLpp = candidate.LcbPctLpp,
                GmEstM = candidate.GmEstM
            },
            Resistance = new
            {
                EhpKw = candidate.EhpKw,
                ShpKw = candidate.ShpKw
            },
            candidate.Score,
            candidate.Rank,
            candidate.IsSelected,
            candidate.CreatedAt
        };

        return JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<string?> ExportCsvAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
    {
        var candidate = await _context.CandidateDesigns
            .Include(cd => cd.SizingRun)
                .ThenInclude(sr => sr.MissionCase)
            .Where(cd => cd.Id == id && cd.SizingRun.MissionCase.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate == null) return null;

        var sb = new StringBuilder();
        sb.AppendLine("Parameter,Value,Unit");
        sb.AppendLine($"Hull Family,{candidate.HullFamily},-");
        sb.AppendLine($"Lpp,{candidate.LppM},m");
        sb.AppendLine($"Lwl,{candidate.LwlM},m");
        sb.AppendLine($"LOA,{candidate.LoaM},m");
        sb.AppendLine($"Beam,{candidate.BM},m");
        sb.AppendLine($"Draft,{candidate.TM},m");
        sb.AppendLine($"Depth,{candidate.DM},m");
        sb.AppendLine($"Cb,{candidate.Cb},-");
        sb.AppendLine($"Cp,{candidate.Cp},-");
        sb.AppendLine($"Cwp,{candidate.Cwp},-");
        sb.AppendLine($"Displacement,{candidate.DisplacementT / 1.025m},m³");
        sb.AppendLine($"Displacement,{candidate.DisplacementT},tonnes");
        sb.AppendLine($"Froude Number,{candidate.Fn},-");
        sb.AppendLine($"KB,{candidate.KbM},m");
        sb.AppendLine($"LCB,{candidate.LcbPctLpp},%Lpp");
        sb.AppendLine($"GM (est),{candidate.GmEstM},m");
        sb.AppendLine($"EHP,{candidate.EhpKw},kW");
        sb.AppendLine($"SHP,{candidate.ShpKw},kW");
        sb.AppendLine($"Score,{candidate.Score},-");
        sb.AppendLine($"Rank,{candidate.Rank},-");

        return sb.ToString();
    }

    public async Task<CandidateDesignDto?> AdjustParameterAsync(
        Guid id,
        AdjustParameterDto dto,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var candidate = await _context.CandidateDesigns
            .Include(cd => cd.SizingRun)
                .ThenInclude(sr => sr.MissionCase)
            .Where(cd => cd.Id == id && cd.SizingRun.MissionCase.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate == null) return null;

        var mission = candidate.SizingRun.MissionCase;

        // Apply the parameter adjustment
        var adjustedValue = dto.Value;
        switch (dto.Parameter.ToLower())
        {
            case "lppm":
                candidate.LppM = adjustedValue;
                candidate.LwlM = adjustedValue * 1.02m; // Approximate Lwl = Lpp * 1.02
                candidate.LoaM = adjustedValue * 1.05m; // Approximate LOA = Lpp * 1.05
                break;
            case "bm":
                candidate.BM = adjustedValue;
                break;
            case "tm":
                candidate.TM = adjustedValue;
                break;
            case "dm":
                candidate.DM = adjustedValue;
                break;
            case "cb":
                candidate.Cb = adjustedValue;
                break;
            case "cp":
                candidate.Cp = adjustedValue;
                break;
            case "cwp":
                candidate.Cwp = adjustedValue;
                break;
            default:
                throw new ArgumentException($"Parameter '{dto.Parameter}' is not adjustable");
        }

        // Recompute derived values (fast approximation)
        // Recalculate displacement
        var volDisp = candidate.LppM * candidate.BM * candidate.TM * candidate.Cb;
        candidate.DisplacementT = volDisp * 1.025m; // Seawater density

        // Recalculate Froude number
        var speedMs = mission.ServiceSpeedKn * 0.5144m;
        candidate.Fn = speedMs / (decimal)Math.Sqrt((double)(candidate.LwlM * 9.81m));

        // Recalculate lwlOverLambda
        var wavelength = 1.56m * (speedMs * speedMs) / 9.81m;
        candidate.LwlOverLambda = candidate.LwlM / wavelength;

        // NOTE: For full physics recomputation (resistance, stability), would need to:
        // - Re-run Holtrop method for EHP/SHP
        // - Re-run stability estimates for KB/LCB/GM
        // Currently preserving original values to maintain fast response time (<300ms target)

        _logger.LogInformation(
            "Parameter adjusted for candidate {Id}: {Parameter}={Value}, New Δ={Disp}t, Fn={Fn}",
            candidate.Id, dto.Parameter, dto.Value, candidate.DisplacementT, candidate.Fn);

        // Save changes
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(candidate);
    }

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
            KgEstM = null,
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

