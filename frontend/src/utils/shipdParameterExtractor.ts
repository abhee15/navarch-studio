/**
 * ShipD Parameter Extractor
 *
 * Extracts and denormalizes ShipD parameters from the 45-element normalized vector.
 * Handles both ratio parameters (which are already in 0-1 range) and physical parameters
 * (which need denormalization using metadata min/max values).
 */

import type { ShipDParameterMetadata } from "../types/sizing";
import type { CandidateDesign } from "../types/sizing";

/**
 * Parameters that are ratios (0-1 range) and should NOT be denormalized
 * These are already in their final form in the normalized vector
 */
const RATIO_PARAMETERS = new Set([
  1, // Lb - Bow length ratio
  2, // Ls - Stern length ratio
  9, // Rc - Bow curvature coefficient
  10, // Rk - Bow knuckle coefficient
  14, // Kappa_bow - Bow curvature type
  22, // Atrans - Transom area ratio
  24, // Kappa_stern - Stern curvature type
  28, // Bc_trans - Transom width ratio
  29, // Rc_trans - Stern curvature coefficient
  30, // Rk_trans - Stern knuckle coefficient
  33, // Lbb - Bulb length ratio
  34, // Hbb - Bulb height ratio
  35, // Bbb - Bulb width ratio
  36, // Lbbm - Bulb asymmetry factor
  37, // Rbb - Bulb fillet radius
]);

/**
 * Parameters that are boolean flags (0 or 1)
 */
const BOOLEAN_PARAMETERS = new Set([
  20, // bit_EP_S - Sheer extrusion
  21, // bit_EP_T - Tumblehome
  31, // bit_BB - Bulbous bow
]);

/**
 * Extracts a single parameter from the ShipD vector
 * @param vector - The 45-element normalized vector
 * @param index - Parameter index (0-44)
 * @param metadata - Parameter metadata for denormalization
 * @returns The extracted and denormalized value, or null if not available
 */
export function extractShipDParameter(
  vector: number[] | null | undefined,
  index: number,
  metadata?: ShipDParameterMetadata[]
): number | null {
  if (!vector || index < 0 || index >= vector.length) {
    return null;
  }

  const normalizedValue = vector[index];

  // Boolean parameters: return 0 or 1
  if (BOOLEAN_PARAMETERS.has(index)) {
    return normalizedValue > 0.5 ? 1 : 0;
  }

  // Ratio parameters: use normalized value directly (it's already the ratio)
  if (RATIO_PARAMETERS.has(index)) {
    return normalizedValue;
  }

  // Physical parameters: denormalize using metadata
  if (metadata && metadata.length > 0) {
    const paramMeta = metadata.find((m) => m.parameterIndex === index);
    if (paramMeta && paramMeta.min != null && paramMeta.max != null) {
      // Linear denormalization: physical = min + (max - min) * normalized
      const physical = paramMeta.min + (paramMeta.max - paramMeta.min) * normalizedValue;
      return physical;
    }
  }

  // Fallback: return normalized value if no metadata available
  return normalizedValue;
}

/**
 * Extracts all ShipD parameters from the vector and populates candidate properties
 * @param candidate - Candidate design with shipdParametersJson
 * @param metadata - ShipD parameter metadata for denormalization
 * @returns Updated candidate with extracted parameters
 */
