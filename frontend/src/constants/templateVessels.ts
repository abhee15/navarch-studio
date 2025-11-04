/**
 * Constants for template vessels
 * These IDs match the backend TemplateVessels constants
 */

/**
 * Fixed GUID for the Hydrostatics template vessel
 * This vessel demonstrates complete hydrostatic analysis capabilities
 */
export const HYDROSTATICS_VESSEL_ID = "00000000-0000-0000-0000-000000000001";

/**
 * Checks if a vessel ID is the hydrostatics template vessel
 */
export function isHydrostaticsVessel(vesselId: string): boolean {
  return vesselId === HYDROSTATICS_VESSEL_ID;
}
