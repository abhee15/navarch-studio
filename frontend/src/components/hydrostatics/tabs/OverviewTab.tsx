import { observer } from "mobx-react-lite";
import type { VesselDetails } from "../../../types/hydrostatics";
import { settingsStore } from "../../../stores/SettingsStore";
import {
  convertLength,
  getLengthUnit,
  UnitSystem,
} from "../../../utils/unitConversion";

interface OverviewTabProps {
  vessel: VesselDetails;
  onUpdate: () => void;
}

export const OverviewTab = observer(({ vessel }: OverviewTabProps) => {
  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  const vesselUnits = (vessel.unitsSystem as UnitSystem) || "SI";
  const displayUnits = settingsStore.preferredUnits;

  const convertValue = (value: number): number => {
    return convertLength(value, vesselUnits, displayUnits);
  };

  return (
    <div className="space-y-6">
      {/* Vessel Information Card */}
      <div className="bg-white shadow rounded-lg">
        <div className="px-6 py-5 border-b border-gray-200">
          <h3 className="text-lg leading-6 font-medium text-gray-900">Vessel Information</h3>
        </div>
        <div className="px-6 py-5">
          <dl className="grid grid-cols-1 gap-x-4 gap-y-6 sm:grid-cols-2">
            <div>
              <dt className="text-sm font-medium text-gray-500">Vessel Name</dt>
              <dd className="mt-1 text-sm text-gray-900">{vessel.name}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">Vessel Native Units</dt>
              <dd className="mt-1 text-sm text-gray-900">
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                  {vessel.unitsSystem}
                </span>
                {vesselUnits !== displayUnits && (
                  <span className="ml-2 text-xs text-gray-500">
                    (Displaying in {displayUnits})
                  </span>
                )}
              </dd>
            </div>
            {vessel.description && (
              <div className="sm:col-span-2">
                <dt className="text-sm font-medium text-gray-500">Description</dt>
                <dd className="mt-1 text-sm text-gray-900">{vessel.description}</dd>
              </div>
            )}
          </dl>
        </div>
      </div>

      {/* Principal Particulars Card */}
      <div className="bg-white shadow rounded-lg">
        <div className="px-6 py-5 border-b border-gray-200">
          <h3 className="text-lg leading-6 font-medium text-gray-900">Principal Particulars</h3>
        </div>
        <div className="px-6 py-5">
          <dl className="grid grid-cols-1 gap-x-4 gap-y-6 sm:grid-cols-3">
            <div>
              <dt className="text-sm font-medium text-gray-500">
                Length Between Perpendiculars (Lpp)
              </dt>
              <dd className="mt-1 text-2xl font-semibold text-gray-900">
                {convertValue(vessel.lpp).toFixed(2)} {getLengthUnit(displayUnits)}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">Beam (B)</dt>
              <dd className="mt-1 text-2xl font-semibold text-gray-900">
                {convertValue(vessel.beam).toFixed(2)} {getLengthUnit(displayUnits)}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">Design Draft (T)</dt>
              <dd className="mt-1 text-2xl font-semibold text-gray-900">
                {convertValue(vessel.designDraft).toFixed(2)} {getLengthUnit(displayUnits)}
              </dd>
            </div>
          </dl>
        </div>
      </div>

      {/* Geometry Status Card */}
      <div className="bg-white shadow rounded-lg">
        <div className="px-6 py-5 border-b border-gray-200">
          <h3 className="text-lg leading-6 font-medium text-gray-900">Geometry Status</h3>
        </div>
        <div className="px-6 py-5">
          <dl className="grid grid-cols-1 gap-x-4 gap-y-6 sm:grid-cols-3">
            <div>
              <dt className="text-sm font-medium text-gray-500">Stations</dt>
              <dd className="mt-1 flex items-baseline">
                <span className="text-2xl font-semibold text-gray-900">{vessel.stationsCount}</span>
                {vessel.stationsCount === 0 && (
                  <span className="ml-2 text-sm text-gray-500">Not defined</span>
                )}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">Waterlines</dt>
              <dd className="mt-1 flex items-baseline">
                <span className="text-2xl font-semibold text-gray-900">
                  {vessel.waterlinesCount}
                </span>
                {vessel.waterlinesCount === 0 && (
                  <span className="ml-2 text-sm text-gray-500">Not defined</span>
                )}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">Offsets</dt>
              <dd className="mt-1 flex items-baseline">
                <span className="text-2xl font-semibold text-gray-900">{vessel.offsetsCount}</span>
                {vessel.offsetsCount === 0 && (
                  <span className="ml-2 text-sm text-gray-500">Not defined</span>
                )}
              </dd>
            </div>
          </dl>

          {vessel.offsetsCount === 0 && (
            <div className="mt-6 bg-yellow-50 border border-yellow-200 rounded-md p-4">
              <div className="flex">
                <div className="flex-shrink-0">
                  <svg
                    className="h-5 w-5 text-yellow-400"
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
                  <h3 className="text-sm font-medium text-yellow-800">No geometry defined</h3>
                  <div className="mt-2 text-sm text-yellow-700">
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
      <div className="bg-white shadow rounded-lg">
        <div className="px-6 py-5 border-b border-gray-200">
          <h3 className="text-lg leading-6 font-medium text-gray-900">Metadata</h3>
        </div>
        <div className="px-6 py-5">
          <dl className="grid grid-cols-1 gap-x-4 gap-y-6 sm:grid-cols-2">
            <div>
              <dt className="text-sm font-medium text-gray-500">Vessel ID</dt>
              <dd className="mt-1 text-sm text-gray-900 font-mono">{vessel.id}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">Created</dt>
              <dd className="mt-1 text-sm text-gray-900">{formatDate(vessel.createdAt)}</dd>
            </div>
            <div className="sm:col-span-2">
              <dt className="text-sm font-medium text-gray-500">Last Updated</dt>
              <dd className="mt-1 text-sm text-gray-900">{formatDate(vessel.updatedAt)}</dd>
            </div>
          </dl>
        </div>
      </div>
    </div>
  );
});

export default OverviewTab;
