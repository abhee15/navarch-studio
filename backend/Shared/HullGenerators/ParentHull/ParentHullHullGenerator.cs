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
    /// Note: ShipD family parameters are ignored as parent hulls already have their shape characteristics
    /// </summary>
    public GeneratedHullGeometry Generate(
        HullDimensions dims,
        decimal cb,
        decimal cp,
        decimal cm,
        decimal cwp,
        int numStations = 23,
        int numWaterlines = 13,
        string? bowFamily = null,
        string? midshipFamily = null,
        string? sternFamily = null,
        string? vesselType = null)
    {
        // Validate inputs
        ValidateInputs(dims, cb, cp, cm, cwp, numStations, numWaterlines);

        // Use vessel type from parameter, or default from constructor, or fallback to "product_carrier"
        // Note: ShipD family parameters are ignored as parent hulls already have their shape characteristics
        string effectiveVesselType = vesselType ?? _defaultVesselType ?? "product_carrier";

        try
        {
            // Try to load parent hull
            var parentHull = _loader.LoadParentHull(effectiveVesselType, cb);

            _logger?.LogInformation(
                "Using parent hull: {VesselType}, Cb={Cb}, Source={Source}",
                parentHull.VesselType, parentHull.Cb, parentHull.Source);

            // Step 1: Scale to target dimensions
            var scaleFactors = ParentHullScaler.CalculateScaleFactors(
                parentHull, dims.Length, dims.Beam, dims.Draft);

            _logger?.LogDebug(
                "Scale factors: L={LengthScale:F3}, B={BreadthScale:F3}, T={DraftScale:F3}",
                scaleFactors.Length, scaleFactors.Breadth, scaleFactors.Draft);

            var scaledHull = ParentHullScaler.ScaleOffsets(parentHull, scaleFactors, dims.Length, dims.Beam);

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

            // Step 3.5: Re-apply bow and stern closure fixes after LCB adjustment and fairing
            // The LCB adjustment and fairing can alter the forward and aft stations, so we need to ensure
            // both ends still close properly after these operations
            fairedOffsets = EnsureBowClosure(
                fairedOffsets,
                scaledHull.Waterlines,
                dims.Beam);

            fairedOffsets = EnsureSternClosure(
                fairedOffsets,
                scaledHull.Waterlines,
                dims.Beam);

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
    /// Ensure bow closure after LCB adjustment and fairing
    /// Re-applies bow closure fix to prevent flared appearance
    /// </summary>
    private List<List<decimal>> EnsureBowClosure(
        List<List<decimal>> offsets,
        List<decimal> waterlines,
        decimal beam)
    {
        if (offsets.Count == 0 || waterlines.Count == 0)
            return offsets;

        var result = new List<List<decimal>>(offsets);
        var maxDraft = waterlines[waterlines.Count - 1];
        if (maxDraft <= 0) maxDraft = 1m;

        // Only fix forward stations: last 20% of stations, but at least 1 and at most 4
        // For very small hulls (< 10 stations), only fix the very last station
        int numBowStations = result.Count < 10
            ? 1
            : Math.Max(1, Math.Min(4, (int)Math.Ceiling(result.Count * 0.2m)));
        int firstBowStationIdx = result.Count - numBowStations;

        for (int stIdx = firstBowStationIdx; stIdx < result.Count; stIdx++)
        {
            var stationOffsets = result[stIdx];
            if (stationOffsets.Count == 0) continue;

            // Calculate how far forward this station is (0 = start of bow region, 1 = forward perpendicular)
            decimal forwardness = numBowStations > 1
                ? (decimal)(stIdx - firstBowStationIdx) / (numBowStations - 1)
                : 1m; // If only one station, it's fully forward

            // Maximum allowed half-breadth decreases as we go forward
            var maxStationHalfBreadth = beam * (0.15m + 0.3m * (1m - forwardness));

            // Ensure keel has zero or very small half-breadth
            var keelHalfBreadth = stationOffsets[0];
            var maxKeelHalfBreadth = beam * (0.02m + 0.03m * (1m - forwardness));

            if (keelHalfBreadth > maxKeelHalfBreadth)
            {
                stationOffsets[0] = Math.Min(keelHalfBreadth, maxKeelHalfBreadth);
            }

            // Ensure the station tapers properly from keel to deck
            for (int wlIdx = 1; wlIdx < stationOffsets.Count && wlIdx < waterlines.Count; wlIdx++)
            {
                var currentHalfBreadth = stationOffsets[wlIdx];
                var prevHalfBreadth = stationOffsets[wlIdx - 1];
                var waterlineZ = waterlines[wlIdx];
                var waterlineNorm = waterlineZ / maxDraft;

                // Cap the maximum half-breadth
                if (currentHalfBreadth > maxStationHalfBreadth)
                {
                    var maxAllowed = maxStationHalfBreadth * (0.3m + 0.7m * waterlineNorm);
                    stationOffsets[wlIdx] = Math.Min(currentHalfBreadth, maxAllowed);
                }

                // Ensure smooth tapering
                if (currentHalfBreadth < prevHalfBreadth * 0.85m)
                {
                    stationOffsets[wlIdx] = Math.Max(currentHalfBreadth, prevHalfBreadth * 0.9m);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Ensure stern closure after LCB adjustment and fairing
    /// POST-PROCESSING ONLY: This does NOT change offset generation, only corrects scaling artifacts
    /// Checks if stern is wider than adjacent station (indicates scaling artifact)
    /// </summary>
    private List<List<decimal>> EnsureSternClosure(
        List<List<decimal>> offsets,
        List<decimal> waterlines,
        decimal beam)
    {
        if (offsets.Count < 2 || waterlines.Count == 0)
            return offsets;

        var result = new List<List<decimal>>(offsets);
        var maxDraft = waterlines[waterlines.Count - 1];
        if (maxDraft <= 0) maxDraft = 1m;

        // Fix the first station (aft perpendicular) if it's wider than adjacent station
        var aftStationOffsets = result[0];
        var adjacentStationOffsets = result[1]; // Station 1 (next forward)

        if (aftStationOffsets.Count > 0 && adjacentStationOffsets.Count > 0)
        {
            // Check if stern is significantly wider than adjacent station at any waterline
            bool sternNeedsFix = false;
            for (int wlIdx = 0; wlIdx < Math.Min(aftStationOffsets.Count, adjacentStationOffsets.Count); wlIdx++)
            {
                // If stern is more than 20% wider than adjacent station, it's likely a scaling artifact
                if (aftStationOffsets[wlIdx] > adjacentStationOffsets[wlIdx] * 1.2m)
                {
                    sternNeedsFix = true;
                    break;
                }
            }

            // Also check absolute threshold as fallback (more than 40% of beam is definitely wrong)
            var maxSternHalfBreadth = aftStationOffsets.Max();
            if (maxSternHalfBreadth > beam * 0.4m)
            {
                sternNeedsFix = true;
            }

            // Only apply fix if stern is wider than it should be
            if (sternNeedsFix)
            {
                // Cap stern to be no wider than adjacent station + 10% (slight taper is OK)
                var maxAllowedSternHalfBreadth = adjacentStationOffsets.Max() * 1.1m;
                // But also cap at absolute maximum (40% of beam)
                maxAllowedSternHalfBreadth = Math.Min(maxAllowedSternHalfBreadth, beam * 0.4m);

                // Ensure keel has zero or very small half-breadth
                var keelHalfBreadth = aftStationOffsets[0];
                var maxKeelHalfBreadth = beam * 0.05m; // Max 5% of beam at keel

                if (keelHalfBreadth > maxKeelHalfBreadth)
                {
                    aftStationOffsets[0] = Math.Min(keelHalfBreadth, maxKeelHalfBreadth);
                }

                // Ensure the stern tapers properly from keel to deck
                for (int wlIdx = 1; wlIdx < aftStationOffsets.Count && wlIdx < waterlines.Count; wlIdx++)
                {
                    var currentHalfBreadth = aftStationOffsets[wlIdx];
                    var prevHalfBreadth = aftStationOffsets[wlIdx - 1];
                    var adjacentHalfBreadth = wlIdx < adjacentStationOffsets.Count
                        ? adjacentStationOffsets[wlIdx]
                        : adjacentStationOffsets[adjacentStationOffsets.Count - 1];
                    var waterlineZ = waterlines[wlIdx];
                    var waterlineNorm = waterlineZ / maxDraft;

                    // Cap stern to be no wider than adjacent station + small margin
                    var maxAllowedForThisWaterline = Math.Min(
                        adjacentHalfBreadth * 1.1m, // 10% wider than adjacent
                        maxAllowedSternHalfBreadth * (0.3m + 0.7m * waterlineNorm) // Allow some increase with height
                    );

                    if (currentHalfBreadth > maxAllowedForThisWaterline)
                    {
                        aftStationOffsets[wlIdx] = Math.Min(currentHalfBreadth, maxAllowedForThisWaterline);
                    }

                    // Ensure smooth tapering
                    if (currentHalfBreadth < prevHalfBreadth * 0.8m)
                    {
                        aftStationOffsets[wlIdx] = Math.Max(currentHalfBreadth, prevHalfBreadth * 0.85m);
                    }
                }
            }
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
