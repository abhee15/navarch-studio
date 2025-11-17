/**
 * ShipD 2D Geometry Extraction Utilities
 *
 * Extracts waterlines, buttocks, and sections from ShipD geometry data
 * for use in 2D plan, profile, and body plan views.
 */

import type { ShipDHullSections } from "./shipdGeometryGenerator";
import type { OffsetsGrid } from "./geometryFormatConverter";

/**
 * Extract waterlines from ShipD sections for Plan View
 */
export function extractWaterlinesFromShipD(
  sections: ShipDHullSections,
  lppM: number,
  waterlineHeights: number[] = [],
  designDraft?: number
): Array<{ depth: number; points: Array<[number, number]>; isDesignWaterline: boolean }> {
  // Validate inputs to prevent NaN
  if (!Number.isFinite(lppM) || lppM <= 0) {
    console.warn("[extractWaterlinesFromShipD] Invalid lppM:", lppM);
    return [];
  }

  const waterlines: Array<{
    depth: number;
    points: Array<[number, number]>;
    isDesignWaterline: boolean;
  }> = [];

  // If no heights specified, generate default waterlines
  if (waterlineHeights.length === 0) {
    const maxDraft = Math.max(
      ...sections.stations.flatMap((s) => Object.keys(s.offsets).map(Number))
    );
    waterlineHeights = Array.from({ length: 7 }, (_, i) => (i / 6) * maxDraft);
  }

  // Debug: Log station positions to verify we have bow, midship, and stern
  const stationPositions = sections.stations.map((s) => s.position).sort((a, b) => a - b);
  console.log("[extractWaterlinesFromShipD] Station positions:", {
    count: sections.stations.length,
    min: stationPositions[0],
    max: stationPositions[stationPositions.length - 1],
    hasStern: stationPositions[0] <= 0.01,
    hasBow: stationPositions[stationPositions.length - 1] >= 0.99,
    lppM,
  });

  for (const height of waterlineHeights) {
    const points: Array<[number, number]> = [];

    // Sort stations by position (aft to forward: 0 = stern/AP, 1 = bow/FP)
    const sortedStations = [...sections.stations].sort((a, b) => a.position - b.position);

    // Ensure we have points at extreme positions (stern and bow) for proper closure
    // Naval architecture standard: Plan View shows waterlines from AP (aft perpendicular) to FP (forward perpendicular)
    // Waterlines must be closed curves: stern centerline → starboard → bow centerline → port → stern centerline

    // First, collect all starboard side points (half-breadth > 0)
    for (const station of sortedStations) {
      // Find closest offset at this height (interpolate if needed)
      const offsets = station.offsets;
      const heights = Object.keys(offsets)
        .map(Number)
        .sort((a, b) => a - b);

      if (heights.length === 0) continue;

      // Find height range with proper interpolation
      let halfBreadth = 0;
      if (height <= heights[0]) {
        // Below lowest point - use keel value (should be 0 at keel)
        halfBreadth = height <= 0 ? 0 : offsets[heights[0]];
      } else if (height >= heights[heights.length - 1]) {
        // Above highest point - extrapolate or use maximum
        halfBreadth = offsets[heights[heights.length - 1]];
      } else {
        // Interpolate between adjacent heights
        for (let i = 0; i < heights.length - 1; i++) {
          if (height >= heights[i] && height <= heights[i + 1]) {
            const h1 = heights[i];
            const h2 = heights[i + 1];
            const y1 = offsets[h1];
            const y2 = offsets[h2];
            if (h2 - h1 > 0.001) {
              // Avoid division by zero
              const t = (height - h1) / (h2 - h1);
              halfBreadth = y1 + t * (y2 - y1);
            } else {
              halfBreadth = y1;
            }
            break;
          }
        }
      }

      // Convert station position (0-1) to longitudinal coordinate
      // Naval architecture convention:
      // - position 0.0 = AP (Aft Perpendicular) = -Lpp/2
      // - position 0.5 = midship = 0
      // - position 1.0 = FP (Forward Perpendicular) = +Lpp/2
      // This matches the 3D coordinate system where hull is centered at origin
      const x = (station.position - 0.5) * lppM;

      // Only add point if both coordinates are valid (finite numbers)
      if (Number.isFinite(x) && Number.isFinite(halfBreadth) && halfBreadth >= 0) {
        points.push([x, halfBreadth]);
      }
    }

    if (points.length > 0) {
      // Ensure waterline is properly closed and ordered from stern to bow
      // Naval architecture standard: waterlines should be closed curves
      // Points should be ordered from stern (AP) to bow (FP) on starboard side
      // We'll mirror to port side in the rendering component

      // Sort points by longitudinal position (x) to ensure proper ordering
      points.sort((a, b) => a[0] - b[0]);

      // Ensure we have centerline points at bow and stern for proper closure
      // Stern centerline: x = -lpp/2, y = 0
      // Bow centerline: x = +lpp/2, y = 0
      const sternX = -lppM / 2;
      const bowX = lppM / 2;

      // Add stern centerline point if not present (or if first point is not at stern)
      if (points.length === 0 || Math.abs(points[0][0] - sternX) > 0.01) {
        points.unshift([sternX, 0]);
      } else if (points[0][1] > 0.01) {
        // If first point is at stern but not on centerline, add centerline point
        points.unshift([sternX, 0]);
      }

      // Add bow centerline point if not present (or if last point is not at bow)
      if (points.length === 0 || Math.abs(points[points.length - 1][0] - bowX) > 0.01) {
        points.push([bowX, 0]);
      } else if (points[points.length - 1][1] > 0.01) {
        // If last point is at bow but not on centerline, add centerline point
        points.push([bowX, 0]);
      }

      // Ensure stern and bow points are exactly at centerline (y = 0)
      if (points.length > 0) {
        if (Math.abs(points[0][0] - sternX) < 0.01) {
          points[0] = [sternX, 0];
        }
        if (Math.abs(points[points.length - 1][0] - bowX) < 0.01) {
          points[points.length - 1] = [bowX, 0];
        }
      }

      // Design waterline is at the draft level (maximum height)
      // For plan view, we show depth below waterline, so depth = draft - height
      const depth = height; // Height from keel (0 = keel, draft = waterline)
      // Design waterline is at the draft level
      const isDesignWaterline =
        designDraft !== undefined
          ? Math.abs(height - designDraft) < 0.01
          : height >= (waterlineHeights[waterlineHeights.length - 1] || 0) * 0.95;

      // Debug: Log first and last points to verify bow/stern representation
      if (points.length > 0) {
        console.log(`[extractWaterlinesFromShipD] Waterline at height ${height.toFixed(3)}m:`, {
          pointCount: points.length,
          sternPoint: points[0], // Should be at x ≈ -lppM/2 (aft)
          bowPoint: points[points.length - 1], // Should be at x ≈ +lppM/2 (forward)
          maxHalfBreadth: Math.max(...points.map((p) => p[1])),
          isDesignWaterline,
        });
      }

      waterlines.push({
        depth: depth,
        points,
        isDesignWaterline,
      });
    }
  }

  return waterlines;
}

