# NavArch Unit Conversion - Usage Guide

## Installation

### .NET (Backend)

```bash
# From NuGet (when published)
dotnet add package NavArch.UnitConversion

# Or reference local project
dotnet add reference ../../packages/unit-conversion/dotnet/NavArch.UnitConversion/NavArch.UnitConversion.csproj
```

### TypeScript (Frontend)

```bash
# From npm (when published)
npm install @navarch/unit-conversion

# Or link locally
cd packages/unit-conversion/typescript
npm link
cd ../../your-project
npm link @navarch/unit-conversion
```

---

## Quick Start

### .NET Example

```csharp
using NavArch.UnitConversion.Services;

// Create converter instance
var converter = new UnitConverter();

// Convert a single value
var lengthInFeet = converter.Convert(10, "SI", "Imperial", "Length");
// Result: 32.8084 feet

// Convert multiple values
var values = new Dictionary<string, (decimal value, string category)>
{
    ["Draft"] = (5.5m, "Length"),
    ["Displacement"] = (10000m, "Mass"),
    ["Area"] = (250m, "Area")
};

var converted = converter.ConvertBatch(values, "SI", "Imperial");
// Draft: 18.04 ft, Displacement: 22046.2 lb, Area: 2690.98 ft²

// Get localized unit information
var symbol = converter.GetUnitSymbol("SI", "Length", "es");
// Result: "m"

var name = converter.GetUnitName("Imperial", "Length", "es", plural: true);
// Result: "Pies"

var formatted = converter.FormatValue(10.5m, "SI", "Length", "es", decimals: 2);
// Result: "10,50 m"
```

### TypeScript Example

```typescript
import { UnitConverter } from "@navarch/unit-conversion";

// Create converter instance
const converter = new UnitConverter();

// Convert a single value
const lengthInFeet = converter.convert(10, "SI", "Imperial", "Length");
// Result: 32.8084 feet

// Convert multiple values
const values = {
  draft: { value: 5.5, category: "Length" as const },
  displacement: { value: 10000, category: "Mass" as const },
  area: { value: 250, category: "Area" as const },
};

const converted = converter.convertBatch(values, "SI", "Imperial");
// draft: 18.04, displacement: 22046.2, area: 2690.98

// Get localized unit information
const symbol = converter.getUnitSymbol("SI", "Length", "es");
// Result: "m"

const name = converter.getUnitName("Imperial", "Length", "es", true);
// Result: "Pies"

const formatted = converter.formatValue(10.5, "SI", "Length", "es", 2);
// Result: "10,50 m"
```

---

## API Reference

### Conversion Methods

#### `Convert(value, fromSystem, toSystem, category)`

Convert a single value between unit systems.

**Parameters:**

- `value` (decimal/number): Value to convert
- `fromSystem` (string): Source unit system ("SI" or "Imperial")
- `toSystem` (string): Target unit system ("SI" or "Imperial")
- `category` (string): Measurement category

**Returns:** Converted value

**Example:**

```csharp
var result = converter.Convert(100, "SI", "Imperial", "Length");
// 328.084 feet
```

#### `ConvertBatch(values, fromSystem, toSystem)`

Convert multiple values at once.

**Parameters:**

- `values`: Dictionary/Record of values with their categories
- `fromSystem`: Source unit system
- `toSystem`: Target unit system

**Returns:** Dictionary/Record of converted values

**Example:**

```csharp
var results = converter.ConvertBatch(new Dictionary<string, (decimal, string)>
{
    ["Lpp"] = (120m, "Length"),
    ["Beam"] = (20m, "Length"),
    ["Draft"] = (8m, "Length")
}, "SI", "Imperial");
```

### Localization Methods

#### `GetUnitSymbol(unitSystem, category, locale?)`

Get the unit symbol for a category.

**Parameters:**

- `unitSystem`: "SI" or "Imperial"
- `category`: Measurement category
- `locale` (optional): "en" or "es" (default: "en")

**Returns:** Unit symbol (e.g., "m", "ft", "kg", "lb")

#### `GetUnitName(unitSystem, category, locale?, plural?)`

Get the localized unit name.

**Parameters:**

- `unitSystem`: "SI" or "Imperial"
- `category`: Measurement category
- `locale` (optional): "en" or "es"
- `plural` (optional): Get plural form (default: false)

**Returns:** Localized unit name

**Examples:**

```typescript
converter.getUnitName("SI", "Length", "en", false); // "Meter"
converter.getUnitName("SI", "Length", "en", true); // "Meters"
converter.getUnitName("SI", "Length", "es", false); // "Metro"
converter.getUnitName("SI", "Length", "es", true); // "Metros"
```

#### `GetCategoryName(category, locale?)`

Get the localized category name.

