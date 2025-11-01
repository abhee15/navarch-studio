import React, { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { ArrowLeft, Copy, Loader2, AlertCircle, AlertTriangle } from "lucide-react";
import { useStore } from "../../stores";
import { Button } from "../../components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "../../components/ui/card";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { AppHeader } from "../../components/AppHeader";
import { getCatalogHull, cloneCatalogHull } from "../../services/catalogApi";
import type { CatalogHull } from "../../types/catalog";
import { toast } from "react-hot-toast";

export const HullDetailPage: React.FC = observer(() => {
  const { authStore } = useStore();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [hull, setHull] = useState<CatalogHull | null>(null);
  const [loading, setLoading] = useState(true);
  const [cloning, setCloning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (id) {
      loadHull(id);
    }
  }, [id]);

  const loadHull = async (hullId: string) => {
    setLoading(true);
    setError(null);
    try {
      const data = await getCatalogHull(hullId);
      setHull(data);
    } catch (err) {
      console.error("Failed to load hull:", err);
      setError("Failed to load catalog hull");
      toast.error("Failed to load catalog hull");
    } finally {
      setLoading(false);
    }
  };

  const handleClone = async () => {
    if (!id || !hull) return;

    if (hull.geometryMissing) {
      toast.error("Cannot clone: Geometry is missing for this hull");
      return;
    }

    setCloning(true);
    try {
      const response = await cloneCatalogHull(id, {
        vesselName: `${hull.title} (Cloned)`,
      });
      toast.success(response.message);
      navigate("/hydrostatics/vessels");
    } catch (err) {
      console.error("Failed to clone hull:", err);
      toast.error("Failed to clone hull");
    } finally {
      setCloning(false);
    }
  };

  const handleLogout = async () => {
    await authStore.logout();
    navigate("/login");
  };

  const getHullTypeIcon = (hullType?: string) => {
    switch (hullType) {
      case "Container":
        return "📦";
      case "Tanker":
        return "🛳️";
      case "Naval":
        return "⚓";
      case "Template":
        return "📐";
      default:
        return "🚢";
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100 dark:from-gray-900 dark:to-gray-800 flex flex-col">
        <AppHeader
          left={
            <>
              <div className="rounded-lg bg-purple-600 p-2">
                <ArrowLeft className="h-5 w-5 text-white" />
              </div>
              <div>
                <h1 className="text-xl font-bold text-gray-900 dark:text-white">Loading...</h1>
              </div>
            </>
          }
          right={<UserProfileMenu onOpenSettings={() => {}} onLogout={handleLogout} />}
        />
        <div className="flex-1 flex items-center justify-center">
          <Loader2 className="h-8 w-8 animate-spin text-primary" />
        </div>
      </div>
    );
  }

  if (error || !hull) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100 dark:from-gray-900 dark:to-gray-800 flex flex-col">
        <AppHeader
          left={
            <>
              <div className="rounded-lg bg-purple-600 p-2">
                <ArrowLeft className="h-5 w-5 text-white" />
              </div>
              <div>
                <h1 className="text-xl font-bold text-gray-900 dark:text-white">Error</h1>
              </div>
            </>
          }
          right={<UserProfileMenu onOpenSettings={() => {}} onLogout={handleLogout} />}
        />
        <div className="flex-1 flex items-center justify-center">
          <AlertCircle className="h-8 w-8 text-red-500 mr-2" />
          <span className="text-red-600">{error || "Hull not found"}</span>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100 dark:from-gray-900 dark:to-gray-800 flex flex-col">
      <AppHeader
        left={
          <>
            <Button variant="ghost" size="sm" onClick={() => navigate("/catalog")} className="mr-2">
              <ArrowLeft className="h-4 w-4 mr-1" />
              Back
            </Button>
            <div className="rounded-lg bg-purple-600 p-2">
              <span className="text-2xl">{getHullTypeIcon(hull.hullType)}</span>
            </div>
            <div>
              <h1 className="text-xl font-bold text-gray-900 dark:text-white">{hull.title}</h1>
              <p className="text-sm text-muted-foreground dark:text-gray-400">{hull.slug}</p>
            </div>
          </>
        }
        right={<UserProfileMenu onOpenSettings={() => {}} onLogout={handleLogout} />}
      />

      {/* Main Content */}
      <main className="container mx-auto px-4 py-8 flex-1 max-w-6xl">
        {/* Alert for missing geometry */}
        {hull.geometryMissing && (
          <div className="mb-6 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4 flex items-start">
            <AlertTriangle className="h-5 w-5 text-yellow-600 dark:text-yellow-400 mr-3 flex-shrink-0 mt-0.5" />
            <div>
              <h3 className="font-medium text-yellow-800 dark:text-yellow-300">
                Geometry Not Available
              </h3>
              <p className="text-sm text-yellow-700 dark:text-yellow-400 mt-1">
                This hull's geometry data is not yet available in the catalog. You can still view
                the principal particulars below.
              </p>
            </div>
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Main Info */}
          <div className="lg:col-span-2 space-y-6">
            {/* Description */}
            <Card>
              <CardHeader>
                <CardTitle>Description</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-gray-700 dark:text-gray-300">
                  {hull.description || "No description available"}
                </p>
              </CardContent>
            </Card>

            {/* Principal Particulars */}
            <Card>
              <CardHeader>
                <CardTitle>Principal Particulars</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                  {hull.lpp && (
                    <div>
                      <div className="text-sm text-gray-600 dark:text-gray-400">Length (Lpp)</div>
                      <div className="text-lg font-semibold text-gray-900 dark:text-white">
                        {hull.lpp.toFixed(2)} m
                      </div>
                    </div>
                  )}
                  {hull.beam && (
                    <div>
                      <div className="text-sm text-gray-600 dark:text-gray-400">Beam (B)</div>
                      <div className="text-lg font-semibold text-gray-900 dark:text-white">
                        {hull.beam.toFixed(2)} m
                      </div>
                    </div>
                  )}
                  {hull.draft && (
                    <div>
                      <div className="text-sm text-gray-600 dark:text-gray-400">Draft (T)</div>
                      <div className="text-lg font-semibold text-gray-900 dark:text-white">
                        {hull.draft.toFixed(2)} m
                      </div>
                    </div>
                  )}
                  {hull.cb && (
                    <div>
                      <div className="text-sm text-gray-600 dark:text-gray-400">
                        Block Coeff. (Cb)
                      </div>
                      <div className="text-lg font-semibold text-gray-900 dark:text-white">
                        {hull.cb.toFixed(3)}
                      </div>
                    </div>
                  )}
                  {hull.cp && (
                    <div>
                      <div className="text-sm text-gray-600 dark:text-gray-400">
                        Prismatic Coeff. (Cp)
                      </div>
                      <div className="text-lg font-semibold text-gray-900 dark:text-white">
                        {hull.cp.toFixed(3)}
                      </div>
                    </div>
                  )}
                  {hull.lcb && (
                    <div>
                      <div className="text-sm text-gray-600 dark:text-gray-400">LCB</div>
                      <div className="text-lg font-semibold text-gray-900 dark:text-white">
                        {hull.lcb.toFixed(2)} m
                      </div>
                    </div>
                  )}
                  {hull.lcf && (
                    <div>
                      <div className="text-sm text-gray-600 dark:text-gray-400">LCF</div>
                      <div className="text-lg font-semibold text-gray-900 dark:text-white">
                        {hull.lcf.toFixed(2)} m
                      </div>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>

            {/* References */}
            {hull.canonicalRefs && (
              <Card>
                <CardHeader>
                  <CardTitle>References</CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="text-sm text-gray-700 dark:text-gray-300 whitespace-pre-line">
                    {hull.canonicalRefs}
                  </p>
                </CardContent>
              </Card>
            )}
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Actions */}
            <Card>
              <CardHeader>
                <CardTitle>Actions</CardTitle>
              </CardHeader>
              <CardContent>
                <Button
                  className="w-full"
                  disabled={hull.geometryMissing || cloning}
                  onClick={handleClone}
                >
                  {cloning ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                      Cloning...
                    </>
                  ) : (
                    <>
                      <Copy className="h-4 w-4 mr-2" />
                      Clone to My Vessels
                    </>
                  )}
                </Button>
                {hull.geometryMissing && (
                  <p className="text-xs text-gray-500 dark:text-gray-400 mt-2 text-center">
                    Geometry must be available to clone
                  </p>
                )}
              </CardContent>
            </Card>

            {/* Metadata */}
            <Card>
              <CardHeader>
                <CardTitle>Metadata</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-3 text-sm">
                  {hull.hullType && (
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Type:</span>
                      <span className="font-medium text-gray-900 dark:text-white">
                        {hull.hullType}
                      </span>
                    </div>
                  )}
                  <div className="flex justify-between">
                    <span className="text-gray-600 dark:text-gray-400">Slug:</span>
                    <span className="font-mono text-gray-900 dark:text-white">{hull.slug}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-600 dark:text-gray-400">Geometry:</span>
                    <span
                      className={`font-medium ${
                        hull.geometryMissing
                          ? "text-yellow-600 dark:text-yellow-400"
                          : "text-green-600 dark:text-green-400"
                      }`}
                    >
                      {hull.geometryMissing ? "Missing" : "Available"}
                    </span>
                  </div>
                  {(hull.stationsCount || 0) > 0 && (
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Stations:</span>
                      <span className="font-medium text-gray-900 dark:text-white">
                        {hull.stationsCount}
                      </span>
                    </div>
                  )}
                  {(hull.waterlinesCount || 0) > 0 && (
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Waterlines:</span>
                      <span className="font-medium text-gray-900 dark:text-white">
                        {hull.waterlinesCount}
                      </span>
                    </div>
                  )}
                  {(hull.offsetsCount || 0) > 0 && (
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Offsets:</span>
                      <span className="font-medium text-gray-900 dark:text-white">
                        {hull.offsetsCount}
                      </span>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </main>
    </div>
  );
});
