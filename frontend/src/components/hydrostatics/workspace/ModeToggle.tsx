import { useEffect } from "react";
import type { WorkspaceMode } from "../../../types/workspace";

interface ModeToggleProps {
  mode: WorkspaceMode;
  onModeChange: (mode: WorkspaceMode) => void;
  disabled?: boolean;
  canSwitchToView?: boolean;
}

export function ModeToggle({ mode, onModeChange, disabled = false, canSwitchToView = true }: ModeToggleProps) {
  // Keyboard shortcuts
  useEffect(() => {
    const handleKeyPress = (e: KeyboardEvent) => {
      // Ignore if user is typing in an input
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) {
        return;
      }

      // 'E' key for Edit mode
      if (e.key === 'e' || e.key === 'E') {
        if (!disabled) {
          onModeChange('edit');
        }
      }

      // 'Escape' key for View mode
      if (e.key === 'Escape') {
        if (!disabled && canSwitchToView) {
          onModeChange('view');
        }
      }
    };

    window.addEventListener('keydown', handleKeyPress);
    return () => window.removeEventListener('keydown', handleKeyPress);
  }, [onModeChange, disabled, canSwitchToView]);

  return (
    <div className="inline-flex rounded-md shadow-sm" role="group">
      <button
        type="button"
        onClick={() => onModeChange('view')}
        disabled={disabled || !canSwitchToView}
        className={`
          inline-flex items-center px-4 py-2 text-sm font-medium border
          ${mode === 'view'
            ? 'bg-primary text-primary-foreground border-primary z-10'
            : 'bg-background text-foreground border-border hover:bg-accent hover:text-accent-foreground'
          }
          ${disabled || !canSwitchToView ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}
          rounded-l-md focus:z-10 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2
          transition-colors
        `}
        title={canSwitchToView ? "View Mode (Esc)" : "No results to view yet"}
      >
        <svg
          className="w-4 h-4 mr-2"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
          />
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
          />
        </svg>
        View
      </button>

      <button
        type="button"
        onClick={() => onModeChange('edit')}
        disabled={disabled}
        className={`
          inline-flex items-center px-4 py-2 text-sm font-medium border
          ${mode === 'edit'
            ? 'bg-primary text-primary-foreground border-primary z-10'
            : 'bg-background text-foreground border-border hover:bg-accent hover:text-accent-foreground'
          }
          ${disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}
          rounded-r-md focus:z-10 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2
          transition-colors
          -ml-px
        `}
        title="Edit Mode (E)"
      >
        <svg
          className="w-4 h-4 mr-2"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
          />
        </svg>
        Edit
      </button>
    </div>
  );
}
