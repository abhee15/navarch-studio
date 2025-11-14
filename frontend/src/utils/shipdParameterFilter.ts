/**
 * ShipD Parameter Filter
 *
 * Determines which ShipD parameters are applicable/adjustable based on:
 * - Vessel type and category
 * - Mask version
 * - Hull families (bow, midship, stern)
 * - Current parameter values (e.g., hasBulb flag)
 */

import type { CandidateDesign } from "../types/sizing";

/**
 * Parameter groups that can be filtered
 */
export type ParameterGroup =
  | "dimensions"
  | "coefficients"
  | "longitudinal"
  | "bow"
  | "stern"
  | "midship"
  | "bulb";

/**
 * Individual parameter identifiers
 */
export type ParameterId =
  // Dimensions
  | "lpp"
  | "beam"
  | "draft"
  | "depth"
  // Coefficients
  | "cb"
  | "cp"
  | "cwp"
  // Longitudinal
  | "bowLength"
  | "sternLength"
  // Bow
  | "bowFlareAngle"
  | "bowCurvature"
  | "bowKnuckle"
  | "deadriseAngle"
  // Stern
  | "sternRakeAngle"
  | "sternCurvature"
  | "sternKnuckle"
  | "transomArea"
  | "transomWidth"
  // Midship
  | "hasSheer"
  | "hasTumblehome"
  // Bulb
  | "hasBulb"
  | "bulbLength"
  | "bulbHeight"
  | "bulbWidth"
  | "bulbAsymmetry"
  | "bulbFillet";

/**
 * Configuration for parameter visibility rules
 */
interface ParameterVisibilityConfig {
  candidate: CandidateDesign;
  vesselType?: string;
  vesselCategory?: string;
  maskVersion?: number;
}

/**
 * Midship families that support tumblehome
 */
const TUMBLEHOME_MIDSHIP_FAMILIES = new Set(["fine_midship", "fine", "ultra_fine"]);

/**
 * Midship families that support sheer
 */
const SHEER_MIDSHIP_FAMILIES = new Set(["full_midship", "full", "moderate_midship", "moderate"]);

/**
 * Stern families that support transom parameters
 */
const TRANSOM_STERN_FAMILIES = new Set(["transom_stern", "transom", "flat_stern"]);

/**
 * Vessel types that commonly have bulbous bows
 */
const BULB_VESSEL_TYPES = new Set([
  "container",
  "bulk_carrier",
  "tanker",
  "general_cargo",
  "lng_carrier",
]);

/**
 * Determines if a parameter should be visible/adjustable
 */
export function isParameterVisible(
  parameterId: ParameterId,
  config: ParameterVisibilityConfig
): boolean {
  const { candidate, vesselType } = config;

  // Core dimensions and coefficients are always visible
  if (["lpp", "beam", "draft", "depth", "cb", "cp", "cwp"].includes(parameterId)) {
    return true;
  }

  // Longitudinal proportions are always visible (core parameter)
  if (["bowLength", "sternLength"].includes(parameterId)) {
    return true;
  }

  // Bow parameters - always visible (all vessels have bows)
  if (["bowFlareAngle", "bowCurvature", "bowKnuckle", "deadriseAngle"].includes(parameterId)) {
    return true;
  }

  // Stern parameters - always visible (all vessels have sterns)
  if (["sternRakeAngle", "sternCurvature", "sternKnuckle"].includes(parameterId)) {
    return true;
  }

  // Transom parameters - only for transom stern families
  if (["transomArea", "transomWidth"].includes(parameterId)) {
    const sternFamily = candidate.sternFamily?.toLowerCase() || "";
    return TRANSOM_STERN_FAMILIES.has(sternFamily);
  }

  // Tumblehome - only for certain midship families
  if (parameterId === "hasTumblehome") {
    const midshipFamily = candidate.midshipFamily?.toLowerCase() || "";
    return TUMBLEHOME_MIDSHIP_FAMILIES.has(midshipFamily);
  }

  // Sheer - only for certain midship families
  if (parameterId === "hasSheer") {
    const midshipFamily = candidate.midshipFamily?.toLowerCase() || "";
    return SHEER_MIDSHIP_FAMILIES.has(midshipFamily);
  }

  // Bulb parameters - only if bulb is enabled or vessel type supports it
  if (
    ["hasBulb", "bulbLength", "bulbHeight", "bulbWidth", "bulbAsymmetry", "bulbFillet"].includes(
      parameterId
    )
  ) {
    // Show if bulb is currently enabled
    if (candidate.hasBulb) {
      return true;
    }

    // Show hasBulb toggle if vessel type commonly has bulbs
    if (parameterId === "hasBulb") {
      const type = (vesselType || candidate.vesselType || "").toLowerCase();
      return BULB_VESSEL_TYPES.has(type);
    }

    // Hide other bulb parameters if bulb is not enabled
    return false;
  }

  // Default: show parameter
  return true;
}

/**
 * Gets the list of visible parameters for a candidate
 */
export function getVisibleParameters(config: ParameterVisibilityConfig): Set<ParameterId> {
  const allParameters: ParameterId[] = [
    // Dimensions
    "lpp",
    "beam",
    "draft",
    "depth",
    // Coefficients
    "cb",
    "cp",
    "cwp",
    // Longitudinal
    "bowLength",
    "sternLength",
    // Bow
    "bowFlareAngle",
    "bowCurvature",
    "bowKnuckle",
    "deadriseAngle",
    // Stern
    "sternRakeAngle",
    "sternCurvature",
    "sternKnuckle",
    "transomArea",
    "transomWidth",
    // Midship
    "hasSheer",
    "hasTumblehome",
    // Bulb
    "hasBulb",
    "bulbLength",
    "bulbHeight",
    "bulbWidth",
    "bulbAsymmetry",
    "bulbFillet",
  ];

  const visible = new Set<ParameterId>();
  for (const param of allParameters) {
    if (isParameterVisible(param, config)) {
      visible.add(param);
    }
  }

  return visible;
}

/**
 * Determines if a parameter group should be shown
 */
export function isParameterGroupVisible(
  group: ParameterGroup,
  config: ParameterVisibilityConfig
): boolean {
  const visibleParams = getVisibleParameters(config);

  switch (group) {
    case "dimensions":
      return (
        visibleParams.has("lpp") ||
        visibleParams.has("beam") ||
        visibleParams.has("draft") ||
        visibleParams.has("depth")
      );
    case "coefficients":
      return visibleParams.has("cb") || visibleParams.has("cp") || visibleParams.has("cwp");
    case "longitudinal":
      return visibleParams.has("bowLength") || visibleParams.has("sternLength");
    case "bow":
      return (
        visibleParams.has("bowFlareAngle") ||
        visibleParams.has("bowCurvature") ||
        visibleParams.has("bowKnuckle") ||
        visibleParams.has("deadriseAngle")
      );
    case "stern":
      return (
        visibleParams.has("sternRakeAngle") ||
        visibleParams.has("sternCurvature") ||
        visibleParams.has("sternKnuckle") ||
        visibleParams.has("transomArea") ||
        visibleParams.has("transomWidth")
      );
    case "midship":
      return visibleParams.has("hasSheer") || visibleParams.has("hasTumblehome");
    case "bulb":
      return (
        visibleParams.has("hasBulb") ||
        visibleParams.has("bulbLength") ||
        visibleParams.has("bulbHeight") ||
        visibleParams.has("bulbWidth") ||
        visibleParams.has("bulbAsymmetry") ||
        visibleParams.has("bulbFillet")
      );
    default:
      return false;
  }
}