/**
 * Extract buttocks from ShipD sections for Profile View
 */
export function extractButtocksFromShipD(
  sections: ShipDHullSections,
  lppM: number,
  draftM: number,
  buttockOffsets: number[] = []
): Array<{ transverseOffset: number; points: Array<[number, number]>; isCenterline: boolean }> {
  // Validate inputs to prevent NaN
  if (!Number.isFinite(lppM) || lppM <= 0) {
    console.warn("[extractButtocksFromShipD] Invalid lppM:", lppM);
    return [];
  }
  if (!Number.isFinite(draftM) || draftM <= 0) {
    console.warn("[extractButtocksFromShipD] Invalid draftM:", draftM);
    return [];
  }

  const buttocks: Array<{
    transverseOffset: number;
    points: Array<[number, number]>;
    isCenterline: boolean;
  }> = [];

  // If no offsets specified, generate default buttocks
  if (buttockOffsets.length === 0) {
    const maxHalfBreadth = Math.max(...sections.stations.flatMap((s) => Object.values(s.offsets)));
    buttockOffsets = Array.from({ length: 5 }, (_, i) => (i / 4) * maxHalfBreadth);
  }

  for (const yOffset of buttockOffsets) {
    const points: Array<[number, number]> = [];

    // Sort stations by position (aft to forward)
    const sortedStations = [...sections.stations].sort((a, b) => a.position - b.position);

    for (const station of sortedStations) {
      // Find height at this transverse offset
      const offsets = station.offsets;
      const heights = Object.keys(offsets)
        .map(Number)
        .sort((a, b) => a - b);

      if (heights.length === 0) continue;

      // Find the height where half-breadth equals yOffset
      // Heights are positive: 0 = keel, draftM = waterline
      let z = 0; // Default to keel

      for (let i = 0; i < heights.length - 1; i++) {
        const h1 = heights[i];
        const h2 = heights[i + 1];
        const y1 = offsets[h1];
        const y2 = offsets[h2];

        if (yOffset >= Math.min(y1, y2) && yOffset <= Math.max(y1, y2)) {
          // Interpolate height
          const t = (yOffset - y1) / (y2 - y1);
          z = h1 + t * (h2 - h1);
          break;
        }
      }

      // If yOffset is beyond max half-breadth, use highest point
      const maxY = Math.max(...Object.values(offsets));
      if (yOffset > maxY && heights.length > 0) {
        z = heights[heights.length - 1];
      }

      // Convert station position (0-1) to longitudinal coordinate (-Lpp/2 to +Lpp/2)
      const x = (station.position - 0.5) * lppM;
      // Profile view expects: y = 0 at waterline, negative below (keel = -draft)
      // ShipD uses: height = 0 at keel, positive above (waterline = draft)
      // Convert: profileY = height - draft (so keel = -draft, waterline = 0)
      const profileY = z - draftM;

      // Only add point if both coordinates are valid (finite numbers)
      if (Number.isFinite(x) && Number.isFinite(profileY)) {
        points.push([x, profileY]);
      }
    }

    if (points.length > 0) {
      buttocks.push({
        transverseOffset: yOffset,
        points,
        isCenterline: Math.abs(yOffset) < 0.01,
      });
    }
  }

  return buttocks;
}

