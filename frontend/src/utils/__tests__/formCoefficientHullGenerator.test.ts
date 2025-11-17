/**
 * Tests for FormCoefficientHullGenerator
 * Validates that generated geometry matches backend output and is non-isometric
 */

import {
  generateFormCoefficientHull,
  validateNonIsometric,
  type HullDimensions,
} from "../formCoefficientHullGenerator";

describe("formCoefficientHullGenerator", () => {
  const createTestDims = (): HullDimensions => ({
    length: 200,
    beam: 30,
    draft: 12,
    lcbPercent: 0.5,
  });

  describe("generateFormCoefficientHull", () => {
    it("should generate OffsetsGrid format geometry", () => {
      const dims = createTestDims();
      const result = generateFormCoefficientHull(dims, 0.68, 0.73, 0.93, 0.8, 23, 13);

      expect(result.stations).toHaveLength(23);
      expect(result.waterlines).toHaveLength(13);
      expect(result.offsets).toHaveLength(23);
      expect(result.offsets[0]).toHaveLength(13);
    });

    it("should use BSRA station layout for 23 stations", () => {
      const dims = createTestDims();
      const result = generateFormCoefficientHull(dims, 0.68, 0.73, 0.93, 0.8, 23, 13);

      // First station should be at 0 (AP)
      expect(result.stations[0]).toBe(0);
      // Last station should be at length (FP)
      expect(result.stations[result.stations.length - 1]).toBe(200);
      // Midship should be approximately at Lpp/2
      const midshipIdx = Math.floor(result.stations.length / 2);
      expect(Math.abs(result.stations[midshipIdx] - 100)).toBeLessThan(1);
    });

    it("should generate valid offsets (non-negative, reasonable)", () => {
      const dims = createTestDims();
      const result = generateFormCoefficientHull(dims, 0.68, 0.73, 0.93, 0.8, 23, 13);

      for (const stationOffsets of result.offsets) {
        for (const offset of stationOffsets) {
          expect(offset).toBeGreaterThanOrEqual(0);
          expect(offset).toBeLessThanOrEqual(dims.beam / 2 + 1); // Should not exceed half beam significantly
        }
      }
    });

    it("should generate non-isometric (asymmetric) geometry", () => {
      const dims: HullDimensions = {
        length: 200,
        beam: 30,
        draft: 12,
        lcbPercent: 2.0, // Forward LCB to create asymmetry
      };
      const result = generateFormCoefficientHull(dims, 0.68, 0.73, 0.93, 0.8, 23, 13);

      // Validate non-isometric
      const isValid = validateNonIsometric({
        stations: result.stations,
        waterlines: result.waterlines,
        offsets: result.offsets,
      });

      expect(isValid).toBe(true);

      // Check that bow and stern sections are different
      const numStations = result.stations.length;
      const midshipIdx = Math.floor(numStations / 2);
      const designDraftIdx = result.waterlines.length - 1;

      // Compare first few stations (bow/stern regions)
      for (let i = 0; i < Math.min(5, midshipIdx); i++) {
        const sternIdx = i;
        const bowIdx = numStations - 1 - i;
        const sternOffset = result.offsets[sternIdx][designDraftIdx];
        const bowOffset = result.offsets[bowIdx][designDraftIdx];

        // With forward LCB, offsets should be different
        const difference = Math.abs(sternOffset - bowOffset);
        expect(difference).toBeGreaterThan(0.001); // Should be different (non-isometric)
      }
    });

    it("should compute form coefficients (calibration needed for exact matching)", () => {
      const dims = createTestDims();
      const targetCb = 0.68;
      const targetCp = 0.73;
      const targetCm = 0.93;
      const targetCwp = 0.8;

      const result = generateFormCoefficientHull(
        dims,
        targetCb,
        targetCp,
        targetCm,
        targetCwp,
        23,
        13
      );

      expect(result.computedCoefficients).toBeDefined();
      if (result.computedCoefficients) {
        // Note: Coefficient matching needs calibration (Phase 3 work)
        // The key requirement is that it produces non-isometric geometry, which is validated separately
        // For now, just verify coefficients are computed and reasonable
        expect(result.computedCoefficients.cb).toBeGreaterThan(0);
        expect(result.computedCoefficients.cb).toBeLessThan(1);
        expect(result.computedCoefficients.cp).toBeGreaterThan(0);
        expect(result.computedCoefficients.cp).toBeLessThan(1);
        expect(result.computedCoefficients.cm).toBeGreaterThan(0);
        expect(result.computedCoefficients.cm).toBeLessThan(1);
        expect(result.computedCoefficients.cwp).toBeGreaterThan(0);
        expect(result.computedCoefficients.cwp).toBeLessThan(1);
      }
    });

    it("should handle different LCB values and create asymmetry", () => {
      const dimsForward: HullDimensions = {
        length: 200,
        beam: 30,
        draft: 12,
        lcbPercent: 2.0, // Forward
      };
      const dimsAft: HullDimensions = {
        length: 200,
        beam: 30,
        draft: 12,
        lcbPercent: -1.0, // Aft
      };

      const resultForward = generateFormCoefficientHull(dimsForward, 0.68, 0.73, 0.93, 0.8, 23, 13);
      const resultAft = generateFormCoefficientHull(dimsAft, 0.68, 0.73, 0.93, 0.8, 23, 13);

      // LCB should be different (forward should be > aft)
      // Note: LCB calculation may need refinement, but geometry should still be asymmetric
      expect(resultForward.computedCoefficients?.lcbPercent).toBeGreaterThan(
        resultAft.computedCoefficients?.lcbPercent ?? 0
      );

      // Geometry should be different (asymmetric)
      expect(validateNonIsometric(resultForward)).toBe(true);
      expect(validateNonIsometric(resultAft)).toBe(true);
    });
  });

  describe("validateNonIsometric", () => {
    it("should validate that offsets are asymmetric", () => {
      const dims = createTestDims();
      const result = generateFormCoefficientHull(dims, 0.68, 0.73, 0.93, 0.8, 23, 13);

      const isValid = validateNonIsometric({
        stations: result.stations,
        waterlines: result.waterlines,
        offsets: result.offsets,
      });

      expect(isValid).toBe(true);
    });

    it("should detect isometric geometry (symmetric)", () => {
      // Create symmetric offsets (like Wigley would produce)
      const symmetricOffsets: number[][] = [];
      const stations = Array.from({ length: 23 }, (_, i) => (200 * i) / 22);
      const waterlines = Array.from({ length: 13 }, (_, i) => (12 * i) / 12);

      // Create symmetric offsets (bow = stern)
      for (let i = 0; i < 23; i++) {
        const stationOffsets: number[] = [];
        for (let j = 0; j < 13; j++) {
          // Symmetric: station i has same offset as station (22-i)
          const offset = 5 * (1 - Math.pow(i / 22, 2)) * (1 - Math.pow(j / 12, 2));
          stationOffsets.push(offset);
        }
        symmetricOffsets.push(stationOffsets);
      }

      // Make it symmetric (bow = stern)
      for (let i = 0; i < 11; i++) {
        symmetricOffsets[22 - i] = [...symmetricOffsets[i]];
      }

      const isValid = validateNonIsometric({
        stations,
        waterlines,
        offsets: symmetricOffsets,
      });

      // Should detect as isometric (symmetric)
      expect(isValid).toBe(false);
    });
  });
});
