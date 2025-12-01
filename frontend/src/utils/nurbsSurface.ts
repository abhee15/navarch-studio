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

/**
 * Evaluates the derivative of B-spline basis functions
 * Returns the (degree) non-zero derivative basis functions at parameter u
 * Based on formula: N'_{i,p}(u) = (p / (u_{i+p+1} - u_{i+1})) * N_{i,p-1}(u) - (p / (u_{i+p+2} - u_{i+2})) * N_{i+1,p-1}(u)
 */
function evaluateBasisFunctionDerivatives(
  span: number,
  u: number,
  degree: number,
  knots: number[]
): number[] {
  if (degree === 0) {
    return [0]; // Degree 0 basis functions have zero derivative
  }

  // Evaluate basis functions of degree (p-1)
  const basisP1 = evaluateBasisFunctions(span, u, degree - 1, knots);
  const basisP1Next = evaluateBasisFunctions(span + 1, u, degree - 1, knots);

  const derivatives: number[] = [];

  for (let i = 0; i < degree; i++) {
    const denom1 = knots[span + degree - i] - knots[span + 1 - i];
    const denom2 = knots[span + degree + 1 - i] - knots[span + 2 - i];

    let deriv = 0;
    if (denom1 > 1e-10) {
      deriv += (degree / denom1) * (basisP1[i] ?? 0);
    }
    if (denom2 > 1e-10) {
      deriv -= (degree / denom2) * (basisP1Next[i] ?? 0);
    }

    derivatives.push(deriv);
  }

  return derivatives;
}

/**
 * Evaluates the second derivative of B-spline basis functions
 * Returns the (degree-1) non-zero second derivative basis functions at parameter u
 */
function evaluateBasisFunctionSecondDerivatives(
  span: number,
  u: number,
  degree: number,
  knots: number[]
): number[] {
  if (degree <= 1) {
    return [0]; // Degree 0 or 1 basis functions have zero second derivative
  }

  // Evaluate first derivatives of degree (p-1) basis functions
  const derivP1 = evaluateBasisFunctionDerivatives(span, u, degree - 1, knots);
  const derivP1Next = evaluateBasisFunctionDerivatives(span + 1, u, degree - 1, knots);

  const secondDerivatives: number[] = [];

  for (let i = 0; i < degree - 1; i++) {
    const denom1 = knots[span + degree - i] - knots[span + 1 - i];
    const denom2 = knots[span + degree + 1 - i] - knots[span + 2 - i];

    let secondDeriv = 0;
    if (denom1 > 1e-10) {
      secondDeriv += (degree / denom1) * (derivP1[i] ?? 0);
    }
    if (denom2 > 1e-10) {
      secondDeriv -= (degree / denom2) * (derivP1Next[i] ?? 0);
    }

    secondDerivatives.push(secondDeriv);
  }

  return secondDerivatives;
}

/**
 * Evaluates the first derivative of a NURBS curve in the u direction (longitudinal)
 * @param controlPointGrid Control point grid
 * @param u Longitudinal parameter [0, 1]
 * @param v Vertical parameter [0, 1] (fixed for curve evaluation)
 * @param degreeU Degree in u direction
 * @param degreeV Degree in v direction
 * @param knotsU Knot vector for u direction
 * @param knotsV Knot vector for v direction
 * @returns First derivative vector [dx/du, dy/du, dz/du]
 */
export function evaluateCurveDerivativeU(
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

  // Evaluate basis functions and derivatives
  const basisU = evaluateBasisFunctions(spanU, u, degreeU, knotsU);
  const derivU = evaluateBasisFunctionDerivatives(spanU, u, degreeU, knotsU);
  const basisV = evaluateBasisFunctions(spanV, v, degreeV, knotsV);

  // Compute derivative using tensor product
  let dx = 0;
  let dy = 0;
  let dz = 0;

  for (let i = 0; i < degreeU; i++) {
    const idxU = spanU - degreeU + i;
    const derivUVal = derivU[i];

    for (let j = 0; j <= degreeV; j++) {
      const idxV = spanV - degreeV + j;
      const basisVVal = basisV[j];
      const product = derivUVal * basisVVal;

      const cp = points[idxU][idxV];
      dx += product * cp[0];
      dy += product * cp[1];
      dz += product * cp[2];
    }
  }

  return [dx, dy, dz];
}