/**
 * Extract sections from ShipD geometry for Body Plan
 */
export function extractSectionsFromShipD(
  sections: ShipDHullSections,
  stationIndices: number[] = []
): Array<{ station: number; points: Array<[number, number]>; isAft: boolean; hasBulb: boolean }> {
  const result: Array<{
    station: number;
    points: Array<[number, number]>;
    isAft: boolean;
    hasBulb: boolean;
  }> = [];

  // If no indices specified, use all stations
  const stationsToUse =
    stationIndices.length > 0
      ? sections.stations.filter((_s, idx) => stationIndices.includes(idx))
      : sections.stations;

  for (const station of stationsToUse) {
    const points: Array<[number, number]> = [];

    // Sort heights (keel to deck)
    const heights = Object.keys(station.offsets)
      .map(Number)
      .sort((a, b) => a - b);

    for (const height of heights) {
      const halfBreadth = station.offsets[height];
      points.push([halfBreadth, height]);
    }

    // Add bulb points if present (merge with main offsets)
    if (station.hasBulb && station.bulbOffsets) {
      const bulbHeights = Object.keys(station.bulbOffsets)
        .map(Number)
        .sort((a, b) => a - b);

      for (const height of bulbHeights) {
        const halfBreadth = station.bulbOffsets[height];
        // Only add if not already present at this height
        if (!points.some((p) => Math.abs(p[1] - height) < 0.001)) {
          points.push([halfBreadth, height]);
        }
      }
    }

    if (points.length > 0) {
      // Re-sort points by height (Z coordinate) to ensure proper ordering
      // This is critical after adding bulb points
      points.sort((a, b) => a[1] - b[1]);

      // Remove duplicate or very close points that could cause sharp angles
      const cleanedPoints: Array<[number, number]> = [];
      const tolerance = 0.001; // 1mm tolerance for point deduplication

      for (let i = 0; i < points.length; i++) {
        const current = points[i];
        if (cleanedPoints.length === 0) {
          cleanedPoints.push(current);
          continue;
        }

        const last = cleanedPoints[cleanedPoints.length - 1];
        const distance = Math.sqrt(
          Math.pow(current[0] - last[0], 2) + Math.pow(current[1] - last[1], 2)
        );

        // Only add point if it's sufficiently different from the last point
        if (distance > tolerance) {
          cleanedPoints.push(current);
        }
      }

      // Determine if aft (position < 0.5) or forward (position >= 0.5)
      result.push({
        station: Math.round(station.position * 10), // Convert 0-1 to 0-10
        points: cleanedPoints,
        isAft: station.position < 0.5,
        hasBulb: station.hasBulb || false,
      });
    }
  }

  return result;
}

