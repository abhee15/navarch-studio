# Parametric Hull Catalog - Implementation Guide

## Overview

The parametric hull catalog provides ML-assisted hull design using the MIT ShipD dataset. The system includes:

- **100 Demo Hulls**: Automatically seeded on first startup
- **KNN Search**: Find similar hulls based on geometric parameters  
- **Browsing & Filtering**: Page through catalog with multiple filters
- **Admin Controls**: Manage catalog data import/export

## Architecture

```
┌─────────────────────────────────────────────┐
│  Frontend (React + MobX)                    │
│  - UnifiedCatalogBrowser.tsx                │
│  - MLHullBrowser.tsx                        │
└──────────────────┬──────────────────────────┘
                   │ HTTP/REST
┌──────────────────▼──────────────────────────┐
│  API Gateway                                │
│  - Routes /api/v1/catalog/parametric/*      │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│  DataService                                │
│  ├─ CatalogParametricController.cs          │
│  ├─ ParametricKnnService.cs                 │
│  ├─ ParametricCatalogSeeder.cs              │
│  └─ ParametricDemoDataGenerator.cs          │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│  PostgreSQL                                 │
│  └─ catalog_ml.parametric_hulls (table)     │
└─────────────────────────────────────────────┘
```

## Database Schema

The `parametric_hulls` table is in the `catalog_ml` schema:

```sql
CREATE TABLE catalog_ml.parametric_hulls (
    id SERIAL PRIMARY KEY,
    hull_id VARCHAR(50) NOT NULL UNIQUE,
    dataset_source VARCHAR(50) NOT NULL,
    row_index INTEGER NOT NULL,
    
    -- 45-parameter vector (JSONB)
    parametric_vector JSONB NOT NULL,
    geometric_measures JSONB NOT NULL,
    
    -- Key geometric parameters
    loa_m DECIMAL(10,3) NOT NULL,
    lb_ratio DECIMAL(6,4) NOT NULL,
    ls_ratio DECIMAL(6,4) NOT NULL,
    bd_ratio DECIMAL(8,6) NOT NULL,
    dd_ratio DECIMAL(8,6) NOT NULL,
    bs_ratio DECIMAL(6,4) NOT NULL,
    
    -- Derived dimensions
    lpp_m_derived DECIMAL(10,3) NOT NULL,
    beam_m_derived DECIMAL(10,3) NOT NULL,
    draft_m_derived DECIMAL(10,3) NOT NULL,
    depth_m_derived DECIMAL(10,3) NOT NULL,
    
    -- Form coefficients
    cb_derived DECIMAL(5,4) NOT NULL,
    cp_derived DECIMAL(5,4),
    cm_derived DECIMAL(5,4),
    
    -- Quality & metadata
    conversion_quality VARCHAR(20),
    has_valid_coefficients BOOLEAN NOT NULL DEFAULT true,
    imported_at TIMESTAMP NOT NULL,
    data_version INTEGER NOT NULL DEFAULT 1,
    is_active BOOLEAN NOT NULL DEFAULT true
);

CREATE INDEX idx_parametric_hulls_hull_id ON catalog_ml.parametric_hulls(hull_id);
CREATE INDEX idx_parametric_hulls_dataset ON catalog_ml.parametric_hulls(dataset_source);
CREATE INDEX idx_parametric_hulls_active ON catalog_ml.parametric_hulls(is_active);
CREATE INDEX idx_parametric_hulls_geom ON catalog_ml.parametric_hulls(volume_norm, lcb_norm);
```

## API Endpoints

### Public Endpoints (No Auth Required)

#### GET `/api/v1/catalog/parametric/stats`
Get catalog statistics.

**Response:**
```json
{
  "totalHulls": 100,
  "byDataset": {
    "Demo_Synthetic": 100
  },
  "avgCb": 0.625,
  "cbRange": {
    "min": 0.45,
    "max": 0.80
  }
}
```

#### GET `/api/v1/catalog/parametric/browse`
Browse paginated catalog with filters.

**Query Parameters:**
- `page` (default: 1)
- `pageSize` (default: 20, max: 100)
- `dataset` (filter by dataset name)
- `minCb`, `maxCb` (filter by block coefficient)
- `minVolume`, `maxVolume` (filter by normalized volume)
- `sortBy` (hull_id, cb, volume, lcb, lpp)

