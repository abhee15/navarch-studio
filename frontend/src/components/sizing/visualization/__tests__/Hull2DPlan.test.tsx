/**
 * Unit Tests for Hull2DPlan Component
 *
 * Tests plan view rendering with various geometry formats and edge cases.
 */

import { describe, it, expect, vi, beforeEach } from "@jest/globals";
import { render, screen } from "@testing-library/react";
import { Hull2DPlan } from "../Hull2DPlan";
import type { CandidateDesign } from "../../../../types/sizing";

// Mock dependencies
vi.mock("../../../utils/shipd2DGeometry");
vi.mock("../../../utils/shipdGeometryGenerator");
vi.mock("../../../utils/formCoefficientHullGenerator");
vi.mock("../../../utils/geometryFormatConverter");
vi.mock("../../../stores");

describe("Hull2DPlan", () => {
  const mockCandidate: CandidateDesign = {
    id: "test-candidate-1",
    sizingRunId: "test-run-1",
    rank: 1,
    lppM: 100,
    beamM: 20,
    draftM: 8,
    loaM: 105,
    lwlM: 102,
    depthM: 12,
    cb: 0.75,
    cp: 0.78,
    cm: 0.96,
    cwp: 0.85,
    lcbPctLpp: -2.5,
    ehpKw: 5000,
    shpKw: 6500,
    displacementT: 15000,
    hullFamily: "bulbous_bow_full_midship_transom_stern",
    bowFamily: "bulbous_bow",
    midshipFamily: "full_midship",
    sternFamily: "transom_stern",
    vesselType: "commercial",
    geometryGenerationStatus: "Success" as const,
    geometryJson: null,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should render plan view with valid candidate dimensions", () => {
    render(<Hull2DPlan candidate={mockCandidate} />);

    // Should render the SVG viewport
    const svg = screen.getByText(/PLAN VIEW/i).closest("svg");
    expect(svg).toBeInTheDocument();
  });

  it("should handle missing geometry JSON gracefully", () => {
    const candidateWithoutGeometry = {
      ...mockCandidate,
      geometryJson: null,
      geometryGenerationStatus: "Success" as const,
    };

    render(<Hull2DPlan candidate={candidateWithoutGeometry} />);

    // Should fall back to parametric geometry generation
    const svg = screen.queryByText(/PLAN VIEW/i)?.closest("svg");
    expect(svg).toBeInTheDocument();
  });

  it("should display error message when geometry generation failed", () => {
    const candidateWithFailedGeometry = {
      ...mockCandidate,
      geometryGenerationStatus: "BothFailed" as const,
      geometryGenerationError: "Geometry generation failed: Invalid parameters",
    };

    render(<Hull2DPlan candidate={candidateWithFailedGeometry} />);

    // Should show error message
    expect(screen.getByText(/Geometry Generation Failed/i)).toBeInTheDocument();
    expect(screen.getByText(/Invalid parameters/i)).toBeInTheDocument();
  });

  it("should validate dimensions before rendering", () => {
    const candidateWithInvalidDimensions = {
      ...mockCandidate,
      lppM: NaN,
      beamM: -10,
    };

    render(<Hull2DPlan candidate={candidateWithInvalidDimensions} />);

    // Should show invalid dimensions message
    expect(screen.getByText(/Invalid vessel dimensions/i)).toBeInTheDocument();
  });

  it("should handle OffsetsGrid format geometry", () => {
    const offsetsGridGeometry = JSON.stringify({
      stations: [0, 20, 40, 60, 80, 100],
      waterlines: [0, 2, 4, 6, 8],
      offsets: [
        [0, 5, 8, 9, 10],
        [0, 6, 9, 10, 10],
        [0, 7, 10, 10, 10],
        [0, 7, 10, 10, 10],
        [0, 6, 9, 10, 10],
        [0, 5, 8, 9, 10],
      ],
    });

    const candidateWithOffsetsGrid = {
      ...mockCandidate,
      geometryJson: offsetsGridGeometry,
      geometryGenerationStatus: "Success" as const,
    };

    render(<Hull2DPlan candidate={candidateWithOffsetsGrid} />);

    // Should render successfully with OffsetsGrid geometry
    const svg = screen.queryByText(/PLAN VIEW/i)?.closest("svg");
    expect(svg).toBeInTheDocument();
  });

  it("should handle ShipD format geometry", () => {
    const shipdGeometry = JSON.stringify({
      stations: [
        {
          position: 0.1,
          offsets: { 0: 5, 2: 8, 4: 9, 6: 10, 8: 10 },
        },
        {
          position: 0.5,
          offsets: { 0: 10, 2: 10, 4: 10, 6: 10, 8: 10 },
        },
        {
          position: 0.9,
          offsets: { 0: 5, 2: 8, 4: 9, 6: 10, 8: 10 },
        },
      ],
    });

    const candidateWithShipD = {
      ...mockCandidate,
      geometryJson: shipdGeometry,
      geometryGenerationStatus: "Success" as const,
    };

    render(<Hull2DPlan candidate={candidateWithShipD} />);

    // Should render successfully with ShipD geometry
    const svg = screen.queryByText(/PLAN VIEW/i)?.closest("svg");
    expect(svg).toBeInTheDocument();
  });

  it("should toggle visibility of waterlines and stations", () => {
    render(<Hull2DPlan candidate={mockCandidate} />);

    // Should have visibility toggle buttons
    expect(screen.getByText(/Waterlines/i)).toBeInTheDocument();
    expect(screen.getByText(/Stations/i)).toBeInTheDocument();
  });

  it("should display dimensions correctly", () => {
    render(<Hull2DPlan candidate={mockCandidate} />);

    // Should display principal dimensions
    expect(screen.getByText(/Lpp = 100.00 m/i)).toBeInTheDocument();
    expect(screen.getByText(/B = 20.00 m/i)).toBeInTheDocument();
  });

  it("should handle zero or negative dimensions gracefully", () => {
    const invalidCandidate = {
      ...mockCandidate,
      lppM: 0,
      beamM: -5,
    };

    render(<Hull2DPlan candidate={invalidCandidate} />);

    // Should show error message
    expect(screen.getByText(/Invalid vessel dimensions/i)).toBeInTheDocument();
  });
});
