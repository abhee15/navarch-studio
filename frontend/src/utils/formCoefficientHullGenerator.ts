/**
 * Form-Coefficient-Based Parametric Hull Generator (TypeScript Port)
 *
 * Port of backend FormCoefficientHullGenerator.cs
 * Generates realistic hull offsets from form coefficients (Cb, Cp, Cm, Cwp, LCB)
 * Based on parametric methods from naval architecture literature
 * Ensures non-isometric (asymmetric) geometry - key distinction from Wigley series
 */

import { createCubicSpline, evaluateSpline, type Point2D } from "./splineInterpolation";
import type { OffsetsGrid } from "./geometryFormatConverter";

export interface HullDimensions {
  length: number;
  beam: number;
  draft: number;
  lcbPercent: number;
}

export interface FormCoefficients {
  cb: number;
  cp: number;
  cm: number;
  cwp: number;
  lcbPercent: number;
}

export interface GeneratedHullGeometry {
  stations: number[];
  waterlines: number[];
  offsets: number[][]; // [stationIndex][waterlineIndex]
  computedCoefficients?: FormCoefficients;
}

/**
 * Generate hull offsets from form coefficients with ShipD family adjustments
 */
export function generateFormCoefficientHull(
  dims: HullDimensions,
  cb: number,
  cp: number,
  cm: number,
  cwp: number,
  numStations: number = 23,
  numWaterlines: number = 13,
  bowFamily?: string | null,
  midshipFamily?: string | null,
  sternFamily?: string | null,
  vesselType?: string | null
): GeneratedHullGeometry {
  // Validate inputs
  if (dims.length <= 0 || dims.beam <= 0 || dims.draft <= 0) {
    throw new Error("Invalid hull dimensions");
  }
  if (cb <= 0 || cp <= 0 || cm <= 0 || cwp <= 0) {
    throw new Error("Invalid form coefficients");
  }

  // Generate BSRA-compatible station layout
  const stations = generateBSRAStations(dims.length, numStations);

  // Generate waterlines
  const waterlines = generateWaterlines(dims.draft, numWaterlines);

  // Step 1: Generate sectional area curve from Cp and LCB
  const sectionalAreas = generateSectionalAreaCurve(
    stations,
    dims.length,
    dims.beam,
    dims.draft,
    cm,
    cp,
    dims.lcbPercent,
    cb
  );

  // Step 2: Generate waterline half-breadths from Cwp with ShipD family adjustments
  const waterlineHalfBreadths = generateWaterlineHalfBreadths(
    stations,
    dims.length,
    dims.beam,
    cwp,
    cb,
    bowFamily,
    sternFamily,
    vesselType
  );

  // Step 3: Generate section shapes from Cm with ShipD family adjustments
  const sectionShapeProfile = generateSectionShapes(
    waterlines,
    dims.beam,
    dims.draft,
    cm,
    cb,
    midshipFamily,
    vesselType
  );

  // Step 4: Combine to generate offsets
  let offsets = combineToOffsets(
    stations,
    waterlines,
    sectionalAreas,
    sectionShapeProfile,
    waterlineHalfBreadths,
    dims.beam,
    dims.draft,
    cm
  );

  // Step 5: Fair the offsets using cubic spline (if BSRA standard layout)
  if (numStations === 23) {
    offsets = fairOffsets(stations, waterlines, offsets);
  }

  // Step 6: Validate and compute form coefficients
  const computedCoeffs = computeFormCoefficients(
    stations,
    waterlines,
    offsets,
    dims.length,
    dims.beam,
    dims.draft,
    cm
  );

  return {
    stations,
    waterlines,
    offsets,
    computedCoefficients: computedCoeffs,
  };
}

/**
 * Generate BSRA-compatible station layout
 * Stations: 0, 0.25, 0.5, 0.75, 1.0, 1.5, 2.0, ..., 10.0 (normalized)
 */
