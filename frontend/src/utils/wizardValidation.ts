/**
 * Validation utilities for mission wizard
 *
 * Provides validation functions and error messages for all wizard steps
 */

import type { CreateMissionCaseDto } from "../types/sizing";

export type ValidationErrors = Record<string, string | null>;
export type TouchedFields = Record<string, boolean>;

/**
 * Validates a single field and returns error message if invalid
 */
export function validateField(
  fieldName: string,
  value: any,
  formData: Partial<CreateMissionCaseDto>
): string | null {
  switch (fieldName) {
    // Step 1: Mission & Cargo
    case "name":
      if (!value || value.trim().length === 0) {
        return "Vessel name is required";
      }
      if (value.trim().length < 3) {
        return "Name must be at least 3 characters";
      }
      if (value.length > 100) {
        return "Name is too long (maximum 100 characters)";
      }
      return null;

    case "missionCategory":
      if (!value) {
        return "Please select a mission category";
      }
      return null;

    case "missionType":
      if (!value) {
        return "Please select a vessel type";
      }
      return null;

    case "teuCount":
    case "cargoValue":
      if (value === undefined || value === null || value === "") {
        if (formData.cargoBasis === "teu") {
          return "TEU count is required";
        } else if (formData.cargoBasis === "weight") {
          return "Cargo weight is required";
        } else if (formData.cargoBasis === "volume") {
          return "Cargo volume is required";
        }
        return "Cargo value is required";
      }
      const numValue = parseFloat(value);
      if (isNaN(numValue) || numValue <= 0) {
        if (formData.cargoBasis === "teu") {
          return "TEU count must be greater than 0";
        } else if (formData.cargoBasis === "weight") {
          return "Cargo weight must be greater than 0";
        } else if (formData.cargoBasis === "volume") {
          return "Cargo volume must be greater than 0";
        }
        return "Value must be greater than 0";
      }
      // Sanity checks for unrealistic values
      if (formData.cargoBasis === "teu" && numValue > 25000) {
        return "TEU count seems unusually high (maximum 25,000)";
      }
      if (formData.cargoBasis === "weight" && numValue > 500000) {
        return "Cargo weight seems unusually high (maximum 500,000 tonnes)";
      }
      if (formData.cargoBasis === "volume" && numValue > 500000) {
        return "Cargo volume seems unusually high (maximum 500,000 m³)";
      }
      return null;

    case "cargoVolumeM3":
      if (formData.cargoBasis === "volume") {
        if (value === undefined || value === null || value === "") {
          return "Cargo volume is required";
        }
        const vol = parseFloat(value);
        if (isNaN(vol) || vol <= 0) {
          return "Cargo volume must be greater than 0";
        }
        if (vol > 500000) {
          return "Cargo volume seems unusually high (maximum 500,000 m³)";
        }
      }
      return null;

    case "cargoDensityTPerM3":
      if (formData.cargoBasis === "volume") {
        if (value === undefined || value === null || value === "") {
          return "Cargo density is required for volume-based cargo";
        }
        const density = parseFloat(value);
        if (isNaN(density) || density <= 0) {
          return "Cargo density must be greater than 0";
        }
        if (density < 0.1) {
          return "Cargo density seems too low (minimum 0.1 t/m³)";
        }
        if (density > 10) {
          return "Cargo density seems too high (maximum 10 t/m³)";
        }
      }
      return null;

    // Step 2: Speed & Environment
    case "serviceSpeedKn":
      if (value === undefined || value === null || value === "") {
        return "Service speed is required";
      }
      const speed = parseFloat(value);
      if (isNaN(speed)) {
        return "Please enter a valid number";
      }
      if (speed < 5) {
        return "Speed seems too low (minimum 5 knots)";
      }
      if (speed > 50) {
        return "Speed seems unusually high (maximum 50 knots)";
      }
      return null;

    case "seaMarginPct":
      if (value === undefined || value === null || value === "") {
        return "Sea margin is required";
      }
      const margin = parseFloat(value);
      if (isNaN(margin)) {
        return "Please enter a valid number";
      }
      if (margin < 0) {
        return "Sea margin cannot be negative";
      }
      if (margin > 50) {
        return "Sea margin seems too high (maximum 50%)";
      }
      return null;

    // Step 3: Constraints
    case "capBeamM":
      if (value !== undefined && value !== null && value !== "") {
        const beam = parseFloat(value);
        if (isNaN(beam)) {
          return "Please enter a valid number";
        }
        if (beam < 5) {
          return "Beam constraint seems too small (minimum 5m)";
        }
        if (beam > 100) {
          return "Beam constraint seems too large (maximum 100m)";
        }
      }
      return null;

    case "capDraftM":
      if (value !== undefined && value !== null && value !== "") {
        const draft = parseFloat(value);
        if (isNaN(draft)) {
          return "Please enter a valid number";
        }
        if (draft < 2) {
          return "Draft constraint seems too small (minimum 2m)";
        }
        if (draft > 30) {
          return "Draft constraint seems too large (maximum 30m)";
        }
      }
      return null;

    default:
      return null;
  }
}

