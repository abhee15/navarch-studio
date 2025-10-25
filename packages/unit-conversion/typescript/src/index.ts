/**
 * NavArch Unit Conversion Library
 * 
 * Standalone bilingual (English/Spanish) unit conversion library
 * for naval architecture applications.
 */

export { UnitConverter, unitConverter } from './UnitConverter';
export type {
  UnitSystemId,
  UnitCategory,
  Locale,
  UnitSystemInfo,
  UnitInfo,
  UnitSystemDefinition,
  CategoryDefinition,
  UnitDefinition,
  ConversionFactor,
} from './models';
export { UNIT_SYSTEMS, CONVERSION_MATRIX } from './config';