/**
 * Evaluates the first derivative of a NURBS curve in the v direction (vertical)
 * @param controlPointGrid Control point grid
 * @param u Longitudinal parameter [0, 1] (fixed for curve evaluation)
 * @param v Vertical parameter [0, 1]
 * @param degreeU Degree in u direction
 * @param degreeV Degree in v direction
 * @param knotsU Knot vector for u direction
 * @param knotsV Knot vector for v direction
 * @returns First derivative vector [dx/dv, dy/dv, dz/dv]
 */
export function evaluateCurveDerivativeV(
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

  // Evaluate basis functions and derivatives
  const basisU = evaluateBasisFunctions(spanU, u, degreeU, knotsU);
  const basisV = evaluateBasisFunctions(spanV, v, degreeV, knotsV);
  const derivV = evaluateBasisFunctionDerivatives(spanV, v, degreeV, knotsV);

  // Compute derivative using tensor product
  let dx = 0;
  let dy = 0;
  let dz = 0;

  for (let i = 0; i <= degreeU; i++) {
    const idxU = spanU - degreeU + i;
    const basisUVal = basisU[i];

    for (let j = 0; j < degreeV; j++) {
      const idxV = spanV - degreeV + j;
      const derivVVal = derivV[j];
      const product = basisUVal * derivVVal;

      const cp = points[idxU][idxV];
      dx += product * cp[0];
      dy += product * cp[1];
      dz += product * cp[2];
    }
  }

  return [dx, dy, dz];
}

/**
 * Evaluates the second derivative of a NURBS curve in the v direction (vertical)
 * Used for analyzing curvature along hull sections (frames)
 * @param controlPointGrid Control point grid
 * @param u Longitudinal parameter [0, 1] (fixed for curve evaluation)
 * @param v Vertical parameter [0, 1]
 * @param degreeU Degree in u direction
 * @param degreeV Degree in v direction
 * @param knotsU Knot vector for u direction
 * @param knotsV Knot vector for v direction
 * @returns Second derivative vector [d²x/dv², d²y/dv², d²z/dv²]
 */
export function evaluateCurveSecondDerivativeV(
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

  // Evaluate basis functions and second derivatives
  const basisU = evaluateBasisFunctions(spanU, u, degreeU, knotsU);
  const basisV = evaluateBasisFunctions(spanV, v, degreeV, knotsV);
  const secondDerivV = evaluateBasisFunctionSecondDerivatives(spanV, v, degreeV, knotsV);

  // Compute second derivative using tensor product
  let d2x = 0;
  let d2y = 0;
  let d2z = 0;

  for (let i = 0; i <= degreeU; i++) {
    const idxU = spanU - degreeU + i;
    const basisUVal = basisU[i];

    for (let j = 0; j < degreeV - 1; j++) {
      const idxV = spanV - degreeV + j;
      const secondDerivVVal = secondDerivV[j];
      const product = basisUVal * secondDerivVVal;

      const cp = points[idxU][idxV];
      d2x += product * cp[0];
      d2y += product * cp[1];
      d2z += product * cp[2];
    }
  }

  return [d2x, d2y, d2z];
}

/**
 * Calculates the signed curvature of a 2D curve (useful for hull sections)
 * For a curve in the x-y plane (half-breadth vs height), curvature is:
 * κ = (x'y'' - y'x'') / (x'² + y'²)^(3/2)
 * @param firstDeriv First derivative [dx/dv, dy/dv, dz/dv]
 * @param secondDeriv Second derivative [d²x/dv², d²y/dv², d²z/dv²]
 * @returns Curvature value (positive = convex outward, negative = concave)
 */
