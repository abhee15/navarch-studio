import { useState, useEffect } from "react";
import { X, Copy, AlertCircle } from "lucide-react";
import type { MissionCase } from "../../types/sizing";

interface CloneBriefDialogProps {
  isOpen: boolean;
  onClose: () => void;
  originalBrief: MissionCase;
  onClone: (newName: string) => Promise<void>;
  existingNames: string[];
}

/**
 * Dialog for cloning a design brief with a new name
 *
 * Features:
 * - Pre-filled name with smart suffix (e.g., "Brief Name - Copy")
 * - Real-time validation for duplicate names
 * - Error handling for API failures
 * - Loading state during clone operation
 */
export const CloneBriefDialog: React.FC<CloneBriefDialogProps> = ({
  isOpen,
  onClose,
  originalBrief,
  onClone,
  existingNames,
}) => {
  const [name, setName] = useState("");
  const [isCloning, setIsCloning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Generate smart default name when dialog opens
  useEffect(() => {
    if (isOpen) {
      const defaultName = generateDefaultName(originalBrief.name, existingNames);
      setName(defaultName);
      setError(null);
    }
  }, [isOpen, originalBrief.name, existingNames]);

  // Validate name in real-time
  const validationError = validateName(name, existingNames);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (validationError) {
      return;
    }

    setIsCloning(true);
    setError(null);

    try {
      await onClone(name.trim());
      onClose();
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : "Failed to clone brief";
      setError(errorMessage);
    } finally {
      setIsCloning(false);
    }
  };

  if (!isOpen) return null;

  return (
    <>
      {/* Backdrop */}
      <div className="fixed inset-0 bg-black/50 z-50 transition-opacity" onClick={onClose} />

      {/* Dialog */}
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div
          className="bg-card rounded-lg shadow-2xl max-w-md w-full border border-border"
          onClick={(e) => e.stopPropagation()}
        >
          {/* Header */}
          <div className="flex items-center justify-between p-6 border-b border-border">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-primary/10 rounded-lg">
                <Copy className="h-5 w-5 text-primary" />
              </div>
              <h2 className="text-xl font-semibold text-foreground">Clone Brief</h2>
            </div>
            <button
              onClick={onClose}
              className="text-muted-foreground hover:text-foreground transition-colors"
              disabled={isCloning}
            >
              <X className="h-5 w-5" />
            </button>
          </div>

          {/* Body */}
          <form onSubmit={handleSubmit} className="p-6">
            <div className="mb-6">
              <p className="text-sm text-muted-foreground mb-4">
                Cloning from:{" "}
                <span className="font-medium text-foreground">{originalBrief.name}</span>
              </p>

              {/* Name Input */}
              <div>
                <label
                  htmlFor="brief-name"
                  className="block text-sm font-medium text-foreground mb-2"
                >
                  New Brief Name *
                </label>
                <input
                  id="brief-name"
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  disabled={isCloning}
                  className={`w-full px-3 py-2 border rounded-lg bg-background text-foreground placeholder-muted-foreground focus:outline-none focus:ring-2 transition-colors ${
                    validationError
                      ? "border-destructive focus:ring-destructive"
                      : "border-border focus:ring-primary"
                  }`}
                  placeholder="Enter brief name..."
                  autoFocus
                />

                {/* Validation Error */}
                {validationError && (
                  <div className="mt-2 flex items-start gap-2 text-sm text-destructive">
                    <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
                    <span>{validationError}</span>
                  </div>
                )}
              </div>
            </div>

            {/* Server Error */}
            {error && (
              <div className="mb-4 p-3 bg-destructive/10 border border-destructive/20 rounded-lg">
                <div className="flex items-start gap-2 text-sm text-destructive">
                  <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
                  <span>{error}</span>
                </div>
              </div>
            )}

            {/* Footer */}
            <div className="flex items-center gap-3 justify-end">
              <button
                type="button"
                onClick={onClose}
                disabled={isCloning}
                className="px-4 py-2 text-sm font-medium text-muted-foreground hover:text-foreground border border-border rounded-lg hover:bg-accent transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isCloning || !!validationError || !name.trim()}
                className="px-4 py-2 text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
              >
                {isCloning ? (
                  <>
                    <div className="h-4 w-4 border-2 border-primary-foreground/30 border-t-primary-foreground rounded-full animate-spin" />
                    Cloning...
                  </>
                ) : (
                  <>
                    <Copy className="h-4 w-4" />
                    Clone Brief
                  </>
                )}
              </button>
            </div>
          </form>
        </div>
      </div>
    </>
  );
};

/**
 * Generate a smart default name for the cloned brief
 * - "Brief Name" → "Brief Name - Copy"
 * - "Brief Name - Copy" → "Brief Name - Copy 2"
 * - "Brief Name - Copy 2" → "Brief Name - Copy 3"
 */
function generateDefaultName(originalName: string, existingNames: string[]): string {
  const copyPattern = /^(.+?)(?: - Copy(?: (\d+))?)?$/;
  const match = originalName.match(copyPattern);

  if (!match) {
    return `${originalName} - Copy`;
  }

  const baseName = match[1];
  const copyNumber = match[2] ? parseInt(match[2], 10) : 1;

  // Try incrementing copy number until we find a unique name
  for (let i = copyNumber + 1; i < copyNumber + 100; i++) {
    const suffix = i === 1 ? " - Copy" : ` - Copy ${i}`;
    const candidateName = `${baseName}${suffix}`;

    if (!existingNames.some((name) => name.toLowerCase() === candidateName.toLowerCase())) {
      return candidateName;
    }
  }

  // Fallback: add timestamp if all else fails
  return `${baseName} - Copy ${Date.now()}`;
}

/**
 * Validate brief name
 * Returns error message if invalid, null if valid
 */
function validateName(name: string, existingNames: string[]): string | null {
  const trimmedName = name.trim();

  if (!trimmedName) {
    return "Brief name is required";
  }

  if (trimmedName.length > 255) {
    return "Brief name cannot exceed 255 characters";
  }

  // Case-insensitive duplicate check
  const isDuplicate = existingNames.some(
    (existingName) => existingName.toLowerCase() === trimmedName.toLowerCase()
  );

  if (isDuplicate) {
    return "A brief with this name already exists";
  }

  return null;
}

