using Shared.HullGenerators.Models;

namespace Shared.HullGenerators;

/// <summary>
/// Form-coefficient-based parametric hull generator
/// Generates realistic hull offsets from form coefficients (Cb, Cp, Cm, Cwp, LCB)
/// Based on parametric methods from naval architecture literature
/// </summary>
public class FormCoefficientHullGenerator : IHullGenerator
{
    // Calibration parameters (can be tuned to match BSRA/Series 60)
    private const decimal SECTIONAL_AREA_EXPONENT = 2.0m; // Controls fullness of ends
    private const decimal WATERLINE_BOW_EXPONENT = 2.5m; // Controls bow shape
    private const decimal WATERLINE_STERN_EXPONENT = 2.0m; // Controls stern shape
    private const decimal WATERLINE_FULLNESS_FACTOR = 0.5m; // Bow vs stern fullness balance

    /// <summary>
    /// Generate hull offsets from form coefficients
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

        // Generate BSRA-compatible station layout
        var stations = GenerateBSRAStations(dims.Length, numStations);

        // Generate waterlines (0 to 1.3T for extended visualization)
        var waterlines = GenerateWaterlines(dims.Draft, numWaterlines);

        // Step 1: Generate sectional area curve from Cp and LCB
        var sectionalAreas = GenerateSectionalAreaCurve(
            stations, dims.Length, dims.Beam, dims.Draft, cm, cp, dims.LcbPercent);

        // Step 2: Generate waterline half-breadths from Cwp
        var waterlineHalfBreadths = GenerateWaterlineHalfBreadths(
            stations, dims.Length, dims.Beam, cwp);

        // Step 3: Generate section shapes from Cm (normalized profile)
        var sectionShapeProfile = GenerateSectionShapes(
            waterlines, dims.Beam, dims.Draft, cm);

        // Step 4: Combine to generate offsets
        var offsets = CombineToOffsets(
            stations, waterlines, sectionalAreas, sectionShapeProfile, waterlineHalfBreadths,
            dims.Beam, dims.Draft, cm);

        // Step 5: Validate and compute form coefficients
        var computedCoeffs = ComputeFormCoefficients(
            stations, waterlines, offsets, dims.Length, dims.Beam, dims.Draft);

