# Seed Data Requirements

**Last Updated**: 2025-11-07  
**Critical**: This data MUST be present for services to function correctly

## Overview

NavArch Studio requires reference data ("seed data") to be populated in the database on first startup. If seed data is missing, core features will **fail silently** with zero results.

## DataService Seed Data

### Location
- File: `backend/DataService/Data/Seeds/CatalogSeeder.cs`
- CSV Files: `backend/DataService/Data/Seeds/*.csv` (NOT USED - data is hardcoded)
- Auto-runs: On service startup (Program.cs lines 398-413)

### Required Data

| Data Type | Expected Count | Critical? | Impact if Missing |
|-----------|---------------|-----------|-------------------|
| **Water Properties** | 6 records | ⚠️ High | Resistance calculations will fail |
| **Propeller Series** | 1+ records | ⚠️ Medium | Propeller analysis unavailable |
| **Benchmark Cases** | 6 records | 🔴 **CRITICAL** | Catalog browser empty, no template vessels |
| **Template Hulls** | 3 records | 🔴 **CRITICAL** | No Wigley/Series60/Prismatic |
| **Benchmark Hulls** | 3 records | 🔴 **CRITICAL** | No KCS/KVLCC2/DTMB-5415 |
| **Wigley Geometry** | 1 record | ⚠️ High | Wigley hull cannot be visualized |

### Water Properties Details
**6 records** (ITTC standard anchor points):
- Fresh water @ 0°C, 15°C, 30°C
- Sea water @ 0°C, 15°C, 30°C

Source: ITTC 7.5-02-01-03 Tables 1 & 2

### Benchmark Cases Details
**6 total records**:

**Templates (3)**:
1. `wigley-hull` - Analytical test hull
2. `series60-like` - Series 60 parent form  
3. `prismatic-npc` - Non-prismatic test hull

**Benchmarks (3)**:
1. `kcs` - KRISO Container Ship (230m, Cb=0.651)
2. `kvlcc2` - Very Large Crude Carrier (320m, Cb=0.810)
3. `dtmb-5415` - Naval Combatant (141.8m, Cb=0.507)

---

## HullSizingService Seed Data

### Location
- File: `backend/HullSizingService/Data/Seeds/CsvDataSeeder.cs`
- CSV Files: `backend/HullSizingService/Data/Seeds/*.csv`
- Auto-runs: On service startup (Program.cs lines 346-360)

### Required Data

| Data Type | Expected Count | Critical? | Impact if Missing |
|-----------|---------------|-----------|-------------------|
| **Hull Family Presets** | 5 records | 🔴 **CRITICAL** | First-principles solver generates **ZERO candidates** |
| **ISO Containers** | 8 records | ⚠️ Medium | Container calculations fail |
| **KPI Weights** | 5 records | 🔴 **CRITICAL** | Candidate scoring/ranking fails |

### Hull Family Presets Details
**5 records** (MUST exist for solver to work):

| Family | L/B Range | B/T Range | Cb Range | Fn Range | Notes |
|--------|-----------|-----------|----------|----------|-------|
| `container` | 6.0-8.0 | 2.3-3.0 | 0.55-0.70 | 0.20-0.28 | Fast cargo ships |
| `tanker` | 5.0-7.0 | 2.0-2.8 | 0.75-0.85 | 0.12-0.18 | Full-bodied displacement |
| `bulker` | 5.5-7.5 | 2.2-2.9 | 0.70-0.80 | 0.14-0.20 | Dry bulk cargo |
| `general_cargo` | 6.0-7.5 | 2.4-3.1 | 0.60-0.72 | 0.18-0.24 | Multi-purpose |
| `fishing` | 4.5-6.5 | 2.5-3.5 | 0.50-0.65 | 0.20-0.32 | Trawlers |

**Critical**: All families must have `IsActive = true` or solver will skip them.

### ISO Containers Details
**8 records**:
- 20' Standard, 20' High Cube
- 40' Standard, 40' High Cube
- 45' High Cube
- 48' Standard
- 53' Standard
- Other variants

### KPI Weights Details
**5 records** (system defaults, sum to 1.0):
- `delta_balance`: 0.35 (35%) - Displacement accuracy
- `installed_power`: 0.25 (25%) - Power requirements
- `constraints_ok`: 0.20 (20%) - Constraint satisfaction
- `stability_screen`: 0.10 (10%) - Stability screening
- `teu_or_volume_fit`: 0.10 (10%) - Volume/TEU fit

---

## Parametric Catalog (Optional)

### Location
- File: `backend/DataService/Services/Catalog/ParametricDemoDataGenerator.cs`
- CSV Data: `backend/DataService/Data/Ship_D_Dataset/` (NOT in Docker container)
- Fallback: 100 synthetic demo hulls generated on startup

### Required Data
- **Parametric Hulls**: 100+ records (demo mode) or 5000+ (production with ShipD dataset)
- **Critical**: NO - Falls back to demo data if dataset missing
- **Impact**: ML/Parametric solver unavailable if missing

