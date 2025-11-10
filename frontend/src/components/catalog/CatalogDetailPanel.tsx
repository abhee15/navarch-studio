import React from "react";
import { X, Copy, ExternalLink, CheckCircle, XCircle, GripHorizontal } from "lucide-react";
import { CatalogHullListItem } from "../../types/catalog";
import { Button } from "../ui/button";

interface CatalogDetailPanelProps {
  hull: CatalogHullListItem;
  isOpen: boolean;
  onClose: () => void;
  onClone?: (hull: CatalogHullListItem) => void;
  onViewDetails?: (hull: CatalogHullListItem) => void;
}

export const CatalogDetailPanel: React.FC<CatalogDetailPanelProps> = ({
  hull,
  isOpen,
  onClose,
  onClone,
  onViewDetails,
}) => {
  if (!isOpen) return null;

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black bg-opacity-50 z-40 transition-opacity duration-300"
        onClick={onClose}
        style={{ opacity: isOpen ? 1 : 0 }}
      />

      {/* Bottom Sheet Panel */}
      <div
        className={`
          fixed bottom-0 left-0 right-0 bg-white dark:bg-gray-900
          shadow-2xl z-50 overflow-y-auto
          transform transition-transform duration-300 ease-out
          ${isOpen ? "translate-y-0" : "translate-y-full"}
        `}
        style={{ maxHeight: "60vh" }}
      >
        {/* Drag Handle */}
        <div className="sticky top-0 bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-700 z-10">
          <div className="flex items-center justify-center py-2 cursor-grab active:cursor-grabbing">
            <GripHorizontal className="h-5 w-5 text-gray-400 dark:text-gray-500" />
          </div>
        </div>

        {/* Header */}
        <div className="sticky top-10 bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-700 px-6 py-4 flex items-center justify-between z-10">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-white">Vessel Details</h2>
          <button
            onClick={onClose}
            className="p-1 rounded hover:bg-muted dark:hover:bg-card transition-colors"
            aria-label="Close panel"
          >
            <X className="h-5 w-5 text-gray-500 dark:text-gray-400" />
          </button>
        </div>

        {/* Content */}
        <div className="px-6 py-6">
          <div className="max-w-6xl mx-auto">
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Left Column */}
              <div className="space-y-6">
                {/* Title & Slug */}
                <div>
                  <h3 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
                    {hull.title}
                  </h3>
                  <p className="text-sm text-gray-500 dark:text-gray-400 font-mono">{hull.slug}</p>
                </div>

                {/* Description */}
                {hull.description && (
                  <div>
                    <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
                      Description
                    </h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed">
                      {hull.description}
                    </p>
                  </div>
                )}

                {/* Metadata */}
                {hull.hullType && (
                  <div>
                    <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
                      Metadata
                    </h4>
                    <div className="space-y-2 text-sm">
                      <div className="flex justify-between">
                        <span className="text-gray-500 dark:text-gray-400">Type:</span>
                        <span className="font-medium text-gray-900 dark:text-white">
                          {hull.hullType}
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-500 dark:text-gray-400">Units:</span>
                        <span className="font-medium text-gray-900 dark:text-white">
                          {hull.units}
                        </span>
                      </div>
                    </div>
                  </div>
                )}
              </div>

              {/* Right Column */}
              <div className="space-y-6">
                {/* Principal Particulars */}
                <div>
                  <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
                    Principal Particulars
                  </h4>
                  <div className="grid grid-cols-2 gap-4">
                    {hull.lpp != null && (
                      <div>
                        <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">
                          Length (Lpp)
                        </div>
                        <div className="text-base font-semibold text-gray-900 dark:text-white">
                          {hull.lpp.toFixed(2)} m
                        </div>
                      </div>
                    )}
                    {hull.beam != null && (
                      <div>
                        <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">
                          Beam (B)
                        </div>
                        <div className="text-base font-semibold text-gray-900 dark:text-white">
                          {hull.beam.toFixed(2)} m
                        </div>
                      </div>
                    )}
                    {hull.draft != null && (
                      <div>
                        <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">
                          Draft (T)
                        </div>
                        <div className="text-base font-semibold text-gray-900 dark:text-white">
                          {hull.draft.toFixed(2)} m
                        </div>
                      </div>
                    )}
                    {hull.cb != null && (
                      <div>
                        <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">
                          Block Coefficient (Cb)
                        </div>
                        <div className="text-base font-semibold text-gray-900 dark:text-white">
                          {hull.cb.toFixed(3)}
                        </div>
                      </div>
                    )}
                  </div>

                  {/* Missing fields warning */}
                  {[hull.lpp, hull.beam, hull.draft, hull.cb].some((v) => v == null) && (
                    <div className="mt-4 p-3 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded text-xs text-yellow-800 dark:text-yellow-300">
                      Some principal particulars are missing or not available.
                    </div>
                  )}
                </div>

                {/* Geometry Status */}
                <div>
                  <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
                    Geometry Status
                  </h4>
                  <div className="p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
                    <div className="flex items-center gap-2">
                      {hull.geometryMissing ? (
                        <>
                          <XCircle className="h-5 w-5 text-red-500 dark:text-red-400" />
                          <div>
                            <div className="text-sm font-medium text-red-700 dark:text-red-300">
                              Geometry Not Available
                            </div>
                            <div className="text-xs text-red-600 dark:text-red-400 mt-1">
                              This vessel's geometry data is not in the catalog.
                            </div>
                          </div>
                        </>
                      ) : (
                        <>
                          <CheckCircle className="h-5 w-5 text-green-500 dark:text-green-400" />
                          <div>
                            <div className="text-sm font-medium text-green-700 dark:text-green-300">
                              Geometry Available
                            </div>
                            <div className="text-xs text-green-600 dark:text-green-400 mt-1">
                              Complete station, waterline, and offset data included.
                            </div>
                          </div>
                        </>
                      )}
                    </div>
                  </div>
                </div>

                {/* Actions */}
                <div className="space-y-3">
                  <Button
                    className="w-full"
                    disabled={hull.geometryMissing}
                    onClick={() => onClone?.(hull)}
                  >
                    <Copy className="h-4 w-4 mr-2" />
                    Clone to My Vessels
                  </Button>
                  {hull.geometryMissing && (
                    <p className="text-xs text-center text-gray-500 dark:text-gray-400">
                      Geometry must be available to clone
                    </p>
                  )}
                  <Button
                    variant="outline"
                    className="w-full"
                    onClick={() => onViewDetails?.(hull)}
                  >
                    <ExternalLink className="h-4 w-4 mr-2" />
                    View Full Details
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};
