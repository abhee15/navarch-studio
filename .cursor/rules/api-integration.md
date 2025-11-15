# API Integration Rules

## Overview

This document provides comprehensive guidelines for API integration between frontend and backend, focusing on parameter mapping, service dependencies, error handling, and contract validation. These rules prevent common integration issues like parameter mismatches, silent failures, and incomplete error handling.

## Critical Rules

**Before implementing any API integration:**
1. Read this entire document
2. Follow the integration checklist
3. Verify parameter names match in both frontend and backend
4. Validate service dependencies before processing
5. Implement comprehensive error handling and logging

## Parameter Mapping Patterns

### Frontend to Backend Parameter Naming

**Rule**: Frontend sends camelCase parameter names, backend converts to lowercase for matching.

**Pattern**:
- Frontend: `sternRakeAngle` → Backend: `"sternrakeangle"` (after `.ToLower()`)
- Frontend: `hasSheer` → Backend: `"hassheer"` (after `.ToLower()`)
- Frontend: `bowFlareAngle` → Backend: `"bowflareangle"` (after `.ToLower()`)

### Parameter Name Verification Checklist

When adding or modifying API parameters, you MUST verify:

1. **Frontend Parameter Mapping** (`frontend/src/pages/sizing/CandidateWorkspace.tsx`):
   - Parameter name in `handleParameterAdjust` matches the property name
   - Value is correctly converted (boolean → number for flags)

2. **Backend Parameter Validation** (`backend/HullSizingService/Services/CandidateDesignService.cs`):
   - Parameter name exists in first switch statement (lines 206-255)
   - Parameter name exists in second switch statement (lines 284-385) for ShipD vector updates
   - Case-insensitive matching works correctly (`.ToLower()` conversion)

3. **Backend DTO Mapping** (`backend/Shared/DTOs/Sizing/CandidateDesignDto.cs`):
   - Property exists in DTO with correct type
   - Property is populated in `MapToDto` method (lines 541-636)

4. **Frontend Type Definition** (`frontend/src/types/sizing.ts`):
   - Property exists in `CandidateDesign` interface
   - Type matches backend DTO type

### Examples of Correct Parameter Mappings

#### Example 1: Basic Dimension Parameter

**Frontend** (`CandidateWorkspace.tsx`):
```typescript
if (updates.lppM !== undefined) {
  parameter = "lppM";
  value = updates.lppM;
}
```

**Backend** (`CandidateDesignService.cs`):
```csharp
case "lppm":  // Note: lowercase after ToLower()
    candidate.LppM = adjustedValue;
    candidate.LwlM = adjustedValue * 1.02m;
    break;
```

#### Example 2: ShipD Parameter (Boolean Flag)

**Frontend** (`CandidateWorkspace.tsx`):
```typescript
else if (updates.hasSheer !== undefined) {
  parameter = "hasSheer";
  value = updates.hasSheer ? 1 : 0; // Convert boolean to number
}
```

**Backend** (`CandidateDesignService.cs`):
```csharp
// First switch - validation
case "hassheer":  // lowercase
    break;  // Handled in ShipD vector update below

// Second switch - ShipD vector update
case "hassheer":
    adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(
        originalVector, 20, adjustedValue > 0.5m ? 1m : 0m, shipdMetadata);
    break;
```

**Backend DTO** (`CandidateDesignDto.cs`):
```csharp
public bool? HasSheer { get; init; }  // Note: PascalCase in DTO

// In MapToDto:
HasSheer = GetShipDParam(shipdVector, 20) > 0.5m,
```

#### Example 3: ShipD Parameter (Numeric)

**Frontend** (`CandidateWorkspace.tsx`):
```typescript
else if (updates.sternRakeAngle !== undefined) {
  parameter = "sternRakeAngle";
  value = updates.sternRakeAngle;
}
```

