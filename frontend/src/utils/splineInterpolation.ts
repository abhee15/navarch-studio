/**
 * Cubic spline interpolation for smooth curve fairing through hull offsets
 * Natural cubic spline with zero second derivative at endpoints
 */

export interface Point2D {
  x: number;
  y: number;
}

export interface SplineSegment {
  x0: number;
  x1: number;
  a: number;
  b: number;
  c: number;
  d: number;
}

/**
 * Creates a natural cubic spline through the given points
 * @param points - Array of points sorted by x coordinate
 * @returns Array of spline segments
 */
export function createCubicSpline(points: Point2D[]): SplineSegment[] {
  if (points.length < 2) {
    return [];
  }

  if (points.length === 2) {
    // Linear interpolation for 2 points
    const slope = (points[1].y - points[0].y) / (points[1].x - points[0].x);
    return [
      {
        x0: points[0].x,
        x1: points[1].x,
        a: points[0].y,
        b: slope,
        c: 0,
        d: 0,
      },
    ];
  }

  const n = points.length;
  const h: number[] = [];
  const alpha: number[] = [];
  const l: number[] = [1];
  const mu: number[] = [0];
  const z: number[] = [0];

  // Calculate intervals
  for (let i = 0; i < n - 1; i++) {
    h[i] = points[i + 1].x - points[i].x;
  }

  // Calculate alpha values
  for (let i = 1; i < n - 1; i++) {
    alpha[i] =
      (3 / h[i]) * (points[i + 1].y - points[i].y) -
      (3 / h[i - 1]) * (points[i].y - points[i - 1].y);
  }

  // Solve tridiagonal system
  for (let i = 1; i < n - 1; i++) {
    l[i] = 2 * (points[i + 1].x - points[i - 1].x) - h[i - 1] * mu[i - 1];
    mu[i] = h[i] / l[i];
    z[i] = (alpha[i] - h[i - 1] * z[i - 1]) / l[i];
  }

  l[n - 1] = 1;
  z[n - 1] = 0;

  const c: number[] = new Array(n).fill(0);
  const b: number[] = new Array(n - 1);
  const d: number[] = new Array(n - 1);

  // Back substitution
  for (let j = n - 2; j >= 0; j--) {
    c[j] = z[j] - mu[j] * c[j + 1];
    b[j] = (points[j + 1].y - points[j].y) / h[j] - (h[j] * (c[j + 1] + 2 * c[j])) / 3;
    d[j] = (c[j + 1] - c[j]) / (3 * h[j]);
  }

  // Create spline segments
  const segments: SplineSegment[] = [];
  for (let i = 0; i < n - 1; i++) {
    segments.push({
      x0: points[i].x,
      x1: points[i + 1].x,
      a: points[i].y,
      b: b[i],
      c: c[i],
      d: d[i],
    });
  }

  return segments;
}

/**
 * Evaluates the spline at a given x coordinate
 * @param segments - Spline segments
 * @param x - X coordinate
 * @returns Interpolated y value
 */
export function evaluateSpline(segments: SplineSegment[], x: number): number {
  if (segments.length === 0) {
    return 0;
  }

  // Find the appropriate segment
  let segment = segments[0];
  for (const seg of segments) {
    if (x >= seg.x0 && x <= seg.x1) {
      segment = seg;
      break;
    }
  }

  // Clamp to first or last segment if out of bounds
  if (x < segments[0].x0) {
    segment = segments[0];
    x = segment.x0;
  } else if (x > segments[segments.length - 1].x1) {
    segment = segments[segments.length - 1];
    x = segment.x1;
  }

  // Evaluate cubic polynomial: S(x) = a + b(x-x0) + c(x-x0)^2 + d(x-x0)^3
  const dx = x - segment.x0;
  return segment.a + segment.b * dx + segment.c * dx * dx + segment.d * dx * dx * dx;
}

/**
 * Generates smooth curve points by interpolating between data points
 * @param points - Input data points
 * @param numInterpPoints - Number of interpolated points to generate
 * @returns Array of interpolated points
 */
export function generateSmoothCurve(points: Point2D[], numInterpPoints: number = 100): Point2D[] {
  if (points.length === 0) {
    return [];
  }

  if (points.length === 1) {
    return [...points];
  }

  // Sort points by x coordinate
  const sortedPoints = [...points].sort((a, b) => a.x - b.x);

  // Create spline
  const segments = createCubicSpline(sortedPoints);

  // Generate interpolated points
  const xMin = sortedPoints[0].x;
  const xMax = sortedPoints[sortedPoints.length - 1].x;
  const step = (xMax - xMin) / (numInterpPoints - 1);

  const interpolated: Point2D[] = [];
  for (let i = 0; i < numInterpPoints; i++) {
    const x = xMin + i * step;
    const y = evaluateSpline(segments, x);
    interpolated.push({ x, y });
  }

  return interpolated;
}

/**
 * Applies fairing to hull offsets by creating smooth splines through waterlines
 * @param offsetsGrid - Grid of half-breadth offsets
 * @returns Faired offset grid with smooth curves
 */
export function fairHullOffsets(offsetsGrid: number[][], numInterpPoints: number = 50): number[][] {
  if (offsetsGrid.length === 0) {
    return [];
  }

  const faired: number[][] = [];

  // For each station, create a smooth curve through its waterlines
  for (let stationIdx = 0; stationIdx < offsetsGrid.length; stationIdx++) {
    const stationOffsets = offsetsGrid[stationIdx];

    // Create points for this station (waterline index as x, half-breadth as y)
    const points: Point2D[] = stationOffsets.map((halfBreadth, wlIdx) => ({
      x: wlIdx,
      y: halfBreadth,
    }));

    // Generate smooth curve
    const smoothPoints = generateSmoothCurve(points, numInterpPoints);
    faired.push(smoothPoints.map((p) => p.y));
  }

  return faired;
}