function generateBSRAStations(length: number, numStations: number): number[] {
  const stations: number[] = [];

  if (numStations === 23) {
    // Standard BSRA layout
    const stationFractions = [
      0.0, 0.25, 0.5, 0.75, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 5.0, 6.0, 6.5, 7.0, 7.5, 8.0, 8.5,
      9.0, 9.25, 9.5, 9.75, 10.0,
    ];

    for (const fraction of stationFractions) {
      stations.push((length * fraction) / 10.0);
    }
  } else {
    // Equally spaced stations
    for (let i = 0; i < numStations; i++) {
      stations.push((length * i) / (numStations - 1));
    }
  }

  return stations;
}

/**
 * Generate waterlines using BSRA standard heights or extended mode
 */
function generateWaterlines(draft: number, numWaterlines: number): number[] {
  const waterlines: number[] = [];

  // BSRA standard waterline heights (% of draft)
  const bsraStandardPercentages = [
    7.69, 15.38, 23.08, 38.46, 53.85, 69.23, 84.62, 100.0, 115.38, 130.77,
  ];

  if (numWaterlines === 10) {
    // Use exact BSRA standard waterlines
    for (const percentage of bsraStandardPercentages) {
      waterlines.push((draft * percentage) / 100.0);
    }
  } else if (numWaterlines === 13) {
    // Extended mode: Use BSRA standard plus additional waterlines
    const extendedPercentages: number[] = [0]; // Keel

    // Add 25%, 50%, 75% if they don't overlap with BSRA standard
    const additionalPercentages = [25.0, 50.0, 75.0];
    for (const pct of additionalPercentages) {
      // Check if this percentage is close to any BSRA standard (within 2%)
      const isCloseToBSRA = bsraStandardPercentages.some((bsra) => Math.abs(bsra - pct) < 2.0);
      if (!isCloseToBSRA) {
        extendedPercentages.push(pct);
      }
    }

    // Add all BSRA standard waterlines
    extendedPercentages.push(...bsraStandardPercentages);

    // Sort and take first 13
    extendedPercentages.sort((a, b) => a - b);
    const finalPercentages = extendedPercentages.slice(0, 13);

    // Convert to actual Z values
    for (const percentage of finalPercentages) {
      waterlines.push((draft * percentage) / 100.0);
    }
  } else {
    // For other counts, use equally spaced but ensure design draft (100%) is included
    const maxZ = draft * 1.3; // Extend above design draft for visualization

    for (let j = 0; j < numWaterlines; j++) {
      let z = (maxZ * j) / (numWaterlines - 1);

      // Ensure design draft is included (within 1% tolerance)
      if (j === numWaterlines - 1 || Math.abs(z - draft) < draft * 0.01) {
        z = draft; // Use exact design draft
      }

      waterlines.push(z);
    }
  }

  return waterlines;
}

/**
 * Generate sectional area curve from Cp and LCB
 * Uses raised cosine base function with Cb-dependent exponent
 * CRITICAL: LCB shift creates asymmetry (non-isometric)
 */
function generateSectionalAreaCurve(
  stations: number[],
  length: number,
  beam: number,
  draft: number,
  cm: number,
  cp: number,
  lcbPercent: number,
  cb: number
): number[] {
  // Midship section area
  const amid = cm * beam * draft;

  // Target volume from Cp
  const targetVolume = cp * amid * length;

  const sectionalAreas: number[] = [];
  const baseShape: number[] = [];

  // Adaptive exponent based on Cb (higher Cb = fuller ends = lower exponent)
  let exponent = 2.0 - 0.5 * (cb - 0.65);
  exponent = Math.max(1.0, Math.min(3.0, exponent));

  // Generate base shape using raised cosine
  for (const x of stations) {
    const xi = x / length; // Normalized position (0 to 1)

    // Raised cosine: φ(ξ) = 1 - cos(πξ)
    const phi = 1 - Math.cos(Math.PI * xi);

    // Apply Cb-dependent exponent for fullness control
    const shapeValue = Math.pow(phi, exponent);
    baseShape.push(shapeValue);
  }

  // Apply LCB shift (skew the curve) - CRITICAL for non-isometric geometry
  const shiftedShape = applyLCBShift(baseShape, stations, length, lcbPercent);

  // Normalize to match Cp
  const tempAreas = shiftedShape.map((s) => s * amid);
  const currentVolume = integrateTrapezoidal(stations, tempAreas);
  const scaleFactor = currentVolume > 0 ? targetVolume / currentVolume : 1;

  for (const shapeValue of shiftedShape) {
    const area = shapeValue * amid * scaleFactor;
    sectionalAreas.push(Math.max(0, area));
  }

  // Verify volume matches target
  const finalVolume = integrateTrapezoidal(stations, sectionalAreas);
  if (finalVolume > 0 && Math.abs(finalVolume - targetVolume) / targetVolume > 0.01) {
    // Re-scale if still off
    const adjustFactor = targetVolume / finalVolume;
    for (let i = 0; i < sectionalAreas.length; i++) {
      sectionalAreas[i] *= adjustFactor;
    }
  }

  return sectionalAreas;
}

