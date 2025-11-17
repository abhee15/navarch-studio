/**
 * Baseline tests for Hull2DSections component
 * Tests current behavior before implementing fixes
 */

import { render } from "@testing-library/react";
import { Hull2DSections } from "../Hull2DSections";
import type { CandidateDesign } from "../../../../types/sizing";

// Mock the sizing store
jest.mock("../../../../stores/SizingStore", () => ({
  useSizingStore: () => ({
    shipdParameters: [],
  }),
}));

describe("Hull2DSections", () => {
  const createMockCandidate = (overrides?: Partial<CandidateDesign>): CandidateDesign => ({
    id: "test-candidate-1",
    sizingRunId: "test-run-1",
    userId: "test-user",
    tenantId: "test-tenant",
    hullFamily: "container",
    lppM: 200,
    lwlM: 205,
    loaM: 210,
    beamM: 30,
    draftM: 12,
    depthM: 15,
    cb: 0.68,
    cp: 0.73,
    cwp: 0.8,
    cm: 0.93,
    dispT: 50000,
    fn: 0.25,
    flagsJson: "{}",
    score: 85,
    rank: 1,
    isSelected: false,
    createdAt: "2025-01-01T00:00:00Z",
    ...overrides,
  });

  describe("rendering with OffsetsGrid format", () => {
    it("should render sections from OffsetsGrid geometry", () => {
      const geometryJson = JSON.stringify({
        stations: [0, 50, 100, 150, 200],
        waterlines: [0, 3, 6, 9, 12],
        offsets: [
          [0.5, 0.6, 0.7, 0.8, 0.9],
          [0.5, 0.6, 0.7, 0.8, 0.9],
          [0.5, 0.6, 0.7, 0.8, 0.9],
          [0.5, 0.6, 0.7, 0.8, 0.9],
          [0.5, 0.6, 0.7, 0.8, 0.9],
        ],
      });

      const candidate = createMockCandidate({ geometryJson });

      const { container } = render(<Hull2DSections candidate={candidate} />);

      // Should render SVG with sections
      const svg = container.querySelector("svg");
      expect(svg).toBeInTheDocument();
    });

    it("should extract sections correctly from OffsetsGrid", () => {
      const geometryJson = JSON.stringify({
        stations: [0, 100, 200],
        waterlines: [0, 6, 12],
        offsets: [
          [0.5, 0.6, 0.7],
          [0.8, 0.9, 1.0],
          [0.3, 0.4, 0.5],
        ],
      });

      const candidate = createMockCandidate({ geometryJson });

      const { container } = render(<Hull2DSections candidate={candidate} />);

      // Should have path elements for sections
      const paths = container.querySelectorAll("path");
      expect(paths.length).toBeGreaterThan(0);
    });
  });

  describe("rendering with ShipD format", () => {
    it("should render sections from ShipD geometry", () => {
      const geometryJson = JSON.stringify({
        stations: [
          {
            position: 0.0,
            offsets: { 0: 0.5, 6: 0.6, 12: 0.7 },
          },
          {
            position: 0.5,
            offsets: { 0: 0.8, 6: 0.9, 12: 1.0 },
          },
          {
            position: 1.0,
            offsets: { 0: 0.3, 6: 0.4, 12: 0.5 },
          },
        ],
      });

      const candidate = createMockCandidate({ geometryJson });

      const { container } = render(<Hull2DSections candidate={candidate} />);

      // Should render SVG
      const svg = container.querySelector("svg");
      expect(svg).toBeInTheDocument();
    });
  });

  describe("missing geometry handling", () => {
    it("should handle missing geometry gracefully", () => {
      const candidate = createMockCandidate({
        geometryJson: undefined,
      });

      const { container } = render(<Hull2DSections candidate={candidate} />);

      // Should render SVG (may be empty or show fallback)
      const svg = container.querySelector("svg");
      expect(svg).toBeInTheDocument();
    });

    it("should handle invalid geometry JSON", () => {
      const candidate = createMockCandidate({
        geometryJson: "invalid json",
      });

      const { container } = render(<Hull2DSections candidate={candidate} />);

      // Should handle gracefully without crashing
      const svg = container.querySelector("svg");
      expect(svg).toBeInTheDocument();
    });
  });

  describe("smoothness behavior", () => {
    it("should render sections with spline interpolation for smooth curves", () => {
      const geometryJson = JSON.stringify({
        stations: [0, 50, 100, 150, 200],
        waterlines: [0, 3, 6, 9, 12],
        offsets: [
          [0.5, 0.6, 0.7, 0.8, 0.9],
          [0.5, 0.6, 0.7, 0.8, 0.9],
          [0.5, 0.6, 0.7, 0.8, 0.9],
          [0.5, 0.6, 0.7, 0.8, 0.9],
          [0.5, 0.6, 0.7, 0.8, 0.9],
        ],
      });

      const candidate = createMockCandidate({ geometryJson });

      const { container } = render(<Hull2DSections candidate={candidate} />);

      // Check that paths are generated (spline interpolation creates many points)
      const paths = container.querySelectorAll("path");
      if (paths.length > 0) {
        const pathData = paths[0].getAttribute("d") || "";
        // Should have many points (80 interpolated points from spline)
        const pointCount = (pathData.match(/[ML]/g) || []).length;
        expect(pointCount).toBeGreaterThan(10); // Should have many points from interpolation
      }
    });
  });
});
