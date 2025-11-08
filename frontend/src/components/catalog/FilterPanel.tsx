import React, { useState, useEffect } from "react";
import { Search, Filter, X, ChevronDown } from "lucide-react";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { Button } from "../ui/button";

export interface CatalogFilters {
  searchText: string;
  hullTypes: string[];
  statuses: string[];
  lppRange: [number, number];
  cbRange: [number, number];
}

interface FilterPanelProps {
  filters: CatalogFilters;
  onFiltersChange: (filters: CatalogFilters) => void;
  totalResults: number;
}

const HULL_TYPES = ["Container", "Tanker", "Naval", "Template"];
const STATUSES = ["Complete", "Partial", "Minimal", "Missing Geometry"];

export const FilterPanel: React.FC<FilterPanelProps> = ({ filters, onFiltersChange, totalResults }) => {
  const [isExpanded, setIsExpanded] = useState(false);
  const [localSearchText, setLocalSearchText] = useState(filters.searchText);

  // Debounce search text
  useEffect(() => {
    const timer = setTimeout(() => {
      if (localSearchText !== filters.searchText) {
        onFiltersChange({ ...filters, searchText: localSearchText });
      }
    }, 300);
    return () => clearTimeout(timer);
  }, [localSearchText]);

  const handleHullTypeToggle = (type: string) => {
    const newTypes = filters.hullTypes.includes(type)
      ? filters.hullTypes.filter((t) => t !== type)
      : [...filters.hullTypes, type];
    onFiltersChange({ ...filters, hullTypes: newTypes });
  };

  const handleStatusToggle = (status: string) => {
    const newStatuses = filters.statuses.includes(status)
      ? filters.statuses.filter((s) => s !== status)
      : [...filters.statuses, status];
    onFiltersChange({ ...filters, statuses: newStatuses });
  };

  const handleReset = () => {
    setLocalSearchText("");
    onFiltersChange({
      searchText: "",
      hullTypes: [],
      statuses: [],
      lppRange: [0, 500],
      cbRange: [0.3, 1.0],
    });
  };

  const hasActiveFilters =
    filters.searchText ||
    filters.hullTypes.length > 0 ||
    filters.statuses.length > 0 ||
    filters.lppRange[0] > 0 ||
    filters.lppRange[1] < 500 ||
    filters.cbRange[0] > 0.3 ||
    filters.cbRange[1] < 1.0;

  return (
    <div className="mb-6 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 shadow-sm">
      {/* Always Visible: Search Bar */}
      <div className="p-4">
        <div className="flex gap-3">
          <div className="flex-1 relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400 dark:text-gray-500" />
            <Input
              type="text"
              placeholder="Search by title, slug, or description..."
              value={localSearchText}
              onChange={(e) => setLocalSearchText(e.target.value)}
              className="pl-10"
            />
            {localSearchText && (
              <button
                onClick={() => setLocalSearchText("")}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:text-gray-500 dark:hover:text-gray-300"
              >
                <X className="h-4 w-4" />
              </button>
            )}
          </div>
          <Button variant="outline" onClick={() => setIsExpanded(!isExpanded)} className="flex items-center gap-2">
            <Filter className="h-4 w-4" />
            Filters
            {hasActiveFilters && (
              <span className="ml-1 px-1.5 py-0.5 text-xs bg-blue-600 text-white rounded-full">
                {filters.hullTypes.length + filters.statuses.length}
              </span>
            )}
            <ChevronDown className={`h-4 w-4 transition-transform ${isExpanded ? "rotate-180" : ""}`} />
          </Button>
        </div>

        {/* Results Count */}
        <div className="mt-2 text-sm text-gray-600 dark:text-gray-400">
          {totalResults} vessel{totalResults !== 1 ? "s" : ""} found
        </div>
      </div>

      {/* Expandable: Advanced Filters */}
      {isExpanded && (
        <div className="px-4 pb-4 space-y-4 border-t border-gray-200 dark:border-gray-700 pt-4">
          {/* Hull Type Filter */}
          <div>
            <Label className="text-sm font-semibold mb-2 block">Hull Type</Label>
            <div className="flex flex-wrap gap-2">
              {HULL_TYPES.map((type) => {
                const isSelected = filters.hullTypes.includes(type);
                return (
                  <button
                    key={type}
                    onClick={() => handleHullTypeToggle(type)}
                    className={`
                      px-3 py-1.5 text-sm rounded-full border transition-colors
                      ${
                        isSelected
                          ? "bg-blue-600 text-white border-blue-600"
                          : "bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-300 border-gray-300 dark:border-gray-600 hover:border-blue-500 dark:hover:border-blue-400"
                      }
                    `}
                  >
                    {type}
                  </button>
                );
              })}
            </div>
          </div>

          {/* Status Filter */}
          <div>
            <Label className="text-sm font-semibold mb-2 block">Data Quality Status</Label>
            <div className="flex flex-wrap gap-2">
              {STATUSES.map((status) => {
                const isSelected = filters.statuses.includes(status);
                return (
                  <button
                    key={status}
                    onClick={() => handleStatusToggle(status)}
                    className={`
                      px-3 py-1.5 text-sm rounded-full border transition-colors
                      ${
                        isSelected
                          ? "bg-blue-600 text-white border-blue-600"
                          : "bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-300 border-gray-300 dark:border-gray-600 hover:border-blue-500 dark:hover:border-blue-400"
                      }
                    `}
                  >
                    {status}
                  </button>
                );
              })}
            </div>
          </div>

          {/* Range Filters */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Lpp Range */}
            <div>
              <Label className="text-sm font-semibold mb-2 block">Length (Lpp)</Label>
              <div className="flex items-center gap-2">
                <Input
                  type="number"
                  min="0"
                  max="500"
                  value={filters.lppRange[0]}
                  onChange={(e) =>
                    onFiltersChange({
                      ...filters,
                      lppRange: [Number(e.target.value), filters.lppRange[1]],
                    })
                  }
                  className="w-24"
                  placeholder="Min"
                />
                <span className="text-gray-500 dark:text-gray-400">to</span>
                <Input
                  type="number"
                  min="0"
                  max="500"
                  value={filters.lppRange[1]}
                  onChange={(e) =>
                    onFiltersChange({
                      ...filters,
                      lppRange: [filters.lppRange[0], Number(e.target.value)],
                    })
                  }
                  className="w-24"
                  placeholder="Max"
                />
                <span className="text-sm text-gray-500 dark:text-gray-400">m</span>
              </div>
            </div>

            {/* Cb Range */}
            <div>
              <Label className="text-sm font-semibold mb-2 block">Block Coefficient (Cb)</Label>
              <div className="flex items-center gap-2">
                <Input
                  type="number"
                  min="0.3"
                  max="1.0"
                  step="0.05"
                  value={filters.cbRange[0]}
                  onChange={(e) =>
                    onFiltersChange({
                      ...filters,
                      cbRange: [Number(e.target.value), filters.cbRange[1]],
                    })
                  }
                  className="w-24"
                  placeholder="Min"
                />
                <span className="text-gray-500 dark:text-gray-400">to</span>
                <Input
                  type="number"
                  min="0.3"
                  max="1.0"
                  step="0.05"
                  value={filters.cbRange[1]}
                  onChange={(e) =>
                    onFiltersChange({
                      ...filters,
                      cbRange: [filters.cbRange[0], Number(e.target.value)],
                    })
                  }
                  className="w-24"
                  placeholder="Max"
                />
              </div>
            </div>
          </div>

          {/* Quick Filters */}
          <div>
            <Label className="text-sm font-semibold mb-2 block">Quick Filters</Label>
            <div className="flex flex-wrap gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() =>
                  onFiltersChange({
                    ...filters,
                    statuses: ["Complete"],
                  })
                }
              >
                Complete Data Only
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() =>
                  onFiltersChange({
                    ...filters,
                    statuses: STATUSES.filter((s) => s !== "Missing Geometry"),
                  })
                }
              >
                Has Geometry
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() =>
                  onFiltersChange({
                    ...filters,
                    lppRange: [200, 500],
                  })
                }
              >
                Large Vessels (Lpp &gt; 200m)
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() =>
                  onFiltersChange({
                    ...filters,
                    cbRange: [0.75, 1.0],
                  })
                }
              >
                High Cb (&gt; 0.75)
              </Button>
            </div>
          </div>

          {/* Reset Button */}
          {hasActiveFilters && (
            <div className="pt-2 border-t border-gray-200 dark:border-gray-700">
              <Button variant="outline" onClick={handleReset} className="w-full">
                <X className="h-4 w-4 mr-2" />
                Reset All Filters
              </Button>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