/**
 * Extract sheerline from ShipD sections
 */
export function extractSheerlineFromShipD(
  sections: ShipDHullSections,
  lppM: number,
  _depthM: number,
  draftM: number
): Array<[number, number]> {
  const points: Array<[number, number]> = [];

  // Sort stations by position (aft to forward)
  const sortedStations = [...sections.stations].sort((a, b) => a.position - b.position);

  for (const station of sortedStations) {
    // Find maximum height (deck level)
    const heights = Object.keys(station.offsets)
      .map(Number)
      .sort((a, b) => b - a); // Descending

    if (heights.length > 0) {
      const maxHeight = heights[0];
      const freeboard = maxHeight - draftM;
      const x = (station.position - 0.5) * lppM;
      points.push([x, freeboard]);
    }
  }

  return points;
}

/**
 * Extract buttocks from OffsetsGrid format for Profile View
 */
export function extractButtocksFromOffsetsGrid(
  offsetsGrid: OffsetsGrid,
  lppM: number,
  draftM: number,
  buttockOffsets: number[] = []
): Array<{ transverseOffset: number; points: Array<[number, number]>; isCenterline: boolean }> {
  // Validate inputs
  if (!Number.isFinite(lppM) || lppM <= 0) {
    console.warn("[extractButtocksFromOffsetsGrid] Invalid lppM:", lppM);
    return [];
  }
  if (!Number.isFinite(draftM) || draftM <= 0) {
    console.warn("[extractButtocksFromOffsetsGrid] Invalid draftM:", draftM);
    return [];
  }

  const { stations, waterlines, offsets } = offsetsGrid;

  if (!stations || !waterlines || !offsets || stations.length === 0 || waterlines.length === 0) {
    console.warn("[extractButtocksFromOffsetsGrid] Invalid OffsetsGrid data");
    return [];
  }

  const buttocks: Array<{
    transverseOffset: number;
    points: Array<[number, number]>;
    isCenterline: boolean;
  }> = [];

  // If no offsets specified, generate default buttocks
  if (buttockOffsets.length === 0) {
    // Find max half-breadth across all stations and waterlines
    const maxHalfBreadth = Math.max(
      ...offsets.flatMap((stationOffsets) => (stationOffsets ? stationOffsets : []))
    );
    buttockOffsets = Array.from({ length: 5 }, (_, i) => (i / 4) * maxHalfBreadth);
  }

  for (const yOffset of buttockOffsets) {
    const points: Array<[number, number]> = [];

    // For each station, find the height (waterline) where half-breadth equals yOffset
    for (let stIdx = 0; stIdx < stations.length; stIdx++) {
      const stationX = stations[stIdx];
      const stationOffsets = offsets[stIdx];

      if (!stationOffsets || stationOffsets.length === 0) continue;

      // Find the waterline where half-breadth equals yOffset
      let z = 0; // Default to keel (waterline 0)

      // Search through waterlines to find where half-breadth matches yOffset
      for (let wlIdx = 0; wlIdx < waterlines.length - 1; wlIdx++) {
        const wl1 = waterlines[wlIdx];
        const wl2 = waterlines[wlIdx + 1];
        const y1 = stationOffsets[wlIdx] ?? 0;
        const y2 = stationOffsets[wlIdx + 1] ?? 0;

        // Check if yOffset is between y1 and y2
        if (yOffset >= Math.min(y1, y2) && yOffset <= Math.max(y1, y2)) {
          // Interpolate height
          if (Math.abs(y2 - y1) > 0.001) {
            const t = (yOffset - y1) / (y2 - y1);
            z = wl1 + t * (wl2 - wl1);
          } else {
            z = wl1;
          }
          break;
        }
      }

      // If yOffset is beyond max half-breadth, use highest waterline
      const maxY = Math.max(...stationOffsets);
      if (yOffset > maxY && waterlines.length > 0) {
        z = waterlines[waterlines.length - 1];
      }

      // Convert station X position to longitudinal coordinate
      // OffsetsGrid stations are in meters from AP (0 = AP, Lpp = FP)
      // Profile view expects: x = 0 at midship, -Lpp/2 at AP, +Lpp/2 at FP
      // Convert: profileX = stationX - Lpp/2
      const x = stationX - lppM / 2;
      // Profile view expects: y = 0 at waterline, negative below (keel = -draft)
      // OffsetsGrid waterlines are in meters from keel (0 = keel, draft = waterline)
      // Convert: profileY = z - draft (so keel = -draft, waterline = 0)
      const profileY = z - draftM;

      if (Number.isFinite(x) && Number.isFinite(profileY)) {
        points.push([x, profileY]);
      }
    }

    if (points.length > 0) {
      buttocks.push({
        transverseOffset: yOffset,
        points,
        isCenterline: Math.abs(yOffset) < 0.01,
      });
    }
  }

  return buttocks;
}

