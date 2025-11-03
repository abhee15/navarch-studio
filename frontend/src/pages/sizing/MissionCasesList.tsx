import React, { useEffect } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate } from "react-router-dom";
import { useStore } from "../../stores";
import { AppHeader } from "../../components/AppHeader";
import { Footer } from "../../components/Footer";
import { Button } from "../../components/ui/button";

export const MissionCasesList: React.FC = observer(() => {
  const navigate = useNavigate();
  const { sizingStore } = useStore();

  useEffect(() => {
    sizingStore.loadMissionCases();
  }, [sizingStore]);

  return (
    <div className="flex min-h-screen flex-col bg-gray-50 dark:bg-gray-900">
      <AppHeader />

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
            <Button onClick={() => navigate("/sizing/wizard")}>+ New Mission</Button>
          </div>

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
              {sizingStore.missionCases.map((mission) => (
                <div
                  key={mission.id}
                  className="cursor-pointer rounded-lg bg-white p-6 shadow transition-shadow hover:shadow-lg dark:bg-gray-800"
                  onClick={() => navigate(`/sizing/missions/${mission.id}`)}
                >
                  <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                    {mission.name}
                  </h3>
                  <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
                    {mission.missionType} • {mission.cargoBasis.toUpperCase()}
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
              ))}
            </div>
          )}
        </div>
      </main>

      <Footer />
    </div>
  );
});
