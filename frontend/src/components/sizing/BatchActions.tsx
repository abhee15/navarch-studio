import { useState } from "react";
import { Button } from "../ui/button";
import { Trash2, AlertTriangle } from "lucide-react";

interface BatchActionsProps {
  selectedMissions: string[];
  onClearSelection: () => void;
  onDelete: (ids: string[]) => void;
  onExportBatch: (ids: string[], format: "json" | "csv") => void;
}

/**
 * Batch Actions Toolbar
 *
 * Appears when missions are selected for bulk operations
 */
export const BatchActions: React.FC<BatchActionsProps> = ({
  selectedMissions,
  onClearSelection,
  onDelete,
  onExportBatch,
}) => {
  const [showConfirm, setShowConfirm] = useState(false);

  if (selectedMissions.length === 0) return null;

  const selectedCount = selectedMissions.length;

  const handleDelete = () => {
    if (showConfirm) {
      onDelete(selectedMissions);
      setShowConfirm(false);
    } else {
      setShowConfirm(true);
      setTimeout(() => setShowConfirm(false), 3000);
    }
  };

  return (
    <div className="fixed bottom-8 left-1/2 -translate-x-1/2 z-50 animate-zoomIn">
      <div className="rounded-lg bg-gradient-to-r from-blue-600 to-cyan-600 p-4 shadow-2xl backdrop-blur-sm">
        <div className="flex items-center gap-4">
          {/* Selection Count */}
          <div className="flex items-center gap-2 text-white">
            <div className="w-8 h-8 rounded-full bg-white/20 flex items-center justify-center font-bold">
              {selectedCount}
            </div>
            <span className="font-medium">
              {selectedCount} mission{selectedCount > 1 ? "s" : ""} selected
            </span>
          </div>

          <div className="h-6 w-px bg-white/30"></div>

          {/* Actions */}
          <div className="flex gap-2">
            <Button
              size="sm"
              variant="outline"
              onClick={onClearSelection}
              className="bg-white/10 text-white border-white/30 hover:bg-white/20"
            >
              Clear
            </Button>

            <Button
              size="sm"
              onClick={() => onExportBatch(selectedMissions, "json")}
              className="bg-white/10 text-white hover:bg-white/20"
            >
              📄 Export JSON
            </Button>

            <Button
              size="sm"
              onClick={() => onExportBatch(selectedMissions, "csv")}
              className="bg-white/10 text-white hover:bg-white/20"
            >
              📊 Export CSV
            </Button>

            <div className="h-6 w-px bg-white/30"></div>

            <Button
              size="sm"
              onClick={handleDelete}
              className={`transition-all ${
                showConfirm
                  ? "bg-red-600 hover:bg-red-700 animate-pulse"
                  : "bg-white/10 text-white hover:bg-white/20"
              }`}
            >
              {showConfirm ? (
                <>
                  <AlertTriangle className="h-4 w-4 mr-1" />
                  Confirm Delete?
                </>
              ) : (
                <>
                  <Trash2 className="h-4 w-4 mr-1" />
                  Delete
                </>
              )}
            </Button>
          </div>
        </div>

        {showConfirm && (
          <p className="text-xs text-white/80 mt-2 text-center">
            Click Delete again to confirm deletion of {selectedCount} mission
            {selectedCount > 1 ? "s" : ""}
          </p>
        )}
      </div>
    </div>
  );
};
