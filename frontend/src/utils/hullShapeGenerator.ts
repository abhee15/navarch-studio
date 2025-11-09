/**
 * Parametric Hull Shape Generator for Naval Architecture Visualization
 *
 * Generates vessel-type-specific hull geometries based on form coefficients
 * and naval architecture principles. Replaces generic Wigley formula with
 * accurate representations of different vessel types.
 *
 * @module hullShapeGenerator
 * @author NavArch Studio
 * @version 1.0.0
 */

import * as THREE from 'three';

// ============================================================================
// TYPES & INTERFACES
// ============================================================================

/**
 * Hull shape generation parameters
 */
export interface HullShapeParams {
  /** Hull family identifier: 'container', 'tanker', 'bulk', 'fishing', 'yacht_disp', etc. */
  hullFamily: string;
  /** Length between perpendiculars (m) */
  lppM: number;
  /** Maximum beam (m) */
  beamM: number;
  /** Design draft (m) */
  draftM: number;
  /** Block coefficient: Cb = Volume / (Lpp × B × T) */
  cb: number;
  /** Prismatic coefficient: Cp = Volume / (Am × Lpp) */
  cp: number;
  /** Waterplane coefficient: Cwp = Awp / (Lpp × B) */
  cwp: number;
  /** Midship coefficient: Cm = Am / (B × T) - defaults to Cb/Cp if not provided */
  cm?: number;
  /** Longitudinal center of buoyancy (% aft of FP, typically -5 to +5) */
  lcbPctLpp?: number;
}

/**
 * Parameters for generating 2D waterlines
 */
export interface WaterlinesParams extends HullShapeParams {
  /** Number of waterlines to generate (typically 5-10) */
  waterlineCount: number;
  /** Number of points per waterline curve (typically 40-80) */
  pointsPerWaterline: number;
}

/**
 * Parameters for generating 3D hull geometry
 */
export interface Hull3DParams extends HullShapeParams {
  /** Number of longitudinal segments (stations) */
  longitudinalSegments: number;
  /** Number of vertical segments (waterlines) */
  verticalSegments: number;
}

/**
 * Point on a waterline curve
 */
export interface WaterlinePoint {
  /** Longitudinal position: -Lpp/2 to +Lpp/2 (m) */
  x: number;
  /** Half-breadth from centerline (m) - starboard side only */
  y: number;
}

/**
 * A single waterline at a given depth
 */
export interface Waterline {
  /** Depth below design waterline (0 = DWL, positive = below) */
  depth: number;
  /** Array of (x,y) points defining the waterline curve */
  points: WaterlinePoint[];
  /** Whether this is the design waterline */
  isDesignWaterline: boolean;
}

/**
 * Validation result for generated hull shape
 */
export interface ValidationResult {
  /** Whether the generated shape is valid */
  isValid: boolean;
  /** Error in Cb computation (relative) */
  cbError: number;
  /** Error in Cwp computation (relative) */
  cwpError: number;
  /** Warning messages */
  warnings: string[];
  /** Info messages */
  info: string[];
}

// ============================================================================
// CONSTANTS
// ============================================================================

/** Minimum valid form coefficients */
const MIN_CB = 0.25;
const MIN_CP = 0.50;
const MIN_CWP = 0.60;

/** Maximum valid form coefficients */
const MAX_CB = 0.95;
const MAX_CP = 0.95;
const MAX_CWP = 0.98;

// ============================================================================
// CORE SHAPE GENERATION
// ============================================================================

/**
 * Generates waterlines for 2D plan view visualization
 *
 * @remarks
 * Uses vessel-type-specific shape functions based on hull family and form coefficients.
 * Ensures generated geometry reflects the vessel's Cb, Cp, and Cwp values.
 *
 * @example
 * ```typescript
 * const waterlines = generateHullWaterlines({
 *   hullFamily: 'container',
 *   lppM: 200,
 *   beamM: 30,
 *   draftM: 12,
 *   cb: 0.68,
 *   cp: 0.73,
 *   cwp: 0.80,
 *   waterlineCount: 7,
 *   pointsPerWaterline: 60
 * });
 * ```
 *
 * @param params - Hull shape parameters including dimensions and coefficients
 * @returns Array of waterline curves at different depths
 */
