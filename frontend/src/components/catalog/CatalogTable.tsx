import React, { useState, useMemo, useRef } from "react";
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getFilteredRowModel,
  flexRender,
  SortingState,
  ColumnFiltersState,
} from "@tanstack/react-table";
import { useVirtualizer } from "@tanstack/react-virtual";
import { CatalogHullListItem } from "../../types/catalog";
import { catalogColumns } from "./CatalogTableColumns";
import { ChevronUp, ChevronDown, ChevronsUpDown } from "lucide-react";

interface CatalogTableProps {
  data: CatalogHullListItem[];
  onRowClick?: (hull: CatalogHullListItem) => void;
  isLoading?: boolean;
}

export const CatalogTable: React.FC<CatalogTableProps> = ({
  data,
  onRowClick,
  isLoading = false,
}) => {
  const [sorting, setSorting] = useState<SortingState>([]);
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([]);

  // Memoize columns for performance
  const columns = useMemo(() => catalogColumns, []);

  const table = useReactTable({
    data,
    columns,
    state: {
      sorting,
      columnFilters,
    },
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
  });

  // Virtual scrolling setup
  const tableContainerRef = useRef<HTMLDivElement>(null);
  const rows = table.getRowModel().rows;

  const rowVirtualizer = useVirtualizer({
    count: rows.length,
    getScrollElement: () => tableContainerRef.current,
    estimateSize: () => 48, // Row height in pixels
    overscan: 10, // Render 10 extra rows above/below viewport
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 dark:border-blue-400 mx-auto mb-4" />
          <p className="text-gray-600 dark:text-gray-400">Loading catalog...</p>
        </div>
      </div>
    );
  }

  if (data.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-center">
        <div className="text-gray-400 dark:text-gray-500 mb-4">
          <svg className="h-16 w-16 mx-auto" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={1.5}
              d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4"
            />
          </svg>
        </div>
        <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">No vessels found</h3>
        <p className="text-sm text-gray-500 dark:text-gray-400">
          Try adjusting your filters or search terms
        </p>
      </div>
    );
  }

  const virtualRows = rowVirtualizer.getVirtualItems();
  const totalSize = rowVirtualizer.getTotalSize();

  return (
    <div className="w-full">
      {/* Table Container with Virtual Scrolling */}
      <div
        ref={tableContainerRef}
        className="overflow-auto border border-gray-200 dark:border-gray-700 rounded-lg shadow-sm"
        style={{ height: "calc(100vh - 400px)", minHeight: "400px" }}
      >
        <table className="w-full text-sm">
          {/* Header - Sticky */}
          <thead className="bg-gray-50 dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 sticky top-0 z-10">
            {table.getHeaderGroups().map((headerGroup) => (
              <tr key={headerGroup.id}>
                {headerGroup.headers.map((header) => {
                  const canSort = header.column.getCanSort();
                  const isSorted = header.column.getIsSorted();

                  return (
                    <th
                      key={header.id}
                      className={`
                        px-4 py-3 text-left text-xs font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider
                        ${canSort ? "cursor-pointer select-none hover:bg-gray-100 dark:hover:bg-gray-700" : ""}
                      `}
                      style={{ width: header.getSize() }}
                      onClick={header.column.getToggleSortingHandler()}
                    >
                      <div className="flex items-center gap-2">
                        {flexRender(header.column.columnDef.header, header.getContext())}
                        {canSort && (
                          <span className="text-gray-400 dark:text-gray-500">
                            {isSorted === "asc" ? (
                              <ChevronUp className="h-4 w-4" />
                            ) : isSorted === "desc" ? (
                              <ChevronDown className="h-4 w-4" />
                            ) : (
                              <ChevronsUpDown className="h-4 w-4" />
                            )}
                          </span>
                        )}
                      </div>
                    </th>
                  );
                })}
              </tr>
            ))}
          </thead>

          {/* Body - Virtualized */}
          <tbody
            className="bg-white dark:bg-gray-900"
            style={{
              height: `${totalSize}px`,
              position: "relative",
            }}
          >
            {virtualRows.map((virtualRow) => {
              const row = rows[virtualRow.index];
              return (
                <tr
                  key={row.id}
                  onClick={() => onRowClick?.(row.original)}
                  className="
                    transition-all duration-150
                    hover:bg-gray-50 dark:hover:bg-gray-800
                    hover:border-l-4 hover:border-l-blue-500
                    cursor-pointer
                    border-b border-gray-200 dark:border-gray-700
                  "
                  style={{
                    position: "absolute",
                    top: 0,
                    left: 0,
                    width: "100%",
                    height: `${virtualRow.size}px`,
                    transform: `translateY(${virtualRow.start}px)`,
                  }}
                >
                  {row.getVisibleCells().map((cell) => (
                    <td key={cell.id} className="px-4 py-3 text-gray-900 dark:text-white">
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </td>
                  ))}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Results Count & Scroll Position */}
      <div className="mt-4 flex items-center justify-between text-sm text-gray-700 dark:text-gray-300">
        <div>
          Showing <span className="font-medium">{table.getFilteredRowModel().rows.length}</span>{" "}
          vessels
        </div>
        {virtualRows.length > 0 && (
          <div className="text-gray-500 dark:text-gray-400">
            Rows {virtualRows[0].index + 1} - {virtualRows[virtualRows.length - 1].index + 1}{" "}
            visible
          </div>
        )}
      </div>
    </div>
  );
};