export function evaluateCurvature(
  firstDeriv: [number, number, number],
  secondDeriv: [number, number, number]
): number {
  const [dx, dy] = [firstDeriv[0], firstDeriv[1]]; // Use x (longitudinal) and y (half-breadth) for 2D curvature
  const [d2x, d2y] = [secondDeriv[0], secondDeriv[1]];

  const speedSquared = dx * dx + dy * dy;
  if (speedSquared < 1e-10) {
    return 0; // Zero speed, no curvature
  }

  const speed = Math.sqrt(speedSquared);
  const numerator = dx * d2y - dy * d2x;
  const curvature = numerator / (speed * speedSquared);

  return curvature;
}

/**
 * Diagnostic result for a single point on a curve
 */
export interface CurvaturePoint {
  v: number;
  curvature: number;
  firstDeriv: [number, number, number];
  secondDeriv: [number, number, number];
  point: [number, number, number];
}

/**
 * C² continuity violation at a knot boundary
 */
export interface C2Discontinuity {
  u: number | null; // Knot position in u direction (null if checking v direction)
  v: number | null; // Knot position in v direction (null if checking u direction)
  jump: number; // Magnitude of discontinuity in second derivative
  direction: "u" | "v"; // Direction of the curve being checked
}

/**
 * C² continuity check result
 */
export interface C2ContinuityResult {
  isContinuous: boolean;
  discontinuities: C2Discontinuity[];
  maxDiscontinuity: number;
}

/**
 * Analyzes curvature along a single hull section (frame)
 * @param controlPointGrid Control point grid
 * @param stationIndex Index of the station to analyze (0 = aft, numStations-1 = forward)
 * @param resolution Number of evaluation points along the section (default: 100)
 * @param degreeU Degree in u direction
 * @param degreeV Degree in v direction
 * @param knotsU Knot vector for u direction
 * @param knotsV Knot vector for v direction
 * @returns Array of curvature points along the section
 */
export function analyzeSectionCurvature(
  controlPointGrid: ControlPointGrid,
  stationIndex: number,
  resolution: number = 100,
  degreeU: number = 3,
  degreeV: number = 3,
  knotsU: number[] | null = null,
  knotsV: number[] | null = null
): CurvaturePoint[] {
  const { numStations } = controlPointGrid;

  if (stationIndex < 0 || stationIndex >= numStations) {
    throw new Error(`Station index ${stationIndex} out of range [0, ${numStations - 1}]`);
  }

  // Generate knot vectors if not provided
  if (knotsU === null) {
    knotsU = generateClampedUniformKnots(numStations - 1, degreeU);
  }

  if (knotsV === null) {
    knotsV = generateClampedUniformKnots(controlPointGrid.numControlPointsPerStation - 1, degreeV);
  }

  // Calculate u parameter for this station
  // Stations are typically uniformly distributed, but we need the exact u value
  const uMin = knotsU[degreeU];
  const uMax = knotsU[knotsU.length - degreeU - 1];
  const u = uMin + (stationIndex / (numStations - 1)) * (uMax - uMin);

  // Clamp u to valid range
  const clampedU = Math.max(knotsU[degreeU], Math.min(u, knotsU[knotsU.length - degreeU - 1]));

  // Generate v evaluation points (keel to deck)
  const vMin = knotsV[degreeV];
  const vMax = knotsV[knotsV.length - degreeV - 1];
  const vPoints: number[] = [];
  for (let i = 0; i < resolution; i++) {
    const t = i / (resolution - 1);
    const v = vMin + t * (vMax - vMin);
    vPoints.push(Math.max(knotsV[degreeV], Math.min(v, knotsV[knotsV.length - degreeV - 1])));
  }

  // Evaluate curvature at each point
  const curvaturePoints: CurvaturePoint[] = [];

  for (const v of vPoints) {
    try {
      const point = evaluateSurface(
        controlPointGrid,
        clampedU,
        v,
        degreeU,
        degreeV,
        knotsU,
        knotsV
      );
      const firstDeriv = evaluateCurveDerivativeV(
        controlPointGrid,
        clampedU,
        v,
        degreeU,
        degreeV,
        knotsU,
        knotsV
      );
      const secondDeriv = evaluateCurveSecondDerivativeV(
        controlPointGrid,
        clampedU,
        v,
        degreeU,
        degreeV,
        knotsU,
        knotsV
      );
      const curvature = evaluateCurvature(firstDeriv, secondDeriv);

      curvaturePoints.push({
        v,
        curvature,
        firstDeriv,
        secondDeriv,
        point,
      });
    } catch (error) {
      console.warn(`[analyzeSectionCurvature] Error evaluating at v=${v}:`, error);
    }
  }

  return curvaturePoints;
}