/**
 * Apply LCB shift to sectional area curve using moment-based adjustment
 * CRITICAL: This creates asymmetry (non-isometric geometry)
 * Positive LCB% = forward shift, Negative = aft shift
 */
function applyLCBShift(
  baseShape: number[],
  stations: number[],
  length: number,
  lcbPercent: number
): number[] {
  if (Math.abs(lcbPercent) < 0.01) {
    return baseShape; // No shift needed, but still non-isometric due to form coefficients
  }

  // Convert LCB% to target position
  const targetLCB = length * (0.5 + lcbPercent / 100); // Convert % to position

  // Calculate current LCB
  const currentVolume = integrateTrapezoidal(stations, baseShape);
  const currentMoment = integrateFirstMoment(stations, baseShape);
  const currentLCB = currentVolume > 0 ? currentMoment / currentVolume : length / 2;

  // If already close enough, return base shape
  if (Math.abs(currentLCB - targetLCB) < length * 0.001) {
    return baseShape;
  }

  // Use iterative moment-based adjustment
  let adjustedShape = [...baseShape];
  const maxIterations = 10;
  const convergenceTolerance = 0.001; // 0.1% of length

  for (let iter = 0; iter < maxIterations; iter++) {
    // Calculate current LCB
    const iterVolume = integrateTrapezoidal(stations, adjustedShape);
    const iterMoment = integrateFirstMoment(stations, adjustedShape);
    const iterLCB = iterVolume > 0 ? iterMoment / iterVolume : length / 2;

    // Check convergence
    const lcbError = Math.abs(iterLCB - targetLCB) / length;
    if (lcbError < convergenceTolerance) {
      break;
    }

    // Calculate adjustment needed
    const lcbErrorAbs = targetLCB - iterLCB;

    // Apply moment-based adjustment
    const newShape: number[] = [];
    const adjustmentFactor = 0.3; // Damping factor for stability

    for (let i = 0; i < stations.length; i++) {
      const x = stations[i];
      const currentValue = adjustedShape[i];

      // Apply adjustment based on position relative to target LCB
      const positionFactor = (x - targetLCB) / length; // -0.5 to +0.5
      const adjustment = 1.0 + positionFactor * adjustmentFactor * (lcbErrorAbs / length) * 2.0;

      const adjustedValue = currentValue * adjustment;
      newShape.push(Math.max(0, adjustedValue));
    }

    // Normalize to preserve volume
    const newVolume = integrateTrapezoidal(stations, newShape);
    if (newVolume > 0 && currentVolume > 0) {
      const volumeScale = currentVolume / newVolume;
      for (let i = 0; i < newShape.length; i++) {
        newShape[i] *= volumeScale;
      }
    }

    adjustedShape = newShape;
  }

  return adjustedShape;
}

/**
 * Get bow family multiplier for bow exponent adjustment
 * Based on ShipD parameterization recommendations
 */
function getBowFamilyMultiplier(bowFamily?: string | null): number {
  if (!bowFamily) return 1.0;

  const family = bowFamily.toLowerCase();
  switch (family) {
    case "bulbous_bow":
      return 0.5; // Fuller bow (bulb adds volume) - INCREASED from 0.7 for more visible difference
    case "axe_bow":
      return 1.8; // Very fine bow (sharp entry) - INCREASED from 1.5
    case "fine_entry":
      return 1.5; // Fine entry (streamlined) - INCREASED from 1.2 for more visible difference
    case "blunt_bow":
      return 0.4; // Very full bow (blunt entry) - INCREASED from 0.6
    default:
      return 1.0; // Default: no adjustment
  }
}

