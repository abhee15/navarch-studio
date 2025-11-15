using Microsoft.Extensions.Logging;
using Shared.HullGenerators.Fairing;
using Shared.HullGenerators.Integration;
using Shared.HullGenerators.Models;
using Shared.HullGenerators.Validation;

namespace Shared.HullGenerators.ParentHull;

/// <summary>
/// Parent hull-based hull generator using BSRA methodology
/// Primary method: Load parent hull, scale, adjust LCB, fair
/// Falls back to parametric method if parent hull unavailable
/// </summary>
public class ParentHullHullGenerator : IHullGenerator
{
    private readonly ILogger<ParentHullHullGenerator>? _logger;
    private readonly ParentHullLoader _loader;
    private readonly string? _defaultVesselType;

    public ParentHullHullGenerator(
        ILogger<ParentHullHullGenerator>? logger = null,
        string? defaultVesselType = null)
    {
        _logger = logger;
        _defaultVesselType = defaultVesselType;
        // Create a logger adapter for ParentHullLoader if needed
        _loader = new ParentHullLoader(null); // ParentHullLoader doesn't require logger
    }

    /// <summary>
    /// Generate hull offsets from form coefficients using parent hull approach
    /// </summary>
    public GeneratedHullGeometry Generate(
        HullDimensions dims,
        decimal cb,
        decimal cp,
        decimal cm,
        decimal cwp,
        int numStations = 23,
        int numWaterlines = 13)
    {
        // Validate inputs
        ValidateInputs(dims, cb, cp, cm, cwp, numStations, numWaterlines);

        // Use default vessel type from constructor, or fallback to "product_carrier"
        string vesselType = _defaultVesselType ?? "product_carrier";

        try
        {
            // Try to load parent hull
            var parentHull = _loader.LoadParentHull(vesselType, cb);

            _logger?.LogInformation(
                "Using parent hull: {VesselType}, Cb={Cb}, Source={Source}",
                parentHull.VesselType, parentHull.Cb, parentHull.Source);

            // Step 1: Scale to target dimensions
            var scaleFactors = ParentHullScaler.CalculateScaleFactors(
                parentHull, dims.Length, dims.Beam, dims.Draft);

            _logger?.LogDebug(
                "Scale factors: L={LengthScale:F3}, B={BreadthScale:F3}, T={DraftScale:F3}",
                scaleFactors.Length, scaleFactors.Breadth, scaleFactors.Draft);

            var scaledHull = ParentHullScaler.ScaleOffsets(parentHull, scaleFactors);

            // Step 2: Adjust LCB by swinging sections
            var adjustedOffsets = LCBSectionSwing.AdjustLCB(
                scaledHull,
                dims.LcbPercent,
                parentHull.LcbPercent,
                dims.Length);

            _logger?.LogDebug("LCB adjusted from {OldLCB}% to {NewLCB}%",
                parentHull.LcbPercent, dims.LcbPercent);

            // Step 3: Fair the lines using cubic splines
            var fairedOffsets = FairOffsets(
                scaledHull.Stations,
                scaledHull.Waterlines,
                adjustedOffsets);

            _logger?.LogDebug("Offsets faired using cubic splines");

            // Step 4: Validate against targets
            var computedCoeffs = ComputeFormCoefficients(
                scaledHull.Stations,
                scaledHull.Waterlines,
                fairedOffsets,
                dims.Length,
                dims.Beam,
                dims.Draft,
                cm);

            // Check if validation passes
            var validationResult = OffsetValidator.Validate(
                computedCoeffs,
                cb, cp, cm, cwp, dims.LcbPercent,
                dims.Length, dims.Beam, dims.Draft);

            if (!validationResult.IsValid)
            {
                _logger?.LogWarning(
                    "Validation failed: {Errors}",
                    string.Join(", ", validationResult.Errors));
            }

            return new GeneratedHullGeometry
            {
                Stations = scaledHull.Stations,
                Waterlines = scaledHull.Waterlines,
                Offsets = fairedOffsets,
                ComputedCoefficients = computedCoeffs
            };
        }
        catch (Exception ex) when (ex is FileNotFoundException || ex is InvalidOperationException)
        {
            _logger?.LogWarning(
                ex,
                "Parent hull not available for {VesselType}, Cb={Cb}. Fallback to parametric method required.",
                vesselType ?? "product_carrier", cb);

            // Re-throw to let factory handle fallback
            throw new InvalidOperationException(
                $"Parent hull not available for vessel type '{vesselType ?? "product_carrier"}' with Cb={cb}. " +
                "Use HullGeneratorFactory to get appropriate generator with fallback.",
                ex);
        }
    }

