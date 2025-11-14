using System.Text;
using System.Text.Json;
using HullSizingService.Data;
using HullSizingService.Services.Geometry;
using HullSizingService.Services.Integration;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;
using Shared.DTOs.Hydrostatics;
using Shared.DTOs.Sizing;
using Shared.Models.Sizing;

namespace HullSizingService.Services;

public class CandidateDesignService : ICandidateDesignService
{
    private readonly SizingDbContext _context;
    private readonly ILogger<CandidateDesignService> _logger;
    private readonly IShipDHullGeometryService? _shipdGeometryService;
    private readonly IDataServiceClient? _dataServiceClient;
    private readonly IShipDToHydroMapper _hydroMapper;

    public CandidateDesignService(
        SizingDbContext context,
        ILogger<CandidateDesignService> logger,
        IShipDHullGeometryService? shipdGeometryService,
        IDataServiceClient? dataServiceClient,
        IShipDToHydroMapper hydroMapper)
    {
        _context = context;
        _logger = logger;
        _shipdGeometryService = shipdGeometryService;
        _dataServiceClient = dataServiceClient;
        _hydroMapper = hydroMapper;
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

        // Regenerate ShipD geometry if parameters are available
        if (_shipdGeometryService != null && _dataServiceClient != null && !string.IsNullOrEmpty(candidate.ShipdParametersJson))
        {
            try
            {
                var shipdVector = JsonSerializer.Deserialize<decimal[]>(candidate.ShipdParametersJson);
                if (shipdVector != null && shipdVector.Length == 45)
                {
                    // Get ShipD metadata
                    var shipdMetadata = await _dataServiceClient.GetShipDParameterMetadataAsync(cancellationToken);

                    // Regenerate hull sections with updated dimensions
                    var sections = await _shipdGeometryService.GenerateSectionsAsync(
                        shipdVector,
                        candidate.LppM,
                        candidate.BM,
                        candidate.TM,
                        shipdMetadata,
                        stationCount: 20,
                        cancellationToken);

                    // Update geometry JSON
                    candidate.GeometryJson = JsonSerializer.Serialize(sections);
                    _logger.LogInformation(
                        "[CANDIDATE_ADJUST] Regenerated ShipD geometry for candidate {Id} after {Parameter} adjustment",
                        candidate.Id, dto.Parameter);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[CANDIDATE_ADJUST] Failed to regenerate ShipD geometry for candidate {Id} after {Parameter} adjustment. Geometry may be out of sync.",
                    candidate.Id, dto.Parameter);
                // Continue without geometry update - don't fail the adjustment
            }
        }

        // NOTE: For full physics recomputation (resistance, stability), would need to:
        // - Re-run Holtrop method for EHP/SHP
        // - Re-run stability estimates for KB/LCB/GM
        // Currently preserving original values to maintain fast response time (<300ms target)

        _logger.LogInformation(
            "[CANDIDATE_ADJUST] Parameter adjusted for candidate {Id}: {Parameter}={Value}, New Δ={Disp}t, Fn={Fn}",
            candidate.Id, dto.Parameter, dto.Value, candidate.DisplacementT, candidate.Fn);

        // Save changes
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(candidate);
    }

    public async Task<PushToHydrostaticsResultDto?> PushToHydrostaticsAsync(
        Guid id,
        PushToHydrostaticsRequestDto request,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (_dataServiceClient == null || _shipdGeometryService == null)
        {
            throw new InvalidOperationException("Hydrostatics integration is not configured for this service.");
        }

        request ??= new PushToHydrostaticsRequestDto();

        var candidate = await _context.CandidateDesigns
            .Include(cd => cd.SizingRun)
                .ThenInclude(sr => sr.MissionCase)
            .Where(cd => cd.Id == id && cd.SizingRun.MissionCase.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(candidate.ShipdParametersJson))
        {
            throw new InvalidOperationException("Candidate is missing ShipD parameters required for geometry export.");
        }

        var mission = candidate.SizingRun.MissionCase;
        var idempotencyKey = request.IdempotencyKey ?? request.SourceDesign?.IdempotencyKey ?? candidate.Id.ToString();

        var existingPush = await _context.PushOperations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.CandidateId == candidate.Id || p.IdempotencyKey == idempotencyKey,
                cancellationToken);

        if (existingPush != null)
        {
            _logger.LogInformation("[PUSH] Candidate {CandidateId} already pushed as vessel {VesselId}", candidate.Id, existingPush.VesselId);
            return new PushToHydrostaticsResultDto
            {
                VesselId = existingPush.VesselId,
                SourceDesign = new SourceDesignDto
                {
                    CandidateId = candidate.Id,
                    SizingRunId = candidate.SizingRunId,
                    MissionCaseId = mission.Id,
                    UserId = mission.UserId,
                    MissionName = mission.Name,
                    SourceSystem = "HullSizingService",
                    IdempotencyKey = existingPush.IdempotencyKey,
                    OriginCreatedAt = candidate.CreatedAt,
                    PushedAt = existingPush.CreatedAt
                }
            };
        }

        var shipdVector = JsonSerializer.Deserialize<decimal[]>(candidate.ShipdParametersJson);
        if (shipdVector == null || shipdVector.Length == 0)
        {
            throw new InvalidOperationException("Unable to parse ShipD parameter vector for candidate.");
        }

