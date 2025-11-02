import { useState, useEffect } from "react";
import { observer } from "mobx-react-lite";
import { BodyPlanViewer } from "../../BodyPlanViewer";
import type { BodyPlanData } from "../../../../types/bodyplan";
import { geometryApi } from "../../../../services/hydrostaticsApi";

interface BodyPlanPanelProps {
  vesselId: string;
  lpp: number;
  onSync3DView?: (stationIndex: number) => void;
}

export const BodyPlanPanel = observer(({ vesselId, lpp, onSync3DView }: BodyPlanPanelProps) => {
  const [bodyPlanData, setBodyPlanData] = useState<BodyPlanData | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadBodyPlanData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [vesselId]);

  const loadBodyPlanData = async () => {
    try {
      setLoading(true);
      setError(null);

      // Fetch offset grid from geometry API
      const offsetsGrid = await geometryApi.getOffsetsGrid(vesselId);

      // Transform to BodyPlanData format
      const data: BodyPlanData = {
        stations: offsetsGrid.stations,
        waterlines: offsetsGrid.waterlines,
        offsets: offsetsGrid.offsets,
      };

      setBodyPlanData(data);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to load body plan data";
      setError(message);
      console.error("Error loading body plan data:", err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      {loading && (
        <div className="flex items-center justify-center py-8">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        </div>
      )}

      {error && (
        <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-md p-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg className="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                <path
                  fillRule="evenodd"
                  d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                  clipRule="evenodd"
                />
              </svg>
            </div>
            <div className="ml-3">
              <h3 className="text-sm font-medium text-red-800 dark:text-red-200">
                Error loading body plan
              </h3>
              <div className="mt-2 text-sm text-red-700 dark:text-red-300">{error}</div>
              <div className="mt-3">
                <button
                  onClick={loadBodyPlanData}
                  className="text-sm font-medium text-red-600 dark:text-red-400 hover:text-red-500"
                >
                  Try again
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {!loading && !error && bodyPlanData && (
        <>
          {bodyPlanData.stations.length === 0 ? (
            <div className="text-center py-8">
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
                  d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                />
              </svg>
              <h3 className="mt-2 text-sm font-medium text-gray-900 dark:text-gray-100">
                No geometry data
              </h3>
              <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
                Import or define hull geometry to view body plan
              </p>
            </div>
          ) : (
            <BodyPlanViewer
              data={bodyPlanData}
              lpp={lpp}
              onSync3DView={onSync3DView}
              height={600}
            />
          )}
        </>
      )}
    </>
  );
});

export default BodyPlanPanel;