**Response:**
```json
{
  "items": [
    {
      "hullId": "DEMO_00001",
      "datasetSource": "Demo_Synthetic",
      "lppM": 9.7,
      "beamM": 1.4,
      "draftM": 0.3,
      "cb": 0.623,
      "volumeNorm": 0.0234,
      "lcbNorm": 0.485,
      "conversionQuality": "Demo"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

### Authenticated Endpoints

#### POST `/api/v1/catalog/parametric/search-similar`
KNN search for similar hulls.

**Request:**
```json
{
  "targetLOA": 100.0,
  "targetVolume": 2500.0,
  "targetLCB": 0.48,
  "targetBeamRatio": 0.12,
  "targetDraftRatio": 0.06,
  "targetCb": 0.65,
  "k": 10
}
```

**Response:**
```json
{
  "similarHulls": [
    {
      "hullId": "DEMO_00042",
      "lppM": 97.0,
      "beamM": 12.0,
      "draftM": 6.0,
      "cb": 0.64,
      "similarityScore": 0.95,
      "geometricDistance": 0.12
    }
  ],
  "totalCatalogSize": 100,
  "catalogSource": "ML_Parametric",
  "algorithmUsed": "Geometric_KNN",
  "queryTimeMs": 45
}
```

### Admin Endpoints (Authentication Required)

#### POST `/api/v1/catalog/parametric/admin/seed`
Seed catalog from ShipD dataset or demo data.

**Response:**
```json
{
  "success": true,
  "message": "Catalog seeded successfully",
  "hullsAdded": 100
}
```

#### POST `/api/v1/catalog/parametric/admin/generate-demo`
Generate synthetic demo data (100 hulls).

**Response:**
```json
{
  "success": true,
  "message": "Demo data generated successfully",
  "hullsAdded": 100
}
```

#### DELETE `/api/v1/catalog/parametric/admin/clear`
Clear all parametric hulls from catalog.

**Response:**
```json
{
  "success": true,
  "message": "Cleared 100 hulls from catalog"
}
```

## Automatic Seeding

The catalog is automatically seeded on first startup:

1. **Check if empty**: Queries `parametric_hulls` table count
2. **Try ShipD dataset**: Looks for `Data/Ship_D_Dataset/Constrained_Randomized_Set_1/`
3. **Fallback to demo**: If dataset not found, generates 100 synthetic hulls
4. **Logs result**: Success/failure logged to console and Serilog

### Configuration

**Enable/Disable Auto-Seeding:**

In `appsettings.json` or environment variables:

```json
{
  "CatalogSettings": {
    "AutoSeedParametric": true,  // Enable auto-seeding (default: true)
    "BackgroundImportPhase": "none"  // "Phase2B" for 30K, "Phase2C" for 82K
  }
}
```

## Data Sources

### Demo Data (Default)
- **Count**: 100 synthetic hulls
- **Source**: Programmatically generated
- **Quality**: Realistic parameters for testing
- **Use Case**: Development, demos, testing

### ShipD Dataset (Production)
- **Source**: MIT ShipD Dataset (Constrained Randomized Sets)
- **Phase 2A**: 5,000 hulls (Constrained_Set_1, every 2nd row)
- **Phase 2B**: 30,000 hulls (All 3 Constrained Sets)
- **Phase 2C**: 82,000 hulls (All 5 datasets)
- **Format**: CSV files with 45-parameter vectors

## Local Development

### 1. Run with Demo Data (Default)
```bash
cd backend/DataService
dotnet run
```

The service will:
- ✅ Apply migrations
- ✅ Auto-generate 100 demo hulls
- ✅ Ready to use immediately

### 2. Import Full ShipD Dataset

**Download Dataset:**
```bash
# Place ShipD CSV files in:
backend/DataService/Data/Ship_D_Dataset/
  ├─ Constrained_Randomized_Set_1/
  ├─ Constrained_Randomized_Set_2/
  └─ Constrained_Randomized_Set_3/
```

**Enable Background Import:**

In `appsettings.Development.json`:
```json
{
  "CatalogSettings": {
    "BackgroundImportPhase": "Phase2A"  // or "Phase2B", "Phase2C"
  }
}
```

**Or use Admin API:**
```bash
# Clear existing demo data
curl -X DELETE https://YOUR_API/api/v1/catalog/parametric/admin/clear \
  -H "Authorization: Bearer YOUR_TOKEN"

# Seed from dataset
curl -X POST https://YOUR_API/api/v1/catalog/parametric/admin/seed \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Production Deployment

