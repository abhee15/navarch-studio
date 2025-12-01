/**
 * NURBS Surface Evaluation Utilities
 *
 * Implements NURBS (Non-Uniform Rational B-Spline) surface evaluation for smooth hull visualization.
 * Converts discrete offset points into C² continuous surfaces using B-spline basis functions.
 *
 * Based on the algorithm from "The NURBS Book" by Piegl & Tiller.
 */

/**
 * Control Point Grid: 2D array of control points defining the hull surface
 * Grid dimensions: [numStations, numControlPointsPerStation]
 * Each control point is (x, y, z) where:
 *   x = longitudinal position (station)
 *   y = half-breadth (transverse)
 *   z = height (vertical)
 */
export interface ControlPointGrid {
  points: Array<Array<[number, number, number]>>;
  numStations: number;
  numControlPointsPerStation: number;
}

/**
 * Quality settings for NURBS evaluation resolution
 */
export type NurbsQuality = "low" | "medium" | "high";

/**
 * Generates clamped uniform knot vector for NURBS
 * Clamped means first (degree+1) knots = 0, last (degree+1) knots = 1
 * This ensures the curve passes through the first and last control points
 */
function generateClampedUniformKnots(n: number, degree: number): number[] {
  const knots: number[] = [];

  // Clamp start: first (degree+1) knots = 0
  for (let i = 0; i <= degree; i++) {
    knots.push(0);
  }

  // Uniform interior knots
  if (n > degree) {
    for (let i = 1; i <= n - degree; i++) {
      knots.push(i / (n - degree + 1));
    }
  }

  // Clamp end: last (degree+1) knots = 1
  for (let i = 0; i <= degree; i++) {
    knots.push(1);
  }

  return knots;
}

/**
 * Finds the knot span containing parameter u using binary search
 */
function findKnotSpan(n: number, degree: number, u: number, knots: number[]): number {
  // Special case: u is at the last knot
  if (u >= knots[n + 1]) {
    return n;
  }

  // Binary search
  let low = degree;
  let high = n + 1;
  let mid = Math.floor((low + high) / 2);

  while (u < knots[mid] || u >= knots[mid + 1]) {
    if (u < knots[mid]) {
      high = mid;
    } else {
      low = mid;
    }
    mid = Math.floor((low + high) / 2);
  }

  return mid;
}

/**
 * Evaluates B-spline basis functions using Cox-de Boor recursion
 * Returns the (degree+1) non-zero basis functions at parameter u
 */
function evaluateBasisFunctions(
  span: number,
  u: number,
  degree: number,
  knots: number[]
): number[] {
  const basisFunctions: number[] = [1];
  const left: number[] = [];
  const right: number[] = [];

  for (let j = 1; j <= degree; j++) {
    left.push(u - knots[span + 1 - j]);
    right.push(knots[span + j] - u);

    let saved = 0;

    for (let r = 0; r < j; r++) {
      const temp = basisFunctions[r] / (right[r] + left[j - r]);
      basisFunctions[r] = saved + right[r] * temp;
      saved = left[j - r] * temp;
    }

    basisFunctions.push(saved);
  }

  return basisFunctions;
}

/**
 * Evaluates NURBS surface S(u,v) at parameter values (u, v)
 * u: longitudinal parameter (0 = aft, 1 = forward)
 * v: vertical parameter (0 = keel, 1 = deck)
 *
 * @param controlPointGrid 2D grid of control points
 * @param u Longitudinal parameter [0, 1]
 * @param v Vertical parameter [0, 1]
 * @param degreeU Degree in longitudinal direction (typically 3 for cubic)
 * @param degreeV Degree in vertical direction (typically 3 for cubic)
 * @param knotsU Knot vector for u direction (if null, generates clamped uniform)
 * @param knotsV Knot vector for v direction (if null, generates clamped uniform)
 * @returns Evaluated point (x, y, z) on the surface
 */
export function evaluateSurface(
  controlPointGrid: ControlPointGrid,
  u: number,
  v: number,
  degreeU: number = 3,
  degreeV: number = 3,
  knotsU: number[] | null = null,
  knotsV: number[] | null = null
): [number, number, number] {
  const { numStations, numControlPointsPerStation, points } = controlPointGrid;

  if (numStations < degreeU + 1 || numControlPointsPerStation < degreeV + 1) {
    throw new Error(
      `Need at least ${degreeU + 1} stations and ${degreeV + 1} control points per station`
    );
  }

  // Generate knot vectors if not provided
  if (knotsU === null) {
    knotsU = generateClampedUniformKnots(numStations - 1, degreeU);
  }

  if (knotsV === null) {
    knotsV = generateClampedUniformKnots(numControlPointsPerStation - 1, degreeV);
  }

  // Clamp parameters to valid range
  u = Math.max(knotsU[degreeU], Math.min(u, knotsU[knotsU.length - degreeU - 1]));
  v = Math.max(knotsV[degreeV], Math.min(v, knotsV[knotsV.length - degreeV - 1]));

  // Find knot spans
  const spanU = findKnotSpan(numStations - 1, degreeU, u, knotsU);
  const spanV = findKnotSpan(numControlPointsPerStation - 1, degreeV, v, knotsV);

  // Evaluate basis functions
  const basisU = evaluateBasisFunctions(spanU, u, degreeU, knotsU);
  const basisV = evaluateBasisFunctions(spanV, v, degreeV, knotsV);

  // Compute surface point using tensor product
  let x = 0;
  let y = 0;
  let z = 0;

  for (let i = 0; i <= degreeU; i++) {
    const idxU = spanU - degreeU + i;
    const basisUVal = basisU[i];

    for (let j = 0; j <= degreeV; j++) {
      const idxV = spanV - degreeV + j;
      const basisVVal = basisV[j];
      const basisProduct = basisUVal * basisVVal;

      const cp = points[idxU][idxV];
      x += basisProduct * cp[0];
      y += basisProduct * cp[1];
      z += basisProduct * cp[2];
    }
  }

  return [x, y, z];
}

