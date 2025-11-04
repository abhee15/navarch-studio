import React, { useEffect, useState } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate } from "react-router-dom";
import { useStore } from "../../stores";
import { Footer } from "../../components/Footer";
import { Button } from "../../components/ui/button";
import { Input } from "../../components/ui/input";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { UserSettingsDialog } from "../../components/UserSettingsDialog";

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

  // Filter missions based on search and type
  const filteredMissions = sizingStore.missionCases.filter((mission) => {
    const matchesSearch =
      !searchQuery ||
      mission.name?.toLowerCase().includes(searchQuery.toLowerCase()) ||
      mission.missionType?.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesType = filterType === "all" || mission.missionType === filterType;
    return matchesSearch && matchesType;
  });

  return (
    <div className="flex min-h-screen flex-col bg-gray-50 dark:bg-gray-900">
      {/* Main Navigation Header */}
      <header className="border-b border-border bg-card/80 backdrop-blur-sm flex-shrink-0 relative z-50">
        <div className="px-4 py-2">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <h1 className="text-lg font-bold text-foreground">NavArch Studio</h1>
            </div>
            <div className="flex items-center space-x-2">
              <button
                onClick={handleHome}
                className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-foreground hover:text-foreground/80 border border-border rounded hover:bg-accent/10"
              >
                <svg
                  className="h-4 w-4 mr-1.5"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"
                  />
                </svg>
                Home
              </button>
              <UserProfileMenu
                onOpenSettings={() => setShowSettings(true)}
                onLogout={handleLogout}
              />
            </div>
          </div>
        </div>
      </header>

      <main className="flex-1 py-8">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          {/* Header */}
          <div className="mb-8 flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
                Hull Sizing - Mission Cases
              </h1>
              <p className="mt-2 text-gray-600 dark:text-gray-400">
                Manage your mission requirements and generate preliminary hull designs
              </p>
            </div>
            <Button
              onClick={() => navigate("/sizing/wizard")}
              className="bg-gradient-to-r from-blue-600 to-cyan-600 hover:from-blue-700 hover:to-cyan-700"
            >
              🚀 New Mission
            </Button>
          </div>

          {/* Search and Filter */}
          {!sizingStore.isLoading && sizingStore.missionCases.length > 0 && (
            <div className="mb-6 flex flex-col sm:flex-row gap-4">
              <div className="flex-1">
                <Input
                  type="text"
                  placeholder="🔍 Search missions by name or type..."
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
                        ? "bg-blue-600 text-white shadow-md"
                        : "bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
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
                    <p className="text-sm font-medium opacity-90">Total Missions</p>
                    <p className="text-3xl font-bold mt-1">{sizingStore.missionCases.length}</p>
                  </div>
                  <div className="text-4xl opacity-75">📋</div>
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
                  <div className="text-4xl opacity-75">🚢</div>
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
                  <div className="text-4xl opacity-75">📦</div>
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
                  <div className="text-4xl opacity-75">⚡</div>
                </div>
              </div>
            </div>
          )}

          {/* Loading State */}
          {sizingStore.isLoading && (
            <div className="text-center py-12">
              <div className="animate-spin h-8 w-8 border-4 border-blue-600 border-t-transparent rounded-full mx-auto"></div>
              <p className="mt-4 text-gray-600 dark:text-gray-400">Loading missions...</p>
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
                No missions yet
              </h3>
              <p className="mt-2 text-gray-600 dark:text-gray-400">
                Get started by creating your first mission case
              </p>
              <Button onClick={() => navigate("/sizing/wizard")} className="mt-6">
                Create First Mission
              </Button>
            </div>
          )}

          {/* Mission Cards */}
          {!sizingStore.isLoading && sizingStore.missionCases.length > 0 && (
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
              {filteredMissions.map((mission) => (
                <div
                  key={mission.id}
                  className="relative rounded-lg bg-white p-6 shadow transition-shadow hover:shadow-lg dark:bg-gray-800"
                >
                  {/* Delete Button */}
                  <button
                    onClick={async (e) => {
                      e.stopPropagation();
                      if (window.confirm(`Delete mission "${mission.name}"?`)) {
                        await sizingStore.deleteMissionCase(mission.id);
                        await sizingStore.loadMissionCases();
                      }
                    }}
                    className="absolute top-4 right-4 p-2 text-gray-400 hover:text-red-600 dark:hover:text-red-400 transition-colors"
                    title="Delete mission"
                  >
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>

                  {/* Card Content - Clickable */}
                  <div
                    className="cursor-pointer"
                    onClick={async () => {
                      // Run solver for this mission
                      const run = await sizingStore.runSolver({
                        missionCaseId: mission.id,
                        mode: "first_principles",
                        locks: undefined,
                        options: undefined,
                      });
                      if (run?.id) {
                        navigate(`/sizing/runs/${run.id}`);
                      }
                    }}
                  >
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white pr-8">
                      {mission.name}
                    </h3>
                  <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
                    {mission.missionType} • {mission.cargoBasis?.toUpperCase() || "N/A"}
                  </p>

                  <dl className="mt-4 grid grid-cols-2 gap-2 text-sm">
                    <div>
                      <dt className="font-medium text-gray-500 dark:text-gray-400">Cargo:</dt>
                      <dd className="text-gray-900 dark:text-white">
                        {mission.cargoBasis === "teu" && `${mission.teuCount} TEU`}
                        {mission.cargoBasis === "weight" && `${mission.cargoValue.toFixed(0)}t`}
                        {mission.cargoBasis === "volume" && `${mission.cargoVolumeM3}m³`}
                      </dd>
                    </div>
                    <div>
                      <dt className="font-medium text-gray-500 dark:text-gray-400">Speed:</dt>
                      <dd className="text-gray-900 dark:text-white">{mission.serviceSpeedKn} kn</dd>
                    </div>
                  </dl>

                  <div className="mt-4 text-xs text-gray-500 dark:text-gray-400">
                    Created {new Date(mission.createdAt).toLocaleDateString()}
                  </div>
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