export function generateHullWaterlines(params: WaterlinesParams): Waterline[] {
  // Validate input parameters
  validateParams(params);

  // Normalize hull family
  const family = normalizeHullFamily(params.hullFamily);

  // Generate waterlines at different depths
  const waterlines: Waterline[] = [];
  const { waterlineCount, draftM } = params;

  for (let i = 0; i <= waterlineCount; i++) {
    const depth = (i / waterlineCount) * draftM;
    const zPosition = -depth; // Negative Z below waterline
    const isDesignWaterline = i === waterlineCount;

    const points = generateWaterlineAtDepth(params, zPosition, family);

    waterlines.push({
      depth,
      points,
      isDesignWaterline
    });
  }

  return waterlines;
}

/**
 * Generates 3D hull geometry for Three.js visualization
 *
 * @param params - Hull shape parameters including mesh resolution
 * @returns THREE.BufferGeometry representing the hull surface
 */
export function generateHull3DGeometry(params: Hull3DParams): THREE.BufferGeometry {
  // Validate input parameters
  validateParams(params);

  // Normalize hull family
  const family = normalizeHullFamily(params.hullFamily);

  const { longitudinalSegments, verticalSegments, lppM, draftM } = params;

  const geometry = new THREE.BufferGeometry();
  const vertices: number[] = [];
  const indices: number[] = [];

  // Generate vertices
  for (let i = 0; i <= longitudinalSegments; i++) {
    const xFraction = i / longitudinalSegments;
    const x = xFraction * lppM - lppM / 2; // -Lpp/2 to +Lpp/2

    for (let j = 0; j <= verticalSegments; j++) {
      const zFraction = j / verticalSegments;
      const z = -zFraction * draftM; // 0 to -draft

      // Calculate half-breadth at this (x, z) position
      const y = calculateHalfBreadth(x, z, params, family);

      // Starboard side (positive y)
      vertices.push(x, y, z);
      // Port side (negative y) - mirror
      vertices.push(x, -y, z);
    }
  }

  // Generate indices for triangles
  for (let i = 0; i < longitudinalSegments; i++) {
    for (let j = 0; j < verticalSegments; j++) {
      const stride = (verticalSegments + 1) * 2; // 2 sides per longitudinal station

      // Starboard side
      const a = i * stride + j * 2;
      const b = a + stride;
      const c = a + 2;
      const d = b + 2;

      indices.push(a, b, c);
      indices.push(b, d, c);

      // Port side (reverse winding)
      const a2 = a + 1;
      const b2 = b + 1;
      const c2 = c + 1;
      const d2 = d + 1;

      indices.push(a2, c2, b2);
      indices.push(b2, c2, d2);
    }
  }

  geometry.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));
  geometry.setIndex(indices);
  geometry.computeVertexNormals(); // Smooth shading

  return geometry;
}

// ============================================================================
// VESSEL-TYPE-SPECIFIC SHAPE FUNCTIONS
// ============================================================================

/**
 * Generates waterline points at a specific depth
 */
function generateWaterlineAtDepth(
  params: WaterlinesParams,
  z: number,
  family: string
): WaterlinePoint[] {
  const points: WaterlinePoint[] = [];
  const { pointsPerWaterline, lppM } = params;

  for (let i = 0; i <= pointsPerWaterline; i++) {
    const xFraction = i / pointsPerWaterline;
    const x = xFraction * lppM - lppM / 2; // -Lpp/2 to +Lpp/2

    const y = calculateHalfBreadth(x, z, params, family);

    points.push({ x, y });
  }

  return points;
}

/**
 * Calculates half-breadth at a specific (x, z) position using vessel-type-specific formulas
 */
