import { useMemo, useRef } from "react";
import { Card } from "../../ui/card";
import type { VesselDetails, HydroResult, Loadcase } from "../../../types/hydrostatics";
import type { HoltropMennenCalculationResult, PowerCurveResult } from "../../../types/resistance";
import html2canvas from "html2canvas";
import jsPDF from "jspdf";
import toast from "react-hot-toast";

interface UnifiedSummaryPanelProps {
  vessel: VesselDetails;
  designDraftHydro: HydroResult | null;
  loadcase: Loadcase | null;
  hmResult: HoltropMennenCalculationResult | null;
  powerResult: PowerCurveResult | null;
  serviceSpeedIndex: number | null;
}

interface SummaryData {
  // Principal dimensions
  lpp: number;
  beam: number;
  designDraft: number;

  // Design draft hydrostatics
  displacement: number | null;
  kb: number | null;
  lcb: number | null;
  gmt: number | null;

  // Resistance at service speed
  rt: number | null;
  ehp: number | null;
  serviceSpeed: number | null;

  // Power
  dhp: number | null;
  pInst: number | null;
  margin: number | null;

  // Status indicators
  gmtStatus: "green" | "yellow" | "red" | "unknown";
  marginStatus: "green" | "yellow" | "red" | "unknown";
}

function getStatusColor(status: "green" | "yellow" | "red" | "unknown"): string {
  switch (status) {
    case "green":
      return "bg-green-500";
    case "yellow":
      return "bg-yellow-500";
    case "red":
      return "bg-red-500";
    default:
      return "bg-gray-400";
  }
}

function getGmtStatus(gmt: number | null): "green" | "yellow" | "red" | "unknown" {
  if (gmt === null) return "unknown";
  if (gmt >= 1.0) return "green";
  if (gmt >= 0.5) return "yellow";
  return "red";
}

function getMarginStatus(margin: number | null): "green" | "yellow" | "red" | "unknown" {
  if (margin === null) return "unknown";
  if (margin >= 15) return "green";
  if (margin >= 10) return "yellow";
  return "red";
}