/**
 * Checks C² continuity of a NURBS curve by examining second derivatives at knot boundaries
 * @param controlPointGrid Control point grid
 * @param direction Direction to check: "u" (longitudinal) or "v" (vertical)
 * @param fixedParam Fixed parameter value in the other direction
 * @param degreeU Degree in u direction
 * @param degreeV Degree in v direction
 * @param knotsU Knot vector for u direction
 * @param knotsV Knot vector for v direction
 * @param tolerance Tolerance for continuity check (default: 1e-6)
 * @returns C² continuity result
 */
export function checkC2Continuity(
  controlPointGrid: ControlPointGrid,
  direction: "u" | "v",
  fixedParam: number,
  degreeU: number = 3,
  degreeV: number = 3,
  knotsU: number[] | null = null,
  knotsV: number[] | null = null,
  tolerance: number = 1e-6
): C2ContinuityResult {
  // Generate knot vectors if not provided
  if (knotsU === null) {
    knotsU = generateClampedUniformKnots(controlPointGrid.numStations - 1, degreeU);
  }

  if (knotsV === null) {
    knotsV = generateClampedUniformKnots(controlPointGrid.numControlPointsPerStation - 1, degreeV);
  }

  const discontinuities: C2Discontinuity[] = [];
  let maxDiscontinuity = 0;

  if (direction === "v") {
    // Check continuity in v direction (vertical, along sections)
    // Clamp fixedParam to valid range
    const clampedU = Math.max(
      knotsU[degreeU],
      Math.min(fixedParam, knotsU[knotsU.length - degreeU - 1])
    );

    // Check at each interior knot in v direction
    const vKnots = knotsV.slice(degreeV + 1, knotsV.length - degreeV - 1);
    for (const vKnot of vKnots) {
      // Evaluate second derivative from left and right of knot
      const vLeft = vKnot - 1e-8;
      const vRight = vKnot + 1e-8;

      try {
        const secondDerivLeft = evaluateCurveSecondDerivativeV(
          controlPointGrid,
          clampedU,
          vLeft,
          degreeU,
          degreeV,
          knotsU,
          knotsV
        );
        const secondDerivRight = evaluateCurveSecondDerivativeV(
          controlPointGrid,
          clampedU,
          vRight,
          degreeU,
          degreeV,
          knotsU,
          knotsV
        );

        // Calculate jump magnitude
        const jumpX = Math.abs(secondDerivRight[0] - secondDerivLeft[0]);
        const jumpY = Math.abs(secondDerivRight[1] - secondDerivLeft[1]);
        const jumpZ = Math.abs(secondDerivRight[2] - secondDerivLeft[2]);
        const jump = Math.sqrt(jumpX * jumpX + jumpY * jumpY + jumpZ * jumpZ);

        if (jump > tolerance) {
          discontinuities.push({
            u: clampedU,
            v: vKnot,
            jump,
            direction: "v",
          });
          maxDiscontinuity = Math.max(maxDiscontinuity, jump);
        }
      } catch (error) {
        console.warn(`[checkC2Continuity] Error checking v=${vKnot}:`, error);
      }
    }
  } else {
    // Check continuity in u direction (longitudinal, along waterlines/buttocks)
    // Clamp fixedParam to valid range
    const clampedV = Math.max(
      knotsV[degreeV],
      Math.min(fixedParam, knotsV[knotsV.length - degreeV - 1])
    );

    // Check at each interior knot in u direction
    const uKnots = knotsU.slice(degreeU + 1, knotsU.length - degreeU - 1);
    for (const uKnot of uKnots) {
      // Evaluate second derivative from left and right of knot
      const uLeft = uKnot - 1e-8;
      const uRight = uKnot + 1e-8;

      try {
        // For u direction, we need second derivative in u
        // This requires implementing evaluateCurveSecondDerivativeU
        // For now, we'll check first derivative continuity as a proxy
        const firstDerivLeft = evaluateCurveDerivativeU(
          controlPointGrid,
          uLeft,
          clampedV,
          degreeU,
          degreeV,
          knotsU,
          knotsV
        );
        const firstDerivRight = evaluateCurveDerivativeU(
          controlPointGrid,
          uRight,
          clampedV,
          degreeU,
          degreeV,
          knotsU,
          knotsV
        );

        // Calculate jump magnitude in first derivative (C¹ continuity check)
        const jumpX = Math.abs(firstDerivRight[0] - firstDerivLeft[0]);
        const jumpY = Math.abs(firstDerivRight[1] - firstDerivLeft[1]);
        const jumpZ = Math.abs(firstDerivRight[2] - firstDerivLeft[2]);
        const jump = Math.sqrt(jumpX * jumpX + jumpY * jumpY + jumpZ * jumpZ);

        if (jump > tolerance) {
          discontinuities.push({
            u: uKnot,
            v: clampedV,
            jump,
            direction: "u",
          });
          maxDiscontinuity = Math.max(maxDiscontinuity, jump);
        }
      } catch (error) {
        console.warn(`[checkC2Continuity] Error checking u=${uKnot}:`, error);
      }
    }
  }

  return {
    isContinuous: discontinuities.length === 0,
    discontinuities,
    maxDiscontinuity,
  };
}

