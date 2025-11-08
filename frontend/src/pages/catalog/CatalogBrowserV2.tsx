import React, { useEffect, useState, useMemo } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate } from "react-router-dom";
import { BookOpen } from "lucide-react";
import { useStore } from "../../stores";
import { getCatalogHulls, cloneCatalogHull } from "../../services/catalogApi";
import { CatalogHullListItem } from "../../types/catalog";
import { AppHeader } from "../../components/AppHeader";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { CatalogTable } from "../../components/catalog/CatalogTable";
import { CatalogDetailPanel } from "../../components/catalog/CatalogDetailPanel";
import { FilterPanel, CatalogFilters } from "../../components/catalog/FilterPanel";
import { BatchActionBar } from "../../components/catalog/BatchActionBar";
import { toast } from "react-hot-toast";

// Helper to determine data quality status
function getDataQualityStatus(hull: CatalogHullListItem): string {
  if (hull.geometryMissing) return "Missing Geometry";
  const requiredFields = [hull.lpp, hull.beam, hull.draft, hull.cb];
  const missingCount = requiredFields.filter((f) => f == null).length;
  if (missingCount === 0) return "Complete";
  if (missingCount <= 2) return "Partial";
  return "Minimal";
}

export const CatalogBrowserV2: React.FC = observer(() => {
  const { authStore } = useStore();
  const navigate = useNavigate();
  const [hulls, setHulls] = useState<CatalogHullListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedHull, setSelectedHull] = useState<CatalogHullListItem | null>(null);
  const [isPanelOpen, setIsPanelOpen] = useState(false);
  const [cloning, setCloning] = useState(false);
  const [selectedVessels, setSelectedVessels] = useState<CatalogHullListItem[]>([]);
  const [filters, setFilters] = useState<CatalogFilters>({
    searchText: "",
    hullTypes: [],
    statuses: [],
    lppRange: [0, 500],
    cbRange: [0.3, 1.0],
  });

  useEffect(() => {
    loadHulls();
  }, []);

  const loadHulls = async () => {
    setLoading(true);
    try {
      const data = await getCatalogHulls();
      setHulls(data);
    } catch (error) {
      console.error("Failed to load catalog:", error);
      toast.error("Failed to load catalog data");
    } finally {
      setLoading(false);
    }
  };

  const handleRowClick = (hull: CatalogHullListItem) => {
    setSelectedHull(hull);
    setIsPanelOpen(true);
  };

  const handleClosePanel = () => {
    setIsPanelOpen(false);
    // Delay clearing selectedHull to allow slide-out animation
    setTimeout(() => setSelectedHull(null), 300);
  };

  const handleClone = async (hull: CatalogHullListItem) => {
    if (hull.geometryMissing) {
      toast.error("Cannot clone: Geometry is missing for this hull");
      return;
    }

    setCloning(true);
    try {
      const response = await cloneCatalogHull(hull.id, {
        vesselName: `${hull.title} (Cloned)`,
      });
      toast.success(response.message);
      navigate("/hydrostatics/vessels");
    } catch (error) {
      console.error("Failed to clone hull:", error);
      toast.error("Failed to clone hull");
    } finally {
      setCloning(false);
    }
  };

  const handleViewDetails = (hull: CatalogHullListItem) => {
    navigate(`/catalog/hulls/${hull.id}`);
  };

  const handleLogout = async () => {
    await authStore.logout();
    navigate("/login");
  };

  const handleBatchClone = async () => {
    const vesselsToClone = selectedVessels.filter((v) => !v.geometryMissing);
    if (vesselsToClone.length === 0) {
      toast.error("No vessels with geometry selected");
      return;
    }

    setCloning(true);
    let successCount = 0;
    let errorCount = 0;

    for (const vessel of vesselsToClone) {
      try {
        await cloneCatalogHull(vessel.id, {
          vesselName: `${vessel.title} (Cloned)`,
        });
        successCount++;
      } catch (error) {
        console.error(`Failed to clone ${vessel.title}:`, error);
        errorCount++;
      }
    }

    setCloning(false);
    setSelectedVessels([]);

    if (successCount > 0) {
      toast.success(`Successfully cloned ${successCount} vessel${successCount > 1 ? "s" : ""}`);
      setTimeout(() => navigate("/hydrostatics/vessels"), 1000);
    }
    if (errorCount > 0) {
      toast.error(`Failed to clone ${errorCount} vessel${errorCount > 1 ? "s" : ""}`);
    }
  };

  const handleBatchCompare = () => {
    if (selectedVessels.length < 2) {
      toast.error("Please select at least 2 vessels to compare");
      return;
    }
    // TODO: Implement comparison view
    toast.success(`Comparing ${selectedVessels.length} vessels (feature coming soon)`);
  };

  const handleBatchExport = () => {
    if (selectedVessels.length === 0) {
      toast.error("No vessels selected");
      return;
    }

    // Export as JSON
    const exportData = selectedVessels.map((v) => ({
      id: v.id,
      title: v.title,
      slug: v.slug,
      description: v.description,
      hullType: v.hullType,
      lpp: v.lpp,
      beam: v.beam,
      draft: v.draft,
      cb: v.cb,
      geometryMissing: v.geometryMissing,
      units: v.units,
    }));

    const blob = new Blob([JSON.stringify(exportData, null, 2)], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `catalog-vessels-${new Date().toISOString().split("T")[0]}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);

    toast.success(`Exported ${selectedVessels.length} vessels to JSON`);
  };

  // Filter hulls based on active filters
  const filteredHulls = useMemo(() => {
    return hulls.filter((hull) => {
      // Text search
      if (filters.searchText) {
        const searchLower = filters.searchText.toLowerCase();
        const matchesSearch =
          hull.title.toLowerCase().includes(searchLower) ||
          hull.slug.toLowerCase().includes(searchLower) ||
          (hull.description && hull.description.toLowerCase().includes(searchLower));
        if (!matchesSearch) return false;
      }

      // Hull type filter
      if (filters.hullTypes.length > 0) {
        if (!hull.hullType || !filters.hullTypes.includes(hull.hullType)) {
          return false;
        }
      }

      // Status filter
      if (filters.statuses.length > 0) {
        const status = getDataQualityStatus(hull);
        if (!filters.statuses.includes(status)) {
          return false;
        }
      }

      // Lpp range filter
      if (hull.lpp != null) {
        if (hull.lpp < filters.lppRange[0] || hull.lpp > filters.lppRange[1]) {
          return false;
        }
      }

      // Cb range filter
      if (hull.cb != null) {
        if (hull.cb < filters.cbRange[0] || hull.cb > filters.cbRange[1]) {
          return false;
        }
      }

      return true;
    });
  }, [hulls, filters]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100 dark:from-gray-900 dark:to-gray-800 flex flex-col">
      <AppHeader
        left={
          <>
            <div className="rounded-lg bg-purple-600 p-2">
              <BookOpen className="h-5 w-5 text-white" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-gray-900 dark:text-white">Catalog</h1>
              <p className="text-sm text-muted-foreground dark:text-gray-400">
                Reference data for naval architecture
              </p>
            </div>
          </>
        }
        right={
          <>
            <button
              onClick={() => navigate("/dashboard")}
              className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-foreground hover:text-foreground/80 border border-border rounded hover:bg-accent/10"
            >
              <svg className="h-4 w-4 mr-1.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"
                />
              </svg>
              Home
            </button>
            <UserProfileMenu onOpenSettings={() => {}} onLogout={handleLogout} />
          </>
        }
      />

      <main className="container mx-auto px-4 py-8 flex-1">
        <FilterPanel
          filters={filters}
          onFiltersChange={setFilters}
          totalResults={filteredHulls.length}
        />
        <CatalogTable
          data={filteredHulls}
          onRowClick={handleRowClick}
          onSelectionChange={setSelectedVessels}
          isLoading={loading}
        />
      </main>

      {/* Batch Action Bar */}
      <BatchActionBar
        selectedVessels={selectedVessels}
        onClear={() => setSelectedVessels([])}
        onCloneAll={handleBatchClone}
        onCompare={handleBatchCompare}
        onExport={handleBatchExport}
      />

      {/* Detail Panel */}
      {selectedHull && (
        <CatalogDetailPanel
          hull={selectedHull}
          isOpen={isPanelOpen}
          onClose={handleClosePanel}
          onClone={handleClone}
          onViewDetails={handleViewDetails}
        />
      )}

      {/* Loading overlay for clone operation */}
      {cloning && (
        <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center">
          <div className="bg-white dark:bg-gray-800 rounded-lg p-6 flex items-center gap-3">
            <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600" />
            <span className="text-gray-900 dark:text-white">Cloning vessel...</span>
          </div>
        </div>
      )}
    </div>
  );
});