/**
 * Get stern family multiplier for stern exponent adjustment
 * Based on ShipD parameterization recommendations
 */
function getSternFamilyMultiplier(sternFamily?: string | null): number {
  if (!sternFamily) return 1.0;

  const family = sternFamily.toLowerCase();
  switch (family) {
    case "transom_stern":
      return 0.5; // Full, blunt stern (flat transom) - INCREASED from 0.7 for more visible difference
    case "twin_skeg":
      return 0.7; // Full stern (with skegs) - INCREASED from 0.8
    case "cruiser_stern":
      return 1.0; // Moderate fullness
    case "fine_stern":
      return 1.6; // Fine, streamlined stern - INCREASED from 1.3
    case "canoe_stern":
      return 1.8; // Very fine, canoe-like stern - INCREASED from 1.4 for more visible difference
    default:
      return 1.0; // Default: no adjustment
  }
}

/**
 * Get midship family adjustment for section shape
 * Based on ShipD parameterization recommendations
 */
function getMidshipFamilyAdjustment(midshipFamily?: string | null): {
  cmAdjustment: number;
  betaAdjustment: number;
} {
  if (!midshipFamily) return { cmAdjustment: 0, betaAdjustment: 0 };

  const family = midshipFamily.toLowerCase();
  switch (family) {
    case "full_midship":
      return { cmAdjustment: 0.08, betaAdjustment: -8 }; // U-shaped sections - INCREASED for more visible difference
    case "fine_midship":
      return { cmAdjustment: -0.08, betaAdjustment: 8 }; // V-shaped sections - INCREASED
    case "deep_v_midship":
      return { cmAdjustment: -0.12, betaAdjustment: 15 }; // Very deep V-sections - INCREASED for more visible difference
    default:
      return { cmAdjustment: 0, betaAdjustment: 0 }; // Default: no adjustment
  }
}

/**
 * Get vessel type multiplier for additional shape adjustments
 */
function getVesselTypeMultiplier(vesselType?: string | null): number {
  if (!vesselType) return 1.0;

  const type = vesselType.toLowerCase();
  switch (type) {
    case "yacht":
    case "recreational":
      return 1.1; // Slightly finer (yachts are more streamlined)
    case "cargo":
    case "bulk":
    case "general_cargo":
      return 0.9; // Slightly fuller (cargo vessels are fuller)
    case "container":
      return 1.0; // Neutral (optimized for speed)
    case "tanker":
      return 0.85; // Fuller (tankers are very full)
    default:
      return 1.0; // Default: no adjustment
  }
}

/**
 * Generate waterline half-breadths from Cwp with Cb-dependent parameters and ShipD family adjustments
 */
