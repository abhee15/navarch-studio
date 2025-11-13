import React from "react";

interface AppHeaderProps {
  left?: React.ReactNode;
  right?: React.ReactNode;
  className?: string;
}

// Consistent system rail/header with fixed height across pages
export function AppHeader({ left, right, className }: AppHeaderProps) {
  return (
    <header
      className={`border-b border-border bg-card/80 backdrop-blur-sm sticky top-0 z-50 ${className ?? ""}`}
    >
      <div className="h-14 px-4">
        <div className="h-full flex items-center justify-between">
          <div className="flex items-center gap-3 min-w-0">{left}</div>
          <div className="flex items-center gap-2 flex-shrink-0">{right}</div>
        </div>
      </div>
    </header>
  );
}
