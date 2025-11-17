/**
 * Tests for Geometry Format Converter
 */

import {
  detectGeometryFormat,
  convertShipDToOffsetsGrid,
  convertOffsetsGridToShipD,
  normalizeGeometry,
  type OffsetsGrid,
  type ShipDGeometry,
} from "../geometryFormatConverter";

describe("geometryFormatConverter", () => {
  describe("detectGeometryFormat", () => {
    it("should detect OffsetsGrid format", () => {
      const geometryJson = JSON.stringify({
        stations: [0, 1, 2],
        waterlines: [0, 1, 2],
        offsets: [
          [0.5, 0.6, 0.7],
          [0.5, 0.6, 0.7],
          [0.5, 0.6, 0.7],
        ],
      });

      expect(detectGeometryFormat(geometryJson)).toBe("offsetsgrid");
    });

    it("should detect ShipD format", () => {
      const geometryJson = JSON.stringify({
        stations: [
          {
            position: 0.0,
            offsets: { 0: 0.5, 1: 0.6 },
          },
        ],
      });

      expect(detectGeometryFormat(geometryJson)).toBe("shipd");
    });

    it("should return unknown for invalid format", () => {
      const geometryJson = JSON.stringify({ invalid: "data" });
      expect(detectGeometryFormat(geometryJson)).toBe("unknown");
    });

    it("should return unknown for invalid JSON", () => {
      expect(detectGeometryFormat("not json")).toBe("unknown");
    });
  });

  describe("convertShipDToOffsetsGrid", () => {
    it("should convert ShipD format to OffsetsGrid", () => {
      const shipdData: ShipDGeometry = {
        stations: [
          {
            position: 0.0,
            offsets: { 0: 0.5, 1: 0.6, 2: 0.7 },
          },
          {
            position: 0.5,
            offsets: { 0: 0.8, 1: 0.9, 2: 1.0 },
          },
          {
            position: 1.0,
            offsets: { 0: 0.3, 1: 0.4, 2: 0.5 },
          },
        ],
        stationPositions: [0.0, 0.5, 1.0],
      };

      const result = convertShipDToOffsetsGrid(shipdData);

      expect(result.stations).toEqual([0.0, 0.5, 1.0]);
      expect(result.waterlines).toEqual([0, 1, 2]);
      expect(result.offsets).toHaveLength(3);
      expect(result.offsets[0]).toEqual([0.5, 0.6, 0.7]);
      expect(result.offsets[1]).toEqual([0.8, 0.9, 1.0]);
      expect(result.offsets[2]).toEqual([0.3, 0.4, 0.5]);
    });

    it("should handle stations with different waterline heights", () => {
      const shipdData: ShipDGeometry = {
        stations: [
          {
            position: 0.0,
            offsets: { 0: 0.5, 1: 0.6 },
          },
          {
            position: 1.0,
            offsets: { 0: 0.3, 1: 0.4, 2: 0.5 },
          },
        ],
      };

      const result = convertShipDToOffsetsGrid(shipdData);

      expect(result.waterlines).toEqual([0, 1, 2]);
      // First station doesn't have waterline 2, so it should extrapolate from waterline 1 (0.6)
      expect(result.offsets[0][2]).toBeCloseTo(0.6, 1); // Extrapolated from last known value
      // Second station has all waterlines
      expect(result.offsets[1][2]).toBe(0.5);
    });

    it("should throw error for empty stations", () => {
      const shipdData: ShipDGeometry = {
        stations: [],
      };

      expect(() => convertShipDToOffsetsGrid(shipdData)).toThrow();
    });

    it("should use station positions from stations array if stationPositions not provided", () => {
      const shipdData: ShipDGeometry = {
        stations: [
          {
            position: 0.2,
            offsets: { 0: 0.5 },
          },
          {
            position: 0.8,
            offsets: { 0: 0.6 },
          },
        ],
      };

      const result = convertShipDToOffsetsGrid(shipdData);

      expect(result.stations).toEqual([0.2, 0.8]);
    });
  });

  describe("convertOffsetsGridToShipD", () => {
    it("should convert OffsetsGrid format to ShipD", () => {
      const offsetsGrid: OffsetsGrid = {
        stations: [0.0, 0.5, 1.0],
        waterlines: [0, 1, 2],
        offsets: [
          [0.5, 0.6, 0.7],
          [0.8, 0.9, 1.0],
          [0.3, 0.4, 0.5],
        ],
      };

      const result = convertOffsetsGridToShipD(offsetsGrid);

      expect(result.stations).toHaveLength(3);
      expect(result.stations[0].position).toBe(0.0);
      expect(result.stations[0].offsets[0]).toBe(0.5);
      expect(result.stations[0].offsets[1]).toBe(0.6);
      expect(result.stations[0].offsets[2]).toBe(0.7);
      expect(result.stationPositions).toEqual([0.0, 0.5, 1.0]);
    });

    it("should handle missing offsets gracefully", () => {
      const offsetsGrid: OffsetsGrid = {
        stations: [0.0, 0.5],
        waterlines: [0, 1],
        offsets: [[0.5, 0.6], []], // Second station has no offsets
      };

      const result = convertOffsetsGridToShipD(offsetsGrid);

      expect(result.stations).toHaveLength(2);
      expect(result.stations[1].offsets[0]).toBe(0);
    });
  });

  describe("normalizeGeometry", () => {
    it("should normalize OffsetsGrid format", () => {
      const geometryJson = JSON.stringify({
        stations: [0, 1, 2],
        waterlines: [0, 1, 2],
        offsets: [
          [0.5, 0.6, 0.7],
          [0.5, 0.6, 0.7],
          [0.5, 0.6, 0.7],
        ],
      });

      const result = normalizeGeometry(geometryJson);

      expect(result).not.toBeNull();
      expect(result?.stations).toEqual([0, 1, 2]);
      expect(result?.waterlines).toEqual([0, 1, 2]);
    });

    it("should normalize ShipD format by converting to OffsetsGrid", () => {
      const geometryJson = JSON.stringify({
        stations: [
          {
            position: 0.0,
            offsets: { 0: 0.5, 1: 0.6 },
          },
          {
            position: 1.0,
            offsets: { 0: 0.3, 1: 0.4 },
          },
        ],
      });

      const result = normalizeGeometry(geometryJson);

      expect(result).not.toBeNull();
      expect(result?.stations).toEqual([0.0, 1.0]);
      expect(result?.waterlines).toEqual([0, 1]);
      expect(result?.offsets).toHaveLength(2);
    });

    it("should return null for invalid JSON", () => {
      const result = normalizeGeometry("not json");
      expect(result).toBeNull();
    });

    it("should return null for unknown format", () => {
      const geometryJson = JSON.stringify({ invalid: "data" });
      const result = normalizeGeometry(geometryJson);
      expect(result).toBeNull();
    });

    it("should return null for empty string", () => {
      const result = normalizeGeometry("");
      expect(result).toBeNull();
    });
  });

  describe("round-trip conversion", () => {
    it("should maintain data integrity when converting ShipD -> OffsetsGrid -> ShipD", () => {
      const original: ShipDGeometry = {
        stations: [
          {
            position: 0.0,
            offsets: { 0: 0.5, 1: 0.6, 2: 0.7 },
          },
          {
            position: 0.5,
            offsets: { 0: 0.8, 1: 0.9, 2: 1.0 },
          },
        ],
        stationPositions: [0.0, 0.5],
      };

      const offsetsGrid = convertShipDToOffsetsGrid(original);
      const convertedBack = convertOffsetsGridToShipD(offsetsGrid);

      expect(convertedBack.stations).toHaveLength(2);
      expect(convertedBack.stations[0].position).toBe(0.0);
      expect(convertedBack.stations[0].offsets[0]).toBe(0.5);
      expect(convertedBack.stations[0].offsets[1]).toBe(0.6);
      expect(convertedBack.stations[0].offsets[2]).toBe(0.7);
    });
  });
});
