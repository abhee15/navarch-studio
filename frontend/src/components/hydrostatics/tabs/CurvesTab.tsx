interface CurvesTabProps {
  vesselId: string;
}

export function CurvesTab({ vesselId: _vesselId }: CurvesTabProps) {
  return (
    <div className="space-y-6">
      <div className="bg-white shadow rounded-lg p-6">
        <div className="text-center py-12">
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
              d="M7 12l3-3 3 3 4-4M8 21l4-4 4 4M3 4h18M4 4h16v12a1 1 0 01-1 1H5a1 1 0 01-1-1V4z"
            />
          </svg>
          <h3 className="mt-2 text-sm font-medium text-gray-900">Hydrostatic Curves</h3>
          <p className="mt-1 text-sm text-gray-500">
            Visualize displacement, KB, LCB, GM, and Bonjean curves
          </p>
          <p className="mt-2 text-xs text-gray-400">Coming soon</p>
        </div>
      </div>
    </div>
  );
}

export default CurvesTab;