**Backend** (`CandidateDesignService.cs`):
```csharp
// First switch - validation
case "sternrakeangle":  // lowercase
    break;  // Handled in ShipD vector update below

// Second switch - ShipD vector update
case "sternrakeangle":
    adjustedVector = _shipdParameterAdapter.UpdateShipDParameter(
        originalVector, 27, adjustedValue, shipdMetadata);
    break;
```

**Backend DTO** (`CandidateDesignDto.cs`):
```csharp
public decimal? SternRakeAngle { get; init; }  // Note: PascalCase in DTO

// In MapToDto:
SternRakeAngle = GetShipDParam(shipdVector, 27),
```

### Common Parameter Mapping Pitfalls

❌ **WRONG**: Frontend sends `"SternRakeAngle"` (PascalCase) - backend won't match after `.ToLower()`
✅ **CORRECT**: Frontend sends `"sternRakeAngle"` (camelCase) - backend converts to `"sternrakeangle"`

❌ **WRONG**: Backend switch case is `"SternRakeAngle"` (PascalCase) - won't match lowercase input
✅ **CORRECT**: Backend switch case is `"sternrakeangle"` (lowercase) - matches after `.ToLower()`

❌ **WRONG**: Parameter exists in first switch but missing in second switch - will pass validation but fail processing
✅ **CORRECT**: Parameter exists in both switch statements with proper handling

❌ **WRONG**: Boolean parameter sent as `true`/`false` - backend expects `1`/`0`
✅ **CORRECT**: Boolean parameter converted to `1`/`0` in frontend before sending

## Service Dependency Validation

### When to Validate Services

**Rule**: Always validate required services are available BEFORE processing any request that depends on them.

**Critical**: If a parameter adjustment requires ShipD services, validate them at the start of the method, not inside conditional blocks.

### How to Validate Services

**Pattern**:
```csharp
public async Task<CandidateDesignDto?> AdjustParameterAsync(
    Guid id,
    AdjustParameterDto dto,
    string tenantId,
    CancellationToken cancellationToken = default)
{
    // ... load candidate ...

    var paramLower = dto.Parameter.ToLower();
    
    // Check if this parameter requires ShipD services
    bool requiresShipDServices = IsShipDParameter(paramLower);
    
    if (requiresShipDServices)
    {
        // Validate services are available BEFORE processing
        if (_shipdGeometryService == null || 
            _shipdParameterAdapter == null || 
            _dataServiceClient == null)
        {
            throw new InvalidOperationException(
                $"Parameter '{dto.Parameter}' requires ShipD services, but they are not available. " +
                "Please ensure ShipD services are properly configured.");
        }
        
        if (string.IsNullOrEmpty(candidate.ShipdParametersJson))
        {
            throw new InvalidOperationException(
                $"Parameter '{dto.Parameter}' requires ShipD vector, but candidate does not have one. " +
                "This candidate may have been created before ShipD integration was added.");
        }
    }
    
    // ... continue processing ...
}

private static bool IsShipDParameter(string paramLower)
{
    return paramLower switch
    {
        "bowlengthratio" or "sternlengthratio" or "bowflareangle" or 
        "bowcurvature" or "bowknuckle" or "deadriseangle" or 
        "sternrakeangle" or "sterncurvature" or "sternknuckle" or 
        "transomarea" or "transomwidth" or "hassheer" or 
        "hastumblehome" or "hasbulb" or "bulblengthratio" or 
        "bulbheightratio" or "bulbwidthratio" or "bulbasymmetry" or 
        "bulbfilletradius" => true,
        _ => false
    };
}
```

### Error Messages for Service Unavailability

**Rule**: Always provide clear, actionable error messages.

**Good Error Messages**:
- `"Parameter 'sternRakeAngle' requires ShipD services, but they are not available. Please ensure ShipD services are properly configured."`
- `"Parameter 'hasSheer' requires ShipD vector, but candidate does not have one. This candidate may have been created before ShipD integration was added."`

**Bad Error Messages**:
- `"Service unavailable"` (not specific)
- `"Error processing parameter"` (doesn't explain what's wrong)
- `"Xr"` (truncated or corrupted)