---

## Real-World Vessel Catalog (Optional)

### Location
- File: `backend/DataService/Services/Catalog/CatalogVesselSeeder.cs`
- CSV Data: `backend/DataService/Data/real_world_vessels.csv` (NOT in Docker container)

### Required Data
- **Real-World Vessels**: 600+ records
- **Critical**: NO - Data-driven real-world mode unavailable if missing
- **Impact**: Data-driven solver fallback to first-principles

---

## How to Verify Seed Data

### DataService
```bash
# Check admin status endpoint
curl https://YOUR-API-URL/api/v1/admin/seeding/status

# Expected response:
{
  "waterProperties": 6,
  "propellerSeries": 1,
  "benchmarkCases": 6,
  "templateHulls": 3,
  "benchmarkGeometries": 1,
  "isComplete": true,
  "severity": "OK"
}
```

### HullSizingService
```bash
# Check diagnostics endpoint
curl https://YOUR-API-URL/api/v1/diagnostics/seed-status

# Expected response:
{
  "hullFamilies": 5,
  "activeFamilies": 5,
  "isoContainers": 8,
  "kpiWeights": 5,
  "seedDataComplete": true,
  "severity": "OK"
}
```

---

## Troubleshooting

### Symptom: Catalog browser shows no vessels
**Cause**: DataService benchmark cases missing (count = 0)
**Solution**: Call `/api/v1/admin/seeding/force-reseed`

### Symptom: First-principles solver generates 0 candidates
**Cause**: HullSizingService hull families missing (count = 0)
**Solution**: Check CSV files exist in deployed container, restart service

### Symptom: "Seeding completed" but data still missing
**Cause**: Seeder threw exception but error was caught and logged as WARNING
**Solution**: Check CloudWatch logs for `[SEED] WARNING` messages

### Symptom: Seed data exists locally but not in production
**Cause**: 
1. CSV files not copied to Docker container
2. Migration regeneration wiped database
3. Seeder logic has bug

**Solution**:
1. Verify CSV files in Dockerfile COPY command
2. Check migration history for `DROP TABLE` commands
3. Run integration tests to verify seeder logic

---

## CI/CD Integration

### Pre-Deployment Checks
Add to GitHub Actions workflow:

```yaml
- name: Verify seed data files
  run: |
    test -f backend/HullSizingService/Data/Seeds/hull_families.csv
    test -f backend/HullSizingService/Data/Seeds/iso_containers.csv
    test -f backend/HullSizingService/Data/Seeds/kpi_weights.csv
```

### Post-Deployment Checks
Add to smoke tests:

```yaml
- name: Verify seed data loaded
  run: |
    # DataService
    STATUS=$(curl -s https://$API_URL/api/v1/admin/seeding/status | jq -r '.isComplete')
    if [ "$STATUS" != "true" ]; then
      echo "ERROR: DataService seed data incomplete!"
      exit 1
    fi
    
    # HullSizingService
    STATUS=$(curl -s https://$API_URL/api/v1/diagnostics/seed-status | jq -r '.seedDataComplete')
    if [ "$STATUS" != "true" ]; then
      echo "ERROR: HullSizingService seed data incomplete!"
      exit 1
    fi
```

---

## Recovery Procedures

### Option 1: Force Re-Seed (Recommended)
```bash
# 1. Check status
curl https://YOUR-API-URL/api/v1/admin/seeding/status

# 2. Force re-seed
curl -X POST https://YOUR-API-URL/api/v1/admin/seeding/force-reseed

# 3. Verify
curl https://YOUR-API-URL/api/v1/admin/seeding/status
```

### Option 2: Restart Service
```bash
# Trigger App Runner deployment (pulls latest code + reruns seeding)
aws apprunner start-deployment --service-arn <SERVICE_ARN>
```

### Option 3: Destroy & Recreate Environment
```bash
cd terraform/deploy
terraform destroy -target=aws_db_instance.main
terraform apply
```

---

## Future Improvements

1. **Fail Fast**: Service should refuse to start if critical seed data missing
2. **Health Check**: `/health` endpoint should return 503 if seed data incomplete
3. **Monitoring**: CloudWatch alert if seed data count < expected
4. **Migration Safety**: Export data before regenerating migrations
5. **Seed Data in Migrations**: Embed critical seed data in migration files (not CSV)

---

## Related Files

- `backend/DataService/Data/Seeds/CatalogSeeder.cs`
- `backend/DataService/Program.cs` (lines 398-413)
- `backend/HullSizingService/Data/Seeds/CsvDataSeeder.cs`
- `backend/HullSizingService/Program.cs` (lines 346-360)
- `backend/DataService.Tests/Integration/SeedDataIntegrationTests.cs`
- `backend/HullSizingService.Tests/Integration/SeedDataIntegrationTests.cs`