export function UnifiedSummaryPanel({
  vessel,
  designDraftHydro,
  loadcase,
  hmResult,
  powerResult,
  serviceSpeedIndex,
}: UnifiedSummaryPanelProps) {
  const cardRef = useRef<HTMLDivElement>(null);

  // Memoized summary data aggregation
  const summaryData = useMemo<SummaryData>(() => {
    // Get resistance and power at service speed
    let rt: number | null = null;
    let ehp: number | null = null;
    let dhp: number | null = null;
    let pInst: number | null = null;
    let serviceSpeed: number | null = null;

    if (serviceSpeedIndex !== null && hmResult && powerResult) {
      if (serviceSpeedIndex >= 0 && serviceSpeedIndex < hmResult.speedGrid.length) {
        serviceSpeed = hmResult.speedGrid[serviceSpeedIndex];
        rt = hmResult.totalResistance[serviceSpeedIndex];
        ehp = hmResult.effectivePower[serviceSpeedIndex];
        dhp = powerResult.deliveredPower[serviceSpeedIndex];
        pInst = powerResult.installedPower[serviceSpeedIndex];
      }
    }

    const margin = powerResult?.serviceMargin ?? null;
    const gmt = designDraftHydro?.gMt ?? null;

    return {
      // Principal dimensions
      lpp: vessel.lpp,
      beam: vessel.beam,
      designDraft: vessel.designDraft,

      // Design draft hydrostatics
      displacement: designDraftHydro?.dispWeight ?? null,
      kb: designDraftHydro?.kBz ?? null,
      lcb: designDraftHydro?.lCBx ?? null,
      gmt,

      // Resistance at service speed
      rt,
      ehp,
      serviceSpeed,

      // Power
      dhp,
      pInst,
      margin,

      // Status indicators
      gmtStatus: getGmtStatus(gmt),
      marginStatus: getMarginStatus(margin),
    };
  }, [vessel, designDraftHydro, hmResult, powerResult, serviceSpeedIndex]);

  const handleExportPDF = async () => {
    if (!cardRef.current) {
      toast.error("Unable to export: card not found");
      return;
    }

    try {
      toast.loading("Generating PDF...");

      // Capture the card as an image
      const canvas = await html2canvas(cardRef.current, {
        logging: false,
      });

      // Create PDF
      const imgWidth = 210; // A4 width in mm
      const imgHeight = (canvas.height * imgWidth) / canvas.width;

      const pdf = new jsPDF({
        orientation: imgHeight > imgWidth ? "portrait" : "landscape",
        unit: "mm",
        format: "a4",
      });

      const imgData = canvas.toDataURL("image/png");
      pdf.addImage(imgData, "PNG", 0, 0, imgWidth, imgHeight);

      // Save the PDF
      const fileName = `${vessel.name}_Summary_${new Date().toISOString().split("T")[0]}.pdf`;
      pdf.save(fileName);

      toast.dismiss();
      toast.success("PDF exported successfully");
    } catch (error) {
      toast.dismiss();
      toast.error("Failed to export PDF");
      console.error("PDF export error:", error);
    }
  };

  return (
    <div className="h-full flex flex-col bg-background">
      <div className="flex-shrink-0 border-b border-border p-3 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-foreground">Unified Summary</h3>
        <button
          onClick={handleExportPDF}
          className="inline-flex items-center px-3 py-1.5 text-xs font-medium rounded border border-border hover:bg-accent/10"
          title="Export to PDF"
        >
          <svg className="h-4 w-4 mr-1.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z"
            />
          </svg>
          Export PDF
        </button>
      </div>

      <div className="flex-1 overflow-auto p-4">
        <Card ref={cardRef} className="p-6 bg-white dark:bg-card">
          {/* Header */}
          <div className="mb-6 border-b pb-4">
            <h2 className="text-2xl font-bold text-foreground mb-1">{vessel.name}</h2>
            <p className="text-sm text-muted-foreground">
              Unified Design Summary
              {loadcase && <span className="ml-2">• Loadcase: {loadcase.name}</span>}
            </p>
            <p className="text-xs text-muted-foreground mt-1">
              Generated on {new Date().toLocaleDateString()} at {new Date().toLocaleTimeString()}
            </p>
          </div>

          {/* Principal Dimensions */}
          <div className="mb-6">
            <h3 className="text-lg font-semibold text-foreground mb-3 flex items-center">
              <span className="w-2 h-5 bg-primary mr-2"></span>
              Principal Dimensions
            </h3>
            <div className="grid grid-cols-3 gap-4">
              <DataItem label="Length (Lpp)" value={summaryData.lpp} unit="m" />
              <DataItem label="Beam" value={summaryData.beam} unit="m" />
              <DataItem label="Design Draft" value={summaryData.designDraft} unit="m" />
            </div>
          </div>

          {/* Design Draft Hydrostatics */}
          <div className="mb-6">
            <h3 className="text-lg font-semibold text-foreground mb-3 flex items-center">
              <span className="w-2 h-5 bg-blue-500 mr-2"></span>
              Design Draft Hydrostatics
            </h3>
            <div className="grid grid-cols-2 gap-4">
              <DataItem
                label="Displacement (Δ)"
                value={summaryData.displacement}
                unit="kg"
                decimals={0}
              />
              <DataItem label="KB" value={summaryData.kb} unit="m" />
              <DataItem label="LCB" value={summaryData.lcb} unit="m" />
              <DataItemWithIndicator
                label="GMt"
                value={summaryData.gmt}
                unit="m"
                status={summaryData.gmtStatus}
                statusLabel={summaryData.gmt !== null && summaryData.gmt >= 1.0 ? "OK" : "Check"}
              />
            </div>
          </div>

          {/* Resistance at Service Speed */}
          <div className="mb-6">
            <h3 className="text-lg font-semibold text-foreground mb-3 flex items-center">
              <span className="w-2 h-5 bg-purple-500 mr-2"></span>
              Resistance at Service Speed
            </h3>
            <div className="grid grid-cols-3 gap-4">
              <DataItem
                label="Service Speed"
                value={summaryData.serviceSpeed}
                unit="m/s"
                secondaryValue={
                  summaryData.serviceSpeed ? summaryData.serviceSpeed * 1.94384 : null
                }
                secondaryUnit="kn"
              />
              <DataItem
                label="Total Resistance (RT)"
                value={summaryData.rt}
                unit="N"
                decimals={0}
              />
              <DataItem label="Effective Power (EHP)" value={summaryData.ehp} unit="kW" />
            </div>
          </div>

          {/* Power */}
          <div className="mb-4">
            <h3 className="text-lg font-semibold text-foreground mb-3 flex items-center">
              <span className="w-2 h-5 bg-orange-500 mr-2"></span>
              Power
            </h3>
            <div className="grid grid-cols-3 gap-4">
              <DataItem label="Delivered Power (DHP)" value={summaryData.dhp} unit="kW" />
              <DataItem label="Installed Power (P_inst)" value={summaryData.pInst} unit="kW" />
              <DataItemWithIndicator
                label="Service Margin"
                value={summaryData.margin}
                unit="%"
                decimals={1}
                status={summaryData.marginStatus}
                statusLabel={summaryData.margin !== null && summaryData.margin >= 15 ? "OK" : "Low"}
              />
            </div>
          </div>

          {/* Legend */}
          <div className="mt-6 pt-4 border-t border-border">
            <div className="flex items-center gap-6 text-xs text-muted-foreground">
              <div className="flex items-center gap-2">
                <span className="font-semibold">Indicators:</span>
                <div className="flex items-center gap-1">
                  <span className={`w-3 h-3 rounded-full ${getStatusColor("green")}`}></span>
                  <span>OK</span>
                </div>
                <div className="flex items-center gap-1">
                  <span className={`w-3 h-3 rounded-full ${getStatusColor("yellow")}`}></span>
                  <span>Warning</span>
                </div>
                <div className="flex items-center gap-1">
                  <span className={`w-3 h-3 rounded-full ${getStatusColor("red")}`}></span>
                  <span>Critical</span>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <span className="font-semibold">Criteria:</span>
                <span>GMt ≥ 1.0m (Green) | Margin ≥ 15% (Green)</span>
              </div>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
}

// Helper components
function DataItem({
  label,
  value,
  unit,
  decimals = 2,
  secondaryValue,
  secondaryUnit,
}: {
  label: string;
  value: number | null;
  unit: string;
  decimals?: number;
  secondaryValue?: number | null;
  secondaryUnit?: string;
}) {
  return (
    <div className="bg-muted/30 p-3 rounded">
      <div className="text-xs text-muted-foreground mb-1">{label}</div>
      {value !== null ? (
        <>
          <div className="text-lg font-semibold text-foreground">
            {value.toFixed(decimals)}{" "}
            <span className="text-sm font-normal text-muted-foreground">{unit}</span>
          </div>
          {secondaryValue !== null && secondaryValue !== undefined && secondaryUnit && (
            <div className="text-xs text-muted-foreground mt-0.5">
              ({secondaryValue.toFixed(decimals)} {secondaryUnit})
            </div>
          )}
        </>
      ) : (
        <div className="text-lg font-semibold text-muted-foreground">N/A</div>
      )}
    </div>
  );
}

function DataItemWithIndicator({
  label,
  value,
  unit,
  decimals = 2,
  status,
  statusLabel,
}: {
  label: string;
  value: number | null;
  unit: string;
  decimals?: number;
  status: "green" | "yellow" | "red" | "unknown";
  statusLabel?: string;
}) {
  return (
    <div
      className="bg-muted/30 p-3 rounded border-l-4"
      style={{
        borderLeftColor:
          status === "green"
            ? "#22c55e"
            : status === "yellow"
              ? "#eab308"
              : status === "red"
                ? "#ef4444"
                : "#9ca3af",
      }}
    >
      <div className="text-xs text-muted-foreground mb-1 flex items-center justify-between">
        <span>{label}</span>
        {status !== "unknown" && (
          <span className="flex items-center gap-1">
            <span className={`w-2 h-2 rounded-full ${getStatusColor(status)}`}></span>
            <span className="text-[10px] font-medium">{statusLabel}</span>
          </span>
        )}
      </div>
      {value !== null ? (
        <div className="text-lg font-semibold text-foreground">
          {value.toFixed(decimals)}{" "}
          <span className="text-sm font-normal text-muted-foreground">{unit}</span>
        </div>
      ) : (
        <div className="text-lg font-semibold text-muted-foreground">N/A</div>
      )}
    </div>
  );
}
