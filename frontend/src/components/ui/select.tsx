import * as React from "react";
import { cn } from "../../lib/utils";
import { ChevronDown, Check } from "lucide-react";

export interface SelectOption {
  value: string;
  label: string;
}

export interface SelectProps {
  value: string;
  onChange: (value: string) => void;
  options: SelectOption[];
  placeholder?: string;
  className?: string;
  disabled?: boolean;
  id?: string;
  name?: string;
}

export const Select = React.forwardRef<HTMLButtonElement, SelectProps>(
  ({ value, onChange, options, placeholder, className, disabled = false, id, name }, ref) => {
    const [isOpen, setIsOpen] = React.useState(false);
    const containerRef = React.useRef<HTMLDivElement>(null);
    const listRef = React.useRef<HTMLUListElement>(null);

    const selectedOption = options.find((opt) => opt.value === value);

    // Close dropdown when clicking outside
    React.useEffect(() => {
      const handleClickOutside = (event: MouseEvent) => {
        if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
          setIsOpen(false);
        }
      };

      if (isOpen) {
        document.addEventListener("mousedown", handleClickOutside);
        return () => document.removeEventListener("mousedown", handleClickOutside);
      }
    }, [isOpen]);

    // Handle keyboard navigation
    const handleKeyDown = (event: React.KeyboardEvent) => {
      if (disabled) return;

      switch (event.key) {
        case "Enter":
        case " ":
          event.preventDefault();
          setIsOpen(!isOpen);
          break;
        case "Escape":
          setIsOpen(false);
          break;
        case "ArrowDown":
          event.preventDefault();
          if (!isOpen) {
            setIsOpen(true);
          } else {
            const currentIndex = options.findIndex((opt) => opt.value === value);
            const nextIndex = Math.min(currentIndex + 1, options.length - 1);
            onChange(options[nextIndex].value);
          }
          break;
        case "ArrowUp":
          event.preventDefault();
          if (!isOpen) {
            setIsOpen(true);
          } else {
            const currentIndex = options.findIndex((opt) => opt.value === value);
            const prevIndex = Math.max(currentIndex - 1, 0);
            onChange(options[prevIndex].value);
          }
          break;
      }
    };

    const handleOptionClick = (optionValue: string) => {
      onChange(optionValue);
      setIsOpen(false);
    };

    return (
      <div ref={containerRef} className={cn("relative", className)}>
        {/* Hidden native select for form compatibility */}
        <select
          id={id}
          name={name}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className="sr-only"
          tabIndex={-1}
          aria-hidden="true"
        >
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>

        {/* Custom select trigger */}
        <button
          ref={ref}
          type="button"
          role="combobox"
          aria-expanded={isOpen}
          aria-haspopup="listbox"
          aria-controls={`${id}-listbox`}
          disabled={disabled}
          onClick={() => !disabled && setIsOpen(!isOpen)}
          onKeyDown={handleKeyDown}
          className={cn(
            "flex h-10 w-full items-center justify-between rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background",
            "focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2",
            "disabled:cursor-not-allowed disabled:opacity-50",
            "transition-colors",
            className
          )}
        >
          <span className={cn("text-foreground", !selectedOption && "text-muted-foreground")}>
            {selectedOption?.label || placeholder || "Select..."}
          </span>
          <ChevronDown
            className={cn(
              "h-4 w-4 text-muted-foreground transition-transform",
              isOpen && "transform rotate-180"
            )}
          />
        </button>

        {/* Dropdown menu */}
        {isOpen && (
          <ul
            ref={listRef}
            role="listbox"
            id={`${id}-listbox`}
            className={cn(
              "absolute z-50 mt-1 w-full rounded-md border border-border bg-popover shadow-lg",
              "max-h-60 overflow-auto",
              "animate-in fade-in-80"
            )}
          >
            {options.map((option) => {
              const isSelected = option.value === value;
              return (
                <li
                  key={option.value}
                  role="option"
                  aria-selected={isSelected}
                  onClick={() => handleOptionClick(option.value)}
                  className={cn(
                    "relative flex cursor-pointer select-none items-center rounded-sm px-3 py-2 text-sm outline-none",
                    "transition-colors",
                    isSelected
                      ? "bg-accent text-accent-foreground font-medium"
                      : "text-popover-foreground hover:bg-accent/50 hover:text-accent-foreground"
                  )}
                >
                  <span className="flex-1">{option.label}</span>
                  {isSelected && <Check className="h-4 w-4" />}
                </li>
              );
            })}
          </ul>
        )}
      </div>
    );
  }
);

Select.displayName = "Select";
