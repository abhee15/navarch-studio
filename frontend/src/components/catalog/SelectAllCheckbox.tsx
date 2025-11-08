import React from "react";
import { Table } from "@tanstack/react-table";
import { CatalogHullListItem } from "../../types/catalog";

interface SelectAllCheckboxProps {
  table: Table<CatalogHullListItem>;
}

export const SelectAllCheckbox: React.FC<SelectAllCheckboxProps> = ({ table }) => {
  const checkboxRef = React.useRef<HTMLInputElement>(null);
  const isSomeSelected = table.getIsSomeRowsSelected();

  React.useEffect(() => {
    if (checkboxRef.current) {
      checkboxRef.current.indeterminate = isSomeSelected;
    }
  }, [isSomeSelected]);

  return (
    <input
      ref={checkboxRef}
      type="checkbox"
      checked={table.getIsAllRowsSelected()}
      onChange={table.getToggleAllRowsSelectedHandler()}
      className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
      aria-label="Select all vessels"
    />
  );
};
