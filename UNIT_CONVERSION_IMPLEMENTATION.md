# Unit Conversion Feature - Implementation Summary

## Overview

Successfully implemented a comprehensive unit conversion system that allows users to set their preferred unit system (SI or Imperial) and view vessel data in their chosen units, regardless of the vessel's native unit system.

## Architecture

### Hybrid Approach

- **User Preference**: Each user has a preferred display unit system stored in their profile
- **Vessel Native Units**: Each vessel stores data in its native unit system (SI or Imperial)
- **Automatic Conversion**: Frontend converts measurements on-the-fly for display based on user preferences
- **Data Integrity**: Backend always stores data in vessel's native units (no conversion on backend)

### Benefits

✅ Preserves vessel data integrity  
✅ User-centric viewing experience  
✅ International collaboration friendly  
✅ Backward compatible  
✅ Clear separation of concerns

---

## Implementation Details

### Backend Changes

#### 1. Database Migration

- **File**: `backend/IdentityService/Migrations/20251025000000_AddUserPreferredUnits.cs`
- Added `PreferredUnits` column to `identity.users` table
- Default value: "SI"
- Max length: 50 characters

#### 2. Models Updated

- **User Model** (`backend/Shared/Models/User.cs`): Added `PreferredUnits` property
- **UserDto** (`backend/Shared/DTOs/UserDto.cs`): Added `PreferredUnits` to DTO

#### 3. New DTOs

- **UserSettingsDto**: For retrieving user settings
- **UpdateUserSettingsDto**: For updating user settings

#### 4. Unit Conversion Utility

- **File**: `backend/Shared/Utilities/UnitConversion.cs`
- Conversion functions for:
  - Length (meters ↔ feet)
  - Area (m² ↔ ft²)
  - Volume (m³ ↔ ft³)
  - Mass (kg ↔ lb)
  - Density (kg/m³ ↔ lb/ft³)
  - Moment of Inertia (m⁴ ↔ ft⁴)
- Unit label getters for display

#### 5. API Endpoints

- **GET** `/api/v1/users/settings` - Get current user settings
- **PUT** `/api/v1/users/settings` - Update user settings

#### 6. Services

- **UserService**: Added `GetUserSettingsAsync` and `UpdateUserSettingsAsync` methods
- **IUserService**: Updated interface with new methods

---

### Frontend Changes

#### 1. Unit Conversion Utilities

- **File**: `frontend/src/utils/unitConversion.ts`
- TypeScript implementation of unit conversions
- Conversion functions matching backend
- `formatWithUnit()` helper for displaying values with units

#### 2. Settings Store

- **File**: `frontend/src/stores/SettingsStore.ts`
- MobX store for managing user settings
- Methods:
  - `loadSettings()` - Load user preferences from backend
  - `updateSettings()` - Update user preferences
  - `updatePreferredUnits()` - Convenience method for units
- Computed property: `preferredUnits`

#### 3. User Settings Dialog

- **File**: `frontend/src/components/UserSettingsDialog.tsx`
- Modal dialog for changing preferred units
- Shows preview of conversions
- Clean, intuitive UI with dropdown for SI/Imperial selection

#### 4. Navigation Integration

- **File**: `frontend/src/pages/DashboardPage.tsx`
- Added "Settings" button in header next to "Sign out"
- Loads user settings on mount
- Opens UserSettingsDialog when clicked

#### 5. Component Updates with Unit Conversion

##### ComputationsTab (`frontend/src/components/hydrostatics/tabs/ComputationsTab.tsx`)

- Loads vessel to get native units
- Converts all hydrostatic results for display:
  - Draft, KB, LCB, BMt, GMt (length)
  - Displacement weight (mass)
  - Waterplane area (area)
- Updates table headers with current unit labels
- Uses MobX `observer` for reactive updates

##### OverviewTab (`frontend/src/components/hydrostatics/tabs/OverviewTab.tsx`)

- Converts principal particulars (Lpp, Beam, Draft)
- Shows vessel's native units with badge
- Indicates when displaying in different units than vessel's native
- Clean visual distinction between storage and display units

##### LoadcasesTab (`frontend/src/components/hydrostatics/tabs/LoadcasesTab.tsx`)

- Converts density (ρ) values for display
- Converts KG (center of gravity) values
- Shows appropriate unit labels in table

##### CreateVesselDialog (`frontend/src/components/hydrostatics/CreateVesselDialog.tsx`)

- Added help text explaining that vessel units can be viewed in any system
- Clarifies this is the "native" storage unit system

---

## Conversion Factors

### Length

- 1 meter = 3.28084 feet

### Area

- 1 m² = 10.7639 ft²

### Volume

- 1 m³ = 35.3147 ft³

### Mass

- 1 kg = 2.20462 lb

### Density

- 1 kg/m³ = 0.062428 lb/ft³

### Moment of Inertia

- 1 m⁴ = 115.862 ft⁴

### Dimensionless Coefficients

- Cb, Cp, Cm, Cwp: No conversion (unitless)

---

## User Experience

### Setting Preferences

1. User clicks "Settings" button in dashboard header
2. Dialog opens showing current preference (default: SI)
3. User selects SI or Imperial
4. Preview shows example conversions
5. Click "Save Settings" to persist

### Viewing Vessels

