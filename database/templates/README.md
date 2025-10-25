# Hull Geometry CSV Templates

This directory contains CSV templates for importing hull geometry into NavArch Studio.

## Template Files

### 1. offsets_template.csv

**Use when**: You have all geometry data in a single file

**Format**: Combined stations, waterlines, and offsets

```csv
station_index, station_x, waterline_index, waterline_z, half_breadth_y
```

**Columns**:

- `station_index`: Station number (0, 1, 2, ...)
- `station_x`: Longitudinal position from aft perpendicular (m)
- `waterline_index`: Waterline number (0, 1, 2, ...)
- `waterline_z`: Vertical position from keel (m)
- `half_breadth_y`: Half-breadth from centerline (m)

**Example**:

```csv
0,0.0,0,0.0,0.0
0,0.0,1,1.0,2.5
1,10.0,0,0.0,0.5
1,10.0,1,1.0,3.0
```

---

### 2. stations_template.csv

**Use when**: Importing stations separately

**Format**: Station positions

```csv
station_index, x
```

**Columns**:

- `station_index`: Station number (0, 1, 2, ...)
- `x`: Longitudinal position from aft perpendicular (m)

**Example**:

```csv
0,0.0
1,10.0
2,20.0
```

---

### 3. waterlines_template.csv

**Use when**: Importing waterlines separately

**Format**: Waterline heights

```csv
waterline_index, z
```

**Columns**:

- `waterline_index`: Waterline number (0, 1, 2, ...)
- `z`: Vertical position from keel (m)

**Example**:

```csv
0,0.0
1,1.0
2,2.0
```

---

### 4. offsets_only_template.csv

**Use when**: Stations and waterlines already defined, importing only offsets

**Format**: Offset values

```csv
station_index, waterline_index, half_breadth_y
```

**Columns**:

- `station_index`: Station number (must exist in vessel)
- `waterline_index`: Waterline number (must exist in vessel)
- `half_breadth_y`: Half-breadth from centerline (m)

**Example**:

```csv
0,0,0.0
0,1,2.5
1,0,0.5
1,1,3.0
```

---

## Import Workflow

### Option A: Single File Import

1. Download `offsets_template.csv`
2. Fill in your hull geometry data
3. Upload via NavArch Studio UI
4. Stations, waterlines, and offsets created automatically

### Option B: Separate Files Import

1. Download `stations_template.csv`, `waterlines_template.csv`, `offsets_only_template.csv`
2. Fill in each file with your data
3. Upload in order:
   - First: stations
   - Second: waterlines
   - Third: offsets

---

## Units

- **Default**: Metric (SI) units

  - Lengths: meters (m)
  - Half-breadths: meters (m)

- **Imperial**: Supported but converted internally to SI
  - Lengths: feet (ft)
  - Half-breadths: feet (ft)

Specify units when creating vessel in NavArch Studio.

---

## Validation Rules

### Stations

✅ **Valid**:

- Monotonically increasing X values: `0.0, 10.0, 20.0, ...`
- Non-negative values: `X >= 0`

❌ **Invalid**:

- Non-monotonic: `0.0, 20.0, 10.0`
- Negative values: `X = -5.0`
- Duplicate indices

### Waterlines

✅ **Valid**:

- Monotonically increasing Z values: `0.0, 1.0, 2.0, ...`
- Non-negative values: `Z >= 0`

❌ **Invalid**:

- Non-monotonic: `0.0, 2.0, 1.0`
- Negative values: `Z = -1.0`
- Duplicate indices

### Offsets (Half-Breadths)

✅ **Valid**:

- Non-negative values: `Y >= 0`
- Zero at keel/centerline: `Y(WL=0) = 0.0` typical

❌ **Invalid**:

- Negative values: `Y = -2.5`

---

## Example Vessels

### Rectangular Barge

Simple validation case with analytical solutions.

**Dimensions**: L=100m, B=20m

**Stations** (11 stations):

```csv
0,0.0
1,10.0
2,20.0
...
10,100.0
```

**Waterlines** (11 waterlines):

```csv
0,0.0
1,1.0
...
10,10.0
```

**Offsets** (all half-breadths = 10.0m):

```csv
0,0,10.0
0,1,10.0
...
10,10,10.0
```

---

### Wigley Hull

Benchmark hull with published results.

**Equation**:

```
y = (B/2) * (1 - z^2) * (1 - x^2)
where x ∈ [-1, 1], z ∈ [0, 1]
```

See `database/seeds/wigley_hull.csv` for full geometry.

---

## Coordinate System

```
       Z (up)
       ^
       |
       |_____ Y (port)
      /
     /
    X (forward)
```

- **Origin**: Aft perpendicular at keel (0, 0, 0)
- **X-axis**: Forward (aft to bow)
- **Y-axis**: Port (centerline to port, starboard is mirrored)
- **Z-axis**: Vertical (keel to deck)

---

## Import Tips

### For Best Results:

1. **Spacing**: Finer spacing at bow/stern where curvature is high
2. **Coverage**: Ensure waterlines cover full operating draft range
3. **Symmetry**: Only need port side (starboard mirrored automatically)
4. **Validation**: Preview data before import to catch errors

### Common Issues:

- **Missing intersections**: Ensure every station/waterline pair has an offset
- **Unit mismatch**: Check units match vessel configuration
- **Excel formatting**: Save as CSV (Comma delimited), not Excel format
- **Decimal separator**: Use period (`.`), not comma (`,`)

---

## Support

For import issues or questions:

1. Check validation error messages (exact row/column indicated)
2. Review this README
3. See Phase 1 documentation: `.plan/phase1-hydrostatics-mvp.md`

---

**Last Updated**: October 25, 2025
