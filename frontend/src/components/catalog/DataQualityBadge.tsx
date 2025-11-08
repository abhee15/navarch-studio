import React from "react";
import { CheckCircle, AlertTriangle, XCircle, Info } from "lucide-react";
import { CatalogHullListItem } from "../../types/catalog";

interface DataQualityBadgeProps {
  hull: CatalogHullListItem;
}

type DataQualityStatus = "complete" | "partial" | "minimal" | "missing-geometry";

export const DataQualityBadge: React.FC<DataQualityBadgeProps> = ({ hull }) => {
  const status = getDataQualityStatus(hull);
  const config = getStatusConfig(status);

  return (
    <span
      className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium ${config.className}`}
    >
      {React.createElement(config.icon, { className: "h-3 w-3" })}
      {config.label}
    </span>
  );
};

function getDataQualityStatus(hull: CatalogHullListItem): DataQualityStatus {
  // Geometry check takes priority
  if (hull.geometryMissing) {
    return "missing-geometry";
  }

  // Count missing fields (excluding optional ones)
  const requiredFields = [hull.lpp, hull.beam, hull.draft, hull.cb];
  const missingCount = requiredFields.filter((f) => f == null || f === undefined).length;

  if (missingCount === 0) {
    return "complete";
  } else if (missingCount <= 2) {
    return "partial";
  } else {
    return "minimal";
  }
}

function getStatusConfig(status: DataQualityStatus) {
  switch (status) {
    case "complete":
      return {
        label: "Complete",
        icon: CheckCircle,
        className: "bg-green-50 text-green-700 dark:bg-green-900/30 dark:text-green-300",
      };
    case "partial":
      return {
        label: "Partial",
        icon: AlertTriangle,
        className: "bg-yellow-50 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-300",
      };
    case "minimal":
      return {
        label: "Minimal",
        icon: Info,
        className: "bg-orange-50 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300",
      };
    case "missing-geometry":
      return {
        label: "No Geometry",
        icon: XCircle,
        className: "bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-300",
      };
  }
}
