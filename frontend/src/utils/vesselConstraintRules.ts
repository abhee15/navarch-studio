/**
 * Vessel-type-specific constraint applicability rules
 * Determines which canal/lock constraints are relevant for each vessel category/type
 */

export type VesselCategory = "commercial" | "government" | "recreational" | "research";

export interface ConstraintApplicability {
  applicable: boolean;
  reason?: string;
}

/**
 * Canal constraint presets and their applicability
 */
export const CANAL_CONSTRAINT_RULES: Record<
  string,
  {
    name: string;
    description: string;
    applicableCategories: VesselCategory[];
    applicableTypes?: string[]; // If undefined, applies to all types in category
    notApplicableReason?: string;
  }
> = {
  panamax: {
    name: "Panamax",
    description: "Original Panama Canal locks (1914-2016)",
    applicableCategories: ["commercial", "government"],
    applicableTypes: [
      "container",
      "bulk_carrier",
      "general_cargo",
      "tanker",
      "lng_carrier",
      "cruise_vessel",
      "naval_auxiliary",
    ],
    notApplicableReason:
      "Panamax constraints are for commercial/naval vessels transiting Panama Canal",
  },
  neopanamax: {
    name: "Neo-Panamax",
    description: "Expanded Panama Canal locks (2016+)",
    applicableCategories: ["commercial", "government"],
    applicableTypes: [
      "container",
      "bulk_carrier",
      "general_cargo",
      "tanker",
      "lng_carrier",
      "cruise_vessel",
      "naval_auxiliary",
    ],
    notApplicableReason: "Neo-Panamax constraints are for large commercial/naval vessels",
  },
  suezmax: {
    name: "Suezmax",
    description: "Suez Canal draft/beam limits",
    applicableCategories: ["commercial", "government"],
    applicableTypes: [
      "container",
      "bulk_carrier",
      "general_cargo",
      "tanker",
      "lng_carrier",
      "naval_auxiliary",
    ],
    notApplicableReason:
      "Suezmax constraints are for large commercial/naval vessels transiting Suez Canal",
  },
  malaccamax: {
    name: "Malaccamax",
    description: "Straits of Malacca depth limits",
    applicableCategories: ["commercial", "government"],
    applicableTypes: ["container", "bulk_carrier", "tanker", "lng_carrier", "naval_auxiliary"],
    notApplicableReason: "Malaccamax constraints are for large cargo/tanker vessels",
  },
};

/**
 * Check if a canal constraint preset is applicable to a vessel category/type
 */
export function isConstraintApplicable(
  presetKey: string,
  vesselCategory?: string,
  vesselType?: string
): ConstraintApplicability {
  // If no category specified, assume applicable (conservative)
  if (!vesselCategory) {
    return { applicable: true };
  }

  const rule = CANAL_CONSTRAINT_RULES[presetKey.toLowerCase()];

  // If no rule defined, assume applicable
  if (!rule) {
    return { applicable: true };
  }

  // Check category applicability
  const categoryLower = vesselCategory.toLowerCase() as VesselCategory;
  if (!rule.applicableCategories.includes(categoryLower)) {
    return {
      applicable: false,
      reason:
        rule.notApplicableReason ||
        `${rule.name} constraints not applicable to ${vesselCategory} vessels`,
    };
  }

  // If rule specifies applicable types, check vessel type
  if (rule.applicableTypes && vesselType) {
    const typeLower = vesselType
      .toLowerCase()
      .replace(/^(commercial|government|recreational|research)\s*[–-]\s*/, "")
      .replace(/\s+/g, "_");

    const isApplicable = rule.applicableTypes.some(
      (allowedType) =>
        typeLower.includes(allowedType.toLowerCase()) ||
        allowedType.toLowerCase().includes(typeLower)
    );

    if (!isApplicable) {
      return {
        applicable: false,
        reason:
          rule.notApplicableReason ||
          `${rule.name} constraints not typically used for ${vesselType}`,
      };
    }
  }

  return { applicable: true };
}

/**
 * Get tooltip text for a constraint preset button
 */
export function getConstraintTooltip(
  presetKey: string,
  isApplicable: boolean,
  reason?: string
): string {
  const rule = CANAL_CONSTRAINT_RULES[presetKey.toLowerCase()];

  if (!rule) return "";

  if (!isApplicable && reason) {
    return `${rule.description}\n\n⚠️ ${reason}`;
  }

  return rule.description;
}
