import { observer } from "mobx-react-lite";
import type { VesselDetails } from "../../../types/hydrostatics";
import { settingsStore } from "../../../stores/SettingsStore";
import { getUnitSymbol } from "../../../utils/unitSymbols";

interface OverviewTabProps {
  vessel: VesselDetails;
  onUpdate: () => void;
}

export const OverviewTab = observer(({ vessel }: OverviewTabProps) => {
  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  // Backend automatically converts values to user's preferred units
  const displayUnits = settingsStore.preferredUnits;
  const lengthUnit = getUnitSymbol(displayUnits, "Length");

  return (
    <div className="space-y-6">
      {/* Vessel Information Card */}
      <div className="bg-white dark:bg-gray-800 shadow rounded-lg">
        <div className="px-6 py-5 border-b border-gray-200 dark:border-gray-700">
          <h3 className="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100">
            Vessel Information
          </h3>
        </div>
        <div className="px-6 py-5">
          <dl className="grid grid-cols-1 gap-x-4 gap-y-6 sm:grid-cols-2">
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Vessel Name</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">{vessel.name}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">
                Vessel Native Units
              </dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200">
                  {vessel.unitsSystem}
                </span>
                {vessel.unitsSystem !== displayUnits && (
                  <span className="ml-2 text-xs text-gray-500 dark:text-gray-400">
                    (Displaying in {displayUnits})
                  </span>
                )}
              </dd>
            </div>
            {vessel.description && (
              <div className="sm:col-span-2">
                <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">
                  Description
                </dt>
                <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">
                  {vessel.description}
                </dd>
              </div>
            )}
          </dl>
        </div>
      </div>

      {/* Principal Particulars Card */}
      <div className="bg-white dark:bg-gray-800 shadow rounded-lg">
        <div className="px-6 py-5 border-b border-gray-200 dark:border-gray-700">
          <h3 className="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100">
            Principal Particulars
          </h3>
        </div>
        <div className="px-6 py-5">
          <dl className="grid grid-cols-1 gap-x-4 gap-y-6 sm:grid-cols-3">
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">
                Length Between Perpendiculars (Lpp)
              </dt>
              <dd className="mt-1 text-2xl font-semibold text-gray-900 dark:text-gray-100">
                {vessel.lpp.toFixed(2)} {lengthUnit}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Beam (B)</dt>
              <dd className="mt-1 text-2xl font-semibold text-gray-900 dark:text-gray-100">
                {vessel.beam.toFixed(2)} {lengthUnit}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">
                Design Draft (T)
              </dt>
              <dd className="mt-1 text-2xl font-semibold text-gray-900 dark:text-gray-100">
                {vessel.designDraft.toFixed(2)} {lengthUnit}
              </dd>
            </div>
          </dl>
        </div>
      </div>

      {/* Geometry Status Card */}
      <div className="bg-white dark:bg-gray-800 shadow rounded-lg">
        <div className="px-6 py-5 border-b border-gray-200 dark:border-gray-700">
          <h3 className="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100">
            Geometry Status
          </h3>
        </div>
        <div className="px-6 py-5">
          <dl className="grid grid-cols-1 gap-x-4 gap-y-6 sm:grid-cols-3">
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Stations</dt>
              <dd className="mt-1 flex items-baseline">
                <span className="text-2xl font-semibold text-gray-900 dark:text-gray-100">
                  {vessel.stationsCount}
                </span>
                {vessel.stationsCount === 0 && (
                  <span className="ml-2 text-sm text-gray-500 dark:text-gray-400">Not defined</span>
                )}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Waterlines</dt>
              <dd className="mt-1 flex items-baseline">
                <span className="text-2xl font-semibold text-gray-900 dark:text-gray-100">
                  {vessel.waterlinesCount}
                </span>
                {vessel.waterlinesCount === 0 && (
                  <span className="ml-2 text-sm text-gray-500 dark:text-gray-400">Not defined</span>
                )}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Offsets</dt>
              <dd className="mt-1 flex items-baseline">
                <span className="text-2xl font-semibold text-gray-900 dark:text-gray-100">
                  {vessel.offsetsCount}
                </span>
                {vessel.offsetsCount === 0 && (
                  <span className="ml-2 text-sm text-gray-500 dark:text-gray-400">Not defined</span>
                )}
              </dd>
            </div>
          </dl>

          {vessel.offsetsCount === 0 && (
            <div className="mt-6 bg-yellow-50 dark:bg-yellow-900/30 border border-yellow-200 dark:border-yellow-800 rounded-md p-4">
              <div className="flex">
                <div className="flex-shrink-0">
                  <svg
                    className="h-5 w-5 text-yellow-400 dark:text-yellow-500"
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                  >
                    <path
                      fillRule="evenodd"
                      d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                      clipRule="evenodd"
                    />
                  </svg>
                </div>
                <div className="ml-3">
                  <h3 className="text-sm font-medium text-yellow-800 dark:text-yellow-200">
                    No geometry defined
                  </h3>
                  <div className="mt-2 text-sm text-yellow-700 dark:text-yellow-300">
                    <p>
                      Define hull geometry in the <strong>Geometry</strong> tab to enable
                      hydrostatic calculations.
                    </p>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Metadata Card */}
      <div className="bg-white dark:bg-gray-800 shadow rounded-lg">
        <div className="px-6 py-5 border-b border-gray-200 dark:border-gray-700">
          <h3 className="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100">
            Metadata
          </h3>
        </div>
        <div className="px-6 py-5">
          <dl className="grid grid-cols-1 gap-x-4 gap-y-6 sm:grid-cols-2">
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Vessel ID</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100 font-mono">
                {vessel.id}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Created</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">
                {formatDate(vessel.createdAt)}
              </dd>
            </div>
            <div className="sm:col-span-2">
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Last Updated</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">
                {formatDate(vessel.updatedAt)}
              </dd>
            </div>
          </dl>
        </div>
      </div>
    </div>
  );
});

export default OverviewTab;
