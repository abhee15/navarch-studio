import { useState } from "react";
import type {
  Ittc57CalculationResult,
  HoltropMennenCalculationResult,
  PowerCurveResult,
} from "../../types/resistance";
import { Dialog, DialogHeader, DialogContent, DialogFooter } from "../ui/dialog";
import { Button } from "../ui/button";
import { Label } from "../ui/label";
import { Download } from "lucide-react";

interface ResistanceExportDialogProps {
  vesselName: string;
  ittc57Result: Ittc57CalculationResult | null;
  hmResult: HoltropMennenCalculationResult | null;
  powerResult: PowerCurveResult | null;
  isOpen: boolean;
  onClose: () => void;
}

type ExportFormat = "csv" | "json";

interface ExportData {
  vessel: string;
  exportedAt: string;
  ittc57?: Ittc57CalculationResult;
  holtropMennen?: HoltropMennenCalculationResult;
  powerCurves?: PowerCurveResult;
}

export function ResistanceExportDialog({
  vesselName,
  ittc57Result,
  hmResult,
  powerResult,
  isOpen,
  onClose,
}: ResistanceExportDialogProps) {
  const [format, setFormat] = useState<ExportFormat>("csv");
  const [exporting, setExporting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const hasResults = ittc57Result || hmResult || powerResult;

  const exportToCSV = () => {
    const rows: string[] = [];

    // ITTC-57 Results
    if (ittc57Result) {
      rows.push("ITTC-57 Friction Calculation Results");
      rows.push("");
      rows.push("Speed (m/s),Speed (knots),Re,Fn,CF,CF_eff");
      ittc57Result.speedGrid.forEach((speed, idx) => {
        const speedKnots = speed / 0.514444;
        const re = ittc57Result.reynoldsNumbers[idx];
        const fn = ittc57Result.froudeNumbers[idx];
        const cf = ittc57Result.frictionCoefficients[idx];
        const cfEff = ittc57Result.effectiveFrictionCoefficients[idx];
        rows.push(
          `${speed.toFixed(3)},${speedKnots.toFixed(3)},${re.toExponential(4)},${fn.toFixed(6)},${cf.toFixed(8)},${cfEff.toFixed(8)}`
        );
      });
      rows.push("");
    }

    // Holtrop-Mennen Results
    if (hmResult) {
      rows.push("Holtrop-Mennen Resistance Calculation Results");
      rows.push("");
      rows.push(
        "Speed (m/s),Speed (knots),Re,Fn,RT (N),RF (N),RR (N),RA (N),RCA (N),RAA (N),EHP (kW)"
      );
      hmResult.speedGrid.forEach((speed, idx) => {
        const speedKnots = speed / 0.514444;
        const re = hmResult.reynoldsNumbers[idx];
        const fn = hmResult.froudeNumbers[idx];
        const rt = hmResult.totalResistance[idx];
        const rf = hmResult.frictionResistance[idx];
        const rr = hmResult.residuaryResistance[idx];
        const ra = hmResult.appendageResistance[idx];
        const rca = hmResult.correlationAllowance[idx];
        const raa = hmResult.airResistance[idx];
        const ehp = hmResult.effectivePower[idx];
        rows.push(
          `${speed.toFixed(3)},${speedKnots.toFixed(3)},${re.toExponential(4)},${fn.toFixed(6)},${rt.toFixed(2)},${rf.toFixed(2)},${rr.toFixed(2)},${ra.toFixed(2)},${rca.toFixed(2)},${raa.toFixed(2)},${ehp.toFixed(2)}`
        );
      });
      rows.push("");
    }

    // Power Curves
    if (powerResult) {
      rows.push("Power Curves");
      rows.push(`Service Margin: ${powerResult.serviceMargin.toFixed(1)}%`);
      rows.push(`Overall Efficiency (ηD): ${(powerResult.etaD ?? 0.65).toFixed(3)}`);
      rows.push("");
      rows.push("Speed (m/s),Speed (knots),EHP (kW),DHP (kW),P_inst (kW)");
      powerResult.speedGrid.forEach((speed, idx) => {
        const speedKnots = speed / 0.514444;
        const ehp = powerResult.effectivePower[idx];
        const dhp = powerResult.deliveredPower[idx];
        const pInst = powerResult.installedPower[idx];
        rows.push(
          `${speed.toFixed(3)},${speedKnots.toFixed(3)},${ehp.toFixed(2)},${dhp.toFixed(2)},${pInst.toFixed(2)}`
        );
      });
    }

    return rows.join("\n");
  };

  const exportToJSON = () => {
    const data: ExportData = {
      vessel: vesselName,
      exportedAt: new Date().toISOString(),
      ...(ittc57Result && { ittc57: ittc57Result }),
      ...(hmResult && { holtropMennen: hmResult }),
      ...(powerResult && { powerCurves: powerResult }),
    };

    return JSON.stringify(data, null, 2);
  };

  const handleExport = async () => {
    if (!hasResults) {
      setError("No results to export");
      return;
    }

    try {
      setExporting(true);
      setError(null);

      let content: string;
      let filename: string;
      let mimeType: string;

      if (format === "csv") {
        content = exportToCSV();
        filename = `${vesselName.replace(/\s+/g, "_")}_resistance_${new Date().toISOString().split("T")[0]}.csv`;
        mimeType = "text/csv";
      } else {
        content = exportToJSON();
        filename = `${vesselName.replace(/\s+/g, "_")}_resistance_${new Date().toISOString().split("T")[0]}.json`;
        mimeType = "application/json";
      }

      const blob = new Blob([content], { type: mimeType });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = filename;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);

      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Export failed");
    } finally {
      setExporting(false);
    }
  };

  return (
    <Dialog isOpen={isOpen} onClose={onClose} maxWidth="lg">
      <DialogHeader icon={<Download className="h-6 w-6 text-primary" />} onClose={onClose}>
        Export Resistance Results
      </DialogHeader>

      <DialogContent>
        {error && (
          <div className="mb-4 bg-destructive/10 border border-destructive/50 text-destructive px-3 py-2 rounded text-sm">
            {error}
          </div>
        )}

        {!hasResults && (
          <div className="mb-4 bg-warning/10 border border-warning/50 text-warning-foreground px-3 py-2 rounded text-sm">
            No calculation results available to export. Please run calculations first.
          </div>
        )}

        <div className="space-y-4">
          <div>
            <Label className="block mb-2">Export Format</Label>
            <div className="space-y-2">
              <label className="flex items-center">
                <input
                  type="radio"
                  name="format"
                  value="csv"
                  checked={format === "csv"}
                  onChange={(e) => setFormat(e.target.value as ExportFormat)}
                  className="mr-2"
                />
                <span className="text-sm text-foreground">CSV (Comma-separated values)</span>
              </label>
              <label className="flex items-center">
                <input
                  type="radio"
                  name="format"
                  value="json"
                  checked={format === "json"}
                  onChange={(e) => setFormat(e.target.value as ExportFormat)}
                  className="mr-2"
                />
                <span className="text-sm text-foreground">JSON (Structured data)</span>
              </label>
            </div>
          </div>

          <div className="text-sm text-muted-foreground">
            <p className="mb-2">This export will include:</p>
            <ul className="list-disc list-inside space-y-1">
              {ittc57Result && <li>ITTC-57 friction results</li>}
              {hmResult && <li>Holtrop-Mennen resistance results</li>}
              {powerResult && <li>Power curves (EHP, DHP, P_inst)</li>}
            </ul>
          </div>
        </div>
      </DialogContent>

      <DialogFooter className="sm:flex sm:flex-row-reverse">
        <Button
          variant="default"
          onClick={handleExport}
          disabled={exporting || !hasResults}
          className="sm:ml-3"
        >
          {exporting ? "Exporting..." : "Export"}
        </Button>
        <Button variant="outline" onClick={onClose}>
          Cancel
        </Button>
      </DialogFooter>
    </Dialog>
  );
}
