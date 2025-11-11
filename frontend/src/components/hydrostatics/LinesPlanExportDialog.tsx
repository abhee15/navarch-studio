import { useState } from "react";
import { observer } from "mobx-react-lite";
import type { VesselDetails } from "../../types/hydrostatics";
import type { LinesPlanExportOptions, IgesExportOptions } from "../../types/linesplan";
import { exportApi } from "../../services/hydrostaticsApi";

interface LinesPlanExportDialogProps {
  vesselId: string;
  vessel: VesselDetails;
  onExport: (options: LinesPlanExportOptions) => Promise<void>;
  onClose: () => void;
}

export const LinesPlanExportDialog = observer(
  ({ vesselId, vessel, onExport, onClose }: LinesPlanExportDialogProps) => {
    const [options, setOptions] = useState<LinesPlanExportOptions>({
      paperSize: "A1",
      scale: "1:100",
      orientation: "Landscape",
      includeTitleBlock: true,
      includeGrid: true,
      includeOffsetsTable: true,
      includeSectionAreaCurve: true,
      includeDiagonals: true,
      quality: "Final",
      colorMode: true,
      watermark: "",
    });

    const [exportFormat, setExportFormat] = useState<"PDF" | "SVG" | "IGES">("PDF");
    const [exporting, setExporting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleExport = async () => {
      setExporting(true);
      setError(null);

      try {
        if (exportFormat === "IGES") {
          const igesOptions: IgesExportOptions = {
            includeStations: true,
            includeWaterlines: true,
            includeButtocks: true,
            includeDiagonals: options.includeDiagonals,
            version: "5.3",
          };

          const blob = await exportApi.exportIges(vesselId, igesOptions);

          // Download IGES file
          const url = URL.createObjectURL(blob);
          const link = document.createElement("a");
          link.href = url;
          link.download = `hull_${vessel.name.replace(/\s+/g, "_")}_${Date.now()}.igs`;
          document.body.appendChild(link);
          link.click();
          document.body.removeChild(link);
          URL.revokeObjectURL(url);
        } else {
          await onExport(options);
        }

        onClose();
      } catch (err) {
        console.error("Export failed:", err);
        setError(err instanceof Error ? err.message : "Export failed");
      } finally {
        setExporting(false);
      }
    };

    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
        <div className="bg-card rounded-lg shadow-xl max-w-2xl w-full p-6 max-h-[90vh] overflow-y-auto">
          <h2 className="text-lg font-semibold text-foreground mb-4">Export Lines Plan</h2>

          {/* Format Selection */}
          <div className="mb-4">
            <label className="block text-sm font-medium text-foreground mb-2">Export Format</label>
            <div className="flex space-x-2">
              {(["PDF", "SVG", "IGES"] as const).map((format) => (
                <button
                  key={format}
                  onClick={() => setExportFormat(format)}
                  className={`px-4 py-2 rounded text-sm font-medium transition-colors ${
                    exportFormat === format
                      ? "bg-primary text-primary-foreground"
                      : "bg-muted text-muted-foreground hover:bg-muted/80"
                  }`}
                >
                  {format}
                </button>
              ))}
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              {exportFormat === "PDF" && "Professional PDF document for printing and sharing"}
              {exportFormat === "SVG" && "Scalable vector graphics for editing"}
              {exportFormat === "IGES" && "Universal CAD format for shipyard systems"}
            </p>
          </div>

          {exportFormat !== "IGES" && (
            <>
              {/* Paper Size */}
              <div className="mb-4">
                <label className="block text-sm font-medium text-foreground mb-2">Paper Size</label>
                <select
                  value={options.paperSize}
                  onChange={(e) =>
                    setOptions({
                      ...options,
                      paperSize: e.target.value as LinesPlanExportOptions["paperSize"],
                    })
                  }
                  className="w-full px-3 py-2 border border-border rounded bg-background text-foreground"
                >
                  {["A0", "A1", "A2", "A3", "Letter", "Tabloid"].map((size) => (
                    <option key={size} value={size}>
                      {size}
                    </option>
                  ))}
                </select>
              </div>

              {/* Scale */}
              <div className="mb-4">
                <label className="block text-sm font-medium text-foreground mb-2">Scale</label>
                <select
                  value={options.scale}
                  onChange={(e) =>
                    setOptions({
                      ...options,
                      scale: e.target.value as LinesPlanExportOptions["scale"],
                    })
                  }
                  className="w-full px-3 py-2 border border-border rounded bg-background text-foreground"
                >
                  {["1:50", "1:100", "1:200", "1:500"].map((scale) => (
                    <option key={scale} value={scale}>
                      {scale}
                    </option>
                  ))}
                </select>
              </div>

              {/* Options */}
              <div className="mb-4 space-y-2">
                <label className="block text-sm font-medium text-foreground mb-2">Include</label>
                {[
                  ["includeTitleBlock", "Title Block"],
                  ["includeGrid", "Grid Lines"],
                  ["includeOffsetsTable", "Offsets Table"],
                  ["includeSectionAreaCurve", "Section Area Curve"],
                  ["includeDiagonals", "Diagonals"],
                ].map(([key, label]) => (
                  <label key={key} className="flex items-center space-x-2 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={options[key as keyof LinesPlanExportOptions] as boolean}
                      onChange={(e) => setOptions({ ...options, [key]: e.target.checked })}
                      className="w-4 h-4 rounded"
                    />
                    <span className="text-sm text-foreground">{label}</span>
                  </label>
                ))}
              </div>

              {/* Quality */}
              <div className="mb-4">
                <label className="block text-sm font-medium text-foreground mb-2">Quality</label>
                <div className="flex space-x-2">
                  {(["Draft", "Final"] as const).map((quality) => (
                    <button
                      key={quality}
                      onClick={() => setOptions({ ...options, quality })}
                      className={`px-4 py-2 rounded text-sm font-medium transition-colors ${
                        options.quality === quality
                          ? "bg-primary text-primary-foreground"
                          : "bg-muted text-muted-foreground hover:bg-muted/80"
                      }`}
                    >
                      {quality}
                    </button>
                  ))}
                </div>
              </div>

              {/* Watermark */}
              <div className="mb-4">
                <label className="block text-sm font-medium text-foreground mb-2">
                  Watermark (Optional)
                </label>
                <input
                  type="text"
                  value={options.watermark}
                  onChange={(e) => setOptions({ ...options, watermark: e.target.value })}
                  placeholder="e.g., DRAFT, CONFIDENTIAL"
                  className="w-full px-3 py-2 border border-border rounded bg-background text-foreground"
                />
              </div>
            </>
          )}

          {exportFormat === "IGES" && (
            <div className="mb-4 p-4 bg-muted rounded">
              <p className="text-sm text-muted-foreground">
                IGES export will include stations, waterlines, buttocks, and diagonals as B-spline
                curves. Compatible with CAD software like FreeCAD, Rhino, AutoCAD, and shipyard
                systems.
              </p>
            </div>
          )}

          {/* Error Display */}
          {error && (
            <div className="mb-4 p-3 bg-destructive/10 border border-destructive rounded">
              <p className="text-sm text-destructive">{error}</p>
            </div>
          )}

          {/* Actions */}
          <div className="flex justify-end space-x-2 mt-6">
            <button
              onClick={onClose}
              disabled={exporting}
              className="px-4 py-2 text-sm font-medium text-foreground bg-muted rounded hover:bg-muted/80 disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              onClick={handleExport}
              disabled={exporting}
              className="px-4 py-2 text-sm font-medium bg-primary text-primary-foreground rounded hover:bg-primary/90 disabled:opacity-50"
            >
              {exporting ? "Exporting..." : `Export ${exportFormat}`}
            </button>
          </div>
        </div>
      </div>
    );
  }
);

LinesPlanExportDialog.displayName = "LinesPlanExportDialog";