function generateWaterlineHalfBreadths(
  stations: number[],
  length: number,
  beam: number,
  cwp: number,
  cb: number,
  bowFamily?: string | null,
  sternFamily?: string | null,
  vesselType?: string | null
): number[] {
  const halfBreadths: number[] = [];
  const targetArea = cwp * length * beam;

  // Cb-dependent base exponents
  const baseBowExponent = 2.0 + 0.5 * (1 - cb);
  const baseSternExponent = 2.0 + 0.3 * (1 - cb);

  // Apply ShipD family multipliers
  const bowFamilyMultiplier = getBowFamilyMultiplier(bowFamily);
  const sternFamilyMultiplier = getSternFamilyMultiplier(sternFamily);
  const vesselTypeMultiplier = getVesselTypeMultiplier(vesselType);

  // Combine multipliers (bow and stern are independent, vessel type applies to both)
  let bowExponent = baseBowExponent * bowFamilyMultiplier * vesselTypeMultiplier;
  let sternExponent = baseSternExponent * sternFamilyMultiplier * vesselTypeMultiplier;

  // Clamp to reasonable ranges
  bowExponent = Math.max(1.0, Math.min(4.0, bowExponent));
  sternExponent = Math.max(1.0, Math.min(3.5, sternExponent));

  // Fullness factor: 0.4 + 0.2 * (cb - 0.65) - higher Cb = more forward fullness
  // Adjust based on bow family (bulbous bow tends to have more forward fullness)
  let fullnessFactor = 0.4 + 0.2 * (cb - 0.65);
  if (bowFamily?.toLowerCase() === "bulbous_bow") {
    fullnessFactor += 0.1; // Bulbous bow adds forward fullness
  }
  fullnessFactor = Math.max(0.3, Math.min(0.7, fullnessFactor));

  // Generate base shape
  const baseShape: number[] = [];
  for (const x of stations) {
    const xi = x / length; // Normalized position (0 to 1)

    // Parametric planform: y = (B/2) * (1 - αξ^p - (1-α)(1-ξ)^q)
    const bowTerm = fullnessFactor * Math.pow(xi, bowExponent);
    const sternTerm = (1 - fullnessFactor) * Math.pow(1 - xi, sternExponent);
    const shapeValue = 1 - bowTerm - sternTerm;

    baseShape.push(Math.max(0, shapeValue));
  }

  // Scale to match Cwp
  const currentHalfBreadths = baseShape.map((s) => (beam / 2) * s);
  const currentArea = integrateTrapezoidal(
    stations,
    currentHalfBreadths.map((hb) => 2 * hb)
  );
  const scaleFactor = currentArea > 0 ? targetArea / currentArea : 1;

  for (const shapeValue of baseShape) {
    const halfBreadth = (beam / 2) * shapeValue * Math.sqrt(scaleFactor);
    halfBreadths.push(Math.max(0, halfBreadth));
  }

  // Verify and adjust if needed
  const finalHalfBreadths = halfBreadths.map((hb) => 2 * hb);
  const finalArea = integrateTrapezoidal(stations, finalHalfBreadths);
  if (Math.abs(finalArea - targetArea) / targetArea > 0.01) {
    // Re-scale if still off
    const adjustFactor = targetArea / finalArea;
    for (let i = 0; i < halfBreadths.length; i++) {
      halfBreadths[i] *= Math.sqrt(adjustFactor);
    }
  }

  return halfBreadths;
}

/**
 * Generate section shapes from Cm with Cb influence and ShipD midship family adjustments
 * Parametric profile: y(z) = (B/2) * (1 - (z/T)^p)^q
 */
function generateSectionShapes(
  waterlines: number[],
  beam: number,
  draft: number,
  cm: number,
  cb: number,
  midshipFamily?: string | null,
  vesselType?: string | null
): number[] {
  // Apply midship family adjustments to Cm
  const { cmAdjustment, betaAdjustment } = getMidshipFamilyAdjustment(midshipFamily);
  let adjustedCm = cm + cmAdjustment;
  adjustedCm = Math.max(0.7, Math.min(1.0, adjustedCm)); // Keep within reasonable bounds

  // Calculate p and q from adjusted Cm
  let p = 1 + 4 * (1 - adjustedCm);
  let q = 0.5 + 3 * (adjustedCm - 0.85);

  // Apply beta adjustment (affects section shape: higher beta = more V-shaped)
  // Beta adjustment affects p: higher beta → lower p (more V-shaped)
  if (betaAdjustment !== 0) {
    // Convert beta adjustment (degrees) to p adjustment
    // Higher beta (more V-shaped) → lower p
    const pAdjustment = -betaAdjustment / 10; // Scale: 10 degrees ≈ 0.1 p change
    p += pAdjustment;
  }

  // Clamp to reasonable ranges
  p = Math.max(0.5, Math.min(3.0, p));
  q = Math.max(0.3, Math.min(4.0, q));

  // Add Cb correction: higher Cb tends to have fuller sections
  // p_adjusted = p * (1 + 0.2 * (Cb - 0.75))
  let cbCorrection = 1 + 0.2 * (cb - 0.75);

  // Apply vessel type multiplier (affects section fullness)
  const vesselTypeMultiplier = getVesselTypeMultiplier(vesselType);
  cbCorrection *= vesselTypeMultiplier;

  p *= cbCorrection;

  // Re-clamp after Cb correction
  p = Math.max(0.5, Math.min(3.0, p));

  const sectionShapes: number[] = [];

  for (const z of waterlines) {
    const zNorm = z / draft; // Normalized depth (0 to 1.3)

    if (zNorm <= 1) {
      // Below design draft: use parametric profile
      const term = Math.pow(zNorm, p);
      const shapeValue = Math.pow(1 - term, q);
      const halfBreadth = (beam / 2) * shapeValue;
      sectionShapes.push(Math.max(0, halfBreadth));
    } else {
      // Above design draft: extrapolate (for visualization)
      sectionShapes.push(0);
    }
  }

  return sectionShapes;
}

