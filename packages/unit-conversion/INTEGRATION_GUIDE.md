# Integration Guide: Using NavArch Unit Conversion in Your Project

This guide shows how to integrate the standalone unit conversion service into the navarch-studio project.

## Overview

The unit conversion service is now a **standalone, reusable package** that can be:

- Used in any NavArch project
- Shared across backend and frontend
- Extended without modifying core application code
- Published as NuGet/npm packages

---

## Integration Steps

### Backend Integration (.NET)

#### 1. Add Project Reference

**Option A: Local Reference (Development)**

```xml
<!-- In backend/Shared/Shared.csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\packages\unit-conversion\dotnet\NavArch.UnitConversion\NavArch.UnitConversion.csproj" />
</ItemGroup>
```

**Option B: NuGet Package (Production)**

```bash
dotnet add package NavArch.UnitConversion
```

#### 2. Replace Existing Utility

**Remove:**

- `backend/Shared/Utilities/UnitConversion.cs`

**Update Services to Use New Package:**

```csharp
// OLD:
using Shared.Utilities;
var result = UnitConversion.ConvertLength(value, fromUnit, toUnit);

// NEW:
using NavArch.UnitConversion.Services;
private readonly IUnitConverter _unitConverter = new UnitConverter();
var result = _unitConverter.Convert(value, fromUnit, toUnit, "Length");
```

#### 3. Dependency Injection (Recommended)

```csharp
// In Program.cs
using NavArch.UnitConversion.Services;

builder.Services.AddSingleton<IUnitConverter, UnitConverter>();

// In your services
public class HydrostaticsService
{
    private readonly IUnitConverter _unitConverter;

    public HydrostaticsService(IUnitConverter unitConverter)
    {
        _unitConverter = unitConverter;
    }

    public HydroResultDto Convert(HydroResult result, string fromUnits, string toUnits)
    {
        return new HydroResultDto
        {
            Draft = _unitConverter.Convert(result.Draft, fromUnits, toUnits, "Length"),
            DispWeight = _unitConverter.Convert(result.DispWeight, fromUnits, toUnits, "Mass"),
            Awp = _unitConverter.Convert(result.Awp, fromUnits, toUnits, "Area"),
            // ... more fields
        };
    }
}
```

---

### Frontend Integration (TypeScript)

#### 1. Install Package

**Option A: Local Link (Development)**

```bash
cd packages/unit-conversion/typescript
npm install
npm run build
npm link

cd frontend
npm link @navarch/unit-conversion
```

**Option B: npm Package (Production)**

```bash
npm install @navarch/unit-conversion
```

#### 2. Replace Existing Utilities

**Remove:**

- `frontend/src/utils/unitConversion.ts`

**Update Imports:**

```typescript
// OLD:
import { convertLength, getLengthUnit } from "../utils/unitConversion";

// NEW:
import { unitConverter } from "@navarch/unit-conversion";

// Usage
const converted = unitConverter.convert(value, "SI", "Imperial", "Length");
const symbol = unitConverter.getUnitSymbol("SI", "Length", "en");
```

#### 3. Update SettingsStore

```typescript
// frontend/src/stores/SettingsStore.ts
import { UnitSystemId } from "@navarch/unit-conversion";

export interface UserSettings {
  preferredUnits: UnitSystemId; // 'SI' | 'Imperial'
  locale: "en" | "es";
}
```

#### 4. Update Components

**Example: ComputationsTab**

```typescript
import { unitConverter, UnitSystemId } from "@navarch/unit-conversion";

export const ComputationsTab = observer(({ vesselId }: Props) => {
  const vesselUnits = vessel?.unitsSystem as UnitSystemId;
  const displayUnits = settingsStore.preferredUnits;

  // Convert values
  const convertedDraft = unitConverter.convert(
    result.draft,
    vesselUnits,
    displayUnits,
    "Length"
  );

  // Get localized labels
  const lengthUnit = unitConverter.getUnitSymbol(
    displayUnits,
    "Length",
    locale
  );

  // Format for display
  const formatted = unitConverter.formatValue(
    convertedDraft,
    displayUnits,
    "Length",
    locale,
    2
  );

  return (
    <div>
      <span>{formatted}</span>
    </div>
  );
});
```

---

## Migration Checklist

### Backend Tasks

- [ ] Add project reference to NavArch.UnitConversion
- [ ] Remove `backend/Shared/Utilities/UnitConversion.cs`
- [ ] Update DataService to use IUnitConverter
- [ ] Update IdentityService if needed
- [ ] Register IUnitConverter in DI container
- [ ] Update tests to use new service
- [ ] Verify all conversions work correctly

### Frontend Tasks

