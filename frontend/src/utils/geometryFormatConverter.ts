/**
 * Geometry Format Converter
 *
 * Centralized utility for converting between different geometry formats
 * and normalizing geometry data for consistent use across components.
 *
 * Formats:
 * - OffsetsGrid: { stations: number[], waterlines: number[], offsets: number[][] }
 * - ShipD: { stations: Array<{ position, offsets: Record<number, number> }> }
 */

export interface OffsetsGrid {
  stations: number[];
  waterlines: number[];
  offsets: number[][]; // [stationIndex][waterlineIndex]
}

export interface ShipDGeometry {
  stations: Array<{
    position: number;
    offsets: Record<number, number>; // height -> halfBreadth
    hasBulb?: boolean;
    bulbOffsets?: Record<number, number>;
  }>;
  stationPositions?: number[];
}

/**
 * Detects the geometry format from a JSON string
 */
export function detectGeometryFormat(geometryJson: string): "shipd" | "offsetsgrid" | "unknown" {
  try {
    const parsed = JSON.parse(geometryJson);

    // Check for OffsetsGrid format
    // OffsetsGrid has: stations (array of numbers), waterlines (array of numbers), offsets (2D array)
    // Handle both camelCase (offsets, stations, waterlines) and PascalCase (Offsets, Stations, Waterlines)
    const stations = parsed.stations ?? parsed.Stations;
    const waterlines = parsed.waterlines ?? parsed.Waterlines;
    const offsets = parsed.offsets ?? parsed.Offsets;

    if (
      Array.isArray(stations) &&
      Array.isArray(waterlines) &&
      Array.isArray(offsets) &&
      stations.length > 0 &&
      waterlines.length > 0 &&
      offsets.length > 0 &&
      typeof stations[0] === "number" && // Stations are numbers (positions in meters)
      typeof waterlines[0] === "number" && // Waterlines are numbers (heights in meters)
      Array.isArray(offsets[0]) // Offsets is 2D array
    ) {
      return "offsetsgrid";
    }

    // Check for ShipD format
    // ShipD has: stations (array of objects with position and offsets)
    if (
      Array.isArray(parsed.stations) &&
      parsed.stations.length > 0 &&
      typeof parsed.stations[0] === "object" &&
      parsed.stations[0] !== null &&
      "position" in parsed.stations[0] &&
      "offsets" in parsed.stations[0]
    ) {
      return "shipd";
    }

    return "unknown";
  } catch {
    return "unknown";
  }
}

/**
 * Converts ShipD geometry format to OffsetsGrid format
 */
export function convertShipDToOffsetsGrid(shipdData: ShipDGeometry): OffsetsGrid {
  if (!shipdData.stations || shipdData.stations.length === 0) {
    throw new Error("ShipD geometry must have at least one station");
  }

  // Extract unique waterline heights from all stations
  const waterlineSet = new Set<number>();
  shipdData.stations.forEach((station) => {
    Object.keys(station.offsets).forEach((heightStr) => {
      waterlineSet.add(parseFloat(heightStr));
    });
  });

  // Sort waterlines from keel (lowest) to deck (highest)
  const waterlines = Array.from(waterlineSet).sort((a, b) => a - b);

  // Extract station positions
  const stations: number[] = [];
  if (shipdData.stationPositions && shipdData.stationPositions.length > 0) {
    stations.push(...shipdData.stationPositions);
  } else {
    // Use positions from stations array
    shipdData.stations.forEach((station) => {
      stations.push(station.position);
    });
  }

  // Build offsets grid [stationIndex][waterlineIndex]
  const offsets: number[][] = [];
  for (let stIdx = 0; stIdx < stations.length; stIdx++) {
    const station = shipdData.stations[stIdx];
    const stationOffsets: number[] = [];

    for (let wlIdx = 0; wlIdx < waterlines.length; wlIdx++) {
      const waterlineHeight = waterlines[wlIdx];
      // Get offset at this waterline (interpolate if needed)
      const offset = getOffsetAtHeight(station, waterlineHeight);
      stationOffsets.push(offset);
    }

    offsets.push(stationOffsets);
  }

  return {
    stations,
    waterlines,
    offsets,
  };
}

/**
 * Gets offset at a specific height, with interpolation if needed
 */
function getOffsetAtHeight(station: ShipDGeometry["stations"][0], targetHeight: number): number {
  const heights = Object.keys(station.offsets)
    .map((h) => parseFloat(h))
    .sort((a, b) => a - b);

  // Exact match
  if (station.offsets[targetHeight] !== undefined) {
    return station.offsets[targetHeight];
  }

  // Find surrounding heights for interpolation
  let lowerIdx = -1;
  let upperIdx = -1;

  for (let i = 0; i < heights.length; i++) {
    if (heights[i] <= targetHeight) {
      lowerIdx = i;
    }
    if (heights[i] >= targetHeight && upperIdx === -1) {
      upperIdx = i;
      break;
    }
  }

  // Extrapolate below
  if (lowerIdx === -1 && upperIdx >= 0) {
    return station.offsets[heights[upperIdx]];
  }

  // Extrapolate above
  if (upperIdx === -1 && lowerIdx >= 0) {
    return station.offsets[heights[lowerIdx]];
  }

  // Interpolate
  if (lowerIdx >= 0 && upperIdx >= 0 && lowerIdx !== upperIdx) {
    const lowerHeight = heights[lowerIdx];
    const upperHeight = heights[upperIdx];
    const lowerOffset = station.offsets[lowerHeight];
    const upperOffset = station.offsets[upperHeight];

    const t = (targetHeight - lowerHeight) / (upperHeight - lowerHeight);
    return lowerOffset + t * (upperOffset - lowerOffset);
  }

  // Fallback to 0 if no data
  return 0;
}

