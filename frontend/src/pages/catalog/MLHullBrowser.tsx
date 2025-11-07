import React, { useState, useEffect } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate } from "react-router-dom";
import { AppHeader } from "../../components/AppHeader";
import { Footer } from "../../components/Footer";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { Button } from "../../components/ui/button";
import { Input } from "../../components/ui/input";
import { Label } from "../../components/ui/label";
import { Database, Filter, TrendingUp, Sparkles, ChevronLeft, ChevronRight } from "lucide-react";
import { api } from "../../services/api";
import { useStore } from "../../stores";

interface ParametricHull {
  hullId: string;
  datasetSource: string;
  lppM: number;
  beamM: number;
  draftM: number;
  cb: number;
  volumeNorm: number;
  lcbNorm: number;
  conversionQuality: string;
}

interface CatalogStats {
  totalHulls: number;
  byDataset: Record<string, number>;
  avgCb: number;
  cbRange: { min: number; max: number };
}

export const MLHullBrowser: React.FC = observer(() => {
  const navigate = useNavigate();
  const { authStore } = useStore();
  const [hulls, setHulls] = useState<ParametricHull[]>([]);
  const [stats, setStats] = useState<CatalogStats | null>(null);
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  // Filters
  const [datasetFilter, setDatasetFilter] = useState("");
  const [minCb, setMinCb] = useState("");
  const [maxCb, setMaxCb] = useState("");
  const [sortBy, setSortBy] = useState("hull_id");

  const loadStats = useCallback(async () => {
    try {
      const response = await api.get("/catalog/parametric/stats");
      setStats(response.data);
    } catch (error) {
      console.error("Failed to load catalog stats:", error);
    }
  }, []);

  const loadHulls = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: "20",
        ...(datasetFilter && { dataset: datasetFilter }),
        ...(minCb && { minCb }),
        ...(maxCb && { maxCb }),
        ...(sortBy && { sortBy }),
      });

      const response = await api.get(`/catalog/parametric/browse?${params}`);
      setHulls(response.data.items);
      setTotalPages(response.data.totalPages);
      setTotalCount(response.data.totalCount);
    } catch (error) {
      console.error("Failed to load hulls:", error);
    } finally {
      setLoading(false);
    }
  }, [page, datasetFilter, minCb, maxCb, sortBy]);

  // Load stats on mount
  useEffect(() => {
    loadStats();
  }, [loadStats]);

  // Load hulls when filters/page change
  useEffect(() => {
    loadHulls();
  }, [loadHulls]);

  const resetFilters = () => {
    setDatasetFilter("");
    setMinCb("");
    setMaxCb("");
    setSortBy("hull_id");
    setPage(1);
  };

  const getQualityColor = (quality: string) => {
    switch (quality) {
      case "Excellent":
        return "text-green-600 bg-green-50";
      case "Good":
        return "text-blue-600 bg-blue-50";
      case "Fair":
        return "text-yellow-600 bg-yellow-50";
      case "Poor":
        return "text-red-600 bg-red-50";
      default:
        return "text-gray-600 bg-gray-50";
    }
  };

  const handleHome = () => {
    navigate("/dashboard");
  };

  const handleLogout = () => {
    authStore.logout();
    navigate("/login");
  };

  return (
    <div className="flex min-h-screen flex-col bg-background">
      <AppHeader
        left={<h1 className="text-lg font-bold text-foreground">NavArch Studio</h1>}
        right={
          <>
            <button
              onClick={handleHome}
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

      <main className="flex-1 container mx-auto px-4 py-6">
        {/* Header */}
        <div className="mb-6">
          <div className="flex items-center gap-3 mb-2">
            <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-purple-500/10">
              <Database className="h-6 w-6 text-purple-600" />
            </div>
            <div>
              <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
                ML/Parametric Hull Catalog
              </h1>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                Browse 82,000+ synthetic hulls from MIT ShipD Dataset
              </p>
            </div>
          </div>
        </div>

        {/* Stats Cards */}
        {stats && (
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
            <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
              <div className="flex items-center gap-2 mb-1">
                <Database className="h-4 w-4 text-purple-600" />
                <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                  Total Hulls
                </span>
              </div>
              <p className="text-2xl font-bold text-gray-900 dark:text-white">
                {stats.totalHulls.toLocaleString()}
              </p>
            </div>

            <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
              <div className="flex items-center gap-2 mb-1">
                <TrendingUp className="h-4 w-4 text-blue-600" />
                <span className="text-sm font-medium text-gray-600 dark:text-gray-400">Avg Cb</span>
              </div>
              <p className="text-2xl font-bold text-gray-900 dark:text-white">
                {stats.avgCb.toFixed(3)}
              </p>
            </div>

            <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
              <div className="flex items-center gap-2 mb-1">
                <Sparkles className="h-4 w-4 text-green-600" />
                <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                  Cb Range
                </span>
              </div>
              <p className="text-lg font-bold text-gray-900 dark:text-white">
                {stats.cbRange.min.toFixed(2)} - {stats.cbRange.max.toFixed(2)}
              </p>
            </div>

            <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
              <div className="flex items-center gap-2 mb-1">
                <Filter className="h-4 w-4 text-orange-600" />
                <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                  Datasets
                </span>
              </div>
              <p className="text-xl font-bold text-gray-900 dark:text-white">
                {Object.keys(stats.byDataset).length}
              </p>
            </div>
          </div>
        )}

        {/* Filters */}
        <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700 mb-6">
          <div className="flex items-center gap-2 mb-4">
            <Filter className="h-4 w-4 text-gray-600" />
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white">Filters</h2>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div>
              <Label htmlFor="dataset">Dataset</Label>
              <select
                id="dataset"
                value={datasetFilter}
                onChange={(e) => {
                  setDatasetFilter(e.target.value);
                  setPage(1);
                }}
                className="mt-1 w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-sm"
              >
                <option value="">All Datasets</option>
                <option value="Constrained">Constrained</option>
                <option value="Diffusion">Diffusion</option>
              </select>
            </div>

            <div>
              <Label htmlFor="minCb">Min Cb</Label>
              <Input
                id="minCb"
                type="number"
                step="0.05"
                placeholder="0.30"
                value={minCb}
                onChange={(e) => {
                  setMinCb(e.target.value);
                  setPage(1);
                }}
              />
            </div>

            <div>
              <Label htmlFor="maxCb">Max Cb</Label>
              <Input
                id="maxCb"
                type="number"
                step="0.05"
                placeholder="0.90"
                value={maxCb}
                onChange={(e) => {
                  setMaxCb(e.target.value);
                  setPage(1);
                }}
              />
            </div>

            <div>
              <Label htmlFor="sortBy">Sort By</Label>
              <select
                id="sortBy"
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value)}
                className="mt-1 w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-sm"
              >
                <option value="hull_id">Hull ID</option>
                <option value="cb">Block Coefficient</option>
                <option value="volume">Volume</option>
                <option value="lcb">LCB</option>
                <option value="lpp">Length</option>
              </select>
            </div>
          </div>

          <div className="mt-4 flex gap-2">
            <Button variant="outline" size="sm" onClick={resetFilters}>
              Reset Filters
            </Button>
            <Button size="sm" onClick={() => setPage(1)}>
              Apply
            </Button>
          </div>
        </div>

        {/* Results Header */}
        <div className="flex items-center justify-between mb-4">
          <p className="text-sm text-gray-600 dark:text-gray-400">
            Showing {(page - 1) * 20 + 1}-{Math.min(page * 20, totalCount)} of{" "}
            {totalCount.toLocaleString()} hulls
          </p>

          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
            >
              <ChevronLeft className="h-4 w-4" />
              Previous
            </Button>
            <span className="text-sm text-gray-600 dark:text-gray-400">
              Page {page} of {totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
            >
              Next
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>

        {/* Grid */}
        {loading ? (
          <div className="flex items-center justify-center py-12">
            <div className="text-center">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-purple-600 mx-auto mb-4"></div>
              <p className="text-gray-600 dark:text-gray-400">Loading hulls...</p>
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {hulls.map((hull) => (
              <div
                key={hull.hullId}
                className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700 hover:border-purple-500 hover:shadow-lg transition-all"
              >
                {/* Hull Header */}
                <div className="flex items-start justify-between mb-3">
                  <div>
                    <h3 className="font-mono font-bold text-purple-900 dark:text-purple-300 text-sm">
                      {hull.hullId}
                    </h3>
                    <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                      {hull.datasetSource}
                    </p>
                  </div>
                  <span
                    className={`px-2 py-1 text-xs font-semibold rounded ${getQualityColor(hull.conversionQuality)}`}
                  >
                    {hull.conversionQuality}
                  </span>
                </div>

                {/* Dimensions */}
                <div className="space-y-2">
                  <div className="flex justify-between text-xs">
                    <span className="text-gray-600 dark:text-gray-400">Lpp:</span>
                    <span className="font-semibold text-gray-900 dark:text-white">
                      {hull.lppM.toFixed(2)} m
                    </span>
                  </div>
                  <div className="flex justify-between text-xs">
                    <span className="text-gray-600 dark:text-gray-400">Beam:</span>
                    <span className="font-semibold text-gray-900 dark:text-white">
                      {hull.beamM.toFixed(2)} m
                    </span>
                  </div>
                  <div className="flex justify-between text-xs">
                    <span className="text-gray-600 dark:text-gray-400">Draft:</span>
                    <span className="font-semibold text-gray-900 dark:text-white">
                      {hull.draftM.toFixed(2)} m
                    </span>
                  </div>
                  <div className="flex justify-between text-xs border-t border-gray-200 dark:border-gray-700 pt-2">
                    <span className="text-gray-600 dark:text-gray-400">Cb:</span>
                    <span className="font-bold text-purple-600 dark:text-purple-400">
                      {hull.cb.toFixed(3)}
                    </span>
                  </div>
                  <div className="flex justify-between text-xs">
                    <span className="text-gray-600 dark:text-gray-400">LCB/Lpp:</span>
                    <span className="font-semibold text-gray-900 dark:text-white">
                      {hull.lcbNorm.toFixed(3)}
                    </span>
                  </div>
                  <div className="flex justify-between text-xs">
                    <span className="text-gray-600 dark:text-gray-400">Volume:</span>
                    <span className="font-mono text-gray-700 dark:text-gray-300 text-[10px]">
                      {hull.volumeNorm.toFixed(6)}
                    </span>
                  </div>
                </div>

                {/* Actions */}
                <div className="mt-4 flex gap-2">
                  <Button variant="outline" size="sm" className="flex-1 text-xs">
                    View Details
                  </Button>
                  <Button size="sm" className="flex-1 text-xs bg-purple-600 hover:bg-purple-700">
                    Use in Brief
                  </Button>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Empty State */}
        {!loading && hulls.length === 0 && (
          <div className="flex flex-col items-center justify-center py-12">
            <Database className="h-16 w-16 text-gray-300 dark:text-gray-600 mb-4" />
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
              No Hulls Found
            </h3>
            <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
              Try adjusting your filters or import the catalog
            </p>
            <Button onClick={resetFilters}>Reset Filters</Button>
          </div>
        )}

        {/* Pagination */}
        {!loading && hulls.length > 0 && (
          <div className="mt-6 flex items-center justify-center gap-2">
            <Button variant="outline" onClick={() => setPage(1)} disabled={page === 1}>
              First
            </Button>
            <Button
              variant="outline"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>

            <div className="flex items-center gap-2">
              {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                const pageNum = page - 2 + i;
                if (pageNum < 1 || pageNum > totalPages) return null;
                return (
                  <Button
                    key={pageNum}
                    variant={pageNum === page ? "default" : "outline"}
                    size="sm"
                    onClick={() => setPage(pageNum)}
                  >
                    {pageNum}
                  </Button>
                );
              })}
            </div>

            <Button
              variant="outline"
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              onClick={() => setPage(totalPages)}
              disabled={page === totalPages}
            >
              Last
            </Button>
          </div>
        )}
      </main>

      <Footer />
    </div>
  );
});
