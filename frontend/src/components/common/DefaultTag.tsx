interface DefaultTagProps {
  provenance?: string;
  onClear?: () => void;
}

/**
 * Tag component to show that a value is auto-filled with default/typical values
 * Displays provenance information and optional clear button
 */
export function DefaultTag({ provenance, onClear }: DefaultTagProps) {
  if (!provenance) return null;

  return (
    <div className="inline-flex items-center gap-1.5 px-2 py-0.5 bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 text-xs rounded-md border border-blue-300 dark:border-blue-700">
      <svg
        className="w-3 h-3"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
        xmlns="http://www.w3.org/2000/svg"
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth={2}
          d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
        />
      </svg>
      <span title={provenance}>Default</span>
      {onClear && (
        <button
          onClick={onClear}
          className="hover:text-blue-900 dark:hover:text-blue-200 transition-colors"
          title="Clear default value"
        >
          <svg
            className="w-3 h-3"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            xmlns="http://www.w3.org/2000/svg"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M6 18L18 6M6 6l12 12"
            />
          </svg>
        </button>
      )}
    </div>
  );
}
