using Shared.DTOs;
using Shared.HullGenerators;
using Shared.HullGenerators.Models;
using Shared.Models.Sizing;

namespace HullSizingService.Services.Geometry;

/// <summary>
/// Service for generating hull geometry (offsets) from solver candidates
/// Uses form-coefficient-based parametric generation
/// </summary>
public class HullGeometryGeneratorService : IHullGeometryGeneratorService
{
    private readonly IHullGenerator _hullGenerator;
    private readonly ILogger<HullGeometryGeneratorService> _logger;

    public HullGeometryGeneratorService(
        ILogger<HullGeometryGeneratorService> logger)
    {
        _hullGenerator = new FormCoefficientHullGenerator();
        _logger = logger;
    }

    /// <summary>
    /// Generate offsets grid from a solver candidate
    /// </summary>
    public Task<OffsetsGridDto?> GenerateOffsetsFromCandidateAsync(
        Solver.SolverCandidate candidate,
        int numStations = 23,
        int numWaterlines = 13,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                _logger.LogDebug(
                    "[GEOMETRY_GEN] Generating offsets for candidate: L={Lpp}m, B={Beam}m, T={Draft}m, Cb={Cb}, Cp={Cp}, Cm={Cm}, Cwp={Cwp}, LCB={LCB}%",
                    candidate.LppM, candidate.BeamM, candidate.DraftM, candidate.Cb, candidate.Cp, candidate.Cm, candidate.Cwp, candidate.LcbPctLpp);

                // Create hull dimensions
                var dims = new HullDimensions(
                    candidate.LppM,
                    candidate.BeamM,
                    candidate.DraftM,
                    candidate.LcbPctLpp ?? 0m);

                // Generate geometry
                var geometry = _hullGenerator.Generate(
                    dims,
                    candidate.Cb,
                    candidate.Cp,
                    candidate.Cm,
                    candidate.Cwp,
                    numStations,
                    numWaterlines);

                // Convert to DTO format
                var offsetsGrid = new OffsetsGridDto
                {
                    Stations = geometry.Stations,
                    Waterlines = geometry.Waterlines,
                    Offsets = geometry.Offsets
                };

                _logger.LogInformation(
                    "[GEOMETRY_GEN] Successfully generated offsets: {StationCount} stations, {WaterlineCount} waterlines",
                    geometry.Stations.Count, geometry.Waterlines.Count);

                // Log computed coefficients for validation
                if (geometry.ComputedCoefficients != null)
                {
                    var computed = geometry.ComputedCoefficients;
                    _logger.LogDebug(
                        "[GEOMETRY_GEN] Computed coefficients: Cb={Cb}, Cp={Cp}, Cm={Cm}, Cwp={Cwp}, LCB={LCB}%",
                        computed.Cb, computed.Cp, computed.Cm, computed.Cwp, computed.LcbPercent);
                }

                return offsetsGrid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[GEOMETRY_GEN] Failed to generate offsets for candidate: L={Lpp}m, B={Beam}m, T={Draft}m",
                    candidate.LppM, candidate.BeamM, candidate.DraftM);
                return null;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Validate that generated offsets match solver form coefficients
    /// </summary>
    public Task<GeometryValidationResult> ValidateFormCoefficientsAsync(
        Solver.SolverCandidate candidate,
        OffsetsGridDto offsets,
        decimal tolerance = 0.10m,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                // Compute form coefficients from offsets
                var computed = ComputeFormCoefficientsFromOffsets(
                    offsets.Stations,
                    offsets.Waterlines,
                    offsets.Offsets,
                    candidate.LppM,
                    candidate.BeamM,
                    candidate.DraftM);

                // Calculate errors
                decimal cbError = Math.Abs(computed.Cb - candidate.Cb) / candidate.Cb;
                decimal cpError = Math.Abs(computed.Cp - candidate.Cp) / candidate.Cp;
                decimal cmError = Math.Abs(computed.Cm - candidate.Cm) / candidate.Cm;
                decimal cwpError = Math.Abs(computed.Cwp - candidate.Cwp) / candidate.Cwp;
                decimal? lcbError = candidate.LcbPctLpp.HasValue && computed.LcbPercent.HasValue
                    ? Math.Abs(computed.LcbPercent.Value - candidate.LcbPctLpp.Value)
                    : null;

                // Check if within tolerance
                bool isValid = cbError <= tolerance &&
                              cpError <= tolerance &&
                              cmError <= tolerance &&
                              cwpError <= tolerance &&
                              (lcbError == null || lcbError <= tolerance * 100m); // LCB tolerance is in percentage points

                var warnings = new List<string>();
                if (cbError > tolerance)
                    warnings.Add($"Cb error: {cbError * 100:F1}% (target: {candidate.Cb}, computed: {computed.Cb:F4})");
                if (cpError > tolerance)
                    warnings.Add($"Cp error: {cpError * 100:F1}% (target: {candidate.Cp}, computed: {computed.Cp:F4})");
                if (cmError > tolerance)
                    warnings.Add($"Cm error: {cmError * 100:F1}% (target: {candidate.Cm}, computed: {computed.Cm:F4})");
                if (cwpError > tolerance)
                    warnings.Add($"Cwp error: {cwpError * 100:F1}% (target: {candidate.Cwp}, computed: {computed.Cwp:F4})");
                if (lcbError.HasValue && lcbError > tolerance * 100m)
                    warnings.Add($"LCB error: {lcbError:F2}% (target: {candidate.LcbPctLpp}%, computed: {computed.LcbPercent:F2}%)");

                if (warnings.Any())
                {
                    _logger.LogWarning(
                        "[GEOMETRY_GEN] Form coefficient validation warnings for candidate: {Warnings}",
                        string.Join("; ", warnings));
                }

                return new GeometryValidationResult
                {
                    IsValid = isValid,
                    ComputedCb = computed.Cb,
                    ComputedCp = computed.Cp,
                    ComputedCm = computed.Cm,
                    ComputedCwp = computed.Cwp,
                    ComputedLcbPercent = computed.LcbPercent,
                    CbError = cbError,
                    CpError = cpError,
                    CmError = cmError,
                    CwpError = cwpError,
                    LcbError = lcbError,
                    Warnings = warnings
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GEOMETRY_GEN] Failed to validate form coefficients");
                return new GeometryValidationResult
                {
                    IsValid = false,
                    Warnings = new List<string> { $"Validation failed: {ex.Message}" }
                };
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Compute form coefficients from offsets (similar to generator's validation)
    /// </summary>
    private (decimal Cb, decimal Cp, decimal Cm, decimal Cwp, decimal? LcbPercent) ComputeFormCoefficientsFromOffsets(
        List<decimal> stations,
        List<decimal> waterlines,
        List<List<decimal>> offsets,
        decimal length,
        decimal beam,
        decimal draft)
    {
        // Filter waterlines up to design draft only
        var activeWaterlines = waterlines.Where(w => w <= draft).ToList();
        if (activeWaterlines.Count < 2)
        {
            activeWaterlines = waterlines.Take(Math.Min(waterlines.Count, 10)).ToList();
        }

        // Compute sectional areas
        var sectionAreas = new List<decimal>();
        foreach (var stationOffsets in offsets)
        {
            // Get half-breadths for active waterlines
            var activeHalfBreadths = new List<decimal>();
            var activeWaterlineZs = new List<decimal>();

            for (int j = 0; j < waterlines.Count && j < stationOffsets.Count; j++)
            {
                if (waterlines[j] <= draft)
                {
                    activeWaterlineZs.Add(waterlines[j]);
                    activeHalfBreadths.Add(stationOffsets[j]);
                }
            }

            // Integrate to get sectional area
            if (activeHalfBreadths.Count >= 2)
            {
                decimal halfArea = IntegrateTrapezoidal(activeWaterlineZs, activeHalfBreadths);
                decimal fullArea = 2m * halfArea; // Mirror to port side
                sectionAreas.Add(fullArea);
            }
            else
            {
                sectionAreas.Add(0m);
            }
        }

        // Compute volume
        decimal volume = IntegrateTrapezoidal(stations, sectionAreas);

        // Compute Cb
        decimal cb = volume > 0 ? volume / (length * beam * draft) : 0m;

        // Compute Cp
        decimal maxSectionArea = sectionAreas.Count > 0 ? sectionAreas.Max() : 0m;
        decimal cp = maxSectionArea > 0 && length > 0 ? volume / (maxSectionArea * length) : 0m;

        // Compute Cm
        int midshipIndex = sectionAreas.Count / 2;
        decimal midshipArea = midshipIndex < sectionAreas.Count ? sectionAreas[midshipIndex] : 0m;
        decimal cm = midshipArea > 0 && beam > 0 && draft > 0 ? midshipArea / (beam * draft) : 0m;

        // Compute Cwp - find waterline at design draft
        int designDraftIndex = -1;
        for (int j = 0; j < waterlines.Count; j++)
        {
            if (Math.Abs(waterlines[j] - draft) < 0.01m || (j > 0 && waterlines[j] > draft && waterlines[j - 1] <= draft))
            {
                designDraftIndex = j;
                break;
            }
        }
        if (designDraftIndex < 0) designDraftIndex = waterlines.Count - 1;

        var waterlineHalfBreadths = new List<decimal>();
        foreach (var stationOffsets in offsets)
        {
            if (designDraftIndex < stationOffsets.Count)
            {
                waterlineHalfBreadths.Add(stationOffsets[designDraftIndex]);
            }
            else
            {
                waterlineHalfBreadths.Add(0m);
            }
        }

        decimal waterplaneArea = IntegrateTrapezoidal(stations, waterlineHalfBreadths.Select(hb => 2m * hb).ToList());
        decimal cwp = waterplaneArea > 0 && length > 0 && beam > 0 ? waterplaneArea / (length * beam) : 0m;

        // Compute LCB
        decimal volumeMoment = IntegrateFirstMoment(stations, sectionAreas);
        decimal lcbPosition = volume > 0 ? volumeMoment / volume : length / 2m;
        decimal? lcbPercent = length > 0 ? ((lcbPosition / length) - 0.5m) * 100m : null;

        return (cb, cp, cm, cwp, lcbPercent);
    }

    // Helper methods for numerical integration (simple trapezoidal rule)

    private decimal IntegrateTrapezoidal(List<decimal> x, List<decimal> y)
    {
        if (x.Count != y.Count || x.Count < 2)
            return 0m;

        decimal sum = 0m;
        for (int i = 0; i < x.Count - 1; i++)
        {
            decimal dx = x[i + 1] - x[i];
            sum += dx * (y[i] + y[i + 1]) / 2m;
        }
        return sum;
    }

    private decimal IntegrateFirstMoment(List<decimal> x, List<decimal> y)
    {
        if (x.Count != y.Count || x.Count < 2)
            return 0m;

        decimal sum = 0m;
        for (int i = 0; i < x.Count - 1; i++)
        {
            decimal dx = x[i + 1] - x[i];
            decimal avgX = (x[i] + x[i + 1]) / 2m;
            decimal avgY = (y[i] + y[i + 1]) / 2m;
            sum += dx * avgX * avgY;
        }
        return sum;
    }
}
