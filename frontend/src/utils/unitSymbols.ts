/**
 * Simple utility to get unit symbols for display
 * Backend handles all conversions, frontend just needs to show correct symbols
 */

export type UnitSystemId = "SI" | "Imperial";

export const getUnitSymbol = (unitSystem: UnitSystemId, category: string): string => {
  const symbols: Record<UnitSystemId, Record<string, string>> = {
    SI: {
      Length: "m",
      Mass: "kg",
      Area: "m²",
      Volume: "m³",
      Density: "kg/m³",
      Force: "N",
      Pressure: "Pa",
      Angle: "deg",
    },
    Imperial: {
      Length: "ft",
      Mass: "lb",
      Area: "ft²",
      Volume: "ft³",
      Density: "lb/ft³",
      Force: "lbf",
      Pressure: "psi",
      Angle: "deg",
    },
  };

  return symbols[unitSystem]?.[category] || "";
};