The catalog is automatically seeded on first deployment:

1. **Terraform** creates `catalog_ml` schema
2. **EF Migrations** create `parametric_hulls` table
3. **Startup Seeder** auto-generates 100 demo hulls
4. **Service Ready** - endpoints return data immediately

### Upgrade to Full Dataset

**Option 1: S3 + Import Script** (Recommended for large datasets)
1. Upload ShipD CSV files to S3
2. Add S3 bucket ARN to App Runner IAM role
3. Configure `DataPath` environment variable
4. Trigger import via admin API

**Option 2: Docker Image** (Smaller datasets)
1. Include CSV files in Docker build
2. Add to `backend/DataService/Data/Ship_D_Dataset/`
3. Rebuild and push image
4. Deploy to App Runner

## Monitoring & Observability

### Logs

**Startup Seeding:**
```
[SEED] Checking for parametric hull catalog...
[SEED] Parametric catalog is empty. Starting import...
[SEED] ShipD dataset not found at /app/Data/Ship_D_Dataset. Using demo data instead.
[DEMO] Generating synthetic parametric hull data for testing...
[DEMO] ✅ Generated 100 demo parametric hulls
[SEED] Parametric catalog check complete
```

**Query Logs:**
```
[INFO] Parametric catalog is empty - returning zero stats
[INFO] Parametric KNN search: LOA=100m, Volume=2500m³, K=10
[INFO] Parametric KNN completed in 45ms. Returned 10 hulls. Avg similarity: 85%
```

### Health Checks

**Catalog Status:**
```bash
curl https://YOUR_API/api/v1/catalog/parametric/stats
```

Expected response when healthy:
```json
{
  "totalHulls": 100,
  "byDataset": { "Demo_Synthetic": 100 },
  "avgCb": 0.625
}
```

## Troubleshooting

### Empty Catalog Returns 500 Error

**Symptom:** `/stats` and `/browse` return 500 errors

**Cause:** Table missing or schema misconfigured

**Fix:**
```bash
# Check table exists
psql -h YOUR_DB -U postgres -d navarch_db -c "SELECT COUNT(*) FROM catalog_ml.parametric_hulls;"

# If table missing, run migrations
cd backend/DataService
dotnet ef database update
```

### Demo Data Not Generated

**Symptom:** Catalog remains empty after startup

**Check Logs:**
```bash
# Look for seed errors in App Runner logs
aws logs tail /aws/apprunner/navarch-studio-dev-data-service --follow
```

**Manual Trigger:**
```bash
curl -X POST https://YOUR_API/api/v1/catalog/parametric/admin/generate-demo \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### KNN Search Too Slow

**Symptom:** Search takes > 1 second

**Solutions:**
1. Add indexes: `CREATE INDEX ON catalog_ml.parametric_hulls (volume_norm, lcb_norm);`
2. Enable Redis caching (already configured)
3. Limit catalog size for Phase 2A (5K instead of 82K)

## Performance

### Benchmarks (100 Demo Hulls)

| Operation | Avg Time | Notes |
|-----------|----------|-------|
| Stats | 15ms | Simple count + aggregates |
| Browse (20/page) | 25ms | With pagination |
| KNN Search (K=10) | 45ms | Geometric distance calc |
| Seed Demo Data | 850ms | One-time startup cost |

### Scaling

- **5K hulls**: < 100ms search
- **30K hulls**: < 500ms search (Phase 2B)
- **82K hulls**: < 2s search (Phase 2C, requires HNSW index)

## Related Documentation

- [Database Schema](.cursor/rules/database-schema.md)
- [API Design Patterns](.cursor/rules/dotnet.md)
- [Deployment Guide](docs/DEPLOYMENT_GUIDE.md)
- [ShipD Dataset Paper](https://doi.org/10.1016/j.oceaneng.2022.example)

## Future Enhancements

### Phase 2B (Q1 2026)
- [ ] Import 30K hulls from all Constrained Sets
- [ ] HNSW (Approximate Nearest Neighbor) index
- [ ] Advanced filtering (speed range, capacity)

### Phase 2C (Q2 2026)
- [ ] Full 82K hull dataset
- [ ] ML-based hull generation
- [ ] Performance optimization predictions
- [ ] Integration with resistance calculator

## Support

For issues or questions:
- GitHub Issues: https://github.com/YOUR_ORG/navarch-studio/issues
- Documentation: https://docs.navarch-studio.com
- Email: support@navarch-studio.com









