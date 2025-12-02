/**
 * Geometry Validation Wrapper Component
 *
 * Wraps visualization components with geometry validation and error handling.
 * Provides consistent error messages and fallback rendering when geometry is invalid.
 */

import React from "react";
import { AlertTriangle, Loader2 } from "lucide-react";
import {
  validateOffsetsGrid,
  type GeometryValidationResult,
} from "../../../utils/geometryValidation";
import type { OffsetsGrid } from "../../../utils/geometryFormatConverter";

interface GeometryValidationWrapperProps {
  geometryJson: string | null | undefined;
  children: (
    validatedGeometry: OffsetsGrid | null,
    validation: GeometryValidationResult
  ) => React.ReactNode;
  showWarnings?: boolean;
  fallbackMessage?: string;
}

/**
 * Validates geometry and provides error/warning UI if needed
 */
export const GeometryValidationWrapper: React.FC<GeometryValidationWrapperProps> = ({
  geometryJson,
  children,
  showWarnings = true,
  fallbackMessage = "Unable to load geometry data",
}) => {
  // Parse and validate geometry
  const [validation, setValidation] = React.useState<GeometryValidationResult | null>(null);
  const [validatedGeometry, setValidatedGeometry] = React.useState<OffsetsGrid | null>(null);
  const [isLoading, setIsLoading] = React.useState(true);

  React.useEffect(() => {
    setIsLoading(true);

    if (!geometryJson) {
      setValidation({
        isValid: false,
        errors: ["No geometry data provided"],
        warnings: [],
      });
      setValidatedGeometry(null);
      setIsLoading(false);
      return;
    }

    try {
      const parsed = JSON.parse(geometryJson);

      // Check if it's an OffsetsGrid format
      const stations = parsed.stations ?? parsed.Stations;
      const waterlines = parsed.waterlines ?? parsed.Waterlines;
      const offsets = parsed.offsets ?? parsed.Offsets;

      if (Array.isArray(stations) && Array.isArray(waterlines) && Array.isArray(offsets)) {
        const grid: OffsetsGrid = {
          stations,
          waterlines,
          offsets,
        };

        const result = validateOffsetsGrid(grid);
        setValidation(result);
        setValidatedGeometry(result.sanitized || grid);
      } else {
        // Not an OffsetsGrid - validation will be done after normalization
        setValidation({
          isValid: true, // Assume valid until normalization
          errors: [],
          warnings: [],
        });
        setValidatedGeometry(null);
      }
    } catch (error) {
      setValidation({
        isValid: false,
        errors: [
          `Failed to parse geometry JSON: ${error instanceof Error ? error.message : String(error)}`,
        ],
        warnings: [],
      });
      setValidatedGeometry(null);
    } finally {
      setIsLoading(false);
    }
  }, [geometryJson]);

  if (isLoading) {
    return (
      <div className="w-full h-full flex items-center justify-center">
        <Loader2 className="h-6 w-6 animate-spin text-gray-400" />
        <span className="ml-2 text-sm text-gray-500">Validating geometry...</span>
      </div>
    );
  }

  if (!validation) {
    return (
      <div className="w-full h-full flex items-center justify-center text-gray-500">
        {fallbackMessage}
      </div>
    );
  }

  // Show error UI if validation failed
  if (!validation.isValid) {
    return (
      <div className="w-full h-full p-4 flex flex-col items-center justify-center">
        <div className="rounded-lg border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 shadow-lg max-w-md w-full">
          <div className="p-6">
            <div className="flex items-center gap-2 mb-3">
              <AlertTriangle className="h-5 w-5 text-red-600 dark:text-red-400" />
              <h3 className="text-red-800 dark:text-red-200 font-bold">
                Geometry Validation Failed
              </h3>
            </div>
            <div className="space-y-1 mb-3">
              {validation.errors.map((error, idx) => (
                <p key={idx} className="text-sm text-red-700 dark:text-red-300">
                  • {error}
                </p>
              ))}
            </div>
            {validation.warnings.length > 0 && showWarnings && (
              <div className="mt-4 pt-4 border-t border-red-200 dark:border-red-800">
                <p className="text-xs font-semibold text-red-600 dark:text-red-400 mb-2">
                  Warnings:
                </p>
                <div className="space-y-1">
                  {validation.warnings.slice(0, 3).map((warning, idx) => (
                    <p key={idx} className="text-xs text-red-600 dark:text-red-400">
                      • {warning}
                    </p>
                  ))}
                  {validation.warnings.length > 3 && (
                    <p className="text-xs text-red-600 dark:text-red-400">
                      ... and {validation.warnings.length - 3} more warnings
                    </p>
                  )}
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    );
  }

  // Show warnings if any (but still render)
  if (validation.warnings.length > 0 && showWarnings) {
    return (
      <div className="w-full h-full flex flex-col">
        {/* Warning banner */}
        <div className="px-4 py-2 bg-yellow-50 dark:bg-yellow-900/20 border-b border-yellow-200 dark:border-yellow-800">
          <div className="flex items-start gap-2">
            <AlertTriangle className="h-4 w-4 text-yellow-600 dark:text-yellow-400 mt-0.5 flex-shrink-0" />
            <div className="flex-1 min-w-0">
              <p className="text-xs font-semibold text-yellow-800 dark:text-yellow-200 mb-1">
                Geometry Validation Warnings
              </p>
              <div className="space-y-0.5">
                {validation.warnings.slice(0, 2).map((warning, idx) => (
                  <p key={idx} className="text-xs text-yellow-700 dark:text-yellow-300">
                    • {warning}
                  </p>
                ))}
                {validation.warnings.length > 2 && (
                  <p className="text-xs text-yellow-600 dark:text-yellow-400">
                    ... and {validation.warnings.length - 2} more warnings
                  </p>
                )}
              </div>
            </div>
          </div>
        </div>
        {/* Render visualization with validated geometry */}
        <div className="flex-1 min-h-0">{children(validatedGeometry, validation)}</div>
      </div>
    );
  }

  // Render normally if valid
  return <>{children(validatedGeometry, validation)}</>;
};
