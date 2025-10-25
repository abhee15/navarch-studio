# NavArch Unit Conversion Service

A standalone, bilingual (English/Spanish) unit conversion library for naval architecture applications.

## Features

- ✅ Conversion between SI and Imperial units
- ✅ Support for Length, Mass, Area, Volume, Density, Moment of Inertia
- ✅ Bilingual support (English/Spanish)
- ✅ XML-based configuration
- ✅ .NET 8 and TypeScript implementations
- ✅ Extensible architecture

## Packages

- **Backend**: `NavArch.UnitConversion` - .NET 8 NuGet package
- **Frontend**: `@navarch/unit-conversion` - TypeScript npm package

## Structure

```
packages/unit-conversion/
├── dotnet/                      # .NET implementation
│   ├── NavArch.UnitConversion/
│   └── NavArch.UnitConversion.Tests/
├── typescript/                  # TypeScript implementation
│   ├── src/
│   └── tests/
├── config/                      # Shared configuration
│   ├── unit-systems.xml
│   └── unit-systems.schema.xsd
└── README.md
```

## Usage

### .NET

```csharp
var converter = new UnitConverter();
var result = converter.Convert(10, "SI", "Imperial", "Length");
// result = 32.8084 (feet)
```

### TypeScript

```typescript
import { UnitConverter } from "@navarch/unit-conversion";

const converter = new UnitConverter();
const result = converter.convert(10, "SI", "Imperial", "Length");
// result = 32.8084 (feet)
```

## License

MIT
