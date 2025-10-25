/**
 * Unit conversion utilities for SI and Imperial systems
 */

export type UnitSystem = "SI" | "Imperial";

// Conversion factors (from SI base units)
const METERS_TO_FEET = 3.28084;
const SQUARE_METERS_TO_SQUARE_FEET = 10.7639;
const CUBIC_METERS_TO_CUBIC_FEET = 35.3147;
const KG_TO_LB = 2.20462;
const KG_PER_CUBIC_METER_TO_LB_PER_CUBIC_FOOT = 0.062428;
const M4_TO_FT4 = 115.862;

/**
 * Convert length between unit systems
 */
export function convertLength(value: number, fromUnit: UnitSystem, toUnit: UnitSystem): number {
  if (fromUnit === toUnit) return value;

  if (fromUnit === "SI" && toUnit === "Imperial") {
    return value * METERS_TO_FEET;
  }

  if (fromUnit === "Imperial" && toUnit === "SI") {
    return value / METERS_TO_FEET;
  }

  return value;
}

/**
 * Convert area between unit systems
 */
export function convertArea(value: number, fromUnit: UnitSystem, toUnit: UnitSystem): number {
  if (fromUnit === toUnit) return value;

  if (fromUnit === "SI" && toUnit === "Imperial") {
    return value * SQUARE_METERS_TO_SQUARE_FEET;
  }

  if (fromUnit === "Imperial" && toUnit === "SI") {
    return value / SQUARE_METERS_TO_SQUARE_FEET;
  }

  return value;
}

/**
 * Convert volume between unit systems
 */
export function convertVolume(value: number, fromUnit: UnitSystem, toUnit: UnitSystem): number {
  if (fromUnit === toUnit) return value;

  if (fromUnit === "SI" && toUnit === "Imperial") {
    return value * CUBIC_METERS_TO_CUBIC_FEET;
  }

  if (fromUnit === "Imperial" && toUnit === "SI") {
    return value / CUBIC_METERS_TO_CUBIC_FEET;
  }

  return value;
}

/**
 * Convert mass between unit systems
 */
export function convertMass(value: number, fromUnit: UnitSystem, toUnit: UnitSystem): number {
  if (fromUnit === toUnit) return value;

  if (fromUnit === "SI" && toUnit === "Imperial") {
    return value * KG_TO_LB;
  }

  if (fromUnit === "Imperial" && toUnit === "SI") {
    return value / KG_TO_LB;
  }

  return value;
}

/**
 * Convert density between unit systems
 */
export function convertDensity(value: number, fromUnit: UnitSystem, toUnit: UnitSystem): number {
  if (fromUnit === toUnit) return value;

  if (fromUnit === "SI" && toUnit === "Imperial") {
    return value * KG_PER_CUBIC_METER_TO_LB_PER_CUBIC_FOOT;
  }

  if (fromUnit === "Imperial" && toUnit === "SI") {
    return value / KG_PER_CUBIC_METER_TO_LB_PER_CUBIC_FOOT;
  }

  return value;
}

/**
 * Convert moment of inertia between unit systems
 */
export function convertMomentOfInertia(
  value: number,
  fromUnit: UnitSystem,
  toUnit: UnitSystem
): number {
  if (fromUnit === toUnit) return value;

  if (fromUnit === "SI" && toUnit === "Imperial") {
    return value * M4_TO_FT4;
  }

  if (fromUnit === "Imperial" && toUnit === "SI") {
    return value / M4_TO_FT4;
  }

  return value;
}

/**
 * Get unit label for length
 */
export function getLengthUnit(unitSystem: UnitSystem): string {
  return unitSystem === "Imperial" ? "ft" : "m";
}

/**
 * Get unit label for area
 */
export function getAreaUnit(unitSystem: UnitSystem): string {
  return unitSystem === "Imperial" ? "ft²" : "m²";
}

/**
 * Get unit label for volume
 */
export function getVolumeUnit(unitSystem: UnitSystem): string {
  return unitSystem === "Imperial" ? "ft³" : "m³";
}

/**
 * Get unit label for mass
 */
export function getMassUnit(unitSystem: UnitSystem): string {
  return unitSystem === "Imperial" ? "lb" : "kg";
}

/**
 * Get unit label for density
 */
export function getDensityUnit(unitSystem: UnitSystem): string {
  return unitSystem === "Imperial" ? "lb/ft³" : "kg/m³";
}

/**
 * Get unit label for moment of inertia
 */
export function getMomentOfInertiaUnit(unitSystem: UnitSystem): string {
  return unitSystem === "Imperial" ? "ft⁴" : "m⁴";
}

/**
 * Format a number with unit label
 */
export function formatWithUnit(
  value: number | null | undefined,
  unitSystem: UnitSystem,
  unitType: "length" | "area" | "volume" | "mass" | "density" | "momentOfInertia",
  decimals: number = 2
): string {
  if (value === null || value === undefined) return "—";

  const formatted = value.toFixed(decimals);

  const unitGetters = {
    length: getLengthUnit,
    area: getAreaUnit,
    volume: getVolumeUnit,
    mass: getMassUnit,
    density: getDensityUnit,
    momentOfInertia: getMomentOfInertiaUnit,
  };

  const unit = unitGetters[unitType](unitSystem);
  return `${formatted} ${unit}`;
}