1. All measurements display in user's preferred units
2. Table headers show current units (e.g., "Draft (ft)" or "Draft (m)")
3. OverviewTab shows vessel's native units with clear indicator
4. Conversion happens transparently in real-time

### Creating Vessels

1. User selects native unit system for the vessel
2. Help text clarifies this can be viewed in any units later
3. Data entered in selected units
4. Stored in vessel's native units in database

---

## Testing

### Build Status

✅ **Backend**: Builds successfully with no errors  
✅ **Frontend**: Builds successfully with no errors  
✅ **TypeScript**: Type checking passes  
✅ **ESLint**: Linting passes

### Manual Testing Checklist

- [ ] Create user and set preferred units to SI
- [ ] Create vessel in SI units
- [ ] View vessel data (should show SI units)
- [ ] Change user preference to Imperial
- [ ] View same vessel (should now show Imperial units)
- [ ] Create vessel in Imperial units
- [ ] Switch back to SI preference
- [ ] View Imperial vessel in SI units (should convert)
- [ ] Verify hydrostatic calculations display correct units
- [ ] Verify loadcase density and KG show correct units
- [ ] Verify overview tab shows principal particulars in correct units

---

## Database Migration

To apply the migration, run:

```bash
cd backend/IdentityService
dotnet ef database update
```

Or if running via Docker:

```bash
docker-compose restart identity-service
```

The migration will:

- Add `PreferredUnits` column to existing users table
- Set default value "SI" for all existing users
- No data loss or breaking changes

---

## API Usage Examples

### Get User Settings

```bash
GET /api/v1/users/settings
Authorization: Bearer <token>

Response:
{
  "preferredUnits": "SI"
}
```

### Update User Settings

```bash
PUT /api/v1/users/settings
Authorization: Bearer <token>
Content-Type: application/json

{
  "preferredUnits": "Imperial"
}

Response:
{
  "preferredUnits": "Imperial"
}
```

---

## Future Enhancements (Not Implemented)

### Custom Unit Systems

- Allow users to define custom unit combinations
- Support for additional units (nautical miles, metric tons, etc.)
- Templates for common regional preferences

### Multi-Unit Display

- Show both vessel units and user units side-by-side
- Toggle between unit systems in UI without changing preference
- Comparison mode for international projects

### Enhanced Export

- Export data in user's preferred units
- Include unit conversion notes in exports
- CSV export with proper unit headers

### Input Validation

- Real-time conversion preview in input fields
- Suggest appropriate precision based on unit system
- Range validation considering unit system

---

## Technical Decisions

### Why Hybrid Approach?

1. **Data Integrity**: Vessel data remains in its native system, preventing precision loss
2. **User Experience**: Users can view any vessel in their preferred units
3. **Collaboration**: Teams with different unit preferences can work on same vessels
4. **Flexibility**: Easy to add new unit systems in the future

### Why Frontend Conversion?

1. **Performance**: No backend conversion overhead on every request
2. **Caching**: Vessel data can be cached without worrying about units
3. **Real-time**: MobX reactivity enables instant UI updates when changing preferences
4. **Separation**: Clear boundary between storage and presentation

### Why Store Native Units per Vessel?

1. **Context**: Preserves the original design intent
2. **Accuracy**: Prevents cumulative rounding errors
3. **Audit**: Clear record of original data entry
4. **Standards**: Respects regional and industry standards

---

## Files Modified

### Backend

- `backend/IdentityService/Migrations/20251025000000_AddUserPreferredUnits.cs` (new)
- `backend/IdentityService/Migrations/IdentityDbContextModelSnapshot.cs` (modified)
- `backend/Shared/Models/User.cs` (modified)
- `backend/Shared/DTOs/UserDto.cs` (modified)
- `backend/Shared/DTOs/UserSettingsDto.cs` (new)
- `backend/Shared/Utilities/UnitConversion.cs` (new)
- `backend/IdentityService/Services/IUserService.cs` (modified)
- `backend/IdentityService/Services/UserService.cs` (modified)
- `backend/IdentityService/Controllers/UsersController.cs` (modified)

### Frontend

- `frontend/src/utils/unitConversion.ts` (new)
- `frontend/src/stores/SettingsStore.ts` (new)
- `frontend/src/stores/AuthStore.ts` (modified)
- `frontend/src/components/UserSettingsDialog.tsx` (new)
- `frontend/src/pages/DashboardPage.tsx` (modified)
- `frontend/src/components/hydrostatics/tabs/ComputationsTab.tsx` (modified)
- `frontend/src/components/hydrostatics/tabs/OverviewTab.tsx` (modified)
- `frontend/src/components/hydrostatics/tabs/LoadcasesTab.tsx` (modified)
- `frontend/src/components/hydrostatics/CreateVesselDialog.tsx` (modified)

### Documentation

- `UNIT_CONVERSION_IMPLEMENTATION.md` (new)

---

## Summary

The unit conversion feature has been successfully implemented with:

- ✅ Complete backend infrastructure
- ✅ Full frontend integration
- ✅ Clean, intuitive user interface
- ✅ Comprehensive conversion support
- ✅ Backward compatibility
- ✅ All builds passing

Users can now:

1. Set their preferred unit system (SI or Imperial)
2. View all vessels in their preferred units
3. Create vessels in any unit system
4. Seamlessly collaborate across different unit preferences

The system maintains data integrity by storing vessels in their native units while providing flexible viewing options for users.
