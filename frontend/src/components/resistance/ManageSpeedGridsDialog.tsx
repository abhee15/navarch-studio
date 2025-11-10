import { useState, useEffect } from "react";
import { speedGridsApi } from "../../services/resistanceApi";
import { getErrorMessage } from "../../types/errors";
import type { SpeedGrid } from "../../types/resistance";
import { SpeedGridEditor } from "./SpeedGridEditor";
import { Dialog, DialogHeader, DialogDescription, DialogContent, DialogFooter } from "../ui/dialog";
import { Button } from "../ui/button";
import { Grid3x3, Plus, Edit2, Trash2 } from "lucide-react";

interface ManageSpeedGridsDialogProps {
  vesselId: string;
  isOpen: boolean;
  onClose: () => void;
  onGridsUpdated: () => void;
}

export function ManageSpeedGridsDialog({
  vesselId,
  isOpen,
  onClose,
  onGridsUpdated,
}: ManageSpeedGridsDialogProps) {
  const [grids, setGrids] = useState<SpeedGrid[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showEditor, setShowEditor] = useState(false);
  const [editingGridId, setEditingGridId] = useState<string | null>(null);
  const [deletingGridId, setDeletingGridId] = useState<string | null>(null);

  const loadGrids = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await speedGridsApi.list(vesselId);
      setGrids(data.speedGrids);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isOpen) {
      loadGrids();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, vesselId]);

  const handleCreateClick = () => {
    setEditingGridId(null);
    setShowEditor(true);
  };

  const handleEditClick = (grid: SpeedGrid) => {
    setEditingGridId(grid.id);
    setShowEditor(true);
  };

  const handleEditorClose = () => {
    setShowEditor(false);
    setEditingGridId(null);
  };

  const handleEditorSave = () => {
    setShowEditor(false);
    setEditingGridId(null);
    loadGrids();
    onGridsUpdated();
  };

  const handleDelete = async (grid: SpeedGrid) => {
    if (!confirm(`Are you sure you want to delete "${grid.name}"?`)) {
      return;
    }

    try {
      setDeletingGridId(grid.id);
      await speedGridsApi.delete(vesselId, grid.id);
      loadGrids();
      onGridsUpdated();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setDeletingGridId(null);
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString();
  };

  return (
    <>
      <Dialog isOpen={isOpen} onClose={onClose} maxWidth="4xl">
        <DialogHeader icon={<Grid3x3 className="h-6 w-6 text-primary" />} onClose={onClose}>
          Manage Speed Grids
        </DialogHeader>

        <DialogDescription>
          Create and manage speed grids for resistance calculations
        </DialogDescription>

        <DialogContent>
          {/* Error Message */}
          {error && (
            <div className="mb-4 bg-destructive/10 border border-destructive/50 text-destructive px-3 py-2 rounded text-sm">
              {error}
            </div>
          )}

          {/* Create Button */}
          <div className="mb-4">
            <Button onClick={handleCreateClick} variant="default" className="w-full sm:w-auto">
              <Plus className="h-4 w-4 mr-2" />
              Create Speed Grid
            </Button>
          </div>

          {/* Grids List */}
          {loading ? (
            <div className="flex justify-center py-8">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
            </div>
          ) : grids.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              <p>No speed grids yet. Create one to get started.</p>
            </div>
          ) : (
            <div className="overflow-x-auto border border-border rounded-lg">
              <table className="min-w-full divide-y divide-border">
                <thead className="bg-muted">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-medium text-foreground uppercase tracking-wider">
                      Name
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-foreground uppercase tracking-wider">
                      Points
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-foreground uppercase tracking-wider">
                      Speed Range
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-foreground uppercase tracking-wider">
                      Created
                    </th>
                    <th className="px-4 py-3 text-right text-xs font-medium text-foreground uppercase tracking-wider">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-card divide-y divide-border">
                  {grids.map((grid) => {
                    const speeds = grid.speedPoints
                      .map((p) => p.speed)
                      .filter((s) => s > 0)
                      .sort((a, b) => a - b);
                    const minSpeed = speeds.length > 0 ? speeds[0] : 0;
                    const maxSpeed = speeds.length > 0 ? speeds[speeds.length - 1] : 0;

                    return (
                      <tr
                        key={grid.id}
                        className="hover:bg-muted/50 cursor-pointer transition-colors"
                        onClick={() => handleEditClick(grid)}
                      >
                        <td className="px-4 py-3 whitespace-nowrap">
                          <div className="text-sm font-medium text-foreground">{grid.name}</div>
                          {grid.description && (
                            <div className="text-xs text-muted-foreground">{grid.description}</div>
                          )}
                        </td>
                        <td className="px-4 py-3 whitespace-nowrap text-sm text-muted-foreground">
                          {grid.speedPoints.length}
                        </td>
                        <td className="px-4 py-3 whitespace-nowrap text-sm text-muted-foreground">
                          {minSpeed.toFixed(2)} - {maxSpeed.toFixed(2)} m/s
                          <br />
                          <span className="text-xs">
                            ({(minSpeed / 0.514444).toFixed(1)} - {(maxSpeed / 0.514444).toFixed(1)}{" "}
                            knots)
                          </span>
                        </td>
                        <td className="px-4 py-3 whitespace-nowrap text-sm text-muted-foreground">
                          {formatDate(grid.createdAt)}
                        </td>
                        <td className="px-4 py-3 whitespace-nowrap text-right text-sm font-medium">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleEditClick(grid);
                            }}
                            className="mr-2"
                          >
                            <Edit2 className="h-3.5 w-3.5 mr-1" />
                            Edit
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleDelete(grid);
                            }}
                            disabled={deletingGridId === grid.id}
                            className="text-destructive hover:text-destructive hover:bg-destructive/10"
                          >
                            <Trash2 className="h-3.5 w-3.5 mr-1" />
                            {deletingGridId === grid.id ? "Deleting..." : "Delete"}
                          </Button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </DialogContent>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            Close
          </Button>
        </DialogFooter>
      </Dialog>

      {/* Speed Grid Editor */}
      <SpeedGridEditor
        vesselId={vesselId}
        gridId={editingGridId || undefined}
        isOpen={showEditor}
        onClose={handleEditorClose}
        onSave={handleEditorSave}
      />
    </>
  );
}