/**
 * Validates all fields in Step 1 and returns errors object
 */
export function validateStep1(
  formData: Partial<CreateMissionCaseDto>,
  touched: TouchedFields
): ValidationErrors {
  const errors: ValidationErrors = {};

  const fieldsToValidate = [
    "name",
    "missionCategory",
    "missionType",
    "cargoValue",
    "cargoDensityTPerM3",
  ];

  fieldsToValidate.forEach((field) => {
    if (touched[field]) {
      const error = validateField(field, (formData as any)[field], formData);
      if (error) {
        errors[field] = error;
      }
    }
  });

  return errors;
}

/**
 * Validates all fields in Step 2 and returns errors object
 */
export function validateStep2(
  formData: Partial<CreateMissionCaseDto>,
  touched: TouchedFields
): ValidationErrors {
  const errors: ValidationErrors = {};

  const fieldsToValidate = ["serviceSpeedKn", "seaMarginPct"];

  fieldsToValidate.forEach((field) => {
    if (touched[field]) {
      const error = validateField(field, (formData as any)[field], formData);
      if (error) {
        errors[field] = error;
      }
    }
  });

  return errors;
}

/**
 * Validates all fields in Step 3 and returns errors object
 */
export function validateStep3(
  formData: Partial<CreateMissionCaseDto>,
  touched: TouchedFields
): ValidationErrors {
  const errors: ValidationErrors = {};

  const fieldsToValidate = ["capBeamM", "capDraftM"];

  fieldsToValidate.forEach((field) => {
    if (touched[field]) {
      const error = validateField(field, (formData as any)[field], formData);
      if (error) {
        errors[field] = error;
      }
    }
  });

  return errors;
}

/**
 * Checks if Step 1 is valid (for Next button)
 */
export function isStep1Valid(formData: Partial<CreateMissionCaseDto>): boolean {
  const trimmedName = formData.name?.trim() ?? "";
  const hasValidName = trimmedName.length >= 3 && trimmedName.length <= 100;
  const hasCategory = !!formData.missionCategory;
  const hasType = !!formData.missionType;
  const hasValidCargo =
    formData.cargoValue !== undefined &&
    formData.cargoValue !== null &&
    formData.cargoValue > 0 &&
    formData.cargoValue <= (formData.cargoBasis === "teu" ? 25000 : 500000);

  let hasValidDensity = true;
  if (formData.cargoBasis === "volume") {
    hasValidDensity =
      formData.cargoDensityTPerM3 !== undefined &&
      formData.cargoDensityTPerM3 !== null &&
      formData.cargoDensityTPerM3 > 0.1 &&
      formData.cargoDensityTPerM3 <= 10;
  }

  return hasValidName && hasCategory && hasType && hasValidCargo && hasValidDensity;
}

/**
 * Checks if Step 2 is valid (for Next button)
 */
export function isStep2Valid(formData: Partial<CreateMissionCaseDto>): boolean {
  const hasValidSpeed =
    formData.serviceSpeedKn !== undefined &&
    formData.serviceSpeedKn >= 5 &&
    formData.serviceSpeedKn <= 50;

  const hasValidMargin =
    formData.seaMarginPct !== undefined &&
    formData.seaMarginPct >= 0 &&
    formData.seaMarginPct <= 50;

  return hasValidSpeed && hasValidMargin;
}

/**
 * Checks if Step 3 is valid (for Next button)
 * Step 3 is always valid since all fields are optional
 */
export function isStep3Valid(formData: Partial<CreateMissionCaseDto>): boolean {
  // Check for any validation errors if fields are filled
  if (formData.capBeamM) {
    if (formData.capBeamM < 5 || formData.capBeamM > 100) return false;
  }
  if (formData.capDraftM) {
    if (formData.capDraftM < 2 || formData.capDraftM > 30) return false;
  }

  return true;
}