/**
 * Combine sectional areas, section shapes, and waterlines to generate offsets
 */
function combineToOffsets(
  stations: number[],
  waterlines: number[],
  sectionalAreas: number[],
  sectionShapeProfile: number[],
  waterlineHalfBreadths: number[],
  beam: number,
  draft: number,
  _cm: number
): number[][] {
  const offsets: number[][] = [];

  for (let i = 0; i < stations.length; i++) {
    const targetArea = sectionalAreas[i];
    const waterlineHalfBreadth = waterlineHalfBreadths[i];

    // Generate initial offsets by blending section shape and waterline
    const initialOffsets: number[] = [];
    for (let j = 0; j < waterlines.length; j++) {
      const z = waterlines[j];
      const zNorm = z / draft;

      if (zNorm > 1) {
        // Above design draft: use waterline constraint
        initialOffsets.push(Math.min(waterlineHalfBreadth, beam / 2));
      } else {
        // Below design draft: blend section shape and waterline constraint
        const sectionHalfBreadth = sectionShapeProfile[j];
        const blendFactor = zNorm;
        const blendedHalfBreadth =
          sectionHalfBreadth * (1 - blendFactor) + waterlineHalfBreadth * blendFactor;
        initialOffsets.push(Math.max(0, Math.min(blendedHalfBreadth, beam / 2)));
      }
    }

    // Calculate current sectional area
    const halfBreadthsForArea = initialOffsets.map((hb) => 2 * hb);
    const currentArea = integrateTrapezoidal(waterlines, halfBreadthsForArea);

    // Iteratively scale to match target area
    let stationOffsets: number[];
    if (targetArea > 0 && currentArea > 0) {
      const refinedOffsets: number[] = [...initialOffsets];
      const maxIterations = 10;
      const tolerance = 0.001; // 0.1% tolerance

      for (let iter = 0; iter < maxIterations; iter++) {
        // Calculate current area
        const iterHalfBreadths = refinedOffsets.map((hb) => 2 * hb);
        const iterArea = integrateTrapezoidal(waterlines, iterHalfBreadths);

        // Check convergence
        const areaError = Math.abs(iterArea - targetArea) / targetArea;
        if (areaError < tolerance) break;

        // Use square root scaling for area matching
        const areaRatio = targetArea / iterArea;
        let iterScaleFactor = Math.pow(areaRatio, 0.5);

        // Apply damping for stability
        if (areaError < 0.05) {
          iterScaleFactor = 1 + (iterScaleFactor - 1) * 0.7;
        }

        // Apply scaling with constraints
        for (let j = 0; j < waterlines.length; j++) {
          const z = waterlines[j];
          const zNorm = z / draft;

          if (zNorm > 1) {
            // Above design draft: keep waterline constraint
            refinedOffsets[j] = Math.min(waterlineHalfBreadth, beam / 2);
          } else if (zNorm >= 0.95) {
            // Near design draft: blend with waterline constraint
            const draftBlend = (zNorm - 0.95) / 0.05;
            const scaledHalfBreadth = refinedOffsets[j] * iterScaleFactor;
            refinedOffsets[j] =
              scaledHalfBreadth * (1 - draftBlend) + waterlineHalfBreadth * draftBlend;
            refinedOffsets[j] = Math.max(0, Math.min(refinedOffsets[j], beam / 2));
          } else {
            // Below 95% draft: scale freely
            const scaledHalfBreadth = refinedOffsets[j] * iterScaleFactor;
            refinedOffsets[j] = Math.max(0, Math.min(scaledHalfBreadth, beam / 2));
          }
        }
      }

      stationOffsets = refinedOffsets;
    } else {
      stationOffsets = initialOffsets;
    }

    offsets.push(stationOffsets);
  }

  return offsets;
}

