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
    <div className="border-b border-border">
      <button
        onClick={() => setIsExpanded(!isExpanded)}
        className={`w-full flex items-center justify-between px-2.5 py-1.5 text-xs font-medium transition-all ${
          isExpanded
            ? "bg-accent/20 text-foreground border-l-4 border-primary"
            : "text-muted-foreground hover:bg-muted/50 border-l-4 border-transparent"
        }`}
        type="button"
      >
        <span className="flex items-center">
          {title}
          {badge !== undefined && badge !== null && (
            <span
              className={`ml-1.5 px-1.5 py-0.5 rounded text-[10px] ${
                isExpanded ? "bg-primary/20 text-foreground" : "bg-muted text-muted-foreground"
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
      {isExpanded && <div className="px-2.5 py-2 bg-card text-card-foreground">{children}</div>}
    </div>
  );
}

export default CollapsibleSection;