export function extractShipDParameters(
  candidate: CandidateDesign,
  metadata?: ShipDParameterMetadata[]
): CandidateDesign {
  // If candidate already has extracted parameters, return as-is
  if (
    candidate.bowLengthRatio != null &&
    candidate.sternLengthRatio != null &&
    candidate.shipdParametersJson
  ) {
    // Parameters already extracted, but verify they're valid
    return validateAndFixParameters(candidate);
  }

  // Extract from vector if available
  if (!candidate.shipdParametersJson) {
    return candidate;
  }

  let vector: number[];
  try {
    vector = JSON.parse(candidate.shipdParametersJson);
    if (!Array.isArray(vector) || vector.length !== 45) {
      return candidate;
    }
  } catch {
    return candidate;
  }

  // Extract parameters
  const extracted: Partial<CandidateDesign> = {
    // Longitudinal proportions (ratios - use directly)
    bowLengthRatio: extractShipDParameter(vector, 1, metadata) ?? undefined,
    sternLengthRatio: extractShipDParameter(vector, 2, metadata) ?? undefined,

    // Bow shape
    bowFlareAngle: extractShipDParameter(vector, 8, metadata) ?? undefined, // Beta - denormalize to degrees
    bowCurvature: extractShipDParameter(vector, 9, metadata) ?? undefined, // Rc - ratio
    bowKnuckle: extractShipDParameter(vector, 10, metadata) ?? undefined, // Rk - ratio
    deadriseAngle: extractShipDParameter(vector, 19, metadata) ?? undefined, // Cdrft - denormalize to degrees

    // Stern shape
    sternRakeAngle: extractShipDParameter(vector, 27, metadata) ?? undefined, // Beta_trans - denormalize to degrees
    sternCurvature: extractShipDParameter(vector, 29, metadata) ?? undefined, // Rc_trans - ratio
    sternKnuckle: extractShipDParameter(vector, 30, metadata) ?? undefined, // Rk_trans - ratio
    transomArea: extractShipDParameter(vector, 22, metadata) ?? undefined, // Atrans - ratio
    transomWidth: extractShipDParameter(vector, 28, metadata) ?? undefined, // Bc_trans - ratio

    // Midship features (boolean)
    hasSheer: extractShipDParameter(vector, 20, metadata) ? true : false,
    hasTumblehome: extractShipDParameter(vector, 21, metadata) ? true : false,

    // Bulbous bow
    hasBulb: extractShipDParameter(vector, 31, metadata) ? true : false,
    bulbLengthRatio: extractShipDParameter(vector, 33, metadata) ?? undefined, // Lbb - ratio
    bulbHeightRatio: extractShipDParameter(vector, 34, metadata) ?? undefined, // Hbb - ratio
    bulbWidthRatio: extractShipDParameter(vector, 35, metadata) ?? undefined, // Bbb - ratio
    bulbAsymmetry: extractShipDParameter(vector, 36, metadata) ?? undefined, // Lbbm - ratio
    bulbFilletRadius: extractShipDParameter(vector, 37, metadata) ?? undefined, // Rbb - ratio
  };

  // Validate and fix parameters
  const updated = { ...candidate, ...extracted };
  return validateAndFixParameters(updated);
}

/**
 * Validates and fixes extracted parameters to ensure they're within valid bounds
 * @param candidate - Candidate with extracted parameters
 * @returns Candidate with validated/fixed parameters
 */
function validateAndFixParameters(candidate: CandidateDesign): CandidateDesign {
  const MIN_BOW_LENGTH = 0.15;
  const MIN_STERN_LENGTH = 0.15;
  const MAX_BOW_LENGTH = 0.45;
  const MAX_STERN_LENGTH = 0.45;
  const MAX_SUM = 0.85; // Ensure at least 15% midship

  let lb = candidate.bowLengthRatio;
  let ls = candidate.sternLengthRatio;

  // If parameters are missing or invalid, try to extract from vector
  if (lb == null || ls == null || lb < MIN_BOW_LENGTH || ls < MIN_STERN_LENGTH || lb + ls > MAX_SUM) {
    if (candidate.shipdParametersJson) {
      try {
        const vector = JSON.parse(candidate.shipdParametersJson);
        if (Array.isArray(vector) && vector.length === 45) {
          // Use vector values directly (they're already ratios)
          lb = vector[1] ?? lb;
          ls = vector[2] ?? ls;
        }
      } catch {
        // Ignore parse errors
      }
    }

    // Validate and fix bounds
    if (lb == null || lb < MIN_BOW_LENGTH || lb > MAX_BOW_LENGTH) {
      lb = 0.3; // Default fallback
    }
    if (ls == null || ls < MIN_STERN_LENGTH || ls > MAX_STERN_LENGTH) {
      ls = 0.3; // Default fallback
    }
    if (lb + ls > MAX_SUM) {
      // Scale down proportionally
      const scale = MAX_SUM / (lb + ls);
      lb = lb * scale;
      ls = ls * scale;
    }
  }

  return {
    ...candidate,
    bowLengthRatio: lb,
    sternLengthRatio: ls,
  };
}
