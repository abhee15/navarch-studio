interface CalculationTypePanelProps {
  calculationType: "ittc57" | "holtrop-mennen";
  onChange: (type: "ittc57" | "holtrop-mennen") => void;
  disabled?: boolean;
}

/**
 * Panel for selecting calculation type (ITTC-57 or Holtrop-Mennen)
 */
export function CalculationTypePanel({
  calculationType,
  onChange,
  disabled = false,
}: CalculationTypePanelProps) {
  return (
    <div className="space-y-2">
      <label className="flex items-center cursor-pointer">
        <input
          type="radio"
          name="calcType"
          value="ittc57"
          checked={calculationType === "ittc57"}
          onChange={() => onChange("ittc57")}
          disabled={disabled}
          className="mr-2"
        />
        <span className="text-sm">ITTC-57 Friction Only</span>
      </label>
      <label className="flex items-center cursor-pointer">
        <input
          type="radio"
          name="calcType"
          value="holtrop-mennen"
          checked={calculationType === "holtrop-mennen"}
          onChange={() => onChange("holtrop-mennen")}
          disabled={disabled}
          className="mr-2"
        />
        <span className="text-sm">Holtrop-Mennen (Full)</span>
      </label>
    </div>
  );
}