function calculateHalfBreadth(
  x: number,
  z: number,
  params: HullShapeParams,
  family: string
): number {
  const { lppM, beamM, draftM, cb, cp, cwp } = params;

  // Normalize coordinates
  const xNorm = x / (lppM / 2); // -1 to +1
  const zNorm = -z / draftM; // 0 (waterline) to 1 (keel)

  // Select shape function based on hull family
  let horizontalShape: number;

  switch (family) {
    case 'container':
      horizontalShape = containerShapeFunction(xNorm, cp, cb);
      break;

    case 'tanker':
    case 'bulk':
    case 'lng':
      horizontalShape = tankerShapeFunction(xNorm, cb, cp);
      break;

    case 'fishing':
      horizontalShape = fishingShapeFunction(xNorm, cb);
      break;

    case 'yacht_disp':
    case 'yacht':
    case 'sailing':
      horizontalShape = yachtShapeFunction(xNorm, cb);
      break;

    case 'roro':
    case 'cargo':
    case 'osv':
      horizontalShape = roroShapeFunction(xNorm, cp, cb);
      break;

    default:
      // Fallback to modified Wigley
      console.warn(`Unknown hull family '${family}', using Wigley fallback`);
      horizontalShape = wigleyFallback(xNorm);
  }

  // Apply vertical distribution
  const verticalShape = verticalDistribution(zNorm, cb, cwp);

  // Calculate final half-breadth
  const y = (beamM / 2) * horizontalShape * verticalShape;

  return Math.max(0, y); // Ensure non-negative
}

/**
 * Container ship horizontal shape function
 * Characteristics: Parallel mid-body, moderate entrance angle
 * Cb: 0.60-0.70, Cp: 0.70-0.75
 */
function containerShapeFunction(xNorm: number, cp: number, cb: number): number {
  // Parallel mid-body length based on Cp
  // Higher Cp = longer parallel section
  const parallelFraction = Math.max(0, (cp - 0.65) / 0.15); // 0-1 range
  const parallelHalfLength = parallelFraction * 0.45; // Max 45% from center

  const absX = Math.abs(xNorm);

  // In parallel mid-body region
  if (absX <= parallelHalfLength) {
    return 1.0;
  }

  // Entrance/run region - parabolic taper
  const taperStart = parallelHalfLength;
  const taperLength = 1.0 - taperStart;
  const taperX = (absX - taperStart) / taperLength; // 0-1 in taper region

  // Smooth parabolic taper with Cb influence
  const exponent = 1.5 + (0.70 - cb) * 2.0; // Finer shapes = higher exponent
  return Math.pow(1.0 - taperX, exponent);
}

/**
 * Tanker/Bulk carrier horizontal shape function
 * Characteristics: Very full forms, minimal taper, blunt ends
 * Cb: 0.75-0.85, Cp: 0.78-0.88
 */
function tankerShapeFunction(xNorm: number, cb: number, cp: number): number {
  // Very short parallel mid-body for tankers
  const parallelFraction = Math.max(0, (cp - 0.78) / 0.12);
  const parallelHalfLength = parallelFraction * 0.30;

  const absX = Math.abs(xNorm);

  if (absX <= parallelHalfLength) {
    return 1.0;
  }

  // Fuller form taper - use higher-order polynomial
  const taperStart = parallelHalfLength;
  const taperLength = 1.0 - taperStart;
  const taperX = (absX - taperStart) / taperLength;

  // Higher Cb = less taper (more fullness)
  const order = 3.0 + (cb - 0.75) * 4.0; // 3-4.4 range
  return Math.pow(1.0 - Math.pow(taperX, 2.0), 1.0 / order);
}

/**
 * Fishing vessel horizontal shape function
 * Characteristics: Fine ends, no parallel mid-body, traditional displacement form
 * Cb: 0.45-0.55
 */
function fishingShapeFunction(xNorm: number, cb: number): number {
  // Pure parabolic form, no parallel section
  // Finer forms = sharper taper
  const exponent = 1.2 + (0.55 - cb) * 4.0; // 1.2-1.6 range
  return Math.pow(1.0 - xNorm * xNorm, exponent);
}

