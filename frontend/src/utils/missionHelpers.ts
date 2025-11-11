import type { MissionCase, CreateMissionCaseDto } from "../types/sizing";

/**
 * Converts a MissionCase object to CreateMissionCaseDto format for editing in the wizard.
 * This strips out the read-only fields (id, userId, tenantId, timestamps) and keeps
 * only the editable fields.
 *
 * @param missionCase - The mission case to convert
 * @returns A DTO suitable for form initialization
 */
export function missionCaseToDto(missionCase: MissionCase): CreateMissionCaseDto {
  return {
    name: missionCase.name,
    missionType: missionCase.missionType,
    cargoBasis: missionCase.cargoBasis,
    cargoValue: missionCase.cargoValue,
    teuCount: missionCase.teuCount,
    cargoVolumeM3: missionCase.cargoVolumeM3,
    cargoDensityTPerM3: missionCase.cargoDensityTPerM3,
    serviceSpeedKn: missionCase.serviceSpeedKn,
    seaMarginPct: missionCase.seaMarginPct,
    envHsM: missionCase.envHsM,
    envTzS: missionCase.envTzS,
    capLoaM: missionCase.capLoaM,
    capBeamM: missionCase.capBeamM,
    capDraftM: missionCase.capDraftM,
    capAirdraftM: missionCase.capAirdraftM,
    enduranceNm: missionCase.enduranceNm,
    notes: missionCase.notes,
  };
}