- [ ] Link or install @navarch/unit-conversion package
- [ ] Remove `frontend/src/utils/unitConversion.ts`
- [ ] Update SettingsStore to use new types
- [ ] Update ComputationsTab
- [ ] Update OverviewTab
- [ ] Update LoadcasesTab
- [ ] Update CreateVesselDialog
- [ ] Update any other components using conversions
- [ ] Run type-check and build
- [ ] Test in browser

### Testing Tasks

- [ ] Test SI to Imperial conversion
- [ ] Test Imperial to SI conversion
- [ ] Test English localization
- [ ] Test Spanish localization
- [ ] Test batch conversions
- [ ] Test formatted output
- [ ] Test error handling

---

## Configuration

### XML Configuration (Backend)

The XML file is embedded in the package by default. To use a custom configuration:

```csharp
// In appsettings.json
{
  "UnitConversion": {
    "ConfigPath": "config/custom-unit-systems.xml"
  }
}

// In Program.cs
var configPath = builder.Configuration["UnitConversion:ConfigPath"];
builder.Services.AddSingleton<IUnitConverter>(sp => new UnitConverter(configPath));
```

### TypeScript Configuration

The configuration is hardcoded in `src/config.ts`. To extend:

```typescript
// Create extended configuration
import { UNIT_SYSTEMS } from "@navarch/unit-conversion";

// Add custom units
UNIT_SYSTEMS["Custom"] = {
  id: "Custom",
  // ... your custom configuration
};
```

---

## API Changes

### Old API → New API Mapping

#### Backend

| Old                                             | New                                            |
| ----------------------------------------------- | ---------------------------------------------- |
| `UnitConversion.ConvertLength(value, from, to)` | `converter.Convert(value, from, to, "Length")` |
| `UnitConversion.ConvertMass(value, from, to)`   | `converter.Convert(value, from, to, "Mass")`   |
| `UnitConversion.GetLengthUnit(system)`          | `converter.GetUnitSymbol(system, "Length")`    |

#### Frontend

| Old                              | New                                                |
| -------------------------------- | -------------------------------------------------- |
| `convertLength(value, from, to)` | `unitConverter.convert(value, from, to, 'Length')` |
| `getLengthUnit(system)`          | `unitConverter.getUnitSymbol(system, 'Length')`    |
| `formatWithUnit(...)`            | `unitConverter.formatValue(...)`                   |

---

## Benefits of Migration

### ✅ Reusability

- Use in multiple NavArch projects
- Share between services
- Consistent conversion logic

### ✅ Maintainability

- Single source of truth
- Easier to add new units
- Centralized bug fixes

### ✅ Testability

- Isolated unit tests
- Mock-friendly interface
- Better test coverage

### ✅ Extensibility

- XML-based configuration
- Easy to add languages
- Support for custom units

### ✅ Documentation

- Comprehensive API docs
- Usage examples
- Type definitions

---

## Troubleshooting

### Backend Issues

**Problem:** Package not found

```bash
# Solution: Build the package first
cd packages/unit-conversion/dotnet/NavArch.UnitConversion
dotnet build
```

**Problem:** XML config not found

```csharp
// Solution: Ensure XML is copied to output
// Already configured in .csproj
```

### Frontend Issues

**Problem:** Module not found

```bash
# Solution: Link the package
cd packages/unit-conversion/typescript
npm link
cd ../../frontend
npm link @navarch/unit-conversion
```

**Problem:** Type errors

```bash
# Solution: Rebuild TypeScript
cd packages/unit-conversion/typescript
npm run build
```

---

## Future Enhancements

Once migrated, you can easily:

1. **Add More Unit Systems**

   - Metric tons
   - Long tons
   - Nautical miles
   - Knots

2. **Add More Languages**

   - French, German, Portuguese
   - Just update XML/config

3. **Custom Units Per Project**

   - Each project can extend base configuration
   - No code changes needed

4. **API Endpoints for Configuration**
   - Allow users to download unit systems
   - Support dynamic loading

---

## Support

For issues or questions:

1. Check `USAGE.md` for API documentation
2. Review `README.md` for overview
3. Check test files for examples
4. Create an issue in the repository

---

## Timeline

**Estimated Migration Time:** 2-4 hours

1. **Backend (1-2 hours)**

   - Add references: 15 min
   - Update services: 30-60 min
   - Testing: 30-45 min

2. **Frontend (1-2 hours)**
   - Install package: 10 min
   - Update components: 45-75 min
   - Testing: 30-45 min

---

## Success Criteria

Migration is complete when:

- ✅ All builds pass (backend & frontend)
- ✅ All type checks pass
- ✅ All tests pass
- ✅ Application runs without errors
- ✅ Unit conversions work in UI
- ✅ Localization works (English & Spanish)
- ✅ Old utility files are removed
- ✅ Documentation is updated
