import React, { useEffect, useState } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate } from "react-router-dom";
import { useStore } from "../../stores";
import { Footer } from "../../components/Footer";
import { Button } from "../../components/ui/button";
import { Input } from "../../components/ui/input";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { UserSettingsDialog } from "../../components/UserSettingsDialog";
import { AppHeader } from "../../components/AppHeader";
import { Ship, Home, Trash2, FileText, Package, Zap, Play, Grid3x3 } from "lucide-react";

export const MissionCasesList: React.FC = observer(() => {
  const navigate = useNavigate();
  const { sizingStore, authStore } = useStore();
  const [searchQuery, setSearchQuery] = useState("");
  const [filterType, setFilterType] = useState<string>("all");
  const [showSettings, setShowSettings] = useState(false);

  useEffect(() => {
    sizingStore.loadMissionCases();
  }, [sizingStore]);

  const handleHome = () => {
    navigate("/dashboard");
  };

  const handleLogout = () => {
    authStore.logout();
    navigate("/login");
  };

  // Filter briefs based on search and type
  const filteredBriefs = sizingStore.missionCases.filter((brief) => {
    const matchesSearch =
      !searchQuery ||
      brief.name?.toLowerCase().includes(searchQuery.toLowerCase()) ||
      brief.missionType?.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesType = filterType === "all" || brief.missionType === filterType;
    return matchesSearch && matchesType;
  });

  return (
    <div className="flex min-h-screen flex-col bg-gray-50 dark:bg-gray-900">
      <AppHeader
        left={<h1 className="text-xl font-semibold text-foreground">Hull Sizing</h1>}
        right={
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="sm" onClick={handleHome}>
              <Home className="h-4 w-4 md:mr-2" />
              <span className="hidden md:inline">Home</span>
            </Button>
            <UserProfileMenu onOpenSettings={() => setShowSettings(true)} onLogout={handleLogout} />
          </div>
        }
      />

      <main className="flex-1 py-8">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          {/* Header */}
          <div className="mb-8 flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
                Hull Sizing - Design Briefs
              </h1>
              <p className="mt-2 text-gray-600 dark:text-gray-400">
                Manage your design briefs and generate preliminary hull candidates
              </p>
            </div>
            <div className="flex gap-2">
              <Button onClick={() => navigate("/sizing/wizard")}>
                <Ship className="h-4 w-4 mr-2" />
                New Brief
              </Button>
            </div>
          </div>

          {/* Search and Filter */}
          {!sizingStore.isLoading && sizingStore.missionCases.length > 0 && (
            <div className="mb-6 flex flex-col sm:flex-row gap-4">
              <div className="flex-1">
                <Input
                  type="text"
                  placeholder="Search briefs by name or type..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full"
                />
              </div>
              <div className="flex gap-2">
                {["all", "commercial", "government", "research", "pleasure"].map((type) => (
                  <button
                    key={type}
                    onClick={() => setFilterType(type)}
                    className={`px-4 py-2 text-sm font-medium rounded-lg transition-all ${
                      filterType === type
                        ? "bg-primary text-primary-foreground shadow-md"
                        : "bg-card text-foreground hover:bg-accent hover:text-accent-foreground"
                    }`}
                  >
                    {type.charAt(0).toUpperCase() + type.slice(1)}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Quick Stats */}
          {!sizingStore.isLoading && sizingStore.missionCases.length > 0 && (
            <div className="mb-6 grid grid-cols-1 md:grid-cols-4 gap-4">
              <div className="rounded-lg bg-gradient-to-br from-blue-500 to-blue-600 p-4 text-white shadow-lg">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium opacity-90">Total Briefs</p>
                    <p className="text-3xl font-bold mt-1">{sizingStore.missionCases.length}</p>
                  </div>
                  <FileText className="h-10 w-10 opacity-75" />
                </div>
              </div>

              <div className="rounded-lg bg-gradient-to-br from-green-500 to-emerald-600 p-4 text-white shadow-lg">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium opacity-90">Commercial</p>
                    <p className="text-3xl font-bold mt-1">
                      {
                        sizingStore.missionCases.filter((m) => m.missionType === "commercial")
                          .length
                      }
                    </p>
                  </div>
                  <Ship className="h-10 w-10 opacity-75" />
                </div>
              </div>

              <div className="rounded-lg bg-gradient-to-br from-purple-500 to-purple-600 p-4 text-white shadow-lg">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium opacity-90">TEU-Based</p>
                    <p className="text-3xl font-bold mt-1">
                      {sizingStore.missionCases.filter((m) => m.cargoBasis === "teu").length}
                    </p>
                  </div>
                  <Package className="h-10 w-10 opacity-75" />
                </div>
              </div>

              <div className="rounded-lg bg-gradient-to-br from-orange-500 to-orange-600 p-4 text-white shadow-lg">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium opacity-90">Avg Speed</p>
                    <p className="text-3xl font-bold mt-1">
                      {(
                        sizingStore.missionCases.reduce(
                          (sum, m) => sum + (m.serviceSpeedKn || 0),
                          0
                        ) / sizingStore.missionCases.length
                      ).toFixed(0)}{" "}
                      kn
                    </p>
                  </div>
                  <Zap className="h-10 w-10 opacity-75" />
                </div>
              </div>
            </div>
          )}

          {/* Loading State */}
          {sizingStore.isLoading && (
            <div className="text-center py-12">
              <div className="animate-spin h-8 w-8 border-4 border-blue-600 border-t-transparent rounded-full mx-auto"></div>
              <p className="mt-4 text-gray-600 dark:text-gray-400">Loading briefs...</p>
            </div>
          )}

          {/* Empty State */}
          {!sizingStore.isLoading && sizingStore.missionCases.length === 0 && (
            <div className="rounded-lg bg-white p-12 text-center shadow dark:bg-gray-800">
              <svg
                className="mx-auto h-12 w-12 text-gray-400"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                />
              </svg>
              <h3 className="mt-4 text-lg font-medium text-gray-900 dark:text-white">
                No design briefs yet
              </h3>
              <p className="mt-2 text-gray-600 dark:text-gray-400">
                Get started by creating your first design brief
              </p>
              <Button onClick={() => navigate("/sizing/wizard")} className="mt-6">
                <Ship className="h-4 w-4 mr-2" />
                Create First Brief
              </Button>
            </div>
          )}

          {/* Brief Cards */}
          {!sizingStore.isLoading && sizingStore.missionCases.length > 0 && (
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
              {filteredBriefs.map((brief) => (
                <div
                  key={brief.id}
                  className="relative rounded-lg bg-white p-6 shadow transition-shadow hover:shadow-lg dark:bg-gray-800"
                >
                  {/* Delete Button */}
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={async (e) => {
                      e.stopPropagation();
                      if (window.confirm(`Delete brief "${brief.name}"?`)) {
                        await sizingStore.deleteMissionCase(brief.id);
                        await sizingStore.loadMissionCases();
                      }
                    }}
                    className="absolute top-2 right-2 text-muted-foreground hover:text-destructive"
                    title="Delete brief"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>

                  {/* Card Content */}
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white pr-8">
                      {brief.name}
                    </h3>
                    <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
                      {brief.missionType} • {brief.cargoBasis?.toUpperCase() || "N/A"}
                    </p>

                    <dl className="mt-4 grid grid-cols-2 gap-2 text-sm">
                      <div>
                        <dt className="font-medium text-gray-500 dark:text-gray-400">Cargo:</dt>
                        <dd className="text-gray-900 dark:text-white">
                          {brief.cargoBasis === "teu" && `${brief.teuCount} TEU`}
                          {brief.cargoBasis === "weight" && `${brief.cargoValue.toFixed(0)}t`}
                          {brief.cargoBasis === "volume" && `${brief.cargoVolumeM3}m³`}
                        </dd>
                      </div>
                      <div>
                        <dt className="font-medium text-gray-500 dark:text-gray-400">Speed:</dt>
                        <dd className="text-gray-900 dark:text-white">{brief.serviceSpeedKn} kn</dd>
                      </div>
                    </dl>

                    <div className="mt-4 text-xs text-gray-500 dark:text-gray-400">
                      Created {new Date(brief.createdAt).toLocaleDateString()}
                    </div>
                  </div>

                  {/* Action Buttons */}
                  <div className="mt-4 pt-4 border-t border-border flex gap-2">
                    <Button
                      size="sm"
                      variant="default"
                      className="flex-1"
                      onClick={async (e) => {
                        e.stopPropagation();
                        // Run solver for this brief
                        const run = await sizingStore.runSolver({
                          missionCaseId: brief.id,
                          mode: "first_principles",
                          locks: undefined,
                          options: undefined,
                        });
                        if (run?.id) {
                          navigate(`/sizing/runs/${run.id}`);
                        }
                      }}
                    >
                      <Play className="h-3 w-3 mr-2" />
                      Run Solver
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={(e) => {
                        e.stopPropagation();
                        navigate(`/sizing/explorer/${brief.id}`);
                      }}
                      title="Explore design space with parameter sweeps"
                    >
                      <Grid3x3 className="h-3 w-3" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </main>

      <Footer />

      {/* Settings Dialog */}
      <UserSettingsDialog isOpen={showSettings} onClose={() => setShowSettings(false)} />
    </div>
  );
});