/**
 * Generates control point grid from discrete offset points
 * Uses direct mapping: each offset point becomes a control point
 * For smoother results, could use least-squares fitting, but direct mapping is simpler and faster
 *
 * @param stations Array of station positions (longitudinal, 0-1)
 * @param waterlines Array of waterline heights (vertical, 0-1)
 * @param offsets 2D array: offsets[stationIndex][waterlineIndex] = halfBreadth
 * @param lppM Length between perpendiculars (m) for scaling
 * @param beamM Maximum beam (m) for scaling
 * @param draftM Design draft (m) for scaling
 * @returns Control point grid
 */
export function generateControlPointGridFromOffsets(
  stations: number[],
  waterlines: number[],
  offsets: number[][],
  _lppM: number,
  beamM: number,
  _draftM: number
): ControlPointGrid {
  const numStations = stations.length;
  const numControlPointsPerStation = waterlines.length;

  const points: Array<Array<[number, number, number]>> = [];

  for (let i = 0; i < numStations; i++) {
    const stationRow: Array<[number, number, number]> = [];
    const stationX = stations[i]; // Longitudinal position (normalized 0-1)

    for (let j = 0; j < numControlPointsPerStation; j++) {
      const waterlineZ = waterlines[j]; // Vertical position (normalized 0-1)
      const halfBreadth = offsets[i]?.[j] ?? 0;

      // Control point: (x, y, z) where:
      // x = longitudinal position (normalized 0-1)
      // y = half-breadth (normalized 0-1, scaled by beamM/2)
      // z = height (normalized 0-1)
      const normalizedHalfBreadth = halfBreadth / (beamM / 2);

      stationRow.push([stationX, normalizedHalfBreadth, waterlineZ]);
    }

    points.push(stationRow);
  }

  return {
    points,
    numStations,
    numControlPointsPerStation,
  };
}

/**
 * Generates hull offsets from NURBS surface by evaluating at discrete stations and waterlines
 * This converts the continuous NURBS surface to the discrete offset format
 *
 * @param controlPointGrid Control point grid defining the surface
 * @param stations List of station positions (longitudinal, 0-1)
 * @param waterlines List of waterline heights (vertical, 0-1)
 * @param lppM Length between perpendiculars (m) for scaling
 * @param beamM Maximum beam (m) for scaling
 * @param draftM Design draft (m) for scaling
 * @returns Dictionary: (stationIndex, waterlineIndex) -> halfBreadth
 */
export function generateOffsetsFromSurface(
  controlPointGrid: ControlPointGrid,
  stations: number[],
  waterlines: number[],
  _lppM: number,
  beamM: number,
  _draftM: number
): Map<[number, number], number> {
  const offsets = new Map<[number, number], number>();

  for (let i = 0; i < stations.length; i++) {
    const u = stations[i]; // Longitudinal parameter [0, 1]

    for (let j = 0; j < waterlines.length; j++) {
      const v = waterlines[j]; // Vertical parameter [0, 1]

      // Evaluate surface at (u, v)
      const point = evaluateSurface(controlPointGrid, u, v);

      // Extract half-breadth (y coordinate)
      // Scale from normalized [0, 1] to physical units
      const halfBreadth = point[1] * (beamM / 2);

      offsets.set([i, j], Math.max(0, halfBreadth));
    }
  }

  return offsets;
}

/**
 * Gets resolution multiplier based on quality setting
 */
export function getResolutionMultiplier(quality: NurbsQuality): number {
  switch (quality) {
    case "low":
      return 2;
    case "medium":
      return 3;
    case "high":
      return 4;
    default:
      return 3;
  }
}

/**
 * Generates high-resolution evaluation points for smooth surface rendering
 *
 * @param originalStations Original station positions
 * @param originalWaterlines Original waterline positions
 * @param quality Quality setting (low/medium/high)
 * @returns Arrays of evaluation points for u and v parameters
 */
export function generateEvaluationPoints(
  originalStations: number[],
  originalWaterlines: number[],
  quality: NurbsQuality = "medium"
): { uPoints: number[]; vPoints: number[] } {
  const multiplier = getResolutionMultiplier(quality);

  // Generate u points (longitudinal)
  const uPoints: number[] = [];
  if (originalStations.length > 1) {
    const uMin = originalStations[0];
    const uMax = originalStations[originalStations.length - 1];
    const numUPoints = originalStations.length * multiplier;

    for (let i = 0; i < numUPoints; i++) {
      const t = i / (numUPoints - 1);
      uPoints.push(uMin + t * (uMax - uMin));
    }
  } else {
    uPoints.push(...originalStations);
  }

  // Generate v points (vertical)
  const vPoints: number[] = [];
  if (originalWaterlines.length > 1) {
    const vMin = originalWaterlines[0];
    const vMax = originalWaterlines[originalWaterlines.length - 1];
    const numVPoints = originalWaterlines.length * multiplier;

    for (let i = 0; i < numVPoints; i++) {
      const t = i / (numVPoints - 1);
      vPoints.push(vMin + t * (vMax - vMin));
    }
  } else {
    vPoints.push(...originalWaterlines);
  }

  return { uPoints, vPoints };
}