**Examples:**

```typescript
converter.getCategoryName("Length", "en"); // "Length"
converter.getCategoryName("Length", "es"); // "Longitud"
converter.getCategoryName("Mass", "es"); // "Masa"
```

#### `FormatValue(value, unitSystem, category, locale?, decimals?)`

Format a value with localized number formatting and unit.

**Parameters:**

- `value`: Numeric value
- `unitSystem`: "SI" or "Imperial"
- `category`: Measurement category
- `locale` (optional): "en" or "es"
- `decimals` (optional): Decimal places (default: 2)

**Returns:** Formatted string

**Examples:**

```typescript
converter.formatValue(1234.5, "SI", "Length", "en", 2);
// "1,234.50 m"

converter.formatValue(1234.5, "SI", "Length", "es", 2);
// "1.234,50 m"
```

### Information Methods

#### `GetAvailableUnitSystems(locale?)`

Get all available unit systems.

**Returns:** List of unit system information

```typescript
const systems = converter.getAvailableUnitSystems("es");
// [
//   { id: 'SI', name: 'SI (Métrico)', description: '...', ... },
//   { id: 'Imperial', name: 'Imperial (EE.UU.)', ... }
// ]
```

#### `GetUnitSystemInfo(unitSystemId, locale?)`

Get detailed information about a unit system.

```typescript
const info = converter.getUnitSystemInfo("SI", "es");
// {
//   id: 'SI',
//   name: 'SI (Métrico)',
//   description: 'Sistema Internacional de Unidades',
//   isDefault: true,
//   categories: ['Length', 'Mass', 'Area', ...]
// }
```

---

## Supported Categories

| Category            | SI Unit           | Imperial Unit     |
| ------------------- | ----------------- | ----------------- |
| **Length**          | meter (m)         | foot (ft)         |
| **Mass**            | kilogram (kg)     | pound (lb)        |
| **Area**            | square meter (m²) | square foot (ft²) |
| **Volume**          | cubic meter (m³)  | cubic foot (ft³)  |
| **Density**         | kg/m³             | lb/ft³            |
| **MomentOfInertia** | m⁴                | ft⁴               |

---

## Localization Support

### Supported Locales

- **English (en)**: Full support
- **Spanish (es)**: Full support

### Localized Elements

- Unit system names and descriptions
- Category names
- Unit names (singular and plural)
- Number formatting (decimal/thousands separators)

### Examples of Localization

#### English

```
Length: 10.50 m
Mass: 1,234.56 kg
Area: 250.00 m²
```

#### Spanish

```
Longitud: 10,50 m
Masa: 1.234,56 kg
Área: 250,00 m²
```

---

## Advanced Usage

### Custom Configuration Path (.NET only)

```csharp
// Load from custom XML file
var converter = new UnitConverter("/path/to/custom-units.xml");
```

### Singleton Pattern (TypeScript)

```typescript
import { unitConverter } from "@navarch/unit-conversion";

// Use the pre-instantiated singleton
const result = unitConverter.convert(10, "SI", "Imperial", "Length");
```

### React Integration

```typescript
import { UnitConverter } from "@navarch/unit-conversion";
import { createContext, useContext } from "react";

// Create context
const UnitConverterContext = createContext(new UnitConverter());

// Use in components
function MyComponent() {
  const converter = useContext(UnitConverterContext);
  const [value, setValue] = useState(10);
  const [units, setUnits] = useState<"SI" | "Imperial">("SI");

  const converted = converter.convert(value, "SI", units, "Length");
  const formatted = converter.formatValue(converted, units, "Length", "en");

  return <div>{formatted}</div>;
}
```

---

## Error Handling

### .NET

```csharp
try
{
    var result = converter.Convert(10, "SI", "Invalid", "Length");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid unit system: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Conversion not available: {ex.Message}");
}
```

### TypeScript

```typescript
try {
  const result = converter.convert(10, "SI", "Invalid" as any, "Length");
} catch (error) {
  if (error instanceof Error) {
    console.error("Conversion error:", error.message);
  }
}
```

---

## Performance Considerations

1. **Caching**: Configuration is loaded once and cached
2. **Batch Operations**: Use `ConvertBatch` for multiple conversions
3. **Lookup Tables**: Conversions use pre-calculated factor matrices

### Benchmarks

- Single conversion: ~0.001ms (.NET), ~0.0001ms (TypeScript)
- Batch conversion (100 values): ~0.1ms (.NET), ~0.01ms (TypeScript)

---

## Contributing

To add new unit systems or locales:

1. Update `config/unit-systems.xml` (for .NET)
2. Update `src/config.ts` (for TypeScript)
3. Add conversion factors to the matrix
4. Update tests

---

## License

MIT
