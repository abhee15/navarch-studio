import React, { useState, useEffect } from "react";
import { Search, Filter, X, ChevronDown } from "lucide-react";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { Button } from "../ui/button";
import { Select } from "../ui/select";
import { getShipDVesselTaxonomy } from "../../services/catalogApi";
import type { ShipDVesselTaxonomy } from "../../types/sizing";

export interface CatalogFilters {
  searchText: string;
  hullTypes: string[];
  statuses: string[];
  lppRange: [number, number];
  cbRange: [number, number];
  // ShipD taxonomy filters
  vesselCategory?: string;
  shipdVesselType?: string;
  bowFamily?: string;
  midshipFamily?: string;
  sternFamily?: string;
}

interface FilterPanelProps {
  filters: CatalogFilters;
  onFiltersChange: (filters: CatalogFilters) => void;
  totalResults: number;
}

const HULL_TYPES = ["Container", "Tanker", "Naval", "Template"];
const STATUSES = ["Complete", "Partial", "Minimal", "Missing Geometry"];

export const FilterPanel: React.FC<FilterPanelProps> = ({
  filters,
  onFiltersChange,
  totalResults,
}) => {
  const [isExpanded, setIsExpanded] = useState(false);
  const [localSearchText, setLocalSearchText] = useState(filters.searchText);
  const [taxonomy, setTaxonomy] = useState<ShipDVesselTaxonomy[]>([]);
  const [loadingTaxonomy, setLoadingTaxonomy] = useState(false);

  // Load ShipD taxonomy on mount
  useEffect(() => {
    const loadTaxonomy = async () => {
      setLoadingTaxonomy(true);
      try {
        const data = await getShipDVesselTaxonomy();
        setTaxonomy(data);
      } catch (error) {
        console.error("Failed to load ShipD taxonomy:", error);
      } finally {
        setLoadingTaxonomy(false);
      }
    };
    loadTaxonomy();
  }, []);

  // Get unique categories from taxonomy
  const categories = Array.from(new Set(taxonomy.map((t) => t.category))).sort();

  // Get vessel types for selected category
  const vesselTypesForCategory = filters.vesselCategory
    ? taxonomy
        .filter((t) => t.category === filters.vesselCategory)
        .map((t) => ({ value: t.type, label: t.displayName }))
    : [];

  // Get taxonomy entry for selected vessel type
  const selectedTaxonomyEntry = filters.shipdVesselType
    ? taxonomy.find((t) => t.type === filters.shipdVesselType)
    : null;

  // Get families from selected taxonomy entry
  const bowFamilies = selectedTaxonomyEntry?.bowFamilies || [];
  const midshipFamilies = selectedTaxonomyEntry?.midshipFamilies || [];
  const sternFamilies = selectedTaxonomyEntry?.sternFamilies || [];

  // Debounce search text
  useEffect(() => {
    const timer = setTimeout(() => {
      if (localSearchText !== filters.searchText) {
        onFiltersChange({ ...filters, searchText: localSearchText });
      }
    }, 300);
    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [localSearchText]); // Intentionally omit filters to avoid infinite loop

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
      vesselCategory: undefined,
      shipdVesselType: undefined,
      bowFamily: undefined,
      midshipFamily: undefined,
      sternFamily: undefined,
    });
  };

  const hasActiveFilters =
    filters.searchText ||
    filters.hullTypes.length > 0 ||
    filters.statuses.length > 0 ||
    filters.lppRange[0] > 0 ||
    filters.lppRange[1] < 500 ||
    filters.cbRange[0] > 0.3 ||
    filters.cbRange[1] < 1.0 ||
    filters.vesselCategory ||
    filters.shipdVesselType ||
    filters.bowFamily ||
    filters.midshipFamily ||
    filters.sternFamily;

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
                className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-foreground"
              >
                <X className="h-4 w-4" />
              </button>
            )}
          </div>
          <Button
            variant="outline"
            onClick={() => setIsExpanded(!isExpanded)}
            className="flex items-center gap-2"
          >
            <Filter className="h-4 w-4" />
            Filters
            {hasActiveFilters && (
              <span className="ml-1 px-1.5 py-0.5 text-xs bg-blue-600 text-white rounded-full">
                {filters.hullTypes.length + filters.statuses.length}
              </span>
            )}
            <ChevronDown
              className={`h-4 w-4 transition-transform ${isExpanded ? "rotate-180" : ""}`}
            />
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

          {/* ShipD Taxonomy Filters */}
          {!loadingTaxonomy && taxonomy.length > 0 && (
            <div className="space-y-4 border-t border-gray-200 dark:border-gray-700 pt-4">
              <div>
                <Label className="text-sm font-semibold mb-2 block">Hull Form Parameters Taxonomy</Label>
                <div className="space-y-3">
                  {/* Vessel Category */}
                  <div>
                    <Label className="text-xs text-gray-600 dark:text-gray-400 mb-1 block">
                      Category
                    </Label>
                    <Select
                      value={filters.vesselCategory || ""}
                      onChange={(value: string) =>
                        onFiltersChange({
                          ...filters,
                          vesselCategory: value || undefined,
                          shipdVesselType: undefined, // Reset vessel type when category changes
                          bowFamily: undefined,
                          midshipFamily: undefined,
                          sternFamily: undefined,
                        })
                      }
                      options={[
                        { value: "", label: "All Categories" },
                        ...categories.map((cat) => ({ value: cat, label: cat })),
                      ]}
                      placeholder="All Categories"
                      className="w-full"
                    />
                  </div>

                  {/* Vessel Type (filtered by category) */}
                  {filters.vesselCategory && vesselTypesForCategory.length > 0 && (
                    <div>
                      <Label className="text-xs text-gray-600 dark:text-gray-400 mb-1 block">
                        Vessel Type
                      </Label>
                      <Select
                        value={filters.shipdVesselType || ""}
                        onChange={(value: string) =>
                          onFiltersChange({
                            ...filters,
                            shipdVesselType: value || undefined,
                            bowFamily: undefined,
                            midshipFamily: undefined,
                            sternFamily: undefined,
                          })
                        }
                        options={[
                          { value: "", label: "All Types" },
                          ...vesselTypesForCategory.map((vt) => ({
                            value: vt.value,
                            label: vt.label,
                          })),
                        ]}
                        placeholder="All Types"
                        className="w-full"
                      />
                    </div>
                  )}

                  {/* Hull Families (shown when vessel type is selected) */}
                  {selectedTaxonomyEntry && (
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                      {/* Bow Family */}
                      {bowFamilies.length > 0 && (
                        <div>
                          <Label className="text-xs text-gray-600 dark:text-gray-400 mb-1 block">
                            Bow Family
                          </Label>
                          <Select
                            value={filters.bowFamily || ""}
                            onChange={(value: string) =>
                              onFiltersChange({
                                ...filters,
                                bowFamily: value || undefined,
                              })
                            }
                            options={[
                              { value: "", label: "All" },
                              ...bowFamilies.map((family) => ({
                                value: family,
                                label: family
                                  .replace(/_/g, " ")
                                  .replace(/\b\w/g, (l) => l.toUpperCase()),
                              })),
                            ]}
                            placeholder="All"
                            className="w-full"
                          />
                        </div>
                      )}

                      {/* Midship Family */}
                      {midshipFamilies.length > 0 && (
                        <div>
                          <Label className="text-xs text-gray-600 dark:text-gray-400 mb-1 block">
                            Midship Family
                          </Label>
                          <Select
                            value={filters.midshipFamily || ""}
                            onChange={(value: string) =>
                              onFiltersChange({
                                ...filters,
                                midshipFamily: value || undefined,
                              })
                            }
                            options={[
                              { value: "", label: "All" },
                              ...midshipFamilies.map((family) => ({
                                value: family,
                                label: family
                                  .replace(/_/g, " ")
                                  .replace(/\b\w/g, (l) => l.toUpperCase()),
                              })),
                            ]}
                            placeholder="All"
                            className="w-full"
                          />
                        </div>
                      )}

                      {/* Stern Family */}
                      {sternFamilies.length > 0 && (
                        <div>
                          <Label className="text-xs text-gray-600 dark:text-gray-400 mb-1 block">
                            Stern Family
                          </Label>
                          <Select
                            value={filters.sternFamily || ""}
                            onChange={(value: string) =>
                              onFiltersChange({
                                ...filters,
                                sternFamily: value || undefined,
                              })
                            }
                            options={[
                              { value: "", label: "All" },
                              ...sternFamilies.map((family) => ({
                                value: family,
                                label: family
                                  .replace(/_/g, " ")
                                  .replace(/\b\w/g, (l) => l.toUpperCase()),
                              })),
                            ]}
                            placeholder="All"
                            className="w-full"
                          />
                        </div>
                      )}
                    </div>
                  )}
                </div>
              </div>
            </div>
          )}

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