/**
 * Converts OffsetsGrid format to ShipD format
 *
 * IMPORTANT: Normalizes station positions from meters (0 to Lpp) to normalized (0 to 1)
 * where 0 = aft (stern) and 1 = forward (bow), as expected by the 3D generator.
 */
export function convertOffsetsGridToShipD(offsetsGrid: OffsetsGrid): ShipDGeometry {
  const stations: ShipDGeometry["stations"] = [];

  // Find min and max station positions to normalize (should be 0 to Lpp)
  const minStation = Math.min(...offsetsGrid.stations);
  const maxStation = Math.max(...offsetsGrid.stations);
  const stationRange = maxStation - minStation;

  // Avoid division by zero - if all stations are at the same position, use 0
  const normalizePosition =
    stationRange > 0 ? (stationX: number) => (stationX - minStation) / stationRange : () => 0;

  // CRITICAL: Create array of station indices with their positions, then sort by position
  // This ensures stations are processed in correct order (aft to forward: 0 to Lpp)
  // Without sorting, stations might be in wrong order, causing helical twist in 3D view
  const stationData = offsetsGrid.stations.map((stationX, idx) => ({
    index: idx,
    position: stationX,
  }));

  // Sort by position (ascending: aft to forward)
  stationData.sort((a, b) => a.position - b.position);

  // Process stations in sorted order
  for (const { index: stIdx, position: stationX } of stationData) {
    const offsets: Record<number, number> = {};

    // Convert waterline-based offsets to height-based offsets
    for (let wlIdx = 0; wlIdx < offsetsGrid.waterlines.length; wlIdx++) {
      const waterlineZ = offsetsGrid.waterlines[wlIdx];
      const halfBreadth = offsetsGrid.offsets[stIdx]?.[wlIdx] ?? 0;
      offsets[waterlineZ] = halfBreadth;
    }

    stations.push({
      position: normalizePosition(stationX), // Normalize: 0 = aft, 1 = forward
      offsets,
    });
  }

  // Also normalize stationPositions array (in sorted order)
  const normalizedStationPositions = stationData.map(({ position }) => normalizePosition(position));

  return {
    stations,
    stationPositions: normalizedStationPositions,
  };
}

/**
 * Sanitizes OffsetsGrid data by replacing NaN, null, undefined with 0
 */
function sanitizeOffsetsGrid(offsetsGrid: OffsetsGrid): OffsetsGrid {
  return {
    stations: offsetsGrid.stations.map((s) => (Number.isFinite(s) ? s : 0)),
    waterlines: offsetsGrid.waterlines.map((w) => (Number.isFinite(w) ? w : 0)),
    offsets: offsetsGrid.offsets.map((stationOffsets) =>
      stationOffsets.map((offset) => (Number.isFinite(offset) ? offset : 0))
    ),
  };
}

/**
 * Normalizes geometry JSON to OffsetsGrid format
 * Handles both ShipD and OffsetsGrid formats
 * Sanitizes NaN, null, undefined values to 0
 */
export function normalizeGeometry(geometryJson: string): OffsetsGrid | null {
  if (!geometryJson) {
    console.debug("[geometryFormatConverter] normalizeGeometry: geometryJson is empty/null");
    return null;
  }

  try {
    const format = detectGeometryFormat(geometryJson);
    const parsed = JSON.parse(geometryJson);

    // Check for both camelCase and PascalCase
    const stations = parsed.stations ?? parsed.Stations;
    const waterlines = parsed.waterlines ?? parsed.Waterlines;
    const offsets = parsed.offsets ?? parsed.Offsets;

    console.debug("[geometryFormatConverter] normalizeGeometry: format detected:", format, {
      hasStations: Array.isArray(stations),
      hasWaterlines: Array.isArray(waterlines),
      hasOffsets: Array.isArray(offsets),
      stationsType: stations?.[0]?.constructor?.name,
      stationsLength: stations?.length,
      hasPascalCase: !!(parsed.Stations || parsed.Waterlines || parsed.Offsets),
      hasCamelCase: !!(parsed.stations || parsed.waterlines || parsed.offsets),
    });

    if (format === "offsetsgrid") {
      // Already in OffsetsGrid format, validate and return
      // Handle both camelCase and PascalCase property names
      const stations = parsed.stations ?? parsed.Stations;
      const waterlines = parsed.waterlines ?? parsed.Waterlines;
      const offsets = parsed.offsets ?? parsed.Offsets;

      if (Array.isArray(stations) && Array.isArray(waterlines) && Array.isArray(offsets)) {
        const normalized = {
          stations,
          waterlines,
          offsets,
        };
        // Sanitize to handle NaN, null, undefined values
        return sanitizeOffsetsGrid(normalized);
      }
    } else if (format === "shipd") {
      // Convert from ShipD to OffsetsGrid
      const converted = convertShipDToOffsetsGrid(parsed as ShipDGeometry);
      // Sanitize to handle NaN, null, undefined values
      return sanitizeOffsetsGrid(converted);
    }

    console.debug("[geometryFormatConverter] normalizeGeometry: format is unknown, returning null");
    return null;
  } catch (error) {
    console.error("[geometryFormatConverter] Failed to normalize geometry:", error);
    return null;
  }
}
