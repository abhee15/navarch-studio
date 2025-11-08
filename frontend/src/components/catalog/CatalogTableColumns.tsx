import { createColumnHelper } from "@tanstack/react-table";
import { CatalogHullListItem } from "../../types/catalog";
import { DataQualityBadge } from "./DataQualityBadge";
import { SelectAllCheckbox } from "./SelectAllCheckbox";
import { Ship, Package, Droplets, Anchor, Ruler } from "lucide-react";

const columnHelper = createColumnHelper<CatalogHullListItem>();

export const catalogColumns = [
  // Selection checkbox
  columnHelper.display({
    id: "select",
    size: 40,
    header: ({ table }) => <SelectAllCheckbox table={table} />,
    cell: ({ row }) => (
      <input
        type="checkbox"
        checked={row.getIsSelected()}
        onChange={row.getToggleSelectedHandler()}
        onClick={(e) => e.stopPropagation()} // Prevent row click
        className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
        aria-label={`Select ${row.original.title}`}
      />
    ),
  }),

  // Title (with icon)
  columnHelper.accessor("title", {
    id: "title",
    size: 200,
    header: "Title",
    cell: (info) => {
      const hull = info.row.original;
      const Icon = getHullTypeIcon(hull.hullType);

      return (
        <div className="flex items-center gap-2">
          <Icon className="h-4 w-4 text-gray-500 dark:text-gray-400 flex-shrink-0" />
          <span className="font-medium text-gray-900 dark:text-white truncate">
            {info.getValue()}
          </span>
        </div>
      );
    },
  }),

  // Hull Type
  columnHelper.accessor("hullType", {
    id: "hullType",
    size: 100,
    header: "Type",
    cell: (info) => {
      const type = info.getValue();
      if (!type) return <span className="text-gray-400 dark:text-gray-500">—</span>;

      const bgColor = getHullTypeBackground(type);
      return <span className={`px-2 py-1 rounded text-xs font-medium ${bgColor}`}>{type}</span>;
    },
  }),

  // Lpp (m)
  columnHelper.accessor("lpp", {
    id: "lpp",
    size: 90,
    header: () => <div className="text-right">Lpp (m)</div>,
    cell: (info) => {
      const value = info.getValue();
      return (
        <div className="text-right">
          {value != null ? (
            <span className="font-mono">{value.toFixed(2)}</span>
          ) : (
            <span className="text-gray-400 dark:text-gray-500">—</span>
          )}
        </div>
      );
    },
  }),

  // Beam (m)
  columnHelper.accessor("beam", {
    id: "beam",
    size: 90,
    header: () => <div className="text-right">Beam (m)</div>,
    cell: (info) => {
      const value = info.getValue();
      return (
        <div className="text-right">
          {value != null ? (
            <span className="font-mono">{value.toFixed(2)}</span>
          ) : (
            <span className="text-gray-400 dark:text-gray-500">—</span>
          )}
        </div>
      );
    },
  }),

  // Draft (m) - hidden by default on smaller screens
  columnHelper.accessor("draft", {
    id: "draft",
    size: 90,
    header: () => <div className="text-right">Draft (m)</div>,
    cell: (info) => {
      const value = info.getValue();
      return (
        <div className="text-right">
          {value != null ? (
            <span className="font-mono">{value.toFixed(2)}</span>
          ) : (
            <span className="text-gray-400 dark:text-gray-500">—</span>
          )}
        </div>
      );
    },
    meta: {
      hideBelow: "md", // Custom meta for responsive hiding
    },
  }),

  // Block Coefficient (Cb)
  columnHelper.accessor("cb", {
    id: "cb",
    size: 80,
    header: () => <div className="text-right">Cb</div>,
    cell: (info) => {
      const value = info.getValue();
      return (
        <div className="text-right">
          {value != null ? (
            <span className="font-mono font-semibold">{value.toFixed(3)}</span>
          ) : (
            <span className="text-gray-400 dark:text-gray-500">—</span>
          )}
        </div>
      );
    },
  }),

  // Data Quality Status
  columnHelper.display({
    id: "status",
    size: 140,
    header: () => <div className="text-center">Status</div>,
    cell: ({ row }) => {
      const hull = row.original;
      return (
        <div className="flex justify-center">
          <DataQualityBadge hull={hull} />
        </div>
      );
    },
  }),

  // Actions
  columnHelper.display({
    id: "actions",
    size: 80,
    header: "",
    cell: () => (
      <button
        onClick={(e) => {
          e.stopPropagation();
          // This will be handled by the parent component (row click)
        }}
        className="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 text-sm font-medium"
      >
        View
      </button>
    ),
  }),
];

// Helper functions
function getHullTypeIcon(type?: string) {
  switch (type) {
    case "Container":
      return Package;
    case "Tanker":
      return Droplets;
    case "Naval":
      return Anchor;
    case "Template":
      return Ruler;
    default:
      return Ship;
  }
}

function getHullTypeBackground(type: string) {
  switch (type) {
    case "Container":
      return "bg-blue-50 text-blue-900 dark:bg-blue-900/20 dark:text-blue-300";
    case "Tanker":
      return "bg-cyan-50 text-cyan-900 dark:bg-cyan-900/20 dark:text-cyan-300";
    case "Naval":
      return "bg-indigo-50 text-indigo-900 dark:bg-indigo-900/20 dark:text-indigo-300";
    case "Template":
      return "bg-purple-50 text-purple-900 dark:bg-purple-900/20 dark:text-purple-300";
    default:
      return "bg-gray-50 text-gray-900 dark:bg-gray-800 dark:text-gray-300";
  }
}
