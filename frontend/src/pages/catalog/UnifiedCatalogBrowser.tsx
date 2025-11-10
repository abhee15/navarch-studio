import React, { useState, useEffect, useCallback } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate } from "react-router-dom";
import toast from "react-hot-toast";
import { AppHeader } from "../../components/AppHeader";
import { Footer } from "../../components/Footer";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { Button } from "../../components/ui/button";
import { Input } from "../../components/ui/input";
import { Label } from "../../components/ui/label";
import {
  Database,
  Filter,
  TrendingUp,
  Sparkles,
  ChevronLeft,
  ChevronRight,
  Ship,
  Cpu,
  Lock,
} from "lucide-react";
import { api } from "../../services/api";
import { useStore } from "../../stores";

type CatalogMode = "real" | "ml";

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

interface RealVessel {
  id: string;
  name: string;
  vesselType?: string;
  lpp?: number;
  beam?: number;
  draft?: number;
  displacement?: number;
  blockCoefficient?: number;
}

interface CatalogStats {
  totalHulls: number;
  byDataset: Record<string, number>;
  avgCb: number;
  cbRange: { min: number; max: number };
}

export const UnifiedCatalogBrowser: React.FC = observer(() => {
  const navigate = useNavigate();
  const { authStore } = useStore();
  const [mode, setMode] = useState<CatalogMode>("real");

  // ML state
  const [mlHulls, setMlHulls] = useState<ParametricHull[]>([]);
  const [mlStats, setMlStats] = useState<CatalogStats | null>(null);
  const [mlLoading, setMlLoading] = useState(false);
  const [mlPage, setMlPage] = useState(1);
  const [mlTotalPages, setMlTotalPages] = useState(1);
  const [mlTotalCount, setMlTotalCount] = useState(0);

  // Real state
  const [realVessels, setRealVessels] = useState<RealVessel[]>([]);
  const [realLoading, setRealLoading] = useState(false);

  // Filters (ML only for now)
  const [datasetFilter, setDatasetFilter] = useState("");
  const [minCb, setMinCb] = useState("");
  const [maxCb, setMaxCb] = useState("");
  const [sortBy, setSortBy] = useState("hull_id");

  const loadMlStats = useCallback(async () => {
    try {
      const response = await api.get("/catalog/parametric/stats");
      setMlStats(response.data);
    } catch (error) {
      console.error("Failed to load ML catalog stats:", error);
    }
  }, []);

  const loadMlHulls = useCallback(async () => {
    setMlLoading(true);
    try {
      const params = new URLSearchParams({
        page: mlPage.toString(),
        pageSize: "20",
        ...(datasetFilter && { dataset: datasetFilter }),
        ...(minCb && { minCb }),
        ...(maxCb && { maxCb }),
        ...(sortBy && { sortBy }),
      });

      const response = await api.get(`/catalog/parametric/browse?${params}`);
      setMlHulls(response.data.items);
      setMlTotalPages(response.data.totalPages);
      setMlTotalCount(response.data.totalCount);
    } catch (error) {
      console.error("Failed to load ML hulls:", error);
    } finally {
      setMlLoading(false);
    }
  }, [mlPage, datasetFilter, minCb, maxCb, sortBy]);

  const loadRealVessels = useCallback(async () => {
    setRealLoading(true);
    try {
      // Load real-world vessel catalog (600+ vessels)
      const response = await api.get<RealVessel[]>("/catalog/hulls");
      setRealVessels(response.data);
    } catch (error) {
      console.error("Failed to load real-world vessels:", error);
      toast.error("Failed to load vessel catalog");
    } finally {
      setRealLoading(false);
    }
  }, []);

  // Load data based on mode
  useEffect(() => {
    if (mode === "ml") {
      loadMlStats();
      loadMlHulls();
    } else {
      loadRealVessels();
    }
  }, [mode, loadMlStats, loadMlHulls, loadRealVessels]);

  const resetFilters = () => {
    setDatasetFilter("");
    setMinCb("");
    setMaxCb("");
    setSortBy("hull_id");
    setMlPage(1);
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
        {/* Header with Mode Toggle */}
        <div className="mb-6">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-3">
              <div
                className={`flex h-12 w-12 items-center justify-center rounded-lg ${
                  mode === "real" ? "bg-blue-500/10" : "bg-purple-500/10"
                }`}
              >
                {mode === "real" ? (
                  <Ship className="h-6 w-6 text-blue-600" />
                ) : (
                  <Cpu className="h-6 w-6 text-purple-600" />
                )}
              </div>
              <div>
                <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Vessel Catalog</h1>
                <p className="text-sm text-gray-600 dark:text-gray-400">
                  {mode === "real"
                    ? "Real-world vessels - Curated reference data"
                    : "ML/Parametric hulls - Read-only reference data"}
                </p>
              </div>
            </div>

            {/* Mode Toggle */}
            <div className="flex items-center gap-3 bg-gray-100 dark:bg-gray-800 rounded-lg p-1">
              <button
                onClick={() => setMode("real")}
                className={`flex items-center gap-2 px-4 py-2 rounded-md transition-all ${
                  mode === "real"
                    ? "bg-white dark:bg-gray-700 shadow text-blue-700 dark:text-blue-400 font-semibold"
                    : "text-gray-600 dark:text-gray-400 hover:text-foreground"
                }`}
              >
                <Ship className="h-4 w-4" />
                <span>Real-World</span>
              </button>
              <button
                onClick={() => setMode("ml")}
                className={`flex items-center gap-2 px-4 py-2 rounded-md transition-all ${
                  mode === "ml"
                    ? "bg-white dark:bg-gray-700 shadow text-purple-700 dark:text-purple-400 font-semibold"
                    : "text-gray-600 dark:text-gray-400 hover:text-foreground"
                }`}
              >
                <Cpu className="h-4 w-4" />
                <span>ML/Parametric</span>
                <Lock className="h-3 w-3" />
              </button>
            </div>
          </div>

          {/* Permission Banner */}
          {mode === "real" ? (
            <div className="flex items-center gap-2 px-4 py-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
              <Database className="h-4 w-4 text-blue-600 dark:text-blue-400" />
              <p className="text-sm text-blue-800 dark:text-blue-300">
                <strong>Curated Data:</strong> Real-world vessel catalog with 600+ vessels from
                various sources
              </p>
            </div>
          ) : (
            <div className="flex items-center gap-2 px-4 py-3 bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-800 rounded-lg">
              <Lock className="h-4 w-4 text-purple-600 dark:text-purple-400" />
              <p className="text-sm text-purple-800 dark:text-purple-300">
                <strong>Read-Only:</strong> ML/Parametric hulls are reference data from MIT ShipD
                Dataset (82,000+ hulls)
              </p>
            </div>
          )}
        </div>

        {/* Real-World Catalog */}
        {mode === "real" && (
          <>
            {/* Stats */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
              <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
                <div className="flex items-center gap-2 mb-1">
                  <Ship className="h-4 w-4 text-blue-600" />
                  <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Total Vessels
                  </span>
                </div>
                <p className="text-2xl font-bold text-gray-900 dark:text-white">
                  {realVessels.length}
                </p>
              </div>

              <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
                <div className="flex items-center gap-2 mb-1">
                  <Database className="h-4 w-4 text-blue-600" />
                  <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Data Source
                  </span>
                </div>
                <p className="text-sm font-semibold text-gray-900 dark:text-white">
                  Curated Vessels
                </p>
              </div>

              <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
                <div className="flex items-center gap-2 mb-1">
                  <Lock className="h-4 w-4 text-gray-600" />
                  <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Access
                  </span>
                </div>
                <p className="text-lg font-bold text-gray-600">Read-Only</p>
              </div>
            </div>

            {/* Real Vessels Grid */}
            {realLoading ? (
              <div className="flex items-center justify-center py-12">
                <div className="text-center">
                  <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
                  <p className="text-gray-600 dark:text-gray-400">Loading vessels...</p>
                </div>
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
                {realVessels.map((vessel) => (
                  <div
                    key={vessel.id}
                    className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700 hover:border-blue-500 hover:shadow-lg transition-all cursor-pointer"
                    onClick={() => navigate(`/catalog/hulls/${vessel.id}`)}
                  >
                    <div className="mb-3">
                      <h3 className="font-bold text-blue-900 dark:text-blue-300 text-sm">
                        {vessel.name}
                      </h3>
                      {vessel.vesselType && (
                        <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                          {vessel.vesselType}
                        </p>
                      )}
                    </div>

                    <div className="space-y-2">
                      {vessel.lpp && (
                        <div className="flex justify-between text-xs">
                          <span className="text-gray-600 dark:text-gray-400">Lpp:</span>
                          <span className="font-semibold text-gray-900 dark:text-white">
                            {vessel.lpp.toFixed(2)} m
                          </span>
                        </div>
                      )}
                      {vessel.beam && (
                        <div className="flex justify-between text-xs">
                          <span className="text-gray-600 dark:text-gray-400">Beam:</span>
                          <span className="font-semibold text-gray-900 dark:text-white">
                            {vessel.beam.toFixed(2)} m
                          </span>
                        </div>
                      )}
                      {vessel.draft && (
                        <div className="flex justify-between text-xs">
                          <span className="text-gray-600 dark:text-gray-400">Draft:</span>
                          <span className="font-semibold text-gray-900 dark:text-white">
                            {vessel.draft.toFixed(2)} m
                          </span>
                        </div>
                      )}
                      {vessel.blockCoefficient && (
                        <div className="flex justify-between text-xs border-t border-gray-200 dark:border-gray-700 pt-2">
                          <span className="text-gray-600 dark:text-gray-400">Cb:</span>
                          <span className="font-bold text-blue-600 dark:text-blue-400">
                            {vessel.blockCoefficient.toFixed(3)}
                          </span>
                        </div>
                      )}
                    </div>

                    <div className="mt-4">
                      <Button size="sm" className="w-full text-xs bg-blue-600 hover:bg-blue-700">
                        View Details
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </>
        )}

        {/* ML/Parametric Catalog */}
        {mode === "ml" && (
          <>
            {/* ML Stats */}
            {mlStats && (
              <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
                <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
                  <div className="flex items-center gap-2 mb-1">
                    <Database className="h-4 w-4 text-purple-600" />
                    <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                      Total Hulls
                    </span>
                  </div>
                  <p className="text-2xl font-bold text-gray-900 dark:text-white">
                    {mlStats.totalHulls.toLocaleString()}
                  </p>
                </div>

                <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
                  <div className="flex items-center gap-2 mb-1">
                    <TrendingUp className="h-4 w-4 text-blue-600" />
                    <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                      Avg Cb
                    </span>
                  </div>
                  <p className="text-2xl font-bold text-gray-900 dark:text-white">
                    {mlStats.avgCb.toFixed(3)}
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
                    {mlStats.cbRange.min.toFixed(2)} - {mlStats.cbRange.max.toFixed(2)}
                  </p>
                </div>

                <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
                  <div className="flex items-center gap-2 mb-1">
                    <Lock className="h-4 w-4 text-purple-600" />
                    <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                      Access
                    </span>
                  </div>
                  <p className="text-lg font-bold text-purple-600">Read-Only</p>
                </div>
              </div>
            )}

            {/* ML Filters */}
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
                      setMlPage(1);
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
                      setMlPage(1);
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
                      setMlPage(1);
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
                <Button size="sm" onClick={() => setMlPage(1)}>
                  Apply
                </Button>
              </div>
            </div>

            {/* ML Hulls Grid */}
            {mlLoading ? (
              <div className="flex items-center justify-center py-12">
                <div className="text-center">
                  <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-purple-600 mx-auto mb-4"></div>
                  <p className="text-gray-600 dark:text-gray-400">Loading hulls...</p>
                </div>
              </div>
            ) : (
              <>
                {/* Results Header */}
                <div className="flex items-center justify-between mb-4">
                  <p className="text-sm text-gray-600 dark:text-gray-400">
                    Showing {(mlPage - 1) * 20 + 1}-{Math.min(mlPage * 20, mlTotalCount)} of{" "}
                    {mlTotalCount.toLocaleString()} hulls
                  </p>

                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setMlPage((p) => Math.max(1, p - 1))}
                      disabled={mlPage === 1}
                    >
                      <ChevronLeft className="h-4 w-4" />
                      Previous
                    </Button>
                    <span className="text-sm text-gray-600 dark:text-gray-400">
                      Page {mlPage} of {mlTotalPages}
                    </span>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setMlPage((p) => Math.min(mlTotalPages, p + 1))}
                      disabled={mlPage === mlTotalPages}
                    >
                      Next
                      <ChevronRight className="h-4 w-4" />
                    </Button>
                  </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
                  {mlHulls.map((hull) => (
                    <div
                      key={hull.hullId}
                      className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700 hover:border-purple-500 hover:shadow-lg transition-all"
                    >
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
                      </div>

                      <div className="mt-4">
                        <Button
                          size="sm"
                          className="w-full text-xs bg-purple-600 hover:bg-purple-700"
                        >
                          Use in Brief
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>

                {/* Pagination */}
                {mlHulls.length > 0 && (
                  <div className="mt-6 flex items-center justify-center gap-2">
                    <Button variant="outline" onClick={() => setMlPage(1)} disabled={mlPage === 1}>
                      First
                    </Button>
                    <Button
                      variant="outline"
                      onClick={() => setMlPage((p) => Math.max(1, p - 1))}
                      disabled={mlPage === 1}
                    >
                      <ChevronLeft className="h-4 w-4" />
                    </Button>

                    <div className="flex items-center gap-2">
                      {Array.from({ length: Math.min(5, mlTotalPages) }, (_, i) => {
                        const pageNum = mlPage - 2 + i;
                        if (pageNum < 1 || pageNum > mlTotalPages) return null;
                        return (
                          <Button
                            key={pageNum}
                            variant={pageNum === mlPage ? "default" : "outline"}
                            size="sm"
                            onClick={() => setMlPage(pageNum)}
                          >
                            {pageNum}
                          </Button>
                        );
                      })}
                    </div>

                    <Button
                      variant="outline"
                      onClick={() => setMlPage((p) => Math.min(mlTotalPages, p + 1))}
                      disabled={mlPage === mlTotalPages}
                    >
                      <ChevronRight className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="outline"
                      onClick={() => setMlPage(mlTotalPages)}
                      disabled={mlPage === mlTotalPages}
                    >
                      Last
                    </Button>
                  </div>
                )}
              </>
            )}
          </>
        )}
      </main>

      <Footer />
    </div>
  );
});