        return new GeneratedHullGeometry
        {
            Stations = stations,
            Waterlines = waterlines,
            Offsets = offsets,
            ComputedCoefficients = computedCoeffs
        };
    }

    /// <summary>
    /// Generate BSRA-compatible station layout
    /// Stations: 0, 0.25, 0.5, 0.75, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 5.0, 6.0, 6.5, 7.0, 7.5, 8.0, 8.5, 9.0, 9.25, 9.5, 9.75, 10.0
    /// </summary>
    private List<decimal> GenerateBSRAStations(decimal length, int numStations)
    {
        var stations = new List<decimal>();

        if (numStations == 23)
        {
            // Standard BSRA layout
            var stationFractions = new[]
            {
                0.0m, 0.25m, 0.5m, 0.75m, 1.0m, 1.5m, 2.0m, 2.5m, 3.0m, 3.5m,
                4.0m, 5.0m, 6.0m, 6.5m, 7.0m, 7.5m, 8.0m, 8.5m, 9.0m, 9.25m, 9.5m, 9.75m, 10.0m
            };

            foreach (var fraction in stationFractions)
            {
                stations.Add(length * fraction / 10.0m);
            }
        }
        else
        {
            // Equally spaced stations
            for (int i = 0; i < numStations; i++)
            {
                stations.Add(length * i / (numStations - 1));
            }
        }

        return stations;
    }

    /// <summary>
    /// Generate waterlines from 0 to 1.3T
    /// </summary>
    private List<decimal> GenerateWaterlines(decimal draft, int numWaterlines)
    {
        var waterlines = new List<decimal>();
        decimal maxZ = draft * 1.3m; // Extend above design draft for visualization

        for (int j = 0; j < numWaterlines; j++)
        {
            waterlines.Add(maxZ * j / (numWaterlines - 1));
        }

        return waterlines;
    }

    /// <summary>
    /// Generate sectional area curve from Cp and LCB
    /// Uses raised cosine base function: φ(ξ) = 1 - cos(πξ) where ξ = x/L
    /// </summary>
    private List<decimal> GenerateSectionalAreaCurve(
        List<decimal> stations,
        decimal length,
        decimal beam,
        decimal draft,
        decimal cm,
        decimal cp,
        decimal lcbPercent)
    {
        // Midship section area
        decimal amid = cm * beam * draft;

        // Target volume from Cp
        decimal targetVolume = cp * amid * length;

        var sectionalAreas = new List<decimal>();
        var baseShape = new List<decimal>();

        // Generate base shape using raised cosine
        foreach (var x in stations)
        {
            decimal xi = x / length; // Normalized position (0 to 1)

            // Raised cosine: φ(ξ) = 1 - cos(πξ)
            decimal phi = 1m - (decimal)Math.Cos((double)((decimal)Math.PI * xi));

            // Apply exponent for fullness control
            decimal shapeValue = (decimal)Math.Pow((double)phi, (double)SECTIONAL_AREA_EXPONENT);
            baseShape.Add(shapeValue);
        }

        // Apply LCB shift (skew the curve)
        var shiftedShape = ApplyLCBShift(baseShape, stations, length, lcbPercent);

        // Normalize to match Cp
        var tempAreas = shiftedShape.Select(s => s * amid).ToList();
        decimal currentVolume = IntegrateTrapezoidal(stations, tempAreas);
        decimal scaleFactor = currentVolume > 0 ? targetVolume / currentVolume : 1m;

        foreach (var shapeValue in shiftedShape)
        {
            decimal area = shapeValue * amid * scaleFactor;
            sectionalAreas.Add(Math.Max(0m, area)); // Ensure non-negative
        }

        // Verify volume matches target
        decimal finalVolume = IntegrateTrapezoidal(stations, sectionalAreas);
        if (finalVolume > 0 && Math.Abs(finalVolume - targetVolume) / targetVolume > 0.01m)
        {
            // Re-scale if still off
            decimal adjustFactor = targetVolume / finalVolume;
            for (int i = 0; i < sectionalAreas.Count; i++)
            {
                sectionalAreas[i] *= adjustFactor;
            }
        }

        return sectionalAreas;
    }

    /// <summary>
    /// Apply LCB shift to sectional area curve
    /// Positive LCB% = forward shift, Negative = aft shift
    /// </summary>
    private List<decimal> ApplyLCBShift(
        List<decimal> baseShape,
        List<decimal> stations,
        decimal length,
        decimal lcbPercent)
    {
        if (Math.Abs(lcbPercent) < 0.01m)
        {
            return baseShape; // No shift needed
        }

        // Convert LCB% to shift distance
        // LCB% is from aft perpendicular, positive forward
        // We need to shift the curve to achieve this LCB
        decimal targetLCB = length * (0.5m + lcbPercent / 100m); // Convert % to position

        // Calculate current LCB
        decimal currentVolume = IntegrateTrapezoidal(stations, baseShape);
        decimal currentMoment = IntegrateFirstMoment(stations, baseShape);
        decimal currentLCB = currentVolume > 0 ? currentMoment / currentVolume : length / 2m;

        // Shift needed
        decimal shiftNeeded = targetLCB - currentLCB;

        // Apply shift by interpolating
        var shifted = new List<decimal>();
        foreach (var x in stations)
        {
            decimal shiftedX = x - shiftNeeded;

            // Clamp to valid range and interpolate
            if (shiftedX <= 0)
            {
                shifted.Add(0m);
            }
            else if (shiftedX >= length)
            {
                shifted.Add(0m);
            }
            else
            {
                // Find interpolated value
                decimal value = Interpolate(stations, baseShape, shiftedX);
                shifted.Add(value);
            }
        }

        return shifted;
    }

    /// <summary>
    /// Generate waterline half-breadths from Cwp
    /// Uses parametric planform curve
    /// </summary>
    private List<decimal> GenerateWaterlineHalfBreadths(
        List<decimal> stations,
        decimal length,
        decimal beam,
        decimal cwp)
    {
        var halfBreadths = new List<decimal>();
        decimal targetArea = cwp * length * beam;

        // Generate base shape
        var baseShape = new List<decimal>();
        foreach (var x in stations)
        {
            decimal xi = x / length; // Normalized position (0 to 1)

            // Parametric planform: y = (B/2) * (1 - αξ^p - (1-α)(1-ξ)^q)
            decimal bowTerm = WATERLINE_FULLNESS_FACTOR * (decimal)Math.Pow((double)xi, (double)WATERLINE_BOW_EXPONENT);
            decimal sternTerm = (1m - WATERLINE_FULLNESS_FACTOR) * (decimal)Math.Pow((double)(1m - xi), (double)WATERLINE_STERN_EXPONENT);
            decimal shapeValue = 1m - bowTerm - sternTerm;

            baseShape.Add(Math.Max(0m, shapeValue));
        }

        // Scale to match Cwp
        // Calculate current area: ∫ 2 * (B/2) * shapeValue dx = ∫ B * shapeValue dx
        var currentHalfBreadths = baseShape.Select(s => (beam / 2m) * s).ToList();
        decimal currentArea = IntegrateTrapezoidal(stations, currentHalfBreadths.Select(hb => 2m * hb).ToList());
        decimal scaleFactor = currentArea > 0 ? targetArea / currentArea : 1m;

        foreach (var shapeValue in baseShape)
        {
            decimal halfBreadth = (beam / 2m) * shapeValue * (decimal)Math.Sqrt((double)scaleFactor);
            halfBreadths.Add(Math.Max(0m, halfBreadth));
        }

        // Verify and adjust if needed
        var finalHalfBreadths = halfBreadths.Select(hb => 2m * hb).ToList();
        decimal finalArea = IntegrateTrapezoidal(stations, finalHalfBreadths);
        if (Math.Abs(finalArea - targetArea) / targetArea > 0.01m)
        {
            // Re-scale if still off
            decimal adjustFactor = targetArea / finalArea;
            for (int i = 0; i < halfBreadths.Count; i++)
            {
                halfBreadths[i] *= (decimal)Math.Sqrt((double)adjustFactor);
            }
        }

        return halfBreadths;
    }

    /// <summary>
    /// Generate section shapes from Cm
    /// Parametric profile: y(z) = (B/2) * (1 - (z/T)^p)^q
    /// Returns half-breadths for each waterline
    /// </summary>
    private List<decimal> GenerateSectionShapes(
        List<decimal> waterlines,
        decimal beam,
        decimal draft,
        decimal cm)
    {
        // Calculate p and q from Cm
        // For U-sections (high Cm: 0.98-1.0): p ≈ 2, q ≈ 0.5
        // For V-sections (low Cm: 0.85-0.90): p ≈ 1, q ≈ 2-3
        decimal p = 1m + 4m * (1m - cm);
        decimal q = 0.5m + 3m * (cm - 0.85m);

        // Clamp to reasonable ranges
        p = Math.Clamp(p, 0.5m, 3.0m);
        q = Math.Clamp(q, 0.3m, 4.0m);

        var sectionShapes = new List<decimal>();

        foreach (var z in waterlines)
        {
            decimal zNorm = z / draft; // Normalized depth (0 to 1.3)

            if (zNorm <= 1m)
            {
                // Below design draft: use parametric profile
                decimal term = (decimal)Math.Pow((double)zNorm, (double)p);
                decimal shapeValue = (decimal)Math.Pow((double)(1m - term), (double)q);
                decimal halfBreadth = (beam / 2m) * shapeValue;
                sectionShapes.Add(Math.Max(0m, halfBreadth));
            }
            else
            {
                // Above design draft: extrapolate (for visualization)
                sectionShapes.Add(0m);
            }
        }

        return sectionShapes;
    }

    /// <summary>
    /// Combine sectional areas, section shapes, and waterlines to generate offsets
    /// Uses iterative approach to match sectional area targets accurately
    /// </summary>
    private List<List<decimal>> CombineToOffsets(
        List<decimal> stations,
        List<decimal> waterlines,
        List<decimal> sectionalAreas,
        List<decimal> sectionShapeProfile,
        List<decimal> waterlineHalfBreadths,
        decimal beam,
        decimal draft,
        decimal cm)
    {
        var offsets = new List<List<decimal>>();

        for (int i = 0; i < stations.Count; i++)
        {
            var stationOffsets = new List<decimal>();
            decimal targetArea = sectionalAreas[i];
            decimal waterlineHalfBreadth = waterlineHalfBreadths[i];

            // Generate initial offsets by blending section shape and waterline
            var initialOffsets = new List<decimal>();
            for (int j = 0; j < waterlines.Count; j++)
            {
                decimal z = waterlines[j];
                decimal zNorm = z / draft;

                if (zNorm > 1m)
                {
                    // Above design draft: use waterline constraint
                    initialOffsets.Add(Math.Min(waterlineHalfBreadth, beam / 2m));
                }
                else
                {
                    // Below design draft: blend section shape and waterline constraint
                    decimal sectionHalfBreadth = sectionShapeProfile[j];
                    decimal blendFactor = zNorm;
                    decimal blendedHalfBreadth = sectionHalfBreadth * (1m - blendFactor) +
                                                 waterlineHalfBreadth * blendFactor;
                    initialOffsets.Add(Math.Clamp(blendedHalfBreadth, 0m, beam / 2m));
                }
            }

            // Calculate current sectional area
            var halfBreadthsForArea = initialOffsets.Select(hb => 2m * hb).ToList();
            decimal currentArea = IntegrateTrapezoidal(waterlines, halfBreadthsForArea);

            // Iteratively scale to match target area accurately
            if (targetArea > 0 && currentArea > 0)
            {
                // Use iterative refinement to match sectional area
                var refinedOffsets = new List<decimal>(initialOffsets);
                const int maxIterations = 5;

                for (int iter = 0; iter < maxIterations; iter++)
                {
                    // Calculate current area
                    var iterHalfBreadths = refinedOffsets.Select(hb => 2m * hb).ToList();
                    decimal iterArea = IntegrateTrapezoidal(waterlines, iterHalfBreadths);

                    if (Math.Abs(iterArea - targetArea) / targetArea < 0.001m) // 0.1% tolerance
                        break;

                    // Calculate scale factor (area scales as square of half-breadth)
                    decimal iterScaleFactor = (decimal)Math.Sqrt((double)(targetArea / iterArea));

                    // Apply scaling with constraints
                    for (int j = 0; j < waterlines.Count; j++)
                    {
                        decimal z = waterlines[j];
                        decimal zNorm = z / draft;

                        if (zNorm > 1m)
                        {
                            // Above design draft: keep waterline constraint
                            refinedOffsets[j] = Math.Min(waterlineHalfBreadth, beam / 2m);
                        }
                        else
                        {
                            // Scale but respect waterline constraint at design draft
                            decimal scaledHalfBreadth = refinedOffsets[j] * iterScaleFactor;

                            // At design draft, ensure we match waterline half-breadth
                            if (zNorm >= 0.90m) // Near design draft
                            {
                                decimal draftBlend = (zNorm - 0.90m) / 0.10m; // Blend from 0.90T to 1.0T
                                scaledHalfBreadth = scaledHalfBreadth * (1m - draftBlend) +
                                                    waterlineHalfBreadth * draftBlend;
                            }

                            refinedOffsets[j] = Math.Clamp(scaledHalfBreadth, 0m, beam / 2m);
                        }
                    }
                }

                stationOffsets = refinedOffsets;
            }
            else
            {
                stationOffsets = initialOffsets;
            }

            offsets.Add(stationOffsets);
        }

        return offsets;
    }

    /// <summary>
    /// Compute form coefficients from generated offsets for validation
    /// Only integrates up to design draft for accurate coefficient calculation
    /// </summary>
    private FormCoefficients ComputeFormCoefficients(
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

        // Compute volume using integration
        var sectionAreas = new List<decimal>();
        foreach (var stationOffsets in offsets)
        {
            // Get half-breadths for active waterlines only
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

            // Integrate half-breadths to get sectional area (multiply by 2 for full section)
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
        decimal lcbPercent = length > 0 ? ((lcbPosition / length) - 0.5m) * 100m : 0m;

        return new FormCoefficients(cb, cp, cm, cwp, lcbPercent, volume);
    }

    // Helper methods for numerical integration

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

    private decimal Interpolate(List<decimal> x, List<decimal> y, decimal targetX)
    {
        if (targetX <= x[0]) return y[0];
        if (targetX >= x[^1]) return y[^1];

        for (int i = 0; i < x.Count - 1; i++)
        {
            if (targetX >= x[i] && targetX <= x[i + 1])
            {
                decimal t = (targetX - x[i]) / (x[i + 1] - x[i]);
                return y[i] * (1m - t) + y[i + 1] * t;
            }
        }

        return y[^1];
    }

    private void ValidateInputs(
        HullDimensions dims,
        decimal cb,
        decimal cp,
        decimal cm,
        decimal cwp,
        int numStations,
        int numWaterlines)
    {
        if (dims.Length <= 0) throw new ArgumentException("Length must be positive", nameof(dims));
        if (dims.Beam <= 0) throw new ArgumentException("Beam must be positive", nameof(dims));
        if (dims.Draft <= 0) throw new ArgumentException("Draft must be positive", nameof(dims));
        if (cb <= 0 || cb > 1) throw new ArgumentException("Cb must be between 0 and 1", nameof(cb));
        if (cp <= 0 || cp > 1) throw new ArgumentException("Cp must be between 0 and 1", nameof(cp));
        if (cm <= 0 || cm > 1) throw new ArgumentException("Cm must be between 0 and 1", nameof(cm));
        if (cwp <= 0 || cwp > 1) throw new ArgumentException("Cwp must be between 0 and 1", nameof(cwp));
        if (numStations < 3) throw new ArgumentException("At least 3 stations required", nameof(numStations));
        if (numWaterlines < 2) throw new ArgumentException("At least 2 waterlines required", nameof(numWaterlines));
        if (cp < cb) throw new ArgumentException("Cp must be >= Cb (Cp = Cb/Cm)", nameof(cp));
    }
}
