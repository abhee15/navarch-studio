using Microsoft.Extensions.Logging;
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

    private readonly ILogger<FormCoefficientHullGenerator>? _logger;

    /// <summary>
    /// Constructor with optional logger for diagnostics
    /// </summary>
    public FormCoefficientHullGenerator(ILogger<FormCoefficientHullGenerator>? logger = null)
    {
        _logger = logger;
    }

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
        int numWaterlines = 13,
        string? bowFamily = null,
        string? midshipFamily = null,
        string? sternFamily = null,
        string? vesselType = null)
    {
        _logger?.LogDebug(
            "[FORM_COEFF] Starting generation: L={L}m, B={B}m, T={T}m, Cb={Cb}, Cp={Cp}, Cm={Cm}, Cwp={Cwp}",
            dims.Length, dims.Beam, dims.Draft, cb, cp, cm, cwp);

        try
        {
            // Validate inputs
            _logger?.LogDebug("[FORM_COEFF] Step 0: Validating inputs...");
            ValidateInputs(dims, cb, cp, cm, cwp, numStations, numWaterlines);
            _logger?.LogDebug("[FORM_COEFF] Step 0: ✅ Validation passed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[FORM_COEFF] Step 0: ❌ VALIDATION FAILED - {Message}", ex.Message);
            throw;
        }

        // Generate BSRA-compatible station layout
        var stations = GenerateBSRAStations(dims.Length, numStations);
        var waterlines = GenerateWaterlines(dims.Draft, numWaterlines);
        _logger?.LogDebug("[FORM_COEFF] Generated {StationCount} stations, {WaterlineCount} waterlines",
            stations.Count, waterlines.Count);

        // Declare variables before try blocks
        List<decimal> sectionalAreas;
        List<decimal> waterlineHalfBreadths;
        List<decimal> sectionShapeProfile;
        List<List<decimal>> offsets;

        try
        {
            // Step 1: Generate sectional area curve from Cp and LCB
            _logger?.LogDebug("[FORM_COEFF] Step 1: Generating sectional area curve (Cp={Cp}, LCB={LCB}%)...", cp, dims.LcbPercent);
            sectionalAreas = GenerateSectionalAreaCurve(
                stations, dims.Length, dims.Beam, dims.Draft, cm, cp, dims.LcbPercent, cb);
            _logger?.LogDebug("[FORM_COEFF] Step 1: ✅ Generated {Count} sectional areas", sectionalAreas.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[FORM_COEFF] Step 1: ❌ FAILED - Sectional area curve generation - {Message}", ex.Message);
            throw;
        }

        try
        {
            // Step 2: Generate waterline half-breadths from Cwp with ShipD family adjustments
            _logger?.LogDebug("[FORM_COEFF] Step 2: Generating waterline half-breadths (Cwp={Cwp}, Bow={Bow}, Stern={Stern})...",
                cwp, bowFamily ?? "none", sternFamily ?? "none");
            waterlineHalfBreadths = GenerateWaterlineHalfBreadths(
                stations, dims.Length, dims.Beam, cwp, cb, bowFamily, sternFamily, vesselType);
            _logger?.LogDebug("[FORM_COEFF] Step 2: ✅ Generated {Count} waterline half-breadths", waterlineHalfBreadths.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[FORM_COEFF] Step 2: ❌ FAILED - Waterline half-breadths generation - {Message}", ex.Message);
            throw;
        }

        try
        {
            // Step 3: Generate section shapes from Cm with ShipD family adjustments
            _logger?.LogDebug("[FORM_COEFF] Step 3: Generating section shapes (Cm={Cm}, Midship={Midship})...",
                cm, midshipFamily ?? "none");
            sectionShapeProfile = GenerateSectionShapes(
                waterlines, dims.Beam, dims.Draft, cm, cb, midshipFamily, vesselType);
            _logger?.LogDebug("[FORM_COEFF] Step 3: ✅ Generated section shape profile with {Count} values", sectionShapeProfile.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[FORM_COEFF] Step 3: ❌ FAILED - Section shapes generation - {Message}", ex.Message);
            throw;
        }

        try
        {
            // Step 4: Combine to generate offsets
            _logger?.LogDebug("[FORM_COEFF] Step 4: Combining to generate offsets...");
            offsets = CombineToOffsets(
                stations, waterlines, sectionalAreas, sectionShapeProfile, waterlineHalfBreadths,
                dims.Beam, dims.Draft, cm);
            _logger?.LogDebug("[FORM_COEFF] Step 4: ✅ Generated {StationCount}x{WaterlineCount} offset grid",
                offsets.Count, offsets[0].Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[FORM_COEFF] Step 4: ❌ FAILED - Combining offsets - {Message}", ex.Message);
            throw;
        }

        try
        {
            // Step 5: Fair the offsets using cubic spline (if BSRA standard layout)
            if (numStations == 23)
            {
                _logger?.LogDebug("[FORM_COEFF] Step 5: Fairing offsets with cubic spline...");
                offsets = FairOffsets(stations, waterlines, offsets);
                // Ensure all faired offsets are non-negative (fairing can produce negative values)
                int negativeCount = 0;
                for (int i = 0; i < offsets.Count; i++)
                {
                    for (int j = 0; j < offsets[i].Count; j++)
                    {
                        if (offsets[i][j] < 0m)
                        {
                            offsets[i][j] = 0m;
                            negativeCount++;
                        }
                    }
                }
                _logger?.LogDebug("[FORM_COEFF] Step 5: ✅ Fairing complete, clamped {Count} negative values to zero", negativeCount);
            }
            else
            {
                _logger?.LogDebug("[FORM_COEFF] Step 5: Skipped fairing (not BSRA standard layout)");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[FORM_COEFF] Step 5: ❌ FAILED - Fairing offsets - {Message}", ex.Message);
            throw;
        }

        FormCoefficients computedCoeffs;
        try
        {
            // Step 6: Validate and compute form coefficients
            _logger?.LogDebug("[FORM_COEFF] Step 6: Computing actual form coefficients...");
            computedCoeffs = ComputeFormCoefficients(
                stations, waterlines, offsets, dims.Length, dims.Beam, dims.Draft, cm);
            _logger?.LogDebug("[FORM_COEFF] Step 6: ✅ Computed: Cb={Cb}, Cp={Cp}, Cm={Cm}, Cwp={Cwp}",
                computedCoeffs.Cb, computedCoeffs.Cp, computedCoeffs.Cm, computedCoeffs.Cwp);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[FORM_COEFF] Step 6: ❌ FAILED - Computing coefficients - {Message}", ex.Message);
            throw;
        }

        _logger?.LogInformation("[FORM_COEFF] ✅ Generation complete successfully");

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
    /// Get bow family multiplier for bow exponent adjustment
    /// Based on ShipD parameterization recommendations
    /// </summary>
    private decimal GetBowFamilyMultiplier(string? bowFamily)
    {
        if (string.IsNullOrWhiteSpace(bowFamily))
            return 1.0m;

        return bowFamily.ToLowerInvariant() switch
        {
            "bulbous_bow" => 0.5m,   // Fuller bow (bulb adds volume) - INCREASED from 0.7m for more visible difference
            "axe_bow" => 1.8m,       // Very fine bow (sharp entry) - INCREASED from 1.5m
            "fine_entry" => 1.5m,    // Fine entry (streamlined) - INCREASED from 1.2m for more visible difference
            "blunt_bow" => 0.4m,     // Very full bow (blunt entry) - INCREASED from 0.6m
            _ => 1.0m                 // Default: no adjustment
        };
    }

    /// <summary>
    /// Get stern family multiplier for stern exponent adjustment
    /// Based on ShipD parameterization recommendations
    /// </summary>
    private decimal GetSternFamilyMultiplier(string? sternFamily)
    {
        if (string.IsNullOrWhiteSpace(sternFamily))
            return 1.0m;

        return sternFamily.ToLowerInvariant() switch
        {
            "transom_stern" => 0.5m,  // Full, blunt stern (flat transom) - INCREASED from 0.7m for more visible difference
            "twin_skeg" => 0.7m,      // Full stern (with skegs) - INCREASED from 0.8m
            "cruiser_stern" => 1.0m,  // Moderate fullness
            "fine_stern" => 1.6m,     // Fine, streamlined stern - INCREASED from 1.3m
            "canoe_stern" => 1.8m,    // Very fine, canoe-like stern - INCREASED from 1.4m for more visible difference
            _ => 1.0m                  // Default: no adjustment
        };
    }

    /// <summary>
    /// Get midship family adjustment for section shape
    /// Based on ShipD parameterization recommendations
    /// </summary>
    private (decimal cmAdjustment, decimal betaAdjustment) GetMidshipFamilyAdjustment(string? midshipFamily)
    {
        if (string.IsNullOrWhiteSpace(midshipFamily))
            return (0m, 0m);

        return midshipFamily.ToLowerInvariant() switch
        {
            "full_midship" => (0.08m, -8m),   // U-shaped sections (higher Cm, lower Beta) - INCREASED for more visible difference
            "fine_midship" => (-0.08m, 8m),   // V-shaped sections (lower Cm, higher Beta) - INCREASED
            "deep_v_midship" => (-0.12m, 15m), // Very deep V-sections (lower Cm, much higher Beta) - INCREASED for more visible difference
            _ => (0m, 0m)                      // Default: no adjustment
        };
    }

    /// <summary>
    /// Get vessel type multiplier for additional shape adjustments
    /// </summary>
    private decimal GetVesselTypeMultiplier(string? vesselType)
    {
        if (string.IsNullOrWhiteSpace(vesselType))
            return 1.0m;

        return vesselType.ToLowerInvariant() switch
        {
            "yacht" or "recreational" => 1.1m,  // Slightly finer (yachts are more streamlined)
            "cargo" or "bulk" or "general_cargo" => 0.9m,  // Slightly fuller (cargo vessels are fuller)
            "container" => 1.0m,                 // Neutral (optimized for speed)
            "tanker" => 0.85m,                   // Fuller (tankers are very full)
            _ => 1.0m                            // Default: no adjustment
        };
    }

    /// <summary>
    /// Generate waterline half-breadths from Cwp with Cb-dependent parameters and ShipD family adjustments
    /// Uses parametric planform curve with Cb-dependent bow/stern exponents and fullness factor
    /// Better matches BSRA waterline half-breadth characteristics
    /// </summary>
    private List<decimal> GenerateWaterlineHalfBreadths(
        List<decimal> stations,
        decimal length,
        decimal beam,
        decimal cwp,
        decimal cb,
        string? bowFamily = null,
        string? sternFamily = null,
        string? vesselType = null)
    {
        var halfBreadths = new List<decimal>();
        decimal targetArea = cwp * length * beam;

        // Cb-dependent base exponents
        // Bow exponent: 2.0 + 0.5 * (1 - Cb) - fuller hulls have less fine bows
        decimal baseBowExponent = 2.0m + 0.5m * (1m - cb);

        // Stern exponent: 2.0 + 0.3 * (1 - Cb) - fuller hulls have less fine sterns
        decimal baseSternExponent = 2.0m + 0.3m * (1m - cb);

        // Apply ShipD family multipliers
        decimal bowFamilyMultiplier = GetBowFamilyMultiplier(bowFamily);
        decimal sternFamilyMultiplier = GetSternFamilyMultiplier(sternFamily);
        decimal vesselTypeMultiplier = GetVesselTypeMultiplier(vesselType);

        // Combine multipliers (bow and stern are independent, vessel type applies to both)
        decimal bowExponent = baseBowExponent * bowFamilyMultiplier * vesselTypeMultiplier;
        decimal sternExponent = baseSternExponent * sternFamilyMultiplier * vesselTypeMultiplier;

        // Clamp to reasonable ranges
        bowExponent = Math.Clamp(bowExponent, 1.0m, 4.0m);
        sternExponent = Math.Clamp(sternExponent, 1.0m, 3.5m);

        // Fullness factor: 0.4 + 0.2 * (Cb - 0.65) - higher Cb = more forward fullness
        // Adjust based on bow family (bulbous bow tends to have more forward fullness)
        decimal fullnessFactor = 0.4m + 0.2m * (cb - 0.65m);
        if (bowFamily?.ToLowerInvariant() == "bulbous_bow")
        {
            fullnessFactor += 0.1m; // Bulbous bow adds forward fullness
        }
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
    /// Generate section shapes from Cm with Cb influence and ShipD midship family adjustments
    /// Parametric profile: y(z) = (B/2) * (1 - (z/T)^p)^q
    /// Returns half-breadths for each waterline
    /// Refined p and q calculation with Cb correction for better BSRA section shape matching
    /// </summary>
    private List<decimal> GenerateSectionShapes(
        List<decimal> waterlines,
        decimal beam,
        decimal draft,
        decimal cm,
        decimal cb,
        string? midshipFamily = null,
        string? vesselType = null)
    {
        // Apply midship family adjustments to Cm
        var (cmAdjustment, betaAdjustment) = GetMidshipFamilyAdjustment(midshipFamily);
        decimal adjustedCm = cm + cmAdjustment;
        adjustedCm = Math.Clamp(adjustedCm, 0.7m, 1.0m); // Keep within reasonable bounds

        decimal p, q;

        // For very high Cm (>= 0.97), use special rectangular section parameterization
        // This is critical for achieving truly rectangular sections in full-form vessels
        // Reference: prefinal_1.DOC Cm=0.99 requires almost perfectly rectangular sections
        if (adjustedCm >= 0.97m)
        {
            // Rectangular section: very high p (nearly vertical sides), very low q (nearly flat bottom)
            // p controls side steepness: higher p = more vertical
            // q controls bottom flatness: lower q = flatter
            p = 5m + 20m * (adjustedCm - 0.97m); // p=5 at Cm=0.97, p=25 at Cm=1.0
            q = 0.4m - 1.0m * (adjustedCm - 0.97m); // q=0.4 at Cm=0.97, q=0.1 at Cm=1.0 (very flat)

            // For full_midship with Cm >= 0.97, enhance rectangularity further
            if (midshipFamily != null && midshipFamily.ToLowerInvariant() == "full_midship")
            {
                p += 5m; // Even more vertical sides
                q = Math.Max(0.05m, q - 0.1m); // Even flatter bottom
            }
        }
        else
        {
            // Normal Cm range: use existing logic
            // For U-sections (high Cm: 0.90-0.97): p ≈ 1.5-5.0, q ≈ 0.4-1.0
            // For V-sections (low Cm: 0.85-0.90): p ≈ 0.8-1.5, q ≈ 1.5-2.5
            p = 1m + 4m * (1m - adjustedCm);
            q = 0.5m + 3m * (adjustedCm - 0.85m);

            // Apply beta adjustment (affects section shape: higher beta = more V-shaped)
            // Beta adjustment affects p: higher beta → lower p (more V-shaped)
            if (betaAdjustment != 0m)
            {
                // Convert beta adjustment (degrees) to p adjustment
                // Higher beta (more V-shaped) → lower p
                decimal pAdjustment = -betaAdjustment / 10m; // Scale: 10 degrees ≈ 0.1 p change
                p += pAdjustment;
            }

            // Add Cb correction: higher Cb tends to have fuller sections
            // p_adjusted = p * (1 + 0.2 * (Cb - 0.75))
            decimal cbCorrection = 1m + 0.2m * (cb - 0.75m);

            // Apply vessel type multiplier (affects section fullness)
            decimal vesselTypeMultiplier = GetVesselTypeMultiplier(vesselType);
            cbCorrection *= vesselTypeMultiplier;
            p *= cbCorrection;
        }

        // Clamp to reasonable ranges (wider range for rectangular sections)
        p = Math.Clamp(p, 0.5m, 30.0m); // Increased max from 3.0 to 30.0 for rectangular sections
        q = Math.Clamp(q, 0.05m, 4.0m); // Decreased min from 0.3 to 0.05 for flat bottoms

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

            // Final safety clamp: ensure all offsets are non-negative
            for (int j = 0; j < stationOffsets.Count; j++)
            {
                stationOffsets[j] = Math.Max(0m, Math.Clamp(stationOffsets[j], 0m, beam / 2m));
            }

            offsets.Add(stationOffsets);
        }

        // Ensure forward stations close properly to form a fine bow
        // The bow should taper from keel to deck, with keel at zero or very small value
        // This prevents the flared, wing-like appearance at the front
        // Only apply to the forward region (last 20% of stations, minimum 1, maximum 4)
        if (offsets.Count > 0 && waterlines.Count > 0)
        {
            // Get the maximum draft to normalize waterline positions
            var maxDraft = waterlines[waterlines.Count - 1];
            if (maxDraft <= 0) maxDraft = draft; // Use design draft as fallback

            // Only fix forward stations: last 20% of stations, but at least 1 and at most 4
            // For very small hulls (< 10 stations), only fix the very last station
            int numBowStations = offsets.Count < 10
                ? 1
                : Math.Max(1, Math.Min(4, (int)Math.Ceiling(offsets.Count * 0.2m)));
            int firstBowStationIdx = offsets.Count - numBowStations;

            for (int stIdx = firstBowStationIdx; stIdx < offsets.Count; stIdx++)
            {
                var stationOffsets = offsets[stIdx];
                if (stationOffsets.Count == 0) continue;

                // Calculate how far forward this station is (0 = start of bow region, 1 = forward perpendicular)
                decimal forwardness = numBowStations > 1
                    ? (decimal)(stIdx - firstBowStationIdx) / (numBowStations - 1)
                    : 1m; // If only one station, it's fully forward

                // Maximum allowed half-breadth decreases as we go forward
                // Station 22 (forward perpendicular): max 15% of beam
                // Station 21: max 25% of beam
                // Station 20: max 35% of beam
                // Station 19: max 45% of beam
                var maxStationHalfBreadth = beam * (0.15m + 0.3m * (1m - forwardness));

                // Ensure keel (waterline 0) has zero or very small half-breadth
                var keelHalfBreadth = stationOffsets[0];
                var maxKeelHalfBreadth = beam * (0.02m + 0.03m * (1m - forwardness)); // 2-5% of beam

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
                    var waterlineNorm = waterlineZ / maxDraft; // Normalize to 0-1

                    // Cap the maximum half-breadth based on forwardness and waterline height
                    if (currentHalfBreadth > maxStationHalfBreadth)
                    {
                        // Allow some increase with height, but cap based on forwardness
                        var maxAllowed = maxStationHalfBreadth * (0.3m + 0.7m * waterlineNorm);
                        stationOffsets[wlIdx] = Math.Min(currentHalfBreadth, maxAllowed);
                    }

                    // Ensure smooth tapering - each waterline should be >= previous
                    if (currentHalfBreadth < prevHalfBreadth * 0.85m)
                    {
                        // If current is significantly less than previous, increase it slightly
                        stationOffsets[wlIdx] = Math.Max(currentHalfBreadth, prevHalfBreadth * 0.9m);
                    }
                }
            }
        }

        // Ensure aft perpendicular station (first station, index 0) closes properly to form a fine stern
        // POST-PROCESSING ONLY: This does NOT change offset generation, only corrects scaling artifacts
        // Check if stern is wider than adjacent station (indicates scaling artifact)
        if (offsets.Count > 1 && waterlines.Count > 0)
        {
            var maxDraft = waterlines[waterlines.Count - 1];
            if (maxDraft <= 0) maxDraft = draft;

            // Fix the first station (aft perpendicular) if it's wider than adjacent station
            var aftStationOffsets = offsets[0];
            var adjacentStationOffsets = offsets[1]; // Station 1 (next forward)

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

                    // Ensure keel (waterline 0) has zero or very small half-breadth
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
        }

        // Phase 3: Apply cubic spline fairing to stern region for smooth continuity
        // Fair stern stations (0-2) to ensure smooth transitions without sharp breaks
        if (offsets.Count >= 3 && waterlines.Count >= 2)
        {
            // Fair each stern station (0, 1, 2) to remove any remaining abrupt changes
            for (int stIdx = 0; stIdx < Math.Min(3, offsets.Count); stIdx++)
            {
                var stationOffsets = offsets[stIdx];
                if (stationOffsets.Count == waterlines.Count)
                {
                    // Fair the section profile to ensure smooth curve
                    var fairedSection = CubicSplineFairing.FairSection(waterlines, stationOffsets);

                    // Ensure non-negative and smooth transition
                    for (int wlIdx = 0; wlIdx < fairedSection.Count; wlIdx++)
                    {
                        fairedSection[wlIdx] = Math.Max(0m, fairedSection[wlIdx]);
                    }

                    offsets[stIdx] = fairedSection;
                }
            }

            // Ensure smooth transition from stern station 0 to station 1
            // The transition should be continuous without sharp breaks
            if (offsets.Count >= 2)
            {
                var sternStation = offsets[0];
                var nextStation = offsets[1];

                for (int wlIdx = 0; wlIdx < Math.Min(sternStation.Count, nextStation.Count); wlIdx++)
                {
                    // Ensure stern station doesn't have abrupt jumps relative to next station
                    // Allow gradual transition (stern can be slightly narrower, but not dramatically different)
                    var maxAllowedJump = nextStation[wlIdx] * 0.3m; // Max 30% difference
                    var currentDiff = Math.Abs(sternStation[wlIdx] - nextStation[wlIdx]);

                    if (currentDiff > maxAllowedJump && sternStation[wlIdx] > nextStation[wlIdx])
                    {
                        // Smooth the transition by limiting the difference
                        sternStation[wlIdx] = nextStation[wlIdx] + maxAllowedJump;
                    }
                }
            }
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
    /// Applies fairing to both waterlines (longitudinal) and sections (vertical) for smooth geometry
    /// </summary>
    private List<List<decimal>> FairOffsets(
        List<decimal> stations,
        List<decimal> waterlines,
        List<List<decimal>> offsets)
    {
        // Step 1: Fair each waterline (longitudinal fairing)
        var waterlineFaired = new List<List<decimal>>();

        for (int wlIdx = 0; wlIdx < waterlines.Count; wlIdx++)
        {
            var halfBreadths = offsets.Select(st => st[wlIdx]).ToList();
            var fairedHalfBreadths = CubicSplineFairing.FairWaterline(
                stations, halfBreadths, waterlines[wlIdx]);

            waterlineFaired.Add(fairedHalfBreadths);
        }

        // Transpose back to [station][waterline] format after waterline fairing
        var waterlineFairedOffsets = new List<List<decimal>>();
        for (int stIdx = 0; stIdx < stations.Count; stIdx++)
        {
            var stationOffsets = waterlineFaired.Select(wlOffsets => wlOffsets[stIdx]).ToList();
            waterlineFairedOffsets.Add(stationOffsets);
        }

        // Step 2: Fair each section (vertical fairing)
        // This removes abrupt changes in section curves
        var result = new List<List<decimal>>();
        for (int stIdx = 0; stIdx < stations.Count; stIdx++)
        {
            var stationOffsets = waterlineFairedOffsets[stIdx];

            // Fair the section profile (half-breadths vs waterline heights)
            var fairedSection = CubicSplineFairing.FairSection(waterlines, stationOffsets);

            // Ensure non-negative values (fairing can produce negative values)
            for (int wlIdx = 0; wlIdx < fairedSection.Count; wlIdx++)
            {
                fairedSection[wlIdx] = Math.Max(0m, fairedSection[wlIdx]);
            }

            result.Add(fairedSection);
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

    /// <summary>
    /// Interpolate a curve at intermediate points using cubic spline for smooth curves
    /// Used for generating smooth buttocks and waterlines without discretization artifacts
    /// </summary>
    private List<decimal> InterpolateCurveSmooth(
        List<decimal> x,
        List<decimal> y,
        List<decimal> targetX,
        decimal? startSlope = null,
        decimal? endSlope = null)
    {
        if (x.Count != y.Count || x.Count < 2)
            throw new ArgumentException("x and y must have the same length and at least 2 points");

        if (x.Count == 2)
        {
            // Fallback to linear interpolation for 2 points
            return targetX.Select(tx => Interpolate(x, y, tx)).ToList();
        }

        // Estimate end slopes if not provided
        decimal dx0 = startSlope ?? (x.Count > 1 ? (y[1] - y[0]) / (x[1] - x[0]) : 0m);
        decimal dxn = endSlope ?? (x.Count > 1
            ? (y[x.Count - 1] - y[x.Count - 2]) / (x[x.Count - 1] - x[x.Count - 2])
            : 0m);

        // Fit cubic spline
        var coefficients = CubicSplineFairing.Fit(x, y, dx0, dxn);

        // Evaluate at target points
        var result = new List<decimal>();
        foreach (var tx in targetX)
        {
            result.Add(CubicSplineFairing.Evaluate(coefficients, tx));
        }

        return result;
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
