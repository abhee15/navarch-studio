import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { BookOpen, Ship, Anchor, Droplets, Loader2, AlertCircle } from "lucide-react";
import { useStore } from "../../stores";
import { Button } from "../../components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../components/ui/card";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { Footer } from "../../components/Footer";
import { AppHeader } from "../../components/AppHeader";
import { getCatalogHulls, getWaterProperties, getPropellerSeries } from "../../services/catalogApi";
import type {
  CatalogHullListItem,
  CatalogWaterProperty,
  CatalogPropellerSeriesListItem,
} from "../../types/catalog";
import { toast } from "react-hot-toast";

type TabType = "hulls" | "propellers" | "water";

export const CatalogBrowser: React.FC = observer(() => {
  const { authStore } = useStore();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<TabType>("hulls");
  const [hulls, setHulls] = useState<CatalogHullListItem[]>([]);
  const [waterProperties, setWaterProperties] = useState<CatalogWaterProperty[]>([]);
  const [propellerSeries, setPropellerSeries] = useState<CatalogPropellerSeriesListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadData = async () => {
      setLoading(true);
      setError(null);
      try {
        if (activeTab === "hulls") {
          const data = await getCatalogHulls();
          setHulls(data);
        } else if (activeTab === "propellers") {
          const data = await getPropellerSeries();
          setPropellerSeries(data);
        } else if (activeTab === "water") {
          const data = await getWaterProperties();
          setWaterProperties(data);
        }
      } catch (err) {
        console.error("Failed to load catalog data:", err);
        setError("Failed to load catalog data");
        toast.error("Failed to load catalog data");
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [activeTab]);

  const handleLogout = async () => {
    await authStore.logout();
    navigate("/login");
  };

  const handleHullClick = (hullId: string) => {
    navigate(`/catalog/hulls/${hullId}`);
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

  const renderHullsTab = () => {
    if (loading) {
      return (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="h-8 w-8 animate-spin text-primary" />
        </div>
      );
    }

    if (error) {
      return (
        <div className="flex items-center justify-center py-12">
          <AlertCircle className="h-8 w-8 text-red-500 mr-2" />
          <span className="text-red-600">{error}</span>
        </div>
      );
    }

    if (hulls.length === 0) {
      return (
        <div className="text-center py-12 text-gray-500 dark:text-gray-400">
          <Ship className="h-12 w-12 mx-auto mb-4 opacity-50" />
          <p>No catalog hulls available</p>
        </div>
      );
    }

    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {hulls.map((hull) => (
          <Card
            key={hull.id}
            className="hover:shadow-lg transition-shadow duration-200 cursor-pointer"
            onClick={() => handleHullClick(hull.id)}
          >
            <CardHeader>
              <div className="flex items-start justify-between mb-2">
                <span className="text-3xl">{getHullTypeIcon(hull.hullType)}</span>
                {hull.geometryMissing && (
                  <span className="text-xs bg-yellow-100 dark:bg-yellow-900 text-yellow-800 dark:text-yellow-200 px-2 py-1 rounded">
                    No Geometry
                  </span>
                )}
              </div>
              <CardTitle>{hull.title}</CardTitle>
              <CardDescription className="line-clamp-2">{hull.description}</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-2 text-sm text-gray-600 dark:text-gray-400">
                {hull.hullType && (
                  <div className="flex justify-between">
                    <span>Type:</span>
                    <span className="font-medium">{hull.hullType}</span>
                  </div>
                )}
                {hull.lpp && (
                  <div className="flex justify-between">
                    <span>Lpp:</span>
                    <span className="font-medium">{hull.lpp.toFixed(2)} m</span>
                  </div>
                )}
                {hull.cb && (
                  <div className="flex justify-between">
                    <span>Cb:</span>
                    <span className="font-medium">{hull.cb.toFixed(3)}</span>
                  </div>
                )}
              </div>
              <Button className="w-full mt-4" variant="outline">
                View Details →
              </Button>
            </CardContent>
          </Card>
        ))}
      </div>
    );
  };

  const renderPropellersTab = () => {
    if (loading) {
      return (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="h-8 w-8 animate-spin text-primary" />
        </div>
      );
    }

    if (error) {
      return (
        <div className="flex items-center justify-center py-12">
          <AlertCircle className="h-8 w-8 text-red-500 mr-2" />
          <span className="text-red-600">{error}</span>
        </div>
      );
    }

    if (propellerSeries.length === 0) {
      return (
        <div className="text-center py-12 text-gray-500 dark:text-gray-400">
          <Anchor className="h-12 w-12 mx-auto mb-4 opacity-50" />
          <p className="mb-2">No propeller series available</p>
          <p className="text-sm">
            Wageningen B-series data is being prepared. Check back soon!
          </p>
        </div>
      );
    }

    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {propellerSeries.map((series) => (
          <Card key={series.id} className="hover:shadow-lg transition-shadow duration-200">
            <CardHeader>
              <div className="flex items-start justify-between mb-2">
                <span className="text-3xl">⚓</span>
                {series.isDemo && (
                  <span className="text-xs bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 px-2 py-1 rounded">
                    DEMO
                  </span>
                )}
              </div>
              <CardTitle>{series.name}</CardTitle>
              <CardDescription>
                {series.bladeCount}-blade propeller series
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-2 text-sm text-gray-600 dark:text-gray-400">
                <div className="flex justify-between">
                  <span>Blades:</span>
                  <span className="font-medium">{series.bladeCount}</span>
                </div>
                <div className="flex justify-between">
                  <span>AE/A0:</span>
                  <span className="font-medium">{series.expandedAreaRatio.toFixed(3)}</span>
                </div>
                {series.pitchDiameterRatio && (
                  <div className="flex justify-between">
                    <span>P/D:</span>
                    <span className="font-medium">{series.pitchDiameterRatio.toFixed(2)}</span>
                  </div>
                )}
                <div className="flex justify-between">
                  <span>Points:</span>
                  <span className="font-medium">{series.pointsCount}</span>
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    );
  };

  const renderWaterTab = () => {
    if (loading) {
      return (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="h-8 w-8 animate-spin text-primary" />
        </div>
      );
    }

    if (error) {
      return (
        <div className="flex items-center justify-center py-12">
          <AlertCircle className="h-8 w-8 text-red-500 mr-2" />
          <span className="text-red-600">{error}</span>
        </div>
      );
    }

    const groupedByMedium = waterProperties.reduce((acc, prop) => {
      if (!acc[prop.medium]) {
        acc[prop.medium] = [];
      }
      acc[prop.medium].push(prop);
      return acc;
    }, {} as Record<string, CatalogWaterProperty[]>);

    return (
      <div className="space-y-8">
        {Object.entries(groupedByMedium).map(([medium, properties]) => (
          <div key={medium}>
            <h3 className="text-xl font-semibold mb-4 text-gray-900 dark:text-white">
              {medium} Water
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {properties.map((prop) => (
                <Card key={prop.id}>
                  <CardHeader>
                    <CardTitle className="text-lg">{prop.temperature_C}°C</CardTitle>
                    <CardDescription>{prop.sourceRef}</CardDescription>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2 text-sm">
                      <div className="flex justify-between">
                        <span className="text-gray-600 dark:text-gray-400">Density:</span>
                        <span className="font-medium">{prop.density.toFixed(1)} kg/m³</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-600 dark:text-gray-400">
                          Viscosity:
                        </span>
                        <span className="font-medium">
                          {(prop.kinematicViscosity_m2s * 1e6).toFixed(2)} ×10⁻⁶ m²/s
                        </span>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          </div>
        ))}
      </div>
    );
  };

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
        right={<UserProfileMenu onOpenSettings={() => {}} onLogout={handleLogout} />}
      />

      {/* Main Content */}
      <main className="container mx-auto px-4 py-8 flex-1">
        {/* Tab Navigation */}
        <div className="mb-6">
          <div className="border-b border-gray-200 dark:border-gray-700">
            <nav className="-mb-px flex space-x-8" aria-label="Tabs">
              <button
                onClick={() => setActiveTab("hulls")}
                className={`whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm flex items-center ${
                  activeTab === "hulls"
                    ? "border-purple-500 text-purple-600 dark:text-purple-400"
                    : "border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300"
                }`}
              >
                <Ship className="h-5 w-5 mr-2" />
                Hulls ({hulls.length})
              </button>
              <button
                onClick={() => setActiveTab("propellers")}
                className={`whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm flex items-center ${
                  activeTab === "propellers"
                    ? "border-purple-500 text-purple-600 dark:text-purple-400"
                    : "border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300"
                }`}
              >
                <Anchor className="h-5 w-5 mr-2" />
                Propellers ({propellerSeries.length})
              </button>
              <button
                onClick={() => setActiveTab("water")}
                className={`whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm flex items-center ${
                  activeTab === "water"
                    ? "border-purple-500 text-purple-600 dark:text-purple-400"
                    : "border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300"
                }`}
              >
                <Droplets className="h-5 w-5 mr-2" />
                Water Properties ({waterProperties.length})
              </button>
            </nav>
          </div>
        </div>

        {/* Tab Content */}
        <div>
          {activeTab === "hulls" && renderHullsTab()}
          {activeTab === "propellers" && renderPropellersTab()}
          {activeTab === "water" && renderWaterTab()}
        </div>
      </main>

      <Footer />
    </div>
  );
});
