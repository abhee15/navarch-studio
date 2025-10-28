import { useState } from "react";

interface CollapsibleSectionProps {
  title: string;
  defaultExpanded?: boolean;
  children: React.ReactNode;
  badge?: string | number;
}

export function CollapsibleSection({
  title,
  defaultExpanded = true,
  children,
  badge,
}: CollapsibleSectionProps) {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded);

  return (
    <div className="border-b border-gray-200 dark:border-gray-700">
      <button
        onClick={() => setIsExpanded(!isExpanded)}
        className={`w-full flex items-center justify-between px-2.5 py-1.5 text-xs font-medium transition-all ${
          isExpanded
            ? "bg-blue-50 dark:bg-blue-900/30 text-blue-900 dark:text-blue-100 border-l-4 border-blue-500 dark:border-blue-400"
            : "text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 border-l-4 border-transparent"
        }`}
        type="button"
      >
        <span className="flex items-center">
          {title}
          {badge !== undefined && badge !== null && (
            <span
              className={`ml-1.5 px-1.5 py-0.5 rounded text-[10px] ${
                isExpanded
                  ? "bg-blue-200 dark:bg-blue-800 text-blue-900 dark:text-blue-100"
                  : "bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-400"
              }`}
            >
              {badge}
            </span>
          )}
        </span>
        <svg
          className={`w-3.5 h-3.5 transition-transform ${isExpanded ? "rotate-90" : ""}`}
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
        </svg>
      </button>
      {isExpanded && <div className="px-2.5 py-2 bg-white dark:bg-gray-900">{children}</div>}
    </div>
  );
}

export default CollapsibleSection;
