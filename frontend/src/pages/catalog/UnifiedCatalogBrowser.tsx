import React, { useState, useEffect } from "react";
import { observer } from "mobx-react-lite";
import { AppHeader } from "../../components/AppHeader";
import { Footer } from "../../components/Footer";
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
  Plus,
  Lock,
  Edit,
  Eye,
} from "lucide-react";
import { api } from "../../services/api";
import { useNavigate } from "react-router-dom";

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

  // Load data based on mode
  useEffect(() => {
    if (mode === "ml") {
      loadMlStats();
      loadMlHulls();
    } else {
      loadRealVessels();
    }
  }, [mode, mlPage, datasetFilter, minCb, maxCb, sortBy]);

  const loadMlStats = async () => {
    try {
      const response = await api.get("/catalog/parametric/stats");
      setMlStats(response.data);
    } catch (error) {
      console.error("Failed to load ML catalog stats:", error);
    }
  };

  const loadMlHulls = async () => {
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
  };

  const loadRealVessels = async () => {
    setRealLoading(true);
    try {
      const response = await api.get("/catalog/hulls");
      setRealVessels(response.data);
    } catch (error) {
      console.error("Failed to load real vessels:", error);
    } finally {
      setRealLoading(false);
    }
  };

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

  return (
    <div className="flex min-h-screen flex-col bg-background">
      <AppHeader />

      <main className="flex-1 container mx-auto px-4 py-6">
        {/* Header with Mode Toggle */}
        <div className="mb-6">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-3">
              <div
                className={`flex h-12 w-12 items-center justify-center rounded-lg ${
                  mode === "real" ? "bg-green-500/10" : "bg-purple-500/10"
                }`}
              >
                {mode === "real" ? (
                  <Ship className="h-6 w-6 text-green-600" />
                ) : (
                  <Cpu className="h-6 w-6 text-purple-600" />
                )}
              </div>
              <div>
                <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Vessel Catalog</h1>
                <p className="text-sm text-gray-600 dark:text-gray-400">
                  {mode === "real"
                    ? "Real-world vessels - Editable & expandable"
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
                    ? "bg-white dark:bg-gray-700 shadow text-green-700 dark:text-green-400 font-semibold"
                    : "text-gray-600 dark:text-gray-400 hover:text-gray-900"
                }`}
              >
                <Ship className="h-4 w-4" />
                <span>Real-World</span>
                <Edit className="h-3 w-3" />
              </button>
              <button
                onClick={() => setMode("ml")}
                className={`flex items-center gap-2 px-4 py-2 rounded-md transition-all ${
                  mode === "ml"
                    ? "bg-white dark:bg-gray-700 shadow text-purple-700 dark:text-purple-400 font-semibold"
                    : "text-gray-600 dark:text-gray-400 hover:text-gray-900"
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
            <div className="flex items-center gap-2 px-4 py-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg">
              <Edit className="h-4 w-4 text-green-600 dark:text-green-400" />
              <p className="text-sm text-green-800 dark:text-green-300">
                <strong>Editable:</strong> You can add, edit, and delete vessels in this catalog
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
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
              <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
                <div className="flex items-center gap-2 mb-1">
                  <Ship className="h-4 w-4 text-green-600" />
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
                  <Edit className="h-4 w-4 text-green-600" />
                  <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Editable
                  </span>
                </div>
                <p className="text-lg font-bold text-green-600">✓ Full Access</p>
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
                <Button
                  className="w-full bg-green-600 hover:bg-green-700"
                  onClick={() => navigate("/catalog/vessels/new")}
                >
                  <Plus className="h-4 w-4 mr-2" />
                  Add New Vessel
                </Button>
              </div>
            </div>

            {/* Real Vessels Grid */}
            {realLoading ? (
              <div className="flex items-center justify-center py-12">
                <div className="text-center">
                  <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-green-600 mx-auto mb-4"></div>
                  <p className="text-gray-600 dark:text-gray-400">Loading vessels...</p>
                </div>
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
                {realVessels.map((vessel) => (
                  <div
                    key={vessel.id}
                    className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700 hover:border-green-500 hover:shadow-lg transition-all cursor-pointer"
                    onClick={() => navigate(`/catalog/hulls/${vessel.id}`)}
                  >
                    <div className="flex items-start justify-between mb-3">
                      <div>
                        <h3 className="font-bold text-green-900 dark:text-green-300 text-sm">
                          {vessel.name}
                        </h3>
                        {vessel.vesselType && (
                          <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                            {vessel.vesselType}
                          </p>
                        )}
                      </div>
                      <span className="px-2 py-1 text-xs font-semibold rounded bg-green-50 text-green-700">
                        Editable
                      </span>
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
                          <span className="font-bold text-green-600 dark:text-green-400">
                            {vessel.blockCoefficient.toFixed(3)}
                          </span>
                        </div>
                      )}
                    </div>

                    <div className="mt-4 flex gap-2">
                      <Button variant="outline" size="sm" className="flex-1 text-xs">
                        <Edit className="h-3 w-3 mr-1" />
                        Edit
                      </Button>
                      <Button size="sm" className="flex-1 text-xs bg-green-600 hover:bg-green-700">
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
                        <div className="flex flex-col gap-1 items-end">
                          <span
                            className={`px-2 py-1 text-xs font-semibold rounded ${getQualityColor(hull.conversionQuality)}`}
                          >
                            {hull.conversionQuality}
                          </span>
                          <span className="px-2 py-1 text-xs font-semibold rounded bg-purple-50 text-purple-700 flex items-center gap-1">
                            <Lock className="h-2.5 w-2.5" />
                            Read-Only
                          </span>
                        </div>
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

                      <div className="mt-4 flex gap-2">
                        <Button variant="outline" size="sm" className="flex-1 text-xs" disabled>
                          <Eye className="h-3 w-3 mr-1" />
                          View Only
                        </Button>
                        <Button
                          size="sm"
                          className="flex-1 text-xs bg-purple-600 hover:bg-purple-700"
                        >
                          Use in Mission
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