/**
 * Fair offsets using cubic spline fairing (for BSRA standard layout)
 */
function fairOffsets(stations: number[], waterlines: number[], offsets: number[][]): number[][] {
  const faired: number[][] = [];

  // Fair each waterline
  for (let wlIdx = 0; wlIdx < waterlines.length; wlIdx++) {
    const halfBreadths = offsets.map((st) => st[wlIdx]);
    const fairedHalfBreadths = fairWaterline(stations, halfBreadths, waterlines[wlIdx]);

    faired.push(fairedHalfBreadths);
  }

  // Transpose back to [station][waterline] format
  const result: number[][] = [];
  for (let stIdx = 0; stIdx < stations.length; stIdx++) {
    const stationOffsets = faired.map((wlOffsets) => wlOffsets[stIdx]);
    result.push(stationOffsets);
  }

  return result;
}

/**
 * Fair a waterline using cubic spline with BSRA end slopes
 */
function fairWaterline(
  stations: number[],
  halfBreadths: number[],
  _waterlineHeight: number
): number[] {
  if (stations.length < 2 || halfBreadths.length < 2) {
    return halfBreadths;
  }

  // Create points for spline
  const points: Point2D[] = stations.map((x, i) => ({ x, y: halfBreadths[i] }));

  // Create spline (natural cubic spline)
  const segments = createCubicSpline(points);

  // Evaluate at original station positions
  const faired: number[] = [];
  for (const x of stations) {
    const y = evaluateSpline(segments, x);
    faired.push(Math.max(0, y)); // Ensure non-negative
  }

  return faired;
}

/**
 * Compute form coefficients from generated offsets for validation
 */
function computeFormCoefficients(
  stations: number[],
  waterlines: number[],
  offsets: number[][],
  length: number,
  beam: number,
  draft: number,
  _cm: number
): FormCoefficients {
  // Filter waterlines up to design draft only
  const activeWaterlines = waterlines.filter((w) => w <= draft);
  const finalActiveWaterlines =
    activeWaterlines.length >= 2
      ? activeWaterlines
      : waterlines.slice(0, Math.min(waterlines.length, 10));

  // Compute volume using integration
  const sectionAreas: number[] = [];
  for (const stationOffsets of offsets) {
    // Get half-breadths for active waterlines only
    const activeHalfBreadths: number[] = [];
    const activeWaterlineZs: number[] = [];

    for (let j = 0; j < waterlines.length && j < stationOffsets.length; j++) {
      if (waterlines[j] <= draft) {
        activeWaterlineZs.push(waterlines[j]);
        activeHalfBreadths.push(stationOffsets[j]);
      }
    }

    // Use finalActiveWaterlines if we don't have enough active waterlines
    if (activeWaterlineZs.length < 2) {
      for (let j = 0; j < finalActiveWaterlines.length && j < stationOffsets.length; j++) {
        const wlIdx = waterlines.indexOf(finalActiveWaterlines[j]);
        if (wlIdx >= 0 && wlIdx < stationOffsets.length) {
          if (!activeWaterlineZs.includes(finalActiveWaterlines[j])) {
            activeWaterlineZs.push(finalActiveWaterlines[j]);
            activeHalfBreadths.push(stationOffsets[wlIdx]);
          }
        }
      }
    }

    // Integrate half-breadths to get sectional area
    if (activeHalfBreadths.length >= 2) {
      const halfArea = integrateTrapezoidal(activeWaterlineZs, activeHalfBreadths);
      const fullArea = 2 * halfArea; // Mirror to port side
      sectionAreas.push(fullArea);
    } else {
      sectionAreas.push(0);
    }
  }

  // Compute volume
  const volume = integrateTrapezoidal(stations, sectionAreas);

  // Compute Cb
  const cb = volume > 0 ? volume / (length * beam * draft) : 0;

  // Compute Cp
  const maxSectionArea = sectionAreas.length > 0 ? Math.max(...sectionAreas) : 0;
  const cp = maxSectionArea > 0 && length > 0 ? volume / (maxSectionArea * length) : 0;

  // Compute Cm
  const midshipIndex = Math.floor(sectionAreas.length / 2);
  const midshipArea = midshipIndex < sectionAreas.length ? sectionAreas[midshipIndex] : 0;
  const computedCm = midshipArea > 0 && beam > 0 && draft > 0 ? midshipArea / (beam * draft) : 0;

  // Compute Cwp - find waterline at design draft
  let designDraftIndex = -1;
  for (let j = 0; j < waterlines.length; j++) {
    if (
      Math.abs(waterlines[j] - draft) < 0.01 ||
      (j > 0 && waterlines[j] > draft && waterlines[j - 1] <= draft)
    ) {
      designDraftIndex = j;
      break;
    }
  }
  if (designDraftIndex < 0) designDraftIndex = waterlines.length - 1;

  const waterlineHalfBreadths: number[] = [];
  for (const stationOffsets of offsets) {
    if (designDraftIndex < stationOffsets.length) {
      waterlineHalfBreadths.push(stationOffsets[designDraftIndex]);
    } else {
      waterlineHalfBreadths.push(0);
    }
  }

  const waterplaneArea = integrateTrapezoidal(
    stations,
    waterlineHalfBreadths.map((hb) => 2 * hb)
  );
  const cwp = waterplaneArea > 0 && length > 0 && beam > 0 ? waterplaneArea / (length * beam) : 0;

  // Compute LCB
  const volumeMoment = integrateFirstMoment(stations, sectionAreas);
  const lcbPosition = volume > 0 ? volumeMoment / volume : length / 2;
  // LCB% is from aft perpendicular: positive forward, negative aft
  // lcbPosition is from 0 (AP) to length (FP)
  // LCB% = ((lcbPosition / length) - 0.5) * 100
  // If lcbPosition = length/2, LCB% = 0 (midship)
  // If lcbPosition > length/2, LCB% > 0 (forward)
  // If lcbPosition < length/2, LCB% < 0 (aft)
  const lcbPercent = length > 0 ? (lcbPosition / length - 0.5) * 100 : 0;

  return {
    cb,
    cp,
    cm: computedCm,
    cwp,
    lcbPercent,
  };
}

