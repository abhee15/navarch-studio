import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { ExportDialog } from "./ExportDialog";
import { exportApi } from "../../services/hydrostaticsApi";
import { toast } from "../common/Toast";

// Mock dependencies
jest.mock("../../services/hydrostaticsApi");
jest.mock("../common/Toast");

const mockExportApi = exportApi as jest.Mocked<typeof exportApi>;
const mockToast = toast as jest.Mocked<typeof toast>;

describe("ExportDialog", () => {
  const mockOnClose = jest.fn();
  const mockResults = [
    {
      draft: 5.0,
      dispVolume: 5000,
      dispWeight: 5125000,
      kBz: 2.5,
      lCBx: 50,
      tCBy: 0,
      bMt: 8.5,
      bMl: 150,
      gMt: 6.0,
      gMl: 147.5,
      awp: 1800,
      iwp: 25000,
      cb: 0.65,
      cp: 0.72,
      cm: 0.9,
      cwp: 0.9,
    },
  ];

  const defaultProps = {
    isOpen: true,
    onClose: mockOnClose,
    vesselId: "vessel-123",
    vesselName: "Test Vessel",
    loadcaseId: undefined,
    results: mockResults,
  };

  beforeEach(() => {
    jest.clearAllMocks();
    // Setup toast mocks
    mockToast.loading = jest.fn().mockReturnValue("toast-id");
    mockToast.success = jest.fn();
    mockToast.error = jest.fn();
  });

  describe("Rendering", () => {
    it("should not render when isOpen is false", () => {
      render(<ExportDialog {...defaultProps} isOpen={false} />);
      expect(screen.queryByText("Export Hydrostatic Data")).not.toBeInTheDocument();
    });

    it("should render when isOpen is true", () => {
      render(<ExportDialog {...defaultProps} />);
      expect(screen.getByText("Export Hydrostatic Data")).toBeInTheDocument();
    });

    it("should display vessel name in subtitle", () => {
      render(<ExportDialog {...defaultProps} />);
      expect(screen.getByText(/Test Vessel/)).toBeInTheDocument();
    });

    it("should render all format options", () => {
      render(<ExportDialog {...defaultProps} />);
      expect(screen.getByLabelText(/CSV/)).toBeInTheDocument();
      expect(screen.getByLabelText(/JSON/)).toBeInTheDocument();
      expect(screen.getByLabelText(/PDF/)).toBeInTheDocument();
      expect(screen.getByLabelText(/Excel/)).toBeInTheDocument();
    });

    it("should have CSV selected by default", () => {
      render(<ExportDialog {...defaultProps} />);
      const csvRadio = screen.getByLabelText(/CSV/) as HTMLInputElement;
      expect(csvRadio.checked).toBe(true);
    });

    it("should render include curves checkbox", () => {
      render(<ExportDialog {...defaultProps} />);
      expect(screen.getByLabelText(/Include hydrostatic curves/)).toBeInTheDocument();
    });

    it("should disable include curves checkbox for CSV", () => {
      render(<ExportDialog {...defaultProps} />);
      const checkbox = screen.getByLabelText(/Include hydrostatic curves/) as HTMLInputElement;
      expect(checkbox.disabled).toBe(true);
    });
  });

  describe("Format Selection", () => {
    it("should allow selecting different formats", () => {
      render(<ExportDialog {...defaultProps} />);

      const jsonRadio = screen.getByLabelText(/JSON/) as HTMLInputElement;
      fireEvent.click(jsonRadio);
      expect(jsonRadio.checked).toBe(true);

      const pdfRadio = screen.getByLabelText(/PDF/) as HTMLInputElement;
      fireEvent.click(pdfRadio);
      expect(pdfRadio.checked).toBe(true);
    });

    it("should enable include curves checkbox for PDF", () => {
      render(<ExportDialog {...defaultProps} />);

      const pdfRadio = screen.getByLabelText(/PDF/);
      fireEvent.click(pdfRadio);

      const checkbox = screen.getByLabelText(/Include hydrostatic curves/) as HTMLInputElement;
      expect(checkbox.disabled).toBe(false);
    });

    it("should enable include curves checkbox for Excel", () => {
      render(<ExportDialog {...defaultProps} />);

      const excelRadio = screen.getByLabelText(/Excel/);
      fireEvent.click(excelRadio);

      const checkbox = screen.getByLabelText(/Include hydrostatic curves/) as HTMLInputElement;
      expect(checkbox.disabled).toBe(false);
    });
  });

  describe("CSV Export", () => {
    it("should export CSV successfully", async () => {
      const mockBlob = new Blob(["csv data"], { type: "text/csv" });
      mockExportApi.exportCsv.mockResolvedValue(mockBlob);

      // Mock URL.createObjectURL
      global.URL.createObjectURL = jest.fn(() => "blob:mock-url");
      global.URL.revokeObjectURL = jest.fn();

      render(<ExportDialog {...defaultProps} />);

      const exportButton = screen.getByRole("button", { name: /Export/ });
      fireEvent.click(exportButton);

      await waitFor(() => {
        expect(mockExportApi.exportCsv).toHaveBeenCalledWith("vessel-123", mockResults);
      });

      expect(mockToast.loading).toHaveBeenCalledWith("Preparing export...");
      expect(mockToast.success).toHaveBeenCalled();
      expect(mockOnClose).toHaveBeenCalled();
    });

    it("should handle CSV export failure", async () => {
      mockExportApi.exportCsv.mockRejectedValue(new Error("Export failed"));

      render(<ExportDialog {...defaultProps} />);

      const exportButton = screen.getByRole("button", { name: /Export/ });
      fireEvent.click(exportButton);

      await waitFor(() => {
        expect(mockToast.error).toHaveBeenCalledWith(
          expect.stringContaining("Export failed"),
          expect.any(Object)
        );
      });

      expect(mockOnClose).not.toHaveBeenCalled();
    });
  });

  describe("JSON Export", () => {
    it("should export JSON successfully", async () => {
      const mockBlob = new Blob(["json data"], { type: "application/json" });
      mockExportApi.exportJson.mockResolvedValue(mockBlob);

      global.URL.createObjectURL = jest.fn(() => "blob:mock-url");
      global.URL.revokeObjectURL = jest.fn();

      render(<ExportDialog {...defaultProps} />);

      // Select JSON format
      const jsonRadio = screen.getByLabelText(/JSON/);
      fireEvent.click(jsonRadio);

      const exportButton = screen.getByRole("button", { name: /Export/ });
      fireEvent.click(exportButton);

      await waitFor(() => {
        expect(mockExportApi.exportJson).toHaveBeenCalledWith("vessel-123", mockResults);
      });

      expect(mockToast.success).toHaveBeenCalled();
      expect(mockOnClose).toHaveBeenCalled();
    });
  });

  describe("PDF Export", () => {
    it("should export PDF successfully without curves", async () => {
      const mockBlob = new Blob(["pdf data"], { type: "application/pdf" });
      mockExportApi.exportPdf.mockResolvedValue(mockBlob);

      global.URL.createObjectURL = jest.fn(() => "blob:mock-url");
      global.URL.revokeObjectURL = jest.fn();

      render(<ExportDialog {...defaultProps} />);

      // Select PDF format
      const pdfRadio = screen.getByLabelText(/PDF/);
      fireEvent.click(pdfRadio);

      const exportButton = screen.getByRole("button", { name: /Export/ });
      fireEvent.click(exportButton);

      await waitFor(() => {
        expect(mockExportApi.exportPdf).toHaveBeenCalledWith("vessel-123", undefined, false);
      });

      expect(mockToast.loading).toHaveBeenCalledWith(
        "Generating PDF report...",
        expect.any(Object)
      );
      expect(mockToast.success).toHaveBeenCalled();
      expect(mockOnClose).toHaveBeenCalled();
    });

    it("should export PDF with curves when checked", async () => {
      const mockBlob = new Blob(["pdf data"], { type: "application/pdf" });
      mockExportApi.exportPdf.mockResolvedValue(mockBlob);

      global.URL.createObjectURL = jest.fn(() => "blob:mock-url");
      global.URL.revokeObjectURL = jest.fn();

      render(<ExportDialog {...defaultProps} />);

      // Select PDF format
      const pdfRadio = screen.getByLabelText(/PDF/);
      fireEvent.click(pdfRadio);

      // Check include curves
      const checkbox = screen.getByLabelText(/Include hydrostatic curves/);
      fireEvent.click(checkbox);

      const exportButton = screen.getByRole("button", { name: /Export/ });
      fireEvent.click(exportButton);

      await waitFor(() => {
        expect(mockExportApi.exportPdf).toHaveBeenCalledWith("vessel-123", undefined, true);
      });
    });
  });

  describe("Excel Export", () => {
    it("should export Excel successfully", async () => {
      const mockBlob = new Blob(["excel data"], {
        type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      });
      mockExportApi.exportExcel.mockResolvedValue(mockBlob);

      global.URL.createObjectURL = jest.fn(() => "blob:mock-url");
      global.URL.revokeObjectURL = jest.fn();

      render(<ExportDialog {...defaultProps} />);

      // Select Excel format
      const excelRadio = screen.getByLabelText(/Excel/);
      fireEvent.click(excelRadio);

      const exportButton = screen.getByRole("button", { name: /Export/ });
      fireEvent.click(exportButton);

      await waitFor(() => {
        expect(mockExportApi.exportExcel).toHaveBeenCalledWith("vessel-123", undefined, false);
      });

      expect(mockToast.loading).toHaveBeenCalledWith(
        "Generating Excel workbook...",
        expect.any(Object)
      );
      expect(mockToast.success).toHaveBeenCalled();
      expect(mockOnClose).toHaveBeenCalled();
    });

    it("should pass loadcaseId if provided", async () => {
      const mockBlob = new Blob(["excel data"], {
        type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      });
      mockExportApi.exportExcel.mockResolvedValue(mockBlob);

      global.URL.createObjectURL = jest.fn(() => "blob:mock-url");
      global.URL.revokeObjectURL = jest.fn();

      render(<ExportDialog {...defaultProps} loadcaseId="loadcase-123" />);

      // Select Excel format
      const excelRadio = screen.getByLabelText(/Excel/);
      fireEvent.click(excelRadio);

      const exportButton = screen.getByRole("button", { name: /Export/ });
      fireEvent.click(exportButton);

      await waitFor(() => {
        expect(mockExportApi.exportExcel).toHaveBeenCalledWith("vessel-123", "loadcase-123", false);
      });
    });
  });

  describe("Dialog Controls", () => {
    it("should call onClose when Cancel button is clicked", () => {
      render(<ExportDialog {...defaultProps} />);

      const cancelButton = screen.getByRole("button", { name: /Cancel/ });
      fireEvent.click(cancelButton);

      expect(mockOnClose).toHaveBeenCalled();
    });

    it("should call onClose when close button (X) is clicked", () => {
      render(<ExportDialog {...defaultProps} />);

      const closeButton = screen.getByRole("button", { name: /Close/ });
      fireEvent.click(closeButton);

      expect(mockOnClose).toHaveBeenCalled();
    });

    it("should disable export button while exporting", async () => {
      const mockBlob = new Blob(["csv data"], { type: "text/csv" });
      mockExportApi.exportCsv.mockImplementation(
        () => new Promise((resolve) => setTimeout(() => resolve(mockBlob), 100))
      );

      render(<ExportDialog {...defaultProps} />);

      const exportButton = screen.getByRole("button", { name: /Export/ });
      fireEvent.click(exportButton);

      // Button should be disabled immediately
      expect(exportButton).toBeDisabled();

      await waitFor(() => {
        expect(mockOnClose).toHaveBeenCalled();
      });
    });
  });

  describe("File Download", () => {
    it("should create download link with correct filename for CSV", async () => {
      const mockBlob = new Blob(["csv data"], { type: "text/csv" });
      mockExportApi.exportCsv.mockResolvedValue(mockBlob);

      global.URL.createObjectURL = jest.fn(() => "blob:mock-url");
      global.URL.revokeObjectURL = jest.fn();

      const createElementSpy = jest.spyOn(document, "createElement");
      const appendChildSpy = jest.spyOn(document.body, "appendChild");

      render(<ExportDialog {...defaultProps} />);

      const exportButton = screen.getByRole("button", { name: /Export/ });
      fireEvent.click(exportButton);

      await waitFor(() => {
        expect(createElementSpy).toHaveBeenCalledWith("a");
      });

      const link = createElementSpy.mock.results[0].value as HTMLAnchorElement;
      expect(link.download).toBe("Test_Vessel_hydrostatics.csv");
      expect(link.href).toBe("blob:mock-url");
      expect(appendChildSpy).toHaveBeenCalledWith(link);
    });

    it("should clean up object URL after download", async () => {
      const mockBlob = new Blob(["csv data"], { type: "text/csv" });
      mockExportApi.exportCsv.mockResolvedValue(mockBlob);

      const mockUrl = "blob:mock-url";
      global.URL.createObjectURL = jest.fn(() => mockUrl);
      global.URL.revokeObjectURL = jest.fn();

      render(<ExportDialog {...defaultProps} />);

      const exportButton = screen.getByRole("button", { name: /Export/ });
      fireEvent.click(exportButton);

      await waitFor(() => {
        expect(global.URL.revokeObjectURL).toHaveBeenCalledWith(mockUrl);
      });
    });
  });
});
