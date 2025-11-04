interface MobileActionBarProps {
  onCancel: () => void;
  onSave: () => void;
  loading?: boolean;
}

export function MobileActionBar({ onCancel, onSave, loading }: MobileActionBarProps) {
  return (
    <div className="fixed bottom-0 left-0 right-0 bg-card/95 backdrop-blur-sm border-t border-border p-4 md:hidden safe-area-pb">
      <div className="grid grid-cols-2 gap-3">
        <button
          onClick={onCancel}
          disabled={loading}
          className="px-4 py-3 rounded-xl border border-border text-sm font-medium text-foreground hover:bg-accent/10 disabled:opacity-50"
        >
          Cancel
        </button>
        <button
          onClick={onSave}
          disabled={loading}
          className="px-4 py-3 rounded-xl bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? "Saving..." : "Save Vessel"}
        </button>
      </div>
    </div>
  );
}
