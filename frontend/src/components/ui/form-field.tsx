import * as React from "react";
import { Label } from "./label";
import { AlertCircle } from "lucide-react";
import { cn } from "../../lib/utils";

interface FormFieldProps {
  label: string;
  htmlFor: string;
  error?: string | null;
  touched?: boolean;
  required?: boolean;
  helpText?: string;
  children: React.ReactElement;
  className?: string;
}

/**
 * FormField - Wrapper component for form inputs with validation
 *
 * Provides consistent styling for:
 * - Labels with required indicator
 * - Error messages with icon
 * - Help text
 * - Error state styling (red border)
 *
 * @example
 * <FormField
 *   label="Vessel Name"
 *   htmlFor="name"
 *   required
 *   error={errors.name}
 *   touched={touched.name}
 *   helpText="Use a unique name"
 * >
 *   <Input id="name" value={value} onChange={onChange} />
 * </FormField>
 */
export const FormField: React.FC<FormFieldProps> = ({
  label,
  htmlFor,
  error,
  touched,
  required,
  helpText,
  children,
  className,
}) => {
  const hasError = touched && error;

  return (
    <div className={cn("space-y-2", className)}>
      <Label htmlFor={htmlFor}>
        {label}
        {required && <span className="text-red-500 dark:text-red-400 ml-1">*</span>}
      </Label>

      {/* Clone child element and add error styling */}
      {React.cloneElement(children, {
        className: cn(
          (children.props as Record<string, unknown>).className as string | undefined,
          hasError &&
            "border-red-500 dark:border-red-400 focus-visible:ring-red-500 dark:focus-visible:ring-red-400"
        ),
        "aria-invalid": hasError || undefined,
        "aria-describedby": hasError
          ? `${htmlFor}-error`
          : helpText
            ? `${htmlFor}-help`
            : undefined,
      } as Record<string, unknown>)}

      {/* Help text - shown when no error */}
      {!hasError && helpText && (
        <p id={`${htmlFor}-help`} className="text-xs text-gray-500 dark:text-gray-400">
          {helpText}
        </p>
      )}

      {/* Error message - shown when field is touched and has error */}
      {hasError && (
        <p
          id={`${htmlFor}-error`}
          className="text-xs font-medium text-red-600 dark:text-red-400 flex items-center gap-1 animate-in fade-in slide-in-from-top-1 duration-200"
        >
          <AlertCircle className="h-3 w-3 flex-shrink-0" />
          <span>{error}</span>
        </p>
      )}
    </div>
  );
};