        var metadata = await _dataServiceClient.GetShipDParameterMetadataAsync(cancellationToken);
        if (metadata.Count == 0)
        {
            throw new InvalidOperationException("Failed to load ShipD parameter metadata from DataService.");
        }

        var sections = await _shipdGeometryService.GenerateSectionsAsync(
            shipdVector,
            candidate.LppM,
            candidate.BM,
            candidate.TM,
            metadata,
            stationCount: 25,
            cancellationToken);

        var geometry = _hydroMapper.ConvertSections(sections, candidate.LppM);
        var vesselDto = BuildVesselDto(candidate, mission, request);
        vesselDto.SourceDesign = BuildSourceDesign(candidate, mission, request, idempotencyKey);

        var importRequest = new HydrostaticsImportRequestDto
        {
            Vessel = vesselDto,
            Stations = geometry.stations,
            Waterlines = geometry.waterlines,
            Offsets = geometry.offsets,
            IdempotencyKey = vesselDto.SourceDesign?.IdempotencyKey ?? idempotencyKey,
            CreateDefaultLoadcase = true
        };

        var importResult = await _dataServiceClient.ImportHydrostaticsVesselAsync(importRequest, cancellationToken);
        if (importResult == null)
        {
            throw new InvalidOperationException("DataService rejected the Hydrostatics import request.");
        }

        await SavePushOperationAsync(candidate.Id, importResult.Id, importRequest.IdempotencyKey!, cancellationToken);

        return new PushToHydrostaticsResultDto
        {
            VesselId = importResult.Id,
            SourceDesign = importResult.SourceDesign ?? vesselDto.SourceDesign
        };
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
            GeometryJson = entity.GeometryJson,
            CreatedAt = entity.CreatedAt
        };
    }

    private static VesselDto BuildVesselDto(CandidateDesign candidate, MissionCase mission, PushToHydrostaticsRequestDto request)
    {
        var vesselName = string.IsNullOrWhiteSpace(request.VesselName)
            ? $"{mission.Name ?? "Candidate"} - {candidate.HullFamily} #{candidate.Rank}"
            : request.VesselName;

        var description = string.IsNullOrWhiteSpace(request.Description)
            ? $"Generated from mission \"{mission.Name}\" via HullSizingService."
            : request.Description;

        var shipdCategory = request.ShipdCategory ?? candidate.VesselCategory ?? mission.MissionCategory;
        var shipdType = request.ShipdType ?? candidate.VesselType ?? mission.MissionType;
        var shipdBow = request.ShipdBowFamily ?? candidate.BowFamily ?? mission.BowFamily;
        var shipdMidship = request.ShipdMidshipFamily ?? candidate.MidshipFamily ?? mission.MidshipFamily;
        var shipdStern = request.ShipdSternFamily ?? candidate.SternFamily ?? mission.SternFamily;
        var shipdMaskVersion = request.ShipdMaskVersion ?? candidate.FamilyMaskVersion ?? mission.FamilyMaskVersion;

        return new VesselDto
        {
            Name = vesselName,
            Description = description,
            ShipdCategory = shipdCategory,
            ShipdType = shipdType,
            ShipdTypeDisplayName = request.ShipdTypeDisplayName ?? shipdType,
            ShipdBowFamily = shipdBow,
            ShipdMidshipFamily = shipdMidship,
            ShipdSternFamily = shipdStern,
            ShipdMaskVersion = shipdMaskVersion,
            ShipdParametersJson = candidate.ShipdParametersJson,
            Lpp = candidate.LppM,
            Beam = candidate.BM,
            DesignDraft = candidate.TM,
            Units = "SI",
            Metadata = new VesselMetadataDto
            {
                VesselType = shipdType ?? candidate.HullFamily,
                Size = mission.MissionCategory,
                BlockCoefficient = candidate.Cb,
                HullFamily = candidate.HullFamily,
                ShipdCategory = shipdCategory,
                ShipdType = shipdType,
                ShipdMaskVersion = shipdMaskVersion
            }
        };
    }

    private static SourceDesignDto BuildSourceDesign(
        CandidateDesign candidate,
        MissionCase mission,
        PushToHydrostaticsRequestDto request,
        string idempotencyKey)
    {
        return new SourceDesignDto
        {
            CandidateId = candidate.Id,
            SizingRunId = candidate.SizingRunId,
            MissionCaseId = mission.Id,
            UserId = mission.UserId,
            MissionName = mission.Name,
            RunName = candidate.SizingRun.Mode,
            DesignName = request.SourceDesign?.DesignName ?? candidate.HullFamily,
            SourceSystem = request.SourceDesign?.SourceSystem ?? "HullSizingService",
            IdempotencyKey = idempotencyKey,
            OriginCreatedAt = candidate.CreatedAt,
            PushedAt = DateTime.UtcNow,
            UserDisplayName = request.SourceDesign?.UserDisplayName
        };
    }

    private async Task SavePushOperationAsync(Guid candidateId, Guid vesselId, string idempotencyKey, CancellationToken cancellationToken)
    {
        var pushOp = new PushOperation
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            VesselId = vesselId,
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTime.UtcNow
        };

        _context.PushOperations.Add(pushOp);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