    /// <summary>
    /// Fair offsets using cubic spline fairing
    /// </summary>
    private List<List<decimal>> FairOffsets(
        List<decimal> stations,
        List<decimal> waterlines,
        List<List<decimal>> offsets)
    {
        var faired = new List<List<decimal>>();

        // Fair each waterline
        for (int wlIdx = 0; wlIdx < waterlines.Count; wlIdx++)
        {
            var halfBreadths = offsets.Select(st => st[wlIdx]).ToList();
            var fairedHalfBreadths = CubicSplineFairing.FairWaterline(
                stations, halfBreadths, waterlines[wlIdx]);

            faired.Add(fairedHalfBreadths);
        }

        // Transpose back to [station][waterline] format
        var result = new List<List<decimal>>();
        for (int stIdx = 0; stIdx < stations.Count; stIdx++)
        {
            var stationOffsets = faired.Select(wlOffsets => wlOffsets[stIdx]).ToList();
            result.Add(stationOffsets);
        }

        return result;
    }

    /// <summary>
    /// Compute form coefficients from generated offsets
    /// </summary>
    private FormCoefficients ComputeFormCoefficients(
        List<decimal> stations,
        List<decimal> waterlines,
        List<List<decimal>> offsets,
        decimal length,
        decimal beam,
        decimal draft,
        decimal cm)
    {
        // Calculate sectional areas at each station
        var sectionalAreas = new List<decimal>();
        for (int stIdx = 0; stIdx < stations.Count; stIdx++)
        {
            var stationOffsets = offsets[stIdx];
            // Integrate half-breadth vertically to get sectional area
            decimal area = 0;
            for (int wlIdx = 0; wlIdx < waterlines.Count - 1; wlIdx++)
            {
                decimal dz = waterlines[wlIdx + 1] - waterlines[wlIdx];
                decimal hbAvg = (stationOffsets[wlIdx] + stationOffsets[wlIdx + 1]) / 2;
                area += 2 * hbAvg * dz; // Factor of 2 for port + starboard
            }
            sectionalAreas.Add(area);
        }

        // Calculate volume using BSRA Simpson integration
        decimal volume = BSRASimpsonIntegration.CalculateVolume(stations, sectionalAreas, length);

        // Calculate Cb
        decimal cb = volume / (length * beam * draft);

        // Calculate Cp
        decimal amid = cm * beam * draft;
        decimal cp = volume / (amid * length);

        // Calculate LCB
        decimal lcb = BSRASimpsonIntegration.CalculateLCB(stations, sectionalAreas, length);
        decimal lcbPercent = (lcb - length / 2.0m) / length * 100m;

        // Calculate Cwp (waterplane area coefficient)
        var designWaterlineIdx = waterlines
            .Select((wl, idx) => new { wl, idx })
            .OrderBy(x => Math.Abs(x.wl - draft))
            .First().idx;

        var designWaterlineHalfBreadths = offsets.Select(st => st[designWaterlineIdx]).ToList();
        decimal waterplaneArea = BSRASimpsonIntegration.CalculateWaterplaneArea(
            stations, designWaterlineHalfBreadths, length);
        decimal cwp = waterplaneArea / (length * beam);

        return new FormCoefficients(cb, cp, cm, cwp, lcbPercent, volume);
    }

    /// <summary>
    /// Validate inputs
    /// </summary>
    private static void ValidateInputs(
        HullDimensions dims,
        decimal cb,
        decimal cp,
        decimal cm,
        decimal cwp,
        int numStations,
        int numWaterlines)
    {
        if (dims.Length <= 0)
            throw new ArgumentException("Length must be positive", nameof(dims));
        if (dims.Beam <= 0)
            throw new ArgumentException("Beam must be positive", nameof(dims));
        if (dims.Draft <= 0)
            throw new ArgumentException("Draft must be positive", nameof(dims));
        if (cb <= 0 || cb > 1)
            throw new ArgumentException("Cb must be between 0 and 1", nameof(cb));
        if (cp <= 0 || cp > 1)
            throw new ArgumentException("Cp must be between 0 and 1", nameof(cp));
        if (cm <= 0 || cm > 1)
            throw new ArgumentException("Cm must be between 0 and 1", nameof(cm));
        if (cwp <= 0 || cwp > 1)
            throw new ArgumentException("Cwp must be between 0 and 1", nameof(cwp));
        if (numStations != 23)
            throw new ArgumentException("Parent hull generator requires exactly 23 stations (BSRA standard)", nameof(numStations));
    }
}
