import React, { useState, useMemo } from "react";
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  flexRender,
  SortingState,
  ColumnFiltersState,
} from "@tanstack/react-table";
import { CatalogHullListItem } from "../../types/catalog";
import { catalogColumns } from "./CatalogTableColumns";
import { ChevronUp, ChevronDown, ChevronsUpDown } from "lucide-react";

interface CatalogTableProps {
  data: CatalogHullListItem[];
  onRowClick?: (hull: CatalogHullListItem) => void;
  onSelectionChange?: (selectedHulls: CatalogHullListItem[]) => void;
  isLoading?: boolean;
}

export const CatalogTable: React.FC<CatalogTableProps> = ({
  data,
  onRowClick,
  onSelectionChange,
  isLoading = false,
}) => {
  const [sorting, setSorting] = useState<SortingState>([]);
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([]);
  const [rowSelection, setRowSelection] = useState({});

  // Memoize columns for performance
  const columns = useMemo(() => catalogColumns, []);

  const table = useReactTable({
    data,
    columns,
    state: {
      sorting,
      columnFilters,
      rowSelection,
    },
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    onRowSelectionChange: setRowSelection,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    initialState: {
      pagination: {
        pageSize: 20,
      },
    },
    enableRowSelection: true,
  });

  // Notify parent of selection changes
  React.useEffect(() => {
    const selectedRows = table.getSelectedRowModel().rows;
    const selectedHulls = selectedRows.map((row) => row.original);
    onSelectionChange?.(selectedHulls);
  }, [rowSelection, onSelectionChange]);

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
          <svg
            className="h-16 w-16 mx-auto"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={1.5}
              d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4"
            />
          </svg>
        </div>
        <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
          No vessels found
        </h3>
        <p className="text-sm text-gray-500 dark:text-gray-400">
          Try adjusting your filters or search terms
        </p>
      </div>
    );
  }

  const selectedCount = Object.keys(rowSelection).length;

  return (
    <div className="w-full">
      {/* Selection Info Bar */}
      {selectedCount > 0 && (
        <div className="mb-4 p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg flex items-center justify-between">
          <div className="text-sm text-blue-800 dark:text-blue-300">
            <strong>{selectedCount}</strong> vessel{selectedCount > 1 ? "s" : ""} selected
          </div>
          <button
            onClick={() => setRowSelection({})}
            className="text-sm text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 font-medium"
          >
            Clear selection
          </button>
        </div>
      )}

      {/* Table Container */}
      <div className="overflow-x-auto border border-gray-200 dark:border-gray-700 rounded-lg shadow-sm">
        <table className="w-full text-sm">
          {/* Header */}
          <thead className="bg-gray-50 dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
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

          {/* Body */}
          <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
            {table.getRowModel().rows.map((row) => {
              const isSelected = row.getIsSelected();

              return (
                <tr
                  key={row.id}
                  onClick={() => onRowClick?.(row.original)}
                  className={`
                    transition-all duration-150
                    hover:bg-gray-50 dark:hover:bg-gray-800
                    hover:border-l-4 hover:border-l-blue-500
                    cursor-pointer
                    ${isSelected ? "bg-blue-50 dark:bg-blue-900/20 border-l-4 border-l-blue-500" : ""}
                  `}
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

      {/* Pagination */}
      <div className="flex items-center justify-between mt-4">
        <div className="text-sm text-gray-700 dark:text-gray-300">
          Showing{" "}
          <span className="font-medium">
            {table.getState().pagination.pageIndex * table.getState().pagination.pageSize + 1}
          </span>{" "}
          to{" "}
          <span className="font-medium">
            {Math.min(
              (table.getState().pagination.pageIndex + 1) * table.getState().pagination.pageSize,
              table.getFilteredRowModel().rows.length
            )}
          </span>{" "}
          of <span className="font-medium">{table.getFilteredRowModel().rows.length}</span> vessels
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => table.previousPage()}
            disabled={!table.getCanPreviousPage()}
            className="px-3 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded hover:bg-gray-50 dark:hover:bg-gray-800 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            Previous
          </button>
          <span className="text-sm text-gray-700 dark:text-gray-300">
            Page <span className="font-medium">{table.getState().pagination.pageIndex + 1}</span> of{" "}
            <span className="font-medium">{table.getPageCount()}</span>
          </span>
          <button
            onClick={() => table.nextPage()}
            disabled={!table.getCanNextPage()}
            className="px-3 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded hover:bg-gray-50 dark:hover:bg-gray-800 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            Next
          </button>
        </div>
      </div>
    </div>
  );
};
