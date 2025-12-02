/**
 * Geometry Validation Utilities
 *
 * Comprehensive validation for hull geometry data to ensure visualization components
 * receive clean, valid data and handle edge cases gracefully.
 */

import type { OffsetsGrid } from "./geometryFormatConverter";

export interface GeometryValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
  sanitized?: OffsetsGrid;
}

/**
 * Validates and sanitizes an OffsetsGrid
 * Returns validation result with sanitized data if valid
 */
export function validateOffsetsGrid(
  grid: OffsetsGrid | null | undefined
): GeometryValidationResult {
  const errors: string[] = [];
  const warnings: string[] = [];

  if (!grid) {
    return {
      isValid: false,
      errors: ["OffsetsGrid is null or undefined"],
      warnings: [],
    };
  }

  // Validate stations array
  if (!Array.isArray(grid.stations)) {
    errors.push("Stations must be an array");
    return { isValid: false, errors, warnings };
  }

  if (grid.stations.length === 0) {
    errors.push("At least one station is required");
    return { isValid: false, errors, warnings };
  }

  if (grid.stations.length < 3) {
    warnings.push(`Only ${grid.stations.length} stations found (minimum 3 recommended for accurate visualization)`);
  }

  // Validate waterlines array
  if (!Array.isArray(grid.waterlines)) {
    errors.push("Waterlines must be an array");
    return { isValid: false, errors, warnings };
  }

  if (grid.waterlines.length === 0) {
    errors.push("At least one waterline is required");
    return { isValid: false, errors, warnings };
  }

  if (grid.waterlines.length < 3) {
    warnings.push(`Only ${grid.waterlines.length} waterlines found (minimum 3 recommended)`);
  }

  // Validate offsets array
  if (!Array.isArray(grid.offsets)) {
    errors.push("Offsets must be a 2D array");
    return { isValid: false, errors, warnings };
  }

  if (grid.offsets.length !== grid.stations.length) {
    errors.push(
      `Offsets array length (${grid.offsets.length}) does not match stations length (${grid.stations.length})`
    );
    return { isValid: false, errors, warnings };
  }

  // Check for invalid values in stations
  const invalidStations: number[] = [];
  grid.stations.forEach((station, idx) => {
    if (!Number.isFinite(station)) {
      invalidStations.push(idx);
      errors.push(`Station ${idx} has invalid value: ${station}`);
    }
    if (station < 0) {
      warnings.push(`Station ${idx} has negative position: ${station}m`);
    }
  });

  // Check for invalid values in waterlines
  const invalidWaterlines: number[] = [];
  grid.waterlines.forEach((waterline, idx) => {
    if (!Number.isFinite(waterline)) {
      invalidWaterlines.push(idx);
      errors.push(`Waterline ${idx} has invalid value: ${waterline}`);
    }
    if (waterline < 0) {
      warnings.push(`Waterline ${idx} has negative height: ${waterline}m`);
    }
  });

  // Check if waterlines are sorted (not strictly required but recommended)
  const isWaterlinesSorted = grid.waterlines.every((wl, idx) =>
    idx === 0 || wl >= grid.waterlines[idx - 1]
  );
  if (!isWaterlinesSorted) {
    warnings.push("Waterlines are not sorted in ascending order (may affect visualization quality)");
  }

  // Check if stations are sorted
  const isStationsSorted = grid.stations.every((st, idx) =>
    idx === 0 || st >= grid.stations[idx - 1]
  );
  if (!isStationsSorted) {
    warnings.push("Stations are not sorted in ascending order (may cause rendering issues)");
  }

  // Validate offsets grid structure
  let invalidOffsetCount = 0;
  let negativeOffsetCount = 0;
  let extremeOffsetCount = 0;

  grid.offsets.forEach((stationOffsets, stIdx) => {
    if (!Array.isArray(stationOffsets)) {
      errors.push(`Station ${stIdx} offsets is not an array`);
      return;
    }

    if (stationOffsets.length !== grid.waterlines.length) {
      errors.push(
        `Station ${stIdx} offsets length (${stationOffsets.length}) does not match waterlines length (${grid.waterlines.length})`
      );
      return;
    }

    stationOffsets.forEach((offset, wlIdx) => {
      if (!Number.isFinite(offset)) {
        invalidOffsetCount++;
        if (invalidOffsetCount <= 5) {
          // Limit error messages
          errors.push(`Station ${stIdx}, Waterline ${wlIdx} has invalid offset: ${offset}`);
        }
      } else {
        if (offset < 0) {
          negativeOffsetCount++;
          if (negativeOffsetCount <= 5) {
            warnings.push(`Station ${stIdx}, Waterline ${wlIdx} has negative half-breadth: ${offset}m`);
          }
        }

        // Check for extreme values (likely data errors)
        const maxBeam = Math.max(...grid.stations) * 0.6; // Rough estimate
        if (Math.abs(offset) > maxBeam) {
          extremeOffsetCount++;
          if (extremeOffsetCount <= 5) {
            warnings.push(
              `Station ${stIdx}, Waterline ${wlIdx} has extreme half-breadth: ${offset}m (expected < ${maxBeam.toFixed(1)}m)`
            );
          }
        }
      }
    });
  });

  if (invalidOffsetCount > 5) {
    errors.push(`... and ${invalidOffsetCount - 5} more invalid offsets`);
  }

  if (negativeOffsetCount > 5) {
    warnings.push(`... and ${negativeOffsetCount - 5} more negative offsets`);
  }

  if (extremeOffsetCount > 5) {
    warnings.push(`... and ${extremeOffsetCount - 5} more extreme offsets`);
  }

  // If there are critical errors, return early
  if (errors.length > 0) {
    return { isValid: false, errors, warnings };
  }

  // Sanitize the grid (replace invalid values with 0)
  const sanitized: OffsetsGrid = {
    stations: grid.stations.map((s) => (Number.isFinite(s) ? Math.max(0, s) : 0)),
    waterlines: grid.waterlines.map((w) => (Number.isFinite(w) ? Math.max(0, w) : 0)),
    offsets: grid.offsets.map((stationOffsets) =>
      stationOffsets.map((offset) => {
        if (!Number.isFinite(offset)) return 0;
        if (offset < 0) {
          warnings.push("Negative offset sanitized to 0");
          return 0;
        }
        return offset;
      })
    ),
  };

  // Sort stations and waterlines if needed
  if (!isStationsSorted || !isWaterlinesSorted) {
    // Create index maps for reordering
    const stationOrder = grid.stations
      .map((val, idx) => ({ val, idx }))
      .sort((a, b) => a.val - b.val)
      .map((item) => item.idx);

    const waterlineOrder = grid.waterlines
      .map((val, idx) => ({ val, idx }))
      .sort((a, b) => a.val - b.val)
      .map((item) => item.idx);

    // Reorder stations and offsets
    sanitized.stations = stationOrder.map((idx) => sanitized.stations[idx]);
    sanitized.waterlines = waterlineOrder.map((idx) => sanitized.waterlines[idx]);

    // Reorder offsets grid
    const reorderedOffsets: number[][] = [];
    stationOrder.forEach((stIdx) => {
      const stationOffsets: number[] = [];
      waterlineOrder.forEach((wlIdx) => {
        stationOffsets.push(sanitized.offsets[stIdx][wlIdx]);
      });
      reorderedOffsets.push(stationOffsets);
    });
    sanitized.offsets = reorderedOffsets;

    warnings.push("Stations and/or waterlines were reordered to ensure proper sorting");
  }

  // Check for empty geometry (all offsets are zero)
  const allZero = sanitized.offsets.every((stationOffsets) =>
    stationOffsets.every((offset) => offset === 0)
  );
  if (allZero) {
    errors.push("All offsets are zero - geometry appears empty");
    return { isValid: false, errors, warnings };
  }

  // Check for reasonable aspect ratios
  const maxStation = Math.max(...sanitized.stations);
  const minStation = Math.min(...sanitized.stations);
  const stationRange = maxStation - minStation;

  const maxWaterline = Math.max(...sanitized.waterlines);
  const minWaterline = Math.min(...sanitized.waterlines);
  const waterlineRange = maxWaterline - minWaterline;

  if (stationRange === 0) {
    errors.push("All stations are at the same position");
    return { isValid: false, errors, warnings };
  }

  if (waterlineRange === 0) {
    errors.push("All waterlines are at the same height");
    return { isValid: false, errors, warnings };
  }

  const maxHalfBreadth = Math.max(
    ...sanitized.offsets.flatMap((stationOffsets) => stationOffsets)
  );

  if (maxHalfBreadth === 0) {
    errors.push("Maximum half-breadth is zero");
    return { isValid: false, errors, warnings };
  }

  // Warn about extreme aspect ratios
  const aspectRatio = stationRange / (2 * maxHalfBreadth);
  if (aspectRatio > 20) {
    warnings.push(`Extreme length-to-beam ratio: ${aspectRatio.toFixed(1)}:1 (typical range: 4-8:1)`);
  }
  if (aspectRatio < 2) {
    warnings.push(`Very low length-to-beam ratio: ${aspectRatio.toFixed(1)}:1 (typical range: 4-8:1)`);
  }

  const draftRatio = waterlineRange / (2 * maxHalfBreadth);
  if (draftRatio > 1.5) {
    warnings.push(`High draft-to-beam ratio: ${draftRatio.toFixed(2)}:1 (typical range: 0.3-0.8:1)`);
  }

  return {
    isValid: true,
    errors: [],
    warnings,
    sanitized,
  };
}

/**
 * Quick validation check - returns boolean only
 */
export function isValidOffsetsGrid(grid: OffsetsGrid | null | undefined): boolean {
  const result = validateOffsetsGrid(grid);
  return result.isValid;
}