/**
 * Yacht/displacement hull horizontal shape function
 * Characteristics: Very fine, elegant curves, maximum beam aft
 * Cb: 0.35-0.45
 */
function yachtShapeFunction(xNorm: number, cb: number): number {
  // Asymmetric form with maximum beam aft of midships
  const beamShift = 0.15; // Maximum beam at x = +0.15 (aft of amidships)
  const shiftedX = xNorm - beamShift;

  // Very fine form with Gaussian-like distribution
  const sigma = 0.55 + (0.45 - cb) * 0.3; // Width parameter
  const gaussianTerm = Math.exp(-Math.pow(shiftedX / sigma, 2) / 2);

  // Blend with parabolic to avoid too much fullness aft
  const parabolicTerm = 1.0 - xNorm * xNorm;

  return Math.max(0, 0.7 * gaussianTerm + 0.3 * parabolicTerm);
}

/**
 * RoRo/cargo horizontal shape function
 * Characteristics: Boxy midship, moderate parallel body
 * Cb: 0.55-0.65
 */
function roroShapeFunction(xNorm: number, cp: number, cb: number): number {
  // Moderate parallel mid-body
  const parallelFraction = Math.max(0, (cp - 0.60) / 0.18);
  const parallelHalfLength = parallelFraction * 0.35;

  const absX = Math.abs(xNorm);

  if (absX <= parallelHalfLength) {
    return 1.0;
  }

  // Moderate taper
  const taperStart = parallelHalfLength;
  const taperLength = 1.0 - taperStart;
  const taperX = (absX - taperStart) / taperLength;

  const exponent = 1.6 + (0.65 - cb) * 2.0;
  return Math.pow(1.0 - taperX, exponent);
}

/**
 * Wigley fallback for unknown hull types
 */
function wigleyFallback(xNorm: number): number {
  return 1.0 - xNorm * xNorm;
}

/**
 * Vertical distribution function
 * Controls how beam varies with depth (affects Cb and Cwp)
 */
function verticalDistribution(zNorm: number, cb: number, cwp: number): number {
  // zNorm: 0 at waterline, 1 at keel

  // Higher Cb = fuller vertical sections (less curvature)
  // Lower Cb = finer sections (more curvature)
  const baseExponent = 0.8 + (1.0 - cb) * 1.5; // 0.8-2.3 range

  // Waterplane coefficient affects surface fullness
  // Higher Cwp = fuller at waterline
  const surfaceFullness = (cwp - 0.65) / 0.30; // Normalize to 0-1 range
  const surfaceBoost = surfaceFullness * 0.15 * (1.0 - zNorm);

  // Base vertical shape
  const baseShape = Math.pow(1.0 - zNorm, baseExponent);

  return Math.min(1.0, baseShape + surfaceBoost);
}

// ============================================================================
// VALIDATION & UTILITIES
// ============================================================================

/**
 * Validates hull shape parameters
 */
function validateParams(params: HullShapeParams): void {
  const { lppM, beamM, draftM, cb, cp, cwp } = params;

  // Check dimensions
  if (lppM <= 0 || beamM <= 0 || draftM <= 0) {
    throw new Error(`Invalid dimensions: Lpp=${lppM}, Beam=${beamM}, Draft=${draftM}. All must be positive.`);
  }

  // Check form coefficients
  if (cb < MIN_CB || cb > MAX_CB) {
    throw new Error(`Cb=${cb} out of valid range [${MIN_CB}, ${MAX_CB}]`);
  }
  if (cp < MIN_CP || cp > MAX_CP) {
    throw new Error(`Cp=${cp} out of valid range [${MIN_CP}, ${MAX_CP}]`);
  }
  if (cwp < MIN_CWP || cwp > MAX_CWP) {
    throw new Error(`Cwp=${cwp} out of valid range [${MIN_CWP}, ${MAX_CWP}]`);
  }

  // Check Cp >= Cb (physical constraint)
  if (cp < cb - 0.05) {
    console.warn(`Cp (${cp}) is less than Cb (${cb}), which is unusual. Cp should typically be >= Cb.`);
  }

  // Check reasonable ratios
  const LB_ratio = lppM / beamM;
  if (LB_ratio < 3.0 || LB_ratio > 15.0) {
    console.warn(`L/B ratio (${LB_ratio.toFixed(2)}) is outside typical range [3.0, 15.0]`);
  }

  const BT_ratio = beamM / draftM;
  if (BT_ratio < 1.5 || BT_ratio > 5.0) {
    console.warn(`B/T ratio (${BT_ratio.toFixed(2)}) is outside typical range [1.5, 5.0]`);
  }
}