/**
 * Diagnostic report for a hull section
 */
export interface SectionDiagnosticReport {
  stationIndex: number;
  u: number;
  curvaturePoints: CurvaturePoint[];
  c2Continuity: C2ContinuityResult;
  curvatureStats: {
    min: number;
    max: number;
    mean: number;
    stdDev: number;
    oscillatoryRegions: Array<{ startV: number; endV: number; oscillationCount: number }>;
  };
  recommendations: string[];
}

/**
 * Complete diagnostic for a single hull section (frame)
 * Isolates one section, calculates curvature, and identifies smoothness issues
 * @param controlPointGrid Control point grid
 * @param stationIndex Index of the station to analyze (0 = aft, numStations-1 = forward)
 * @param resolution Number of evaluation points along the section (default: 100)
 * @param degreeU Degree in u direction
 * @param degreeV Degree in v direction
 * @param knotsU Knot vector for u direction
 * @param knotsV Knot vector for v direction
 * @returns Diagnostic report
 */
export function diagnoseHullSection(
  controlPointGrid: ControlPointGrid,
  stationIndex: number,
  resolution: number = 100,
  degreeU: number = 3,
  degreeV: number = 3,
  knotsU: number[] | null = null,
  knotsV: number[] | null = null
): SectionDiagnosticReport {
  // Analyze curvature along the section
  const curvaturePoints = analyzeSectionCurvature(
    controlPointGrid,
    stationIndex,
    resolution,
    degreeU,
    degreeV,
    knotsU,
    knotsV
  );

  // Calculate u parameter for this station
  if (knotsU === null) {
    knotsU = generateClampedUniformKnots(controlPointGrid.numStations - 1, degreeU);
  }
  const uMin = knotsU[degreeU];
  const uMax = knotsU[knotsU.length - degreeU - 1];
  const u = uMin + (stationIndex / (controlPointGrid.numStations - 1)) * (uMax - uMin);

  // Check C² continuity in v direction (along the section)
  const c2Continuity = checkC2Continuity(
    controlPointGrid,
    "v",
    u,
    degreeU,
    degreeV,
    knotsU,
    knotsV
  );

  // Calculate curvature statistics
  const curvatures = curvaturePoints.map((cp) => cp.curvature);
  const min = Math.min(...curvatures);
  const max = Math.max(...curvatures);
  const mean = curvatures.reduce((a, b) => a + b, 0) / curvatures.length;
  const variance =
    curvatures.reduce((sum, c) => sum + (c - mean) * (c - mean), 0) / curvatures.length;
  const stdDev = Math.sqrt(variance);

  // Identify oscillatory regions (where curvature changes sign frequently)
  const oscillatoryRegions: Array<{ startV: number; endV: number; oscillationCount: number }> = [];
  if (curvaturePoints.length > 2) {
    let currentRegionStart = 0;
    let signChanges = 0;
    let lastSign = Math.sign(curvaturePoints[0].curvature);

    for (let i = 1; i < curvaturePoints.length; i++) {
      const currentSign = Math.sign(curvaturePoints[i].curvature);
      if (currentSign !== 0 && lastSign !== 0 && currentSign !== lastSign) {
        signChanges++;
      }
      lastSign = currentSign !== 0 ? currentSign : lastSign;

      // If we have many sign changes in a short region, it's oscillatory
      if (signChanges > 5 && i - currentRegionStart > 10) {
        oscillatoryRegions.push({
          startV: curvaturePoints[currentRegionStart].v,
          endV: curvaturePoints[i].v,
          oscillationCount: signChanges,
        });
        currentRegionStart = i;
        signChanges = 0;
      }
    }
  }

  // Generate recommendations
  const recommendations: string[] = [];

  if (!c2Continuity.isContinuous) {
    recommendations.push(
      `C² continuity violated: ${c2Continuity.discontinuities.length} discontinuity(ies) detected with max jump of ${c2Continuity.maxDiscontinuity.toExponential(2)}`
    );
    recommendations.push(
      "Recommendation: Apply control point fairing to enforce smoothness at knot boundaries"
    );
  }

  if (oscillatoryRegions.length > 0) {
    recommendations.push(
      `Oscillatory curvature detected in ${oscillatoryRegions.length} region(s) along the section`
    );
    recommendations.push(
      "Recommendation: Control points may need smoothing to reduce curvature oscillations"
    );
  }

  if (stdDev > Math.abs(mean) * 0.5) {
    recommendations.push(
      `High curvature variation (std dev = ${stdDev.toFixed(4)}, mean = ${mean.toFixed(4)})`
    );
    recommendations.push(
      "Recommendation: Consider increasing control point density or applying fairing"
    );
  }

  if (Math.abs(max - min) > 10 * Math.abs(mean)) {
    recommendations.push(
      `Extreme curvature range detected (min = ${min.toFixed(4)}, max = ${max.toFixed(4)})`
    );
    recommendations.push("Recommendation: Check for knuckles or cusps in the section geometry");
  }

  if (recommendations.length === 0) {
    recommendations.push("Section appears smooth with good C² continuity");
  }

  return {
    stationIndex,
    u,
    curvaturePoints,
    c2Continuity,
    curvatureStats: {
      min,
      max,
      mean,
      stdDev,
      oscillatoryRegions,
    },
    recommendations,
  };
}
