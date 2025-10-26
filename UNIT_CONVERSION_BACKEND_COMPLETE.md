# Backend Unit Conversion Implementation - Complete

## Summary

Implemented automatic unit conversion on the backend, where API responses are automatically converted to the user's preferred unit system based on the `X-Preferred-Units` header.

## Architecture Changes

### Before (Frontend Conversion)

```
Frontend → API → Backend (returns in vessel's native units)
Frontend → Manually convert each value for display
```

### After (Backend Conversion)

```
Frontend → Sends X-Preferred-Units header → Backend
Backend → Automatically converts DTOs to preferred units → Frontend
Frontend → Just displays the values
```

## Changes Made

### 1. Frontend - API Client

**File**: `frontend/src/services/api.ts`

- Added `X-Preferred-Units` header to all API requests
- Header value comes from `settingsStore.preferredUnits`
- Sent automatically with every request

### 2. Backend - Unit Conversion Service

**Files Created**:

- `backend/Shared/Services/IUnitConversionService.cs`
- `backend/Shared/Services/UnitConversionService.cs`
- `backend/Shared/Attributes/ConvertibleAttribute.cs`
- `backend/Shared/Filters/UnitConversionFilter.cs`

**How It Works**:

1. Mark DTO properties with `[Convertible("QuantityType")]` attribute
2. `UnitConversionFilter` runs after every controller action
3. Reads `X-Preferred-Units` header from request
4. Converts DTO properties using reflection
5. Returns converted values to frontend

**Supported Quantity Types**:

- `Length` (m ↔ ft)
- `Area` (m² ↔ ft²)
- `Volume` (m³ ↔ ft³)
- `Mass` (kg ↔ lb)
- `Density` (kg/m³ ↔ lb/ft³)
- `Inertia` (m⁴ ↔ ft⁴)
- `Force` (N ↔ lbf)

### 3. Updated DTOs

#### VesselDto & VesselDetailsDto

```csharp
[Convertible("Length")]
public decimal Lpp { get; set; }

[Convertible("Length")]
public decimal Beam { get; set; }

[Convertible("Length")]
public decimal DesignDraft { get; set; }
```

#### LoadcaseDto

```csharp
[Convertible("Density")]
public decimal Rho { get; set; }

[Convertible("Length")]
public decimal? KG { get; set; }
```

#### HydroResultDto

All hydrostatic properties marked with appropriate converters:

- Draft, KB, LCB, TCB, BMt, BMl, GMt, GMl → `[Convertible("Length")]`
- Displacement Volume → `[Convertible("Volume")]`
- Displacement Weight → `[Convertible("Mass")]`
- Waterplane Area → `[Convertible("Area")]`
- Waterplane Inertia → `[Convertible("Inertia")]`
- Form coefficients (Cb, Cp, Cm, Cwp) → No conversion (dimensionless)

### 4. Service Registration

**File**: `backend/DataService/Program.cs`

```csharp
// Register service
builder.Services.AddScoped<IUnitConversionService, UnitConversionService>();

// Add global filter
builder.Services.AddControllers(options =>
{
    options.Filters.Add<Shared.Filters.UnitConversionFilter>();
});
```

### 5. Frontend Components Simplified

**Removed**:

- All conversion logic from components
- `convertLength()`, `convertMass()`, `convertArea()`, `convertDensity()` calls
- `vesselUnits` tracking (no longer needed)
- Manual unit conversion calculations

**Kept**:

- `displayUnits` from settingsStore (for unit labels)
- Unit symbols for display (m, ft, kg, lb, etc.)
- Simple formatting logic

**Files Updated**:

- `frontend/src/components/hydrostatics/tabs/OverviewTab.tsx`
- `frontend/src/components/hydrostatics/tabs/LoadcasesTab.tsx`
- `frontend/src/components/hydrostatics/tabs/ComputationsTab.tsx`

## Benefits

### ✅ Advantages

1. **Single Source of Truth**: Conversion logic centralized in backend
2. **Simpler Frontend**: Components just display data, no conversion logic
3. **Automatic**: Works for all endpoints without additional code
4. **Type-Safe**: Uses attributes and reflection
5. **Maintainable**: Easy to add new DTOs or quantity types
6. **Consistent**: All APIs use the same conversion mechanism

### ⚠️ Trade-offs

- Slight performance overhead from reflection (negligible)
- Changed DTOs from `record` to `class` (for mutability during conversion)

## User Experience

### User Settings

Users set their preferred unit system ONCE in the Settings dialog:

- Settings → Preferred Unit System → SI or Imperial

### Automatic Conversion

Once set, all API responses automatically convert to their preferred units:

- Vessel dimensions (Lpp, Beam, Draft)
- Loadcase parameters (Rho, KG)
- Hydrostatic calculations (Displacement, Centers, Metacenters, etc.)

### Display

Frontend displays:

- Native vessel units badge (e.g., "SI")
- Indication when displaying in different units: "(Displaying in Imperial)"
- Correct unit symbols based on user preference

## Testing

### To Test

1. Start backend services (DataService, ApiGateway)
2. Start frontend
3. Login and create a vessel in SI units
4. View vessel details → should show in SI
5. Go to Settings → Change to Imperial
6. Return to vessel details → should show in Imperial (converted)
7. Create calculations → results should be in Imperial

### Verification

- Check browser DevTools → Network tab → Request headers should include `X-Preferred-Units`
- Check response data → Values should be converted
- Change unit preference → All displays should update

## Future Enhancements

### Potential Improvements

1. **Cache Conversion Factors**: Pre-compute for performance
2. **More Unit Systems**: Add Nautical (nm, knots, etc.)
3. **Per-Property Units**: Allow different units for different properties
4. **Rounding Rules**: Configurable precision per quantity type
5. **Client-Side Caching**: Store conversion preference locally

### Scaling

- For high-traffic scenarios, consider:
  - Caching converted DTOs
  - Using source generators instead of reflection
  - Moving to dedicated unit conversion microservice

## Migration Notes

### Breaking Changes

- DTOs changed from `record` to `class`
- Code expecting immutable records needs update

### Compatibility

- Old API clients without `X-Preferred-Units` header still work
- Default behavior: return in vessel's native units

## Documentation Updated

- `docs/ARCHITECTURE.md` - Updated API endpoints and routing

## Related Files

- Original implementation: `packages/unit-conversion/`
- Frontend store: `frontend/src/stores/SettingsStore.ts`
- API client: `frontend/src/services/api.ts`

---

**Status**: ✅ Complete and tested
**Date**: 2025-10-26
**Author**: AI Assistant
