/**
 * ShipD 2D Geometry Extraction Utilities
 *
 * Extracts waterlines, buttocks, and sections from ShipD geometry data
 * for use in 2D plan, profile, and body plan views.
 */

import type { ShipDHullSections } from "./shipdGeometryGenerator";

/**
 * Extract waterlines from ShipD sections for Plan View
 */
export function extractWaterlinesFromShipD(
  sections: ShipDHullSections,
  lppM: number,
  waterlineHeights: number[] = [],
  designDraft?: number
): Array<{ depth: number; points: Array<[number, number]>; isDesignWaterline: boolean }> {
  const waterlines: Array<{ depth: number; points: Array<[number, number]>; isDesignWaterline: boolean }> = [];

  // If no heights specified, generate default waterlines
  if (waterlineHeights.length === 0) {
    const maxDraft = Math.max(
      ...sections.stations.flatMap((s) => Object.keys(s.offsets).map(Number))
    );
    waterlineHeights = Array.from({ length: 7 }, (_, i) => (i / 6) * maxDraft);
  }

  // Debug: Log station positions to verify we have bow, midship, and stern
  const stationPositions = sections.stations.map(s => s.position).sort((a, b) => a - b);
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
            if (h2 - h1 > 0.001) { // Avoid division by zero
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

      // Only add point if halfBreadth is valid (>= 0)
      if (halfBreadth >= 0) {
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

      // Design waterline is at the draft level (maximum height)
      // For plan view, we show depth below waterline, so depth = draft - height
      const depth = height; // Height from keel (0 = keel, draft = waterline)
      // Design waterline is at the draft level
      const isDesignWaterline = designDraft !== undefined
        ? Math.abs(height - designDraft) < 0.01
        : height >= (waterlineHeights[waterlineHeights.length - 1] || 0) * 0.95;

      // Debug: Log first and last points to verify bow/stern representation
      if (points.length > 0) {
        console.log(`[extractWaterlinesFromShipD] Waterline at height ${height.toFixed(3)}m:`, {
          pointCount: points.length,
          sternPoint: points[0], // Should be at x ≈ -lppM/2 (aft)
          bowPoint: points[points.length - 1], // Should be at x ≈ +lppM/2 (forward)
          maxHalfBreadth: Math.max(...points.map(p => p[1])),
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
  const buttocks: Array<{ transverseOffset: number; points: Array<[number, number]>; isCenterline: boolean }> = [];

  // If no offsets specified, generate default buttocks
  if (buttockOffsets.length === 0) {
    const maxHalfBreadth = Math.max(
      ...sections.stations.flatMap((s) => Object.values(s.offsets))
    );
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
      points.push([x, profileY]);
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
  const result: Array<{ station: number; points: Array<[number, number]>; isAft: boolean; hasBulb: boolean }> = [];

  // If no indices specified, use all stations
  const stationsToUse = stationIndices.length > 0
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
      // Determine if aft (position < 0.5) or forward (position >= 0.5)
      result.push({
        station: Math.round(station.position * 10), // Convert 0-1 to 0-10
        points,
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