// Helper methods for numerical integration

function integrateTrapezoidal(x: number[], y: number[]): number {
  if (x.length !== y.length || x.length < 2) {
    return 0;
  }

  let sum = 0;
  for (let i = 0; i < x.length - 1; i++) {
    const dx = x[i + 1] - x[i];
    sum += (dx * (y[i] + y[i + 1])) / 2;
  }
  return sum;
}

function integrateFirstMoment(x: number[], y: number[]): number {
  if (x.length !== y.length || x.length < 2) {
    return 0;
  }

  let sum = 0;
  for (let i = 0; i < x.length - 1; i++) {
    const dx = x[i + 1] - x[i];
    const avgX = (x[i] + x[i + 1]) / 2;
    const avgY = (y[i] + y[i + 1]) / 2;
    sum += dx * avgX * avgY;
  }
  return sum;
}

/**
 * Validate that generated offsets are non-isometric (asymmetric)
 * CRITICAL: This is the key distinction from Wigley series
 */
export function validateNonIsometric(offsets: OffsetsGrid): boolean {
  const numStations = offsets.stations.length;
  const midshipIdx = Math.floor(numStations / 2);
  const tolerance = 0.001; // 1mm tolerance

  // Check bow vs stern asymmetry
  for (let i = 0; i < midshipIdx; i++) {
    const sternIdx = i;
    const bowIdx = numStations - 1 - i;

    // Check at design draft waterline (last waterline)
    const designDraftIdx = offsets.waterlines.length - 1;
    const sternOffset = offsets.offsets[sternIdx]?.[designDraftIdx] ?? 0;
    const bowOffset = offsets.offsets[bowIdx]?.[designDraftIdx] ?? 0;

    // Offsets should be different (not symmetric)
    if (Math.abs(sternOffset - bowOffset) < tolerance) {
      return false; // Too symmetric, likely isometric
    }
  }

  return true; // Asymmetric, good
}