### Don't Silently Fail

**Rule**: Never silently fail when services are unavailable. Always throw an exception with a clear message.

❌ **WRONG**:
```csharp
if (_shipdGeometryService == null)
{
    // Silently skip - user gets no feedback
    return MapToDto(candidate);
}
```

✅ **CORRECT**:
```csharp
if (_shipdGeometryService == null)
{
    throw new InvalidOperationException(
        "ShipD geometry service is required but not available. " +
        "Please ensure the service is properly configured.");
}
```

## Error Handling Patterns

### Controller Error Handling

**Rule**: Controllers must catch both `ArgumentException` and `InvalidOperationException`.

**Pattern**:
```csharp
[HttpPost("{id}/adjust")]
public async Task<ActionResult<CandidateDesignDto>> AdjustParameter(
    Guid id,
    [FromBody] AdjustParameterDto dto,
    CancellationToken ct)
{
    var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-default-tenant";

    try
    {
        var result = await _service.AdjustParameterAsync(id, dto, tenantId, ct);

        if (result == null)
            return NotFound(new { error = "Candidate design not found" });

        return Ok(result);
    }
    catch (ArgumentException ex)
    {
        // Invalid parameter name or value
        return BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        // Service unavailable or missing dependencies
        return StatusCode(503, new { error = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error adjusting parameter {Parameter} for candidate {Id}", 
            dto.Parameter, id);
        return StatusCode(500, new { error = "An unexpected error occurred" });
    }
}
```

### HTTP Status Codes

**Rule**: Return appropriate HTTP status codes:

- `400 Bad Request`: Invalid parameter name, invalid parameter value, validation errors
- `404 Not Found`: Resource not found (candidate, run, etc.)
- `503 Service Unavailable`: Required services are not available
- `500 Internal Server Error`: Unexpected errors (with logging)

### Frontend Error Extraction

**Rule**: Frontend must properly extract and display error messages from API responses.

**Pattern**:
```typescript
catch (error: any) {
  // Extract error message from various possible locations
  let errorMessage = "Unknown error";
  if (error?.response?.data?.error) {
    errorMessage = error.response.data.error;
  } else if (error?.response?.data?.message) {
    errorMessage = error.response.data.message;
  } else if (typeof error?.response?.data === "string") {
    errorMessage = error.response.data;
  } else if (error?.message) {
    errorMessage = error.message;
  }
  
  // Log full error for debugging
  console.error(`[Parameter Adjust Error] Parameter: ${parameter}, Value: ${value}`);
  console.error(`[Parameter Adjust Error] Full error object:`, JSON.stringify(error, null, 2));
  console.error(`[Parameter Adjust Error] Error response:`, error?.response?.data);
  console.error(`[Parameter Adjust Error] Error message: ${errorMessage}`);
  
  // Show user-friendly error
  toast.error(`Failed to update parameter: ${errorMessage}`);
}
```

## Logging Standards

### What to Log

**Rule**: Log all critical information for debugging and monitoring.

**Required Logging**:
1. **Parameter adjustments**: Parameter name, value, candidate ID
2. **Service availability**: Whether required services are available
3. **ShipD vector updates**: Success/failure of vector updates
4. **Error conditions**: All exceptions with full context

### When to Log

**Rule**: Log at appropriate levels:

- **Information**: Normal operations (parameter adjustments, successful updates)
- **Warning**: Recoverable issues (missing optional data, fallback behavior)
- **Error**: Exceptions and failures

### Structured Logging Pattern

**Pattern**:
```csharp
_logger.LogInformation(
    "[CANDIDATE_ADJUST] Parameter adjusted for candidate {Id}: {Parameter}={Value}, New Δ={Disp}t, Fn={Fn}",
    candidate.Id, dto.Parameter, dto.Value, candidate.DisplacementT, candidate.Fn);

_logger.LogWarning(
    "[CANDIDATE_ADJUST] Failed to regenerate ShipD geometry for candidate {Id} after {Parameter} adjustment. Geometry may be out of sync.",
    candidate.Id, dto.Parameter);

_logger.LogError(ex,
    "[CANDIDATE_ADJUST] Unexpected error adjusting parameter {Parameter} for candidate {Id}",
    dto.Parameter, candidate.Id);
```

