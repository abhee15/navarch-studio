# Hull Sizing Solver - Assumptions and Default Values Documentation

## Critical Assumptions

### 1. Propulsive Efficiency (η_prop)
- **Value**: 0.60 (60%)
- **Source**: Conservative industry standard for preliminary design
- **Rationale**: Accounts for propeller, hull, and transmission losses
- **Impact**: Higher assumed efficiency → lower SHP estimates
- **Recommendation**: Should be configurable per vessel type (container: ~0.65, bulk: ~0.58)

### 2. Service and Sea Margins
- **Sea Margin**: 15% (added to base SHP)
- **Service Margin**: 10% (added after sea margin)
- **Total Margin**: ~26.5% (multiplicative)
- **Rationale**: Standard practice for weather and fouling allowances
- **Impact**: Conservative power estimates (good for safety, may oversize)

### 3. DWT/Displacement Ratios
These ratios determine target displacement from deadweight:
- **Container Ships**: 0.70 (typical range: 0.65-0.75)
- **Large Tankers/Bulk**: 0.85 (typical range: 0.80-0.87)
- **General Commercial**: 0.75 (typical range: 0.70-0.80)
- **Default/Unknown**: 0.65 (conservative, typical range: 0.60-0.70)

**Validation Added**: 
- Logs the ratio used for traceability
- Warns if ratio is outside typical range [0.40, 0.90]
- Helps identify misclassification of vessel types

### 4. Cargo Density (Volume-Based Missions)
- **Default**: 0.5 t/m³
- **Typical Range**: 0.1 - 2.5 t/m³
- **Examples**: 
  - Light cargo (paper, furniture): 0.1-0.3 t/m³
  - General cargo: 0.5-0.8 t/m³
  - Steel/machinery: 2.0-2.5 t/m³
- **Impact**: Incorrect density → wrong displacement → wrong hull size
- **Validation Added**: 
  - Warns if density outside typical range
  - Clamps to reasonable bounds [0.1, 2.5] t/m³
  - Logs when default is used

### 5. TEU to Weight Conversion
- **Value**: 14 tonnes per TEU (average)
- **Rationale**: Mix of laden and empty containers
- **Impact**: May underestimate for fully laden, overestimate for empty
- **Recommendation**: Should consider TEU utilization rate

## Fallback Logic

### 1. Wetted Surface Area Calculation
- **Issue**: Holtrop formula can return negative/invalid values for extreme parameters
- **Fallback**: Simple box approximation: `S = Lwl × B × 2.5`
- **Validation Added**:
  - Checks for negative or non-normal values
  - Validates result is in reasonable range [1.5×L×B, 5×L×B]
  - Logs detailed warning with all input parameters when fallback used

### 2. Default Waterplane Coefficient
- **Value**: 0.85 (used when not calculated)
- **Rationale**: Typical for moderate hull forms
- **Impact**: Minor - only affects initial estimates
- **Recommendation**: Should always be calculated from geometry

## Limitations

### Wave Resistance Calculation
- **Status**: Simplified polynomial approximation (MVP placeholder)
- **Accuracy**: Low for extreme speeds or unusual hull forms
- **TODO**: Replace with full Holtrop-Mennen formula (Phase 3)
- **Impact**: Resistance estimates may be inaccurate, especially:
  - High Froude Numbers (Fn > 0.35)
  - Very full forms (Cb > 0.85)
  - Very fine forms (Cb < 0.50)

### Form Factor Estimation
- **Status**: Simplified formula (not full Holtrop)
- **Accuracy**: Moderate - works well for typical commercial vessels
- **Limitations**: May be inaccurate for:
  - Extremely full forms
  - Transom sterns at high speeds
  - Bulbous bow effects

## Recommendations for Improvement

1. **Make assumptions configurable**: Allow users to specify propulsive efficiency, margins
2. **Vessel-type-specific defaults**: Use more accurate defaults based on vessel classification
3. **Replace simplified formulas**: Implement full Holtrop-Mennen in Phase 3
4. **Add validation warnings**: Alert users when assumptions may be inaccurate
5. **Document in UI**: Show users which assumptions are being used

## Validation Improvements Made

All critical assumptions now include:
- Input validation (range checks)
- Warning logs when defaults/fallbacks are used
- Detailed logging with all relevant parameters
- Clamping to reasonable bounds where appropriate

This ensures:
- Users are aware of assumptions being made
- Invalid inputs are caught early
- Debugging is easier with detailed logs
- System behavior is predictable and traceable


