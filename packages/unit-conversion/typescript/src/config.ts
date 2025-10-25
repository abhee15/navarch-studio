import type { UnitSystemDefinition, UnitCategory, Locale, ConversionFactor } from './models';

/**
 * Hardcoded configuration (equivalent to XML in .NET version)
 * In production, this could be loaded from JSON or fetched from an API
 */
export const UNIT_SYSTEMS: Record<string, UnitSystemDefinition> = {
  SI: {
    id: 'SI',
    isDefault: true,
    names: {
      en: 'SI (Metric)',
      es: 'SI (Métrico)',
    },
    descriptions: {
      en: 'International System of Units',
      es: 'Sistema Internacional de Unidades',
    },
    categories: {
      Length: {
        id: 'Length',
        names: { en: 'Length', es: 'Longitud' },
        units: [
          {
            id: 'meter',
            symbol: 'm',
            isBase: true,
            names: { en: 'Meter', es: 'Metro' },
            pluralNames: { en: 'Meters', es: 'Metros' },
          },
        ],
      },
      Mass: {
        id: 'Mass',
        names: { en: 'Mass', es: 'Masa' },
        units: [
          {
            id: 'kilogram',
            symbol: 'kg',
            isBase: true,
            names: { en: 'Kilogram', es: 'Kilogramo' },
            pluralNames: { en: 'Kilograms', es: 'Kilogramos' },
          },
        ],
      },
      Area: {
        id: 'Area',
        names: { en: 'Area', es: 'Área' },
        units: [
          {
            id: 'square-meter',
            symbol: 'm²',
            isBase: true,
            names: { en: 'Square Meter', es: 'Metro Cuadrado' },
            pluralNames: { en: 'Square Meters', es: 'Metros Cuadrados' },
          },
        ],
      },
      Volume: {
        id: 'Volume',
        names: { en: 'Volume', es: 'Volumen' },
        units: [
          {
            id: 'cubic-meter',
            symbol: 'm³',
            isBase: true,
            names: { en: 'Cubic Meter', es: 'Metro Cúbico' },
            pluralNames: { en: 'Cubic Meters', es: 'Metros Cúbicos' },
          },
        ],
      },
      Density: {
        id: 'Density',
        names: { en: 'Density', es: 'Densidad' },
        units: [
          {
            id: 'kg-per-cubic-meter',
            symbol: 'kg/m³',
            isBase: true,
            names: { en: 'Kilogram per Cubic Meter', es: 'Kilogramo por Metro Cúbico' },
            pluralNames: { en: 'Kilograms per Cubic Meter', es: 'Kilogramos por Metro Cúbico' },
          },
        ],
      },
      MomentOfInertia: {
        id: 'MomentOfInertia',
        names: { en: 'Moment of Inertia', es: 'Momento de Inercia' },
        units: [
          {
            id: 'meter-to-fourth',
            symbol: 'm⁴',
            isBase: true,
            names: { en: 'Meter to the Fourth Power', es: 'Metro a la Cuarta Potencia' },
            pluralNames: { en: 'Meters to the Fourth Power', es: 'Metros a la Cuarta Potencia' },
          },
        ],
      },
    },
  },
  Imperial: {
    id: 'Imperial',
    isDefault: false,
    names: {
      en: 'Imperial (US)',
      es: 'Imperial (EE.UU.)',
    },
    descriptions: {
      en: 'United States customary units',
      es: 'Unidades consuetudinarias de Estados Unidos',
    },
    categories: {
      Length: {
        id: 'Length',
        names: { en: 'Length', es: 'Longitud' },
        units: [
          {
            id: 'foot',
            symbol: 'ft',
            isBase: false,
            names: { en: 'Foot', es: 'Pie' },
            pluralNames: { en: 'Feet', es: 'Pies' },
            conversionFactor: 0.3048,
            baseUnit: 'meter',
          },
        ],
      },
      Mass: {
        id: 'Mass',
        names: { en: 'Mass', es: 'Masa' },
        units: [
          {
            id: 'pound',
            symbol: 'lb',
            isBase: false,
            names: { en: 'Pound', es: 'Libra' },
            pluralNames: { en: 'Pounds', es: 'Libras' },
            conversionFactor: 0.453592,
            baseUnit: 'kilogram',
          },
        ],
      },
      Area: {
        id: 'Area',
        names: { en: 'Area', es: 'Área' },
        units: [
          {
            id: 'square-foot',
            symbol: 'ft²',
            isBase: false,
            names: { en: 'Square Foot', es: 'Pie Cuadrado' },
            pluralNames: { en: 'Square Feet', es: 'Pies Cuadrados' },
            conversionFactor: 0.092903,
            baseUnit: 'square-meter',
          },
        ],
      },
      Volume: {
        id: 'Volume',
        names: { en: 'Volume', es: 'Volumen' },
        units: [
          {
            id: 'cubic-foot',
            symbol: 'ft³',
            isBase: false,
            names: { en: 'Cubic Foot', es: 'Pie Cúbico' },
            pluralNames: { en: 'Cubic Feet', es: 'Pies Cúbicos' },
            conversionFactor: 0.0283168,
            baseUnit: 'cubic-meter',
          },
        ],
      },
      Density: {
        id: 'Density',
        names: { en: 'Density', es: 'Densidad' },
        units: [
          {
            id: 'lb-per-cubic-foot',
            symbol: 'lb/ft³',
            isBase: false,
            names: { en: 'Pound per Cubic Foot', es: 'Libra por Pie Cúbico' },
            pluralNames: { en: 'Pounds per Cubic Foot', es: 'Libras por Pie Cúbico' },
            conversionFactor: 16.0185,
            baseUnit: 'kg-per-cubic-meter',
          },
        ],
      },
      MomentOfInertia: {
        id: 'MomentOfInertia',
        names: { en: 'Moment of Inertia', es: 'Momento de Inercia' },
        units: [
          {
            id: 'foot-to-fourth',
            symbol: 'ft⁴',
            isBase: false,
            names: { en: 'Foot to the Fourth Power', es: 'Pie a la Cuarta Potencia' },
            pluralNames: { en: 'Feet to the Fourth Power', es: 'Pies a la Cuarta Potencia' },
            conversionFactor: 0.00863097,
            baseUnit: 'meter-to-fourth',
          },
        ],
      },
    },
  },
};

/**
 * Conversion matrix for quick lookups
 */
export const CONVERSION_MATRIX: Record<string, Record<string, number>> = {
  meter: { foot: 3.28084 },
  foot: { meter: 0.3048 },
  kilogram: { pound: 2.20462 },
  pound: { kilogram: 0.453592 },
  'square-meter': { 'square-foot': 10.7639 },
  'square-foot': { 'square-meter': 0.092903 },
  'cubic-meter': { 'cubic-foot': 35.3147 },
  'cubic-foot': { 'cubic-meter': 0.0283168 },
  'kg-per-cubic-meter': { 'lb-per-cubic-foot': 0.062428 },
  'lb-per-cubic-foot': { 'kg-per-cubic-meter': 16.0185 },
  'meter-to-fourth': { 'foot-to-fourth': 115.862 },
  'foot-to-fourth': { 'meter-to-fourth': 0.00863097 },
};

