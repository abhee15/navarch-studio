using System.Text;
using System.Text.Json;
using HullSizingService.Data;
using HullSizingService.Services.Geometry;
using HullSizingService.Services.Integration;
using HullSizingService.Services.ShipD;
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
    private readonly IShipDParameterAdapter? _shipdParameterAdapter;
    private readonly IDataServiceClient? _dataServiceClient;
    private readonly IShipDToHydroMapper _hydroMapper;

    public CandidateDesignService(
        SizingDbContext context,
        ILogger<CandidateDesignService> logger,
        IShipDHullGeometryService? shipdGeometryService,
        IShipDParameterAdapter? shipdParameterAdapter,
        IDataServiceClient? dataServiceClient,
        IShipDToHydroMapper hydroMapper)
    {
        _context = context;
        _logger = logger;
        _shipdGeometryService = shipdGeometryService;
        _shipdParameterAdapter = shipdParameterAdapter;
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

        // Store old values for ShipD vector adjustment
        var oldLpp = candidate.LppM;
        var oldBeam = candidate.BM;
        var oldDraft = candidate.TM;
        var oldCb = candidate.Cb;
        var oldCp = candidate.Cp;
        var oldCwp = candidate.Cwp;

        // Apply the parameter adjustment
        var adjustedValue = dto.Value;
        var paramLower = dto.Parameter.ToLower();

        _logger.LogInformation(
            "[CANDIDATE_ADJUST] Received parameter adjustment request: Parameter='{Parameter}', Value={Value}, CandidateId={CandidateId}",
            dto.Parameter, dto.Value, id);

        switch (paramLower)
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
            // Direct ShipD parameter adjustments (handled below)
            case "bowlengthratio":
            case "sternlengthratio":
            case "bowflareangle":
            case "bowcurvature":
            case "bowknuckle":
            case "deadriseangle":
            case "sternrakeangle":
            case "sterncurvature":
            case "sternknuckle":
            case "transomarea":
            case "transomwidth":
            case "hassheer":
            case "hastumblehome":
            case "hasbulb":
            case "bulblengthratio":
            case "bulbheightratio":
            case "bulbwidthratio":
            case "bulbasymmetry":
            case "bulbfilletradius":
                // These are handled in ShipD vector update below
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

        // Regenerate ShipD geometry with intelligent vector adjustment
        if (_shipdGeometryService != null && _shipdParameterAdapter != null && _dataServiceClient != null && !string.IsNullOrEmpty(candidate.ShipdParametersJson))
        {
            try
            {
                var originalVector = JsonSerializer.Deserialize<decimal[]>(candidate.ShipdParametersJson);
                if (originalVector != null && originalVector.Length == 45)
                {
                    // Get ShipD metadata
                    var shipdMetadata = await _dataServiceClient.GetShipDParameterMetadataAsync(cancellationToken);

                    decimal[] adjustedVector = originalVector;

                    // Determine adjustment type and apply intelligent scaling
                    switch (paramLower)
                    {
                        // Principal dimensions - scale vector to maintain form
                        case "lppm":
                        case "bm":
                        case "tm":
                        case "dm":
                            adjustedVector = _shipdParameterAdapter.AdjustVectorForDimensionChange(
                                originalVector,
                                oldLpp, candidate.LppM,
                                oldBeam, candidate.BM,
                                oldDraft, candidate.TM,
                                shipdMetadata);
                            _logger.LogInformation(
                                "[CANDIDATE_ADJUST] Applied dimension scaling: {Param} {Old}→{New}",
                                dto.Parameter,
                                paramLower == "lppm" ? oldLpp : paramLower == "bm" ? oldBeam : oldDraft,
                                adjustedValue);
                            break;

                        // Form coefficients - adjust form parameters
                        case "cb":
                            adjustedVector = _shipdParameterAdapter.AdjustVectorForCoefficientChange(
                                originalVector, "Cb", oldCb, candidate.Cb, shipdMetadata);
                            _logger.LogInformation(
                                "[CANDIDATE_ADJUST] Applied Cb adjustment: {Old}→{New}",
                                oldCb, candidate.Cb);
                            break;
                        case "cp":
                            adjustedVector = _shipdParameterAdapter.AdjustVectorForCoefficientChange(
                                originalVector, "Cp", oldCp, candidate.Cp, shipdMetadata);
                            _logger.LogInformation(
                                "[CANDIDATE_ADJUST] Applied Cp adjustment: {Old}→{New}",
                                oldCp, candidate.Cp);
                            break;
                        case "cwp":
                            adjustedVector = _shipdParameterAdapter.AdjustVectorForCoefficientChange(
                                originalVector, "Cwp", oldCwp, candidate.Cwp, shipdMetadata);
                            _logger.LogInformation(
                                "[CANDIDATE_ADJUST] Applied Cwp adjustment: {Old}→{New}",
                                oldCwp, candidate.Cwp);
                            break;

                        // Direct ShipD parameter adjustments
                        case "bowlengthratio":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 1, adjustedValue, shipdMetadata);
                            break;
                        case "sternlengthratio":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 2, adjustedValue, shipdMetadata);
                            break;
                        case "bowflareangle":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 8, adjustedValue, shipdMetadata);
                            break;
                        case "bowcurvature":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 9, adjustedValue, shipdMetadata);
                            break;
                        case "bowknuckle":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 10, adjustedValue, shipdMetadata);
                            break;
                        case "deadriseangle":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 19, adjustedValue, shipdMetadata);
                            break;
                        case "sternrakeangle":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 27, adjustedValue, shipdMetadata);
                            break;
                        case "sterncurvature":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 29, adjustedValue, shipdMetadata);
                            break;
                        case "sternknuckle":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 30, adjustedValue, shipdMetadata);
                            break;
                        case "transomarea":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 22, adjustedValue, shipdMetadata);
                            break;
                        case "transomwidth":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 28, adjustedValue, shipdMetadata);
                            break;
                        case "hassheer":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 20, adjustedValue > 0.5m ? 1m : 0m, shipdMetadata);
                            break;
                        case "hastumblehome":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 21, adjustedValue > 0.5m ? 1m : 0m, shipdMetadata);
                            break;
                        case "hasbulb":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 31, adjustedValue > 0.5m ? 1m : 0m, shipdMetadata);
                            break;
                        case "bulblengthratio":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 33, adjustedValue, shipdMetadata);
                            break;
                        case "bulbheightratio":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 34, adjustedValue, shipdMetadata);
                            break;
                        case "bulbwidthratio":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 35, adjustedValue, shipdMetadata);
                            break;
                        case "bulbasymmetry":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 36, adjustedValue, shipdMetadata);
                            break;
                        case "bulbfilletradius":
                            adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(originalVector, 37, adjustedValue, shipdMetadata);
                            break;
                    }

                    // Store the adjusted vector
                    candidate.ShipdParametersJson = JsonSerializer.Serialize(adjustedVector);

                    // Log the normalized parameter value for stern and bulbous parameters
                    if (paramLower.Contains("stern") || paramLower.Contains("bulb"))
                    {
                        int? paramIndex = paramLower switch
                        {
                            "sternrakeangle" => 27,
                            "sterncurvature" => 29,
                            "sternknuckle" => 30,
                            "hasbulb" => 31,
                            "bulblengthratio" => 33,
                            "bulbheightratio" => 34,
                            "bulbwidthratio" => 35,
                            "bulbasymmetry" => 36,
                            "bulbfilletradius" => 37,
                            _ => null
                        };

                        if (paramIndex.HasValue && paramIndex.Value < adjustedVector.Length)
                        {
                            _logger.LogInformation(
                                "[CANDIDATE_ADJUST] Parameter '{Parameter}' normalized: Input={Input}, Vector[{Index}]={Normalized}",
                                dto.Parameter, adjustedValue, paramIndex.Value, adjustedVector[paramIndex.Value]);
                        }
                    }

                    // Regenerate hull sections with adjusted vector + new dimensions
                    _logger.LogInformation(
                        "[CANDIDATE_ADJUST] Regenerating ShipD geometry for candidate {Id} with adjusted vector (Parameter='{Parameter}', Lpp={Lpp}m, Beam={Beam}m, Draft={Draft}m)",
                        candidate.Id, dto.Parameter, candidate.LppM, candidate.BM, candidate.TM);

                    var sections = await _shipdGeometryService.GenerateSectionsAsync(
                        adjustedVector,
                        candidate.LppM,
                        candidate.BM,
                        candidate.TM,
                        shipdMetadata,
                        stationCount: 20,
                        cancellationToken);

                    // Update geometry JSON
                    candidate.GeometryJson = JsonSerializer.Serialize(sections);
                    _logger.LogInformation(
                        "[CANDIDATE_ADJUST] Successfully regenerated ShipD geometry for candidate {Id} after {Parameter} adjustment: Generated {StationCount} stations",
                        candidate.Id, dto.Parameter, sections.Stations?.Count ?? 0);
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
        // Hybrid mode will queue background solver re-run for accurate results

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
        // Extract ShipD parameters from vector if available
        decimal[]? shipdVector = null;
        if (!string.IsNullOrEmpty(entity.ShipdParametersJson))
        {
            try
            {
                shipdVector = JsonSerializer.Deserialize<decimal[]>(entity.ShipdParametersJson);
            }
            catch
            {
                // Ignore deserialization errors - parameters will be null
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

            // Principal dimensions
            LppM = entity.LppM,
            LwlM = entity.LwlM,
            LoaM = entity.LoaM,
            BeamM = entity.BM,
            DraftM = entity.TM,
            DepthM = entity.DM,

            // Coefficients
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

            // Derived
            DispM3 = entity.DisplacementT / 1.025m,
            DispT = entity.DisplacementT,
            Fn = entity.Fn,
            LwlOverLambda = entity.LwlOverLambda,

            // Stability
            KbM = entity.KbM,
            LcbPctLpp = entity.LcbPctLpp,
            KgEstM = null,
            GmEstM = entity.GmEstM,

            // Resistance
            EhpKw = entity.EhpKw,
            ShpKw = entity.ShpKw,

            // Scoring
            FlagsJson = entity.FlagsJson,
            Score = entity.Score,
            Rank = entity.Rank,
            IsSelected = entity.IsSelected,

            // Geometry
            GeometryJson = entity.GeometryJson,
            CreatedAt = entity.CreatedAt,

            // Provenance
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
