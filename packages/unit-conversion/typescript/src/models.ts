/**
 * Unit system types
 */
export type UnitSystemId = 'SI' | 'Imperial';
export type UnitCategory = 'Length' | 'Mass' | 'Area' | 'Volume' | 'Density' | 'MomentOfInertia';
export type Locale = 'en' | 'es';

/**
 * Unit system definition
 */
export interface UnitSystemDefinition {
  id: UnitSystemId;
  isDefault: boolean;
  names: Record<Locale, string>;
  descriptions: Record<Locale, string>;
  categories: Record<UnitCategory, CategoryDefinition>;
}

/**
 * Category definition
 */
export interface CategoryDefinition {
  id: UnitCategory;
  names: Record<Locale, string>;
  units: UnitDefinition[];
}

/**
 * Unit definition
 */
export interface UnitDefinition {
  id: string;
  symbol: string;
  isBase: boolean;
  names: Record<Locale, string>;
  pluralNames: Record<Locale, string>;
  conversionFactor?: number;
  baseUnit?: string;
}

/**
 * Conversion factor
 */
export interface ConversionFactor {
  from: string;
  to: string;
  factor: number;
}

/**
 * Unit system information for display
 */
export interface UnitSystemInfo {
  id: UnitSystemId;
  name: string;
  description: string;
  isDefault: boolean;
  categories: UnitCategory[];
}

/**
 * Unit information for display
 */
export interface UnitInfo {
  id: string;
  symbol: string;
  name: string;
  pluralName: string;
  category: UnitCategory;
  isBase: boolean;
}

