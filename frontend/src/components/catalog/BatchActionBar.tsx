import React from "react";
import { X, Copy, GitCompare, Download } from "lucide-react";
import { Button } from "../ui/button";
import { CatalogHullListItem } from "../../types/catalog";

interface BatchActionBarProps {
  selectedVessels: CatalogHullListItem[];
  onClear: () => void;
  onCloneAll?: () => void;
  onCompare?: () => void;
  onExport?: () => void;
}

export const BatchActionBar: React.FC<BatchActionBarProps> = ({
  selectedVessels,
  onClear,
  onCloneAll,
  onCompare,
  onExport,
}) => {
  if (selectedVessels.length === 0) return null;

  const hasGeometry = selectedVessels.every((v) => !v.geometryMissing);

  return (
    <div className="fixed bottom-6 left-1/2 -translate-x-1/2 z-40 animate-in slide-in-from-bottom-4 duration-300">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-2xl border border-gray-200 dark:border-gray-700 px-6 py-4">
        <div className="flex items-center gap-4">
          {/* Selection Info */}
          <div className="flex items-center gap-2">
            <div className="h-8 w-8 rounded-full bg-blue-600 text-white flex items-center justify-center text-sm font-semibold">
              {selectedVessels.length}
            </div>
            <span className="text-sm font-medium text-gray-900 dark:text-white">
              vessel{selectedVessels.length > 1 ? "s" : ""} selected
            </span>
          </div>

          {/* Divider */}
          <div className="h-8 w-px bg-gray-300 dark:bg-gray-600" />

          {/* Actions */}
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={onCompare}
              disabled={selectedVessels.length < 2}
              className="flex items-center gap-2"
            >
              <GitCompare className="h-4 w-4" />
              Compare
            </Button>

            <Button
              variant="outline"
              size="sm"
              onClick={onCloneAll}
              disabled={!hasGeometry}
              className="flex items-center gap-2"
              title={!hasGeometry ? "Some vessels are missing geometry" : undefined}
            >
              <Copy className="h-4 w-4" />
              Clone All
            </Button>

            <Button
              variant="outline"
              size="sm"
              onClick={onExport}
              className="flex items-center gap-2"
            >
              <Download className="h-4 w-4" />
              Export
            </Button>
          </div>

          {/* Divider */}
          <div className="h-8 w-px bg-gray-300 dark:bg-gray-600" />

          {/* Clear Button */}
          <button
            onClick={onClear}
            className="p-2 rounded hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
            aria-label="Clear selection"
          >
            <X className="h-4 w-4 text-gray-500 dark:text-gray-400" />
          </button>
        </div>

        {/* Warning if some vessels missing geometry */}
        {!hasGeometry && (
          <div className="mt-3 text-xs text-yellow-700 dark:text-yellow-400 bg-yellow-50 dark:bg-yellow-900/20 px-3 py-2 rounded">
            ⚠️ {selectedVessels.filter((v) => v.geometryMissing).length} of {selectedVessels.length}{" "}
            vessels are missing geometry and cannot be cloned
          </div>
        )}
      </div>
    </div>
  );
};
