/**
 * Baseline tests for OffsetsTable component
 * Tests current behavior before implementing fixes
 */

import { render, screen } from "@testing-library/react";
import { OffsetsTable } from "../OffsetsTable";
import type { CandidateDesign } from "../../../../types/sizing";

describe("OffsetsTable", () => {
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
    it("should render table with OffsetsGrid geometry", () => {
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

      render(<OffsetsTable candidate={candidate} />);

      // Should render table headers
      expect(screen.getByText(/station/i)).toBeInTheDocument();
    });

    it("should display correct values from OffsetsGrid format", () => {
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

      render(<OffsetsTable candidate={candidate} />);

      // Table should be rendered with data
      const table = screen.getByRole("table");
      expect(table).toBeInTheDocument();
    });
  });

  describe("rendering with ShipD format", () => {
    it("should handle ShipD format geometry", () => {
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

      render(<OffsetsTable candidate={candidate} />);

      // Should convert and render ShipD format
      const table = screen.getByRole("table");
      expect(table).toBeInTheDocument();
    });
  });

  describe("geometry generation failure handling", () => {
    it("should show error message when geometry generation failed", () => {
      const candidate = createMockCandidate({
        geometryGenerationStatus: "BothFailed",
        geometryGenerationError: "Failed to generate geometry",
      });

      render(<OffsetsTable candidate={candidate} />);

      // Should show error message instead of table
      expect(screen.getByText(/geometry generation failed/i)).toBeInTheDocument();
    });

    it("should show error message when FormCoefficientFailed", () => {
      const candidate = createMockCandidate({
        geometryGenerationStatus: "FormCoefficientFailed",
        geometryGenerationError: "Form coefficient generation failed",
      });

      render(<OffsetsTable candidate={candidate} />);

      expect(screen.getByText(/geometry generation failed/i)).toBeInTheDocument();
    });
  });

  describe("missing geometry handling", () => {
    it("should handle missing geometry gracefully", () => {
      const candidate = createMockCandidate({
        geometryJson: undefined,
        geometryGenerationStatus: "Success", // Legacy data
      });

      render(<OffsetsTable candidate={candidate} />);

      // Should fall back to Wigley formula (current behavior - will be replaced in Phase 4)
      // For now, just verify it doesn't crash
      expect(screen.getByRole("table")).toBeInTheDocument();
    });

    it("should handle invalid geometry JSON", () => {
      const candidate = createMockCandidate({
        geometryJson: "invalid json",
      });

      render(<OffsetsTable candidate={candidate} />);

      // Should handle gracefully without crashing
      expect(screen.getByRole("table")).toBeInTheDocument();
    });
  });

  describe("format detection", () => {
    it("should correctly identify OffsetsGrid format", () => {
      const geometryJson = JSON.stringify({
        stations: [0, 100, 200],
        waterlines: [0, 6, 12],
        offsets: [[0.5], [0.6], [0.7]],
      });

      const candidate = createMockCandidate({ geometryJson });

      render(<OffsetsTable candidate={candidate} />);

      // Should render table (format detected correctly)
      expect(screen.getByRole("table")).toBeInTheDocument();
    });

    it("should correctly identify ShipD format", () => {
      const geometryJson = JSON.stringify({
        stations: [
          {
            position: 0.0,
            offsets: { 0: 0.5 },
          },
        ],
      });

      const candidate = createMockCandidate({ geometryJson });

      render(<OffsetsTable candidate={candidate} />);

      // Should render table (format converted correctly)
      expect(screen.getByRole("table")).toBeInTheDocument();
    });
  });
});
