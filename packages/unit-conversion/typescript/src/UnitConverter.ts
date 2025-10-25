import type {
  UnitSystemId,
  UnitCategory,
  Locale,
  UnitSystemInfo,
  UnitInfo,
} from './models';
import { UNIT_SYSTEMS, CONVERSION_MATRIX } from './config';

/**
 * Main unit conversion service
 */
export class UnitConverter {
  /**
   * Convert a value between unit systems for a specific category
   */
  convert(
    value: number,
    fromSystem: UnitSystemId,
    toSystem: UnitSystemId,
    category: UnitCategory
  ): number {
    if (fromSystem === toSystem) {
      return value;
    }

    const fromUnitId = this.getUnitIdForCategory(fromSystem, category);
    const toUnitId = this.getUnitIdForCategory(toSystem, category);

    if (!fromUnitId || !toUnitId) {
      throw new Error(`Category '${category}' not found in one of the unit systems`);
    }

    const factor = CONVERSION_MATRIX[fromUnitId]?.[toUnitId];
    if (factor === undefined) {
      throw new Error(`No conversion available from ${fromUnitId} to ${toUnitId}`);
    }

    return value * factor;
  }

  /**
   * Convert multiple values in batch
   */
  convertBatch(
    values: Record<string, { value: number; category: UnitCategory }>,
    fromSystem: UnitSystemId,
    toSystem: UnitSystemId
  ): Record<string, number> {
    const results: Record<string, number> = {};

    for (const [key, { value, category }] of Object.entries(values)) {
      results[key] = this.convert(value, fromSystem, toSystem, category);
    }

    return results;
  }

  /**
   * Get unit symbol for a category in a specific unit system
   */
  getUnitSymbol(unitSystem: UnitSystemId, category: UnitCategory, locale: Locale = 'en'): string {
    const unit = this.getUnitDefinition(unitSystem, category);
    return unit?.symbol ?? '';
  }

  /**
   * Get unit name for a category in a specific unit system
   */
  getUnitName(
    unitSystem: UnitSystemId,
    category: UnitCategory,
    locale: Locale = 'en',
    plural: boolean = false
  ): string {
    const unit = this.getUnitDefinition(unitSystem, category);
    if (!unit) return '';

    const names = plural ? unit.pluralNames : unit.names;
    return names[locale] ?? names.en ?? unit.id;
  }

  /**
   * Get category name
   */
  getCategoryName(category: UnitCategory, locale: Locale = 'en'): string {
    // Get from SI system (they're the same across systems)
    const categoryDef = UNIT_SYSTEMS.SI.categories[category];
    return categoryDef?.names[locale] ?? categoryDef?.names.en ?? category;
  }

  /**
   * Get all available unit systems
   */
  getAvailableUnitSystems(locale: Locale = 'en'): UnitSystemInfo[] {
    return Object.values(UNIT_SYSTEMS).map((system) =>
      this.getUnitSystemInfo(system.id, locale)
    );
  }

  /**
   * Get unit system information
   */
  getUnitSystemInfo(unitSystemId: UnitSystemId, locale: Locale = 'en'): UnitSystemInfo {
    const system = UNIT_SYSTEMS[unitSystemId];
    if (!system) {
      throw new Error(`Unit system '${unitSystemId}' not found`);
    }

    return {
      id: system.id,
      name: system.names[locale] ?? system.names.en ?? system.id,
      description: system.descriptions[locale] ?? system.descriptions.en ?? '',
      isDefault: system.isDefault,
      categories: Object.keys(system.categories) as UnitCategory[],
    };
  }

  /**
   * Format a value with localized unit
   */
  formatValue(
    value: number,
    unitSystem: UnitSystemId,
    category: UnitCategory,
    locale: Locale = 'en',
    decimals: number = 2
  ): string {
    const formattedValue = this.formatNumber(value, locale, decimals);
    const symbol = this.getUnitSymbol(unitSystem, category, locale);

    return `${formattedValue} ${symbol}`;
  }

  /**
   * Format number according to locale
   */
  private formatNumber(value: number, locale: Locale, decimals: number): string {
    return value.toLocaleString(locale === 'es' ? 'es-ES' : 'en-US', {
      minimumFractionDigits: decimals,
      maximumFractionDigits: decimals,
    });
  }

  /**
   * Get unit ID for a category in a unit system
   */
  private getUnitIdForCategory(unitSystem: UnitSystemId, category: UnitCategory): string | null {
    const system = UNIT_SYSTEMS[unitSystem];
    if (!system) return null;

    const categoryDef = system.categories[category];
    if (!categoryDef) return null;

    return categoryDef.units[0]?.id ?? null;
  }

  /**
   * Get unit definition
   */
  private getUnitDefinition(unitSystem: UnitSystemId, category: UnitCategory) {
    const system = UNIT_SYSTEMS[unitSystem];
    if (!system) return null;

    const categoryDef = system.categories[category];
    if (!categoryDef) return null;

    return categoryDef.units[0] ?? null;
  }
}

// Export a singleton instance
export const unitConverter = new UnitConverter();