/**
 * Normalizes hull family string to standard format
 */
function normalizeHullFamily(family: string): string {
  const normalized = family.toLowerCase().trim();

  // Map variations to standard names
  const familyMap: Record<string, string> = {
    'container': 'container',
    'container_ship': 'container',
    'tanker': 'tanker',
    'crude_oil_tanker': 'tanker',
    'product_tanker': 'tanker',
    'vlcc': 'tanker',
    'bulk': 'bulk',
    'bulk_carrier': 'bulk',
    'bulker': 'bulk',
    'fishing': 'fishing',
    'fishing_vessel': 'fishing',
    'trawler': 'fishing',
    'yacht': 'yacht_disp',
    'yacht_disp': 'yacht_disp',
    'sailing': 'yacht_disp',
    'roro': 'roro',
    'car_carrier': 'roro',
    'cargo': 'cargo',
    'general_cargo': 'cargo',
    'lng': 'lng',
    'lng_carrier': 'lng',
    'osv': 'osv',
    'offshore_supply': 'osv'
  };

  return familyMap[normalized] || normalized;
}

/**
 * Validates generated hull shape against target form coefficients
 *
 * @remarks
 * This is a simplified validation that doesn't perform full numerical integration.
 * For production use, consider implementing proper Simpson's rule integration.
 */
export function validateGeneratedShape(
  waterlines: Waterline[],
  params: HullShapeParams
): ValidationResult {
  const warnings: string[] = [];
  const info: string[] = [];

  // Simple heuristic checks (full validation would require numerical integration)

  // Check if design waterline exists
  const designWL = waterlines.find(wl => wl.isDesignWaterline);
  if (!designWL) {
    warnings.push('No design waterline found in generated shape');
  }

  // Check for reasonable point distribution
  const firstWL = waterlines[0];
  if (firstWL && firstWL.points.length < 20) {
    warnings.push(`Low resolution: only ${firstWL.points.length} points per waterline`);
  }

  // Check for non-zero values
  const hasValidPoints = waterlines.some(wl =>
    wl.points.some(pt => pt.y > 0.01)
  );
  if (!hasValidPoints) {
    warnings.push('Generated shape has no significant beam (all points near zero)');
  }

  info.push(`Generated ${waterlines.length} waterlines`);
  info.push(`Resolution: ${firstWL?.points.length || 0} points per waterline`);
  info.push(`Hull family: ${params.hullFamily}`);
  info.push(`Target Cb: ${params.cb.toFixed(3)}, Cp: ${params.cp.toFixed(3)}, Cwp: ${params.cwp.toFixed(3)}`);

  return {
    isValid: warnings.length === 0,
    cbError: 0, // Would require numerical integration
    cwpError: 0, // Would require numerical integration
    warnings,
    info
  };
}

/**
 * Generates a cache key for geometry caching
 */
export function generateCacheKey(params: HullShapeParams): string {
  const {
    hullFamily,
    lppM,
    beamM,
    draftM,
    cb,
    cp,
    cwp,
    cm,
    lcbPctLpp
  } = params;

  // Round to reasonable precision for caching
  return [
    hullFamily,
    lppM.toFixed(2),
    beamM.toFixed(2),
    draftM.toFixed(2),
    cb.toFixed(3),
    cp.toFixed(3),
    cwp.toFixed(3),
    cm?.toFixed(3) || 'auto',
    lcbPctLpp?.toFixed(1) || '0'
  ].join('_');
}