### Logging Checklist

When implementing parameter adjustments, ensure you log:

- [ ] Parameter name and value received
- [ ] Service availability status (if required)
- [ ] ShipD vector update success/failure
- [ ] Final parameter values after adjustment
- [ ] Any errors or warnings

## Frontend-Backend Contract

### DTO Consistency

**Rule**: DTOs must match between frontend TypeScript types and backend C# DTOs.

**Verification Checklist**:
- [ ] All properties exist in both frontend and backend
- [ ] Property types match (number ↔ decimal, boolean ↔ bool)
- [ ] Property names match (camelCase in TypeScript, PascalCase in C# - handled by JSON serialization)
- [ ] Nullable types match (TypeScript `?` ↔ C# `?`)

### Parameter Name Documentation

**Rule**: Parameter names must be documented in both places.

**Documentation Pattern**:
```typescript
// Frontend: frontend/src/pages/sizing/CandidateWorkspace.tsx
// Parameter mapping for API calls
// Maps frontend property names to backend parameter names (sent as-is, backend converts to lowercase)
// Boolean parameters are converted to 1/0 before sending
```

```csharp
// Backend: backend/HullSizingService/Services/CandidateDesignService.cs
// Parameter validation and processing
// Frontend sends camelCase (e.g., "sternRakeAngle"), converted to lowercase for matching
// ShipD parameters require: _shipdGeometryService, _shipdParameterAdapter, _dataServiceClient
```

### Adding New Parameters - Complete Checklist

When adding a new API parameter, you MUST update:

1. **Frontend TypeScript Interface** (`frontend/src/types/sizing.ts`):
   - [ ] Add property to `CandidateDesign` interface
   - [ ] Use correct type (number, boolean, etc.)
   - [ ] Mark as optional if nullable (`?`)

2. **Frontend Parameter Mapping** (`frontend/src/pages/sizing/CandidateWorkspace.tsx`):
   - [ ] Add case in `handleParameterAdjust` method
   - [ ] Convert boolean to number if needed (`value ? 1 : 0`)
   - [ ] Use camelCase parameter name

3. **Backend DTO** (`backend/Shared/DTOs/Sizing/CandidateDesignDto.cs`):
   - [ ] Add property with correct type
   - [ ] Use PascalCase property name
   - [ ] Mark as nullable if optional (`?`)

4. **Backend Parameter Validation** (`backend/HullSizingService/Services/CandidateDesignService.cs`):
   - [ ] Add case in first switch statement (validation) - use lowercase
   - [ ] Add case in second switch statement (processing) - use lowercase
   - [ ] Implement proper handling (direct update or ShipD vector update)

5. **Backend DTO Mapping** (`backend/HullSizingService/Services/CandidateDesignService.cs`):
   - [ ] Update `MapToDto` method to extract/populate the property
   - [ ] Use `GetShipDParam` if extracting from vector
   - [ ] Convert boolean if needed (`> 0.5m`)

6. **Service Dependency Validation**:
   - [ ] Add to `IsShipDParameter` method if it requires ShipD services
   - [ ] Validate services are available before processing

7. **Error Handling**:
   - [ ] Ensure controller catches appropriate exceptions
   - [ ] Return appropriate HTTP status codes
   - [ ] Provide clear error messages

8. **Logging**:
   - [ ] Add logging for parameter adjustments
   - [ ] Log service availability if required
   - [ ] Log success/failure of updates

9. **Testing**:
   - [ ] Test parameter adjustment from frontend
   - [ ] Test with services available
   - [ ] Test with services unavailable (should return clear error)
   - [ ] Test with missing ShipD vector (if applicable)
   - [ ] Verify error messages are user-friendly

## Integration Checklist

### Pre-Implementation

- [ ] Read this entire API integration rules document
- [ ] Identify all parameters that need to be added/modified
- [ ] Determine which parameters require ShipD services
- [ ] Verify service dependencies are available
- [ ] Plan error handling strategy

### Implementation

- [ ] Update frontend TypeScript interface
- [ ] Update frontend parameter mapping
- [ ] Update backend DTO
- [ ] Update backend parameter validation (first switch)
- [ ] Update backend parameter processing (second switch)
- [ ] Update backend DTO mapping
- [ ] Add service dependency validation
- [ ] Implement error handling
- [ ] Add logging

### Post-Implementation Verification

- [ ] Verify parameter names match (case-insensitive)
- [ ] Test parameter adjustment from frontend
- [ ] Test error scenarios (services unavailable, missing data)
- [ ] Verify error messages are clear and actionable
- [ ] Check logs for proper logging
- [ ] Verify DTOs match between frontend and backend
- [ ] Test with all supported parameter types (number, boolean, etc.)

## Common Pitfalls and How to Avoid Them

### Pitfall 1: Parameter Name Mismatch

**Symptom**: 400 Bad Request with message "Parameter 'X' is not adjustable"

**Cause**: Parameter name in frontend doesn't match backend switch case (after lowercase conversion)

**Prevention**:
- Always verify parameter names match in both places
- Use the parameter mapping checklist
- Test parameter adjustments immediately after adding

### Pitfall 2: Missing Service Validation

**Symptom**: Parameter adjustment succeeds but doesn't actually update anything, or fails silently

**Cause**: Services are null but code doesn't check before using them

**Prevention**:
- Always validate services at the start of methods that require them
- Throw `InvalidOperationException` with clear message if unavailable
- Don't silently skip processing

### Pitfall 3: Incomplete Error Handling

**Symptom**: Generic error messages like "Xr" or "Unknown error"

**Cause**: Error messages not properly extracted or logged

**Prevention**:
- Always extract error messages from multiple possible locations
- Log full error objects for debugging
- Provide user-friendly error messages

### Pitfall 4: Missing Switch Cases

**Symptom**: Parameter passes validation but doesn't get processed

**Cause**: Parameter exists in first switch but missing in second switch

**Prevention**:
- Always add parameter to both switch statements
- Use the integration checklist
- Verify both validation and processing logic

### Pitfall 5: Boolean Conversion Issues

**Symptom**: Boolean parameters don't work correctly

**Cause**: Boolean not converted to number (1/0) before sending, or not converted back in backend

**Prevention**:
- Always convert boolean to number in frontend: `value ? 1 : 0`
- Always check boolean in backend: `adjustedValue > 0.5m ? 1m : 0m`
- Document boolean parameters clearly

## Quick Reference

### Parameter Name Conversion
- Frontend sends: `camelCase` (e.g., `sternRakeAngle`)
- Backend receives: `camelCase` (e.g., `"sternRakeAngle"`)
- Backend converts: `.ToLower()` → `"sternrakeangle"`
- Backend switch: Use lowercase (e.g., `case "sternrakeangle":`)

### Boolean Parameters
- Frontend: Convert `boolean` → `number` (`true` → `1`, `false` → `0`)
- Backend: Check `number` → `boolean` (`> 0.5m` → `true`, `≤ 0.5m` → `false`)

### Service Validation
- Check services BEFORE processing: `if (_service == null) throw new InvalidOperationException(...)`
- Return 503 for service unavailable: `return StatusCode(503, new { error = ex.Message })`

### Error Extraction
- Check multiple locations: `error?.response?.data?.error`, `error?.response?.data?.message`, `error?.message`
- Log full error object for debugging
- Show user-friendly message in UI

## Related Documentation

- [.NET Rules](.cursor/rules/dotnet.md) - Backend coding standards
- [React/TypeScript Rules](.cursor/rules/react-typescript.md) - Frontend coding standards
- [Debugging Methodology](.cursor/rules/debugging-methodology.md) - How to debug integration issues