/**
 * Extract sheerline from OffsetsGrid format
 */
export function extractSheerlineFromOffsetsGrid(
  offsetsGrid: OffsetsGrid,
  lppM: number,
  depthM: number,
  draftM: number
): Array<[number, number]> {
  const { stations, waterlines, offsets } = offsetsGrid;

  if (!stations || !waterlines || !offsets || stations.length === 0 || waterlines.length === 0) {
    console.warn("[extractSheerlineFromOffsetsGrid] Invalid OffsetsGrid data");
    return [];
  }

  const points: Array<[number, number]> = [];

  // For each station, find the maximum waterline (deck level)
  for (let stIdx = 0; stIdx < stations.length; stIdx++) {
    const stationX = stations[stIdx];
    const stationOffsets = offsets[stIdx];

    if (!stationOffsets || stationOffsets.length === 0) continue;

    // Find the highest waterline with non-zero half-breadth (deck level)
    let maxHeight = 0;
    for (let wlIdx = waterlines.length - 1; wlIdx >= 0; wlIdx--) {
      const halfBreadth = stationOffsets[wlIdx] ?? 0;
      if (halfBreadth > 0.01) {
        // Non-zero half-breadth indicates deck level
        maxHeight = waterlines[wlIdx];
        break;
      }
    }

    // If no deck level found, use depth
    if (maxHeight < draftM) {
      maxHeight = depthM;
    }

    const freeboard = maxHeight - draftM;
    // Convert station X position to longitudinal coordinate
    // OffsetsGrid stations are in meters from AP (0 = AP, Lpp = FP)
    // Profile view expects: x = 0 at midship, -Lpp/2 at AP, +Lpp/2 at FP
    // Convert: profileX = stationX - Lpp/2
    const x = stationX - lppM / 2;

    if (Number.isFinite(x) && Number.isFinite(freeboard)) {
      points.push([x, freeboard]);
    }
  }

  return points;
}
