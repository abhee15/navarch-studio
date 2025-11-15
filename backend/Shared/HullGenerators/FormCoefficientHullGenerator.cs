using Shared.HullGenerators.Fairing;
using Shared.HullGenerators.Integration;
using Shared.HullGenerators.Models;

namespace Shared.HullGenerators;

/// <summary>
/// Form-coefficient-based parametric hull generator
/// Generates realistic hull offsets from form coefficients (Cb, Cp, Cm, Cwp, LCB)
/// Based on parametric methods from naval architecture literature
/// </summary>
public class FormCoefficientHullGenerator : IHullGenerator
{
    // Note: Calibration parameters are now Cb-dependent and calculated dynamically
    // This provides better matching to BSRA/Series 60 characteristics

    /// <summary>
    /// Check if this generator can generate for the given vessel type and Cb
    /// Parametric generator can always generate (it's the fallback)
    /// </summary>
    public static bool CanGenerate(string? vesselType, decimal cb)
    {
        // Parametric generator can always generate (it's the fallback)
        return true;
    }

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
            stations, dims.Length, dims.Beam, dims.Draft, cm, cp, dims.LcbPercent, cb);

        // Step 2: Generate waterline half-breadths from Cwp
        var waterlineHalfBreadths = GenerateWaterlineHalfBreadths(
            stations, dims.Length, dims.Beam, cwp, cb);

        // Step 3: Generate section shapes from Cm (normalized profile)
        var sectionShapeProfile = GenerateSectionShapes(
            waterlines, dims.Beam, dims.Draft, cm, cb);

        // Step 4: Combine to generate offsets
        var offsets = CombineToOffsets(
            stations, waterlines, sectionalAreas, sectionShapeProfile, waterlineHalfBreadths,
            dims.Beam, dims.Draft, cm);

        // Step 5: Fair the offsets using cubic spline (if BSRA standard layout)
        if (numStations == 23)
        {
            offsets = FairOffsets(stations, waterlines, offsets);
        }

        // Step 6: Validate and compute form coefficients
        var computedCoeffs = ComputeFormCoefficients(
            stations, waterlines, offsets, dims.Length, dims.Beam, dims.Draft, cm);

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
    /// Generate waterlines using BSRA standard heights or extended mode
    /// BSRA standard: A=7.69%, B=15.38%, C=23.08%, D=38.46%, E=53.85%, F=69.23%, G=84.62%, H=100%, J=115.38%, K=130.77%
    /// </summary>
    private List<decimal> GenerateWaterlines(decimal draft, int numWaterlines)
    {
        var waterlines = new List<decimal>();

        // BSRA standard waterline heights (% of draft)
        var bsraStandardPercentages = new[]
        {
            7.69m, 15.38m, 23.08m, 38.46m, 53.85m, 69.23m, 84.62m, 100.0m, 115.38m, 130.77m
        };

        if (numWaterlines == 10)
        {
            // Use exact BSRA standard waterlines
            foreach (var percentage in bsraStandardPercentages)
            {
                waterlines.Add(draft * percentage / 100.0m);
            }
        }
        else if (numWaterlines == 13)
        {
            // Extended mode: Use BSRA standard plus additional waterlines
            // Add waterlines at 0%, 25%, 50%, 75% for better definition
            var extendedPercentages = new List<decimal> { 0m }; // Keel

            // Add 25%, 50%, 75% if they don't overlap with BSRA standard
            var additionalPercentages = new[] { 25.0m, 50.0m, 75.0m };
            foreach (var pct in additionalPercentages)
            {
                // Check if this percentage is close to any BSRA standard (within 2%)
                bool isCloseToBSRA = bsraStandardPercentages.Any(bsra => Math.Abs(bsra - pct) < 2.0m);
                if (!isCloseToBSRA)
                {
                    extendedPercentages.Add(pct);
                }
            }

            // Add all BSRA standard waterlines
            extendedPercentages.AddRange(bsraStandardPercentages);

            // Sort and take first 13
            extendedPercentages.Sort();
            extendedPercentages = extendedPercentages.Take(13).ToList();

            // Convert to actual Z values
            foreach (var percentage in extendedPercentages)
            {
                waterlines.Add(draft * percentage / 100.0m);
            }
        }
        else
        {
            // For other counts, use equally spaced but ensure design draft (100%) is included
            decimal maxZ = draft * 1.3m; // Extend above design draft for visualization

            for (int j = 0; j < numWaterlines; j++)
            {
                decimal z = maxZ * j / (numWaterlines - 1);

                // Ensure design draft is included (within 1% tolerance)
                if (j == numWaterlines - 1 || Math.Abs(z - draft) < draft * 0.01m)
                {
                    z = draft; // Use exact design draft
                }

                waterlines.Add(z);
            }
        }

        return waterlines;
    }

    /// <summary>
    /// Generate sectional area curve from Cp and LCB
    /// Uses raised cosine base function with Cb-dependent exponent to better match BSRA fig.54 curves
    /// Higher Cb = fuller ends = lower exponent
    /// </summary>
    private List<decimal> GenerateSectionalAreaCurve(
        List<decimal> stations,
        decimal length,
        decimal beam,
        decimal draft,
        decimal cm,
        decimal cp,
        decimal lcbPercent,
        decimal cb)
    {
        // Midship section area
        decimal amid = cm * beam * draft;

        // Target volume from Cp
        decimal targetVolume = cp * amid * length;

        var sectionalAreas = new List<decimal>();
        var baseShape = new List<decimal>();

        // Adaptive exponent based on Cb (higher Cb = fuller ends = lower exponent)
        // exp = 2.0 - 0.5 * (Cb - 0.65) clamped to reasonable range
        decimal exponent = 2.0m - 0.5m * (cb - 0.65m);
        exponent = Math.Clamp(exponent, 1.0m, 3.0m);

        // Generate base shape using raised cosine
        foreach (var x in stations)
        {
            decimal xi = x / length; // Normalized position (0 to 1)

            // Raised cosine: φ(ξ) = 1 - cos(πξ)
            decimal phi = 1m - (decimal)Math.Cos((double)((decimal)Math.PI * xi));

            // Apply Cb-dependent exponent for fullness control
            decimal shapeValue = (decimal)Math.Pow((double)phi, (double)exponent);
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
    /// Apply LCB shift to sectional area curve using moment-based adjustment
    /// Positive LCB% = forward shift, Negative = aft shift
    /// Uses iterative moment adjustment for more accurate LCB positioning
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

        // Convert LCB% to target position
        // LCB% is from aft perpendicular, positive forward
        decimal targetLCB = length * (0.5m + lcbPercent / 100m); // Convert % to position

        // Calculate current LCB
        decimal currentVolume = IntegrateTrapezoidal(stations, baseShape);
        decimal currentMoment = IntegrateFirstMoment(stations, baseShape);
        decimal currentLCB = currentVolume > 0 ? currentMoment / currentVolume : length / 2m;

        // If already close enough, return base shape
        if (Math.Abs(currentLCB - targetLCB) < length * 0.001m) // 0.1% tolerance
        {
            return baseShape;
        }

        // Use iterative moment-based adjustment
        var adjustedShape = new List<decimal>(baseShape);
        const int maxIterations = 10;
        const decimal convergenceTolerance = 0.001m; // 0.1% of length

        for (int iter = 0; iter < maxIterations; iter++)
        {
            // Calculate current LCB
            decimal iterVolume = IntegrateTrapezoidal(stations, adjustedShape);
            decimal iterMoment = IntegrateFirstMoment(stations, adjustedShape);
            decimal iterLCB = iterVolume > 0 ? iterMoment / iterVolume : length / 2m;

            // Check convergence
            decimal lcbError = Math.Abs(iterLCB - targetLCB) / length;
            if (lcbError < convergenceTolerance)
            {
                break;
            }

            // Calculate adjustment needed
            decimal lcbErrorAbs = targetLCB - iterLCB;

            // Apply moment-based adjustment: shift the curve to adjust LCB
            // Use a weighted adjustment that preserves volume
            var newShape = new List<decimal>();
            decimal adjustmentFactor = 0.3m; // Damping factor for stability

            for (int i = 0; i < stations.Count; i++)
            {
                decimal x = stations[i];
                decimal currentValue = adjustedShape[i];

                // Apply adjustment based on position relative to target LCB
                // Forward of target: increase if LCB needs to move forward, decrease if aft
                // Aft of target: decrease if LCB needs to move forward, increase if aft
                decimal positionFactor = (x - targetLCB) / length; // -0.5 to +0.5
                decimal adjustment = 1.0m + positionFactor * adjustmentFactor * (lcbErrorAbs / length) * 2.0m;

                decimal adjustedValue = currentValue * adjustment;
                newShape.Add(Math.Max(0m, adjustedValue));
            }

            // Normalize to preserve volume
            decimal newVolume = IntegrateTrapezoidal(stations, newShape);
            if (newVolume > 0 && currentVolume > 0)
            {
                decimal volumeScale = currentVolume / newVolume;
                for (int i = 0; i < newShape.Count; i++)
                {
                    newShape[i] *= volumeScale;
                }
            }

            adjustedShape = newShape;
        }

        return adjustedShape;
    }

    /// <summary>
    /// Generate waterline half-breadths from Cwp with Cb-dependent parameters
    /// Uses parametric planform curve with Cb-dependent bow/stern exponents and fullness factor
    /// Better matches BSRA waterline half-breadth characteristics
    /// </summary>
    private List<decimal> GenerateWaterlineHalfBreadths(
        List<decimal> stations,
        decimal length,
        decimal beam,
        decimal cwp,
        decimal cb)
    {
        var halfBreadths = new List<decimal>();
        decimal targetArea = cwp * length * beam;

        // Cb-dependent exponents and fullness factor
        // Bow exponent: 2.0 + 0.5 * (1 - Cb) - fuller hulls have less fine bows
        decimal bowExponent = 2.0m + 0.5m * (1m - cb);
        bowExponent = Math.Clamp(bowExponent, 1.5m, 3.0m);

        // Stern exponent: 2.0 + 0.3 * (1 - Cb) - fuller hulls have less fine sterns
        decimal sternExponent = 2.0m + 0.3m * (1m - cb);
        sternExponent = Math.Clamp(sternExponent, 1.5m, 2.5m);

        // Fullness factor: 0.4 + 0.2 * (Cb - 0.65) - higher Cb = more forward fullness
        decimal fullnessFactor = 0.4m + 0.2m * (cb - 0.65m);
        fullnessFactor = Math.Clamp(fullnessFactor, 0.3m, 0.7m);

        // Generate base shape
        var baseShape = new List<decimal>();
        foreach (var x in stations)
        {
            decimal xi = x / length; // Normalized position (0 to 1)

            // Parametric planform: y = (B/2) * (1 - αξ^p - (1-α)(1-ξ)^q)
            decimal bowTerm = fullnessFactor * (decimal)Math.Pow((double)xi, (double)bowExponent);
            decimal sternTerm = (1m - fullnessFactor) * (decimal)Math.Pow((double)(1m - xi), (double)sternExponent);
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
    /// Generate section shapes from Cm with Cb influence
    /// Parametric profile: y(z) = (B/2) * (1 - (z/T)^p)^q
    /// Returns half-breadths for each waterline
    /// Refined p and q calculation with Cb correction for better BSRA section shape matching
    /// </summary>
    private List<decimal> GenerateSectionShapes(
        List<decimal> waterlines,
        decimal beam,
        decimal draft,
        decimal cm,
        decimal cb)
    {
        // Calculate p and q from Cm
        // For U-sections (high Cm: 0.98-1.0): p ≈ 1.5-2.0, q ≈ 0.4-0.6
        // For V-sections (low Cm: 0.85-0.90): p ≈ 0.8-1.2, q ≈ 1.5-2.5
        decimal p = 1m + 4m * (1m - cm);
        decimal q = 0.5m + 3m * (cm - 0.85m);

        // Clamp to reasonable ranges
        p = Math.Clamp(p, 0.5m, 3.0m);
        q = Math.Clamp(q, 0.3m, 4.0m);

        // Add Cb correction: higher Cb tends to have fuller sections
        // p_adjusted = p * (1 + 0.2 * (Cb - 0.75))
        decimal cbCorrection = 1m + 0.2m * (cb - 0.75m);
        p *= cbCorrection;

        // Re-clamp after Cb correction
        p = Math.Clamp(p, 0.5m, 3.0m);

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

            // Iteratively scale to match target area accurately using improved Newton-Raphson-like approach
            if (targetArea > 0 && currentArea > 0)
            {
                // Use improved iterative refinement to match sectional area
                var refinedOffsets = new List<decimal>(initialOffsets);
                const int maxIterations = 10;
                const decimal tolerance = 0.001m; // 0.1% tolerance

                for (int iter = 0; iter < maxIterations; iter++)
                {
                    // Calculate current area
                    var iterHalfBreadths = refinedOffsets.Select(hb => 2m * hb).ToList();
                    decimal iterArea = IntegrateTrapezoidal(waterlines, iterHalfBreadths);

                    // Check convergence
                    decimal areaError = Math.Abs(iterArea - targetArea) / targetArea;
                    if (areaError < tolerance)
                        break;

                    // Use Newton-Raphson-like approach: area scales as square of half-breadth
                    // f(x) = area - target, f'(x) ≈ 2 * area / scale
                    // scale_new = scale_old - f(scale) / f'(scale)
                    // Simplified: scale_factor = sqrt(target / current) with damping
                    decimal areaRatio = targetArea / iterArea;
                    decimal iterScaleFactor = (decimal)Math.Pow((double)areaRatio, 0.5); // Square root for area scaling

                    // Apply damping for stability (especially near convergence)
                    if (areaError < 0.05m) // Within 5%, use damping
                    {
                        iterScaleFactor = 1m + (iterScaleFactor - 1m) * 0.7m; // 70% of adjustment
                    }

                    // Apply scaling with constraints, using separate scaling for different draft regions
                    for (int j = 0; j < waterlines.Count; j++)
                    {
                        decimal z = waterlines[j];
                        decimal zNorm = z / draft;

                        if (zNorm > 1m)
                        {
                            // Above design draft: keep waterline constraint
                            refinedOffsets[j] = Math.Min(waterlineHalfBreadth, beam / 2m);
                        }
                        else if (zNorm >= 0.95m)
                        {
                            // Near design draft (95-100%): blend with waterline constraint
                            decimal draftBlend = (zNorm - 0.95m) / 0.05m; // Blend from 0.95T to 1.0T
                            decimal scaledHalfBreadth = refinedOffsets[j] * iterScaleFactor;
                            refinedOffsets[j] = scaledHalfBreadth * (1m - draftBlend) +
                                                waterlineHalfBreadth * draftBlend;
                            refinedOffsets[j] = Math.Clamp(refinedOffsets[j], 0m, beam / 2m);
                        }
                        else
                        {
                            // Below 95% draft: scale freely
                            decimal scaledHalfBreadth = refinedOffsets[j] * iterScaleFactor;
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
        decimal draft,
        decimal cm)
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

        // Use BSRA Simpson integration if we have 23 stations (BSRA standard)
        decimal volume;
        decimal lcb;
        decimal lcbPercent;
        decimal waterplaneArea;
        decimal cwp;

        if (stations.Count == 23)
        {
            // Use BSRA Simpson integration for accuracy
            volume = BSRASimpsonIntegration.CalculateVolume(stations, sectionAreas, length);
            lcb = BSRASimpsonIntegration.CalculateLCB(stations, sectionAreas, length);
            lcbPercent = (lcb - length / 2.0m) / length * 100m;

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

            waterplaneArea = BSRASimpsonIntegration.CalculateWaterplaneArea(stations, waterlineHalfBreadths, length);
            cwp = waterplaneArea > 0 && length > 0 && beam > 0 ? waterplaneArea / (length * beam) : 0m;
        }
        else
        {
            // Fallback to trapezoidal for non-standard station counts
            volume = IntegrateTrapezoidal(stations, sectionAreas);
            decimal volumeMoment = IntegrateFirstMoment(stations, sectionAreas);
            decimal lcbPosition = volume > 0 ? volumeMoment / volume : length / 2m;
            lcbPercent = length > 0 ? ((lcbPosition / length) - 0.5m) * 100m : 0m;

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

            waterplaneArea = IntegrateTrapezoidal(stations, waterlineHalfBreadths.Select(hb => 2m * hb).ToList());
            cwp = waterplaneArea > 0 && length > 0 && beam > 0 ? waterplaneArea / (length * beam) : 0m;
        }

        // Compute Cb
        decimal cb = volume > 0 ? volume / (length * beam * draft) : 0m;

        // Compute Cp
        decimal maxSectionArea = sectionAreas.Count > 0 ? sectionAreas.Max() : 0m;
        decimal cp = maxSectionArea > 0 && length > 0 ? volume / (maxSectionArea * length) : 0m;

        // Compute Cm
        int midshipIndex = sectionAreas.Count / 2;
        decimal midshipArea = midshipIndex < sectionAreas.Count ? sectionAreas[midshipIndex] : 0m;
        decimal computedCm = midshipArea > 0 && beam > 0 && draft > 0 ? midshipArea / (beam * draft) : 0m;

        return new FormCoefficients(cb, cp, computedCm, cwp, lcbPercent, volume);
    }

    /// <summary>
    /// Fair offsets using cubic spline fairing (for BSRA standard layout)
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
