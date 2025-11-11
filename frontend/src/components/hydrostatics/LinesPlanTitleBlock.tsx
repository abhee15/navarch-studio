import { observer } from "mobx-react-lite";
import type { VesselDetails } from "../../types/hydrostatics";
import { settingsStore } from "../../stores/SettingsStore";
import { useStore } from "../../stores";
import { getUnitSymbol } from "../../utils/unitSymbols";

interface LinesPlanTitleBlockProps {
  vessel: VesselDetails;
  x: number;
  y: number;
  width: number;
  height: number;
  scale?: string;
}

export const LinesPlanTitleBlock = observer(
  ({ vessel, x, y, width, height, scale = "1:100" }: LinesPlanTitleBlockProps) => {
    const { authStore } = useStore();
    const displayUnits = settingsStore.preferredUnits;
    const lengthUnit = getUnitSymbol(displayUnits, "Length");
    const currentUser = authStore.user;

    return (
      <g transform={`translate(${x}, ${y})`}>
        {/* Border */}
        <rect
          x={0}
          y={0}
          width={width}
          height={height}
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          className="stroke-foreground"
        />

        {/* Vessel Name */}
        <text x={20} y={25} className="text-lg font-bold fill-foreground">
          {vessel.name}
        </text>

        {/* Subtitle */}
        <text x={20} y={45} className="text-sm fill-muted-foreground">
          LINES PLAN - HULL FORM DOCUMENTATION
        </text>

        {/* Principal Particulars */}
        <g transform="translate(400, 15)">
          <text className="text-xs font-medium fill-foreground">Principal Particulars</text>
          <text x={0} y={15} className="text-xs fill-muted-foreground">
            Lpp: {vessel.lpp.toFixed(2)} {lengthUnit}
          </text>
          <text x={0} y={30} className="text-xs fill-muted-foreground">
            Beam: {vessel.beam.toFixed(2)} {lengthUnit}
          </text>
          <text x={0} y={45} className="text-xs fill-muted-foreground">
            Draft: {vessel.designDraft.toFixed(2)} {lengthUnit}
          </text>
        </g>

        {/* Metadata */}
        <g transform="translate(700, 15)">
          <text className="text-xs font-medium fill-foreground">Document Info</text>
          <text x={0} y={15} className="text-xs fill-muted-foreground">
            Date: {new Date().toLocaleDateString()}
          </text>
          <text x={0} y={30} className="text-xs fill-muted-foreground">
            Drawn By: {currentUser?.name || "User"}
          </text>
          <text x={0} y={45} className="text-xs fill-muted-foreground">
            Scale: {scale}
          </text>
        </g>

        {/* Approval Section */}
        <g transform="translate(950, 15)">
          <text className="text-xs font-medium fill-foreground">Approval</text>
          <text x={0} y={15} className="text-[9px] fill-muted-foreground">
            Draft: ___________
          </text>
          <text x={0} y={30} className="text-[9px] fill-muted-foreground">
            Reviewed: ___________
          </text>
          <text x={0} y={45} className="text-[9px] fill-muted-foreground">
            Approved: ___________
          </text>
        </g>

        {/* Standards Note */}
        <text x={20} y={height - 10} className="text-[8px] fill-muted-foreground italic">
          Per IMO MSC.267(85) - NavArch Studio v1.0
        </text>
      </g>
    );
  }
);

LinesPlanTitleBlock.displayName = "LinesPlanTitleBlock";
