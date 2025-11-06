# Benchmark Catalog Data Import - READY TO IMPLEMENT

**Date:** November 6, 2025  
**Status:** ✅ READY - Data Available, No Blockers  
**Priority:** HIGH - Quick Win  
**Estimated Effort:** 4-6 hours

---

## 🎯 **OBJECTIVE**

Import benchmark hull data from `.plan/app-docs/templates/MLData/` to unlock 3 major catalog features:

1. ✅ **Benchmark Hulls** (9 vessels) - CSV ready
2. ✅ **Test Conditions** (19 scenarios) - CSV ready
3. ✅ **Wageningen B-Series** (33 coefficients) - CSV ready
4. ⏭️ **IGES Geometry** - Future (needs OpenCascade.js or three-iges-loader)

---

## 📦 **WHAT WE HAVE (Ready Now)**

### **1. benchmark_hulls.txt**
- 9 benchmark vessels (KVLCC2, KCS, DTMB5415, etc.)
- Full particulars (Lpp, B, T, Cb, displacement)
- Both full-scale and model-scale data
- Sources: SIMMAN 2008/2014/2020, DARPA

### **2. benchmark_test_conditions.txt**
- 19 validation test scenarios
- Resistance, self-propulsion, turning, seakeeping
- Froude numbers, Reynolds numbers, wave conditions
- ITTC and SIMMAN standards

### **3. wageningen_coefficients.txt + wageningen_parameters.txt**
- 33 polynomial terms for KT and KQ
- Complete B-series calculation formulas
- Parameter ranges (Z, AE/A0, P/D, J)

### **4. iges_entity_types.txt**
- 31 common IGES entity types
- Reference for future IGES import
- Requires: OpenCascade.js or three-iges-loader (later)

---

## 🚀 **IMPLEMENTATION PLAN (4-6 hours)**

### **Phase 1: Benchmark Hulls Import (2 hours)**

#### **Step 1.1: Create Importer Service** (30 min)

**File:** `backend/DataService/Services/Catalog/BenchmarkHullImporter.cs`

```csharp
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace DataService.Services.Catalog;

public class BenchmarkHullImporter
{
    private readonly DataDbContext _context;
    private readonly ILogger<BenchmarkHullImporter> _logger;
    private readonly string _dataPath;

    public BenchmarkHullImporter(
        DataDbContext context,
        IConfiguration configuration,
        ILogger<BenchmarkHullImporter> logger)
    {
        _context = context;
        _logger = logger;
        _dataPath = configuration["DataPath"] ?? "Data";
    }

    public async Task ImportAsync(CancellationToken cancellationToken = default)
    {
        var csvPath = Path.Combine(_dataPath, "templates/MLData/benchmark_hulls.txt");
        
        if (!File.Exists(csvPath))
        {
            _logger.LogWarning("Benchmark hulls file not found: {Path}", csvPath);
            return;
        }

        _logger.LogInformation("[BENCHMARK] Importing benchmark hulls from {Path}", csvPath);

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        var records = csv.GetRecords<BenchmarkHullCsvRow>().ToList();
        var imported = 0;

        foreach (var record in records)
        {
            // Check if already exists
            var existing = await _context.CatalogVessels
                .FirstOrDefaultAsync(v => v.Name == record.Hull_Name, cancellationToken);

            if (existing != null)
            {
                _logger.LogDebug("Benchmark hull already exists: {Name}", record.Hull_Name);
                continue;
            }

            var vessel = new CatalogVessel
            {
                Id = Guid.NewGuid(),
                Name = record.Hull_Name,
                VesselType = record.Type,
                LppM = record.Length_PP_m,
                BeamM = record.Beam_m,
                DraftM = record.Draft_m,
                DisplacementM3 = record.Displacement_m3,
                BlockCoefficient = record.Block_Coefficient,
                LcbFromMidship = record.LCB_from_midship_m,
                Source = record.Data_Source,
                IsTemplate = true,
                IsBenchmark = true,
                HasGeometry = false, // Will be true when IGES imported
                CreatedAt = DateTime.UtcNow
            };

            await _context.CatalogVessels.AddAsync(vessel, cancellationToken);
            imported++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[BENCHMARK] ✅ Imported {Count} benchmark hulls", imported);
    }
}

// CSV row model
public class BenchmarkHullCsvRow
{
    public string Hull_Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Length_PP_m { get; set; }
    public decimal Beam_m { get; set; }
    public decimal Draft_m { get; set; }
    public decimal Displacement_m3 { get; set; }
    public decimal Block_Coefficient { get; set; }
    public decimal LCB_from_midship_m { get; set; }
    public string Scale { get; set; } = string.Empty;
    public decimal Full_Scale_Length_m { get; set; }
    public string Data_Source { get; set; } = string.Empty;
}
```

#### **Step 1.2: Register Service** (5 min)

**File:** `backend/DataService/Program.cs`

```csharp
// Add to service registration section
builder.Services.AddScoped<BenchmarkHullImporter>();
```

#### **Step 1.3: Add to Seeding** (10 min)

**File:** `backend/DataService/Data/Seeds/CatalogSeeder.cs`

```csharp
public class CatalogSeeder
{
    private readonly BenchmarkHullImporter _benchmarkImporter;
    
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Existing seeding...
        await SeedWigleyHull(cancellationToken);
        await SeedWaterProperties(cancellationToken);
        
        // NEW: Benchmark hulls
        await _benchmarkImporter.ImportAsync(cancellationToken);
    }
}
```

#### **Step 1.4: Test** (15 min)

```bash
cd backend/DataService
dotnet ef database update

# Check logs for:
# [BENCHMARK] ✅ Imported 9 benchmark hulls

# Verify in database:
docker exec -it navarch-studio-postgres-1 psql -U postgres -d sri_template_dev
SELECT name, vessel_type, lpp_m, cb, source, is_benchmark FROM catalog_real.vessels WHERE is_benchmark = true;
```

**Expected Output:** 9 rows (KVLCC2, KCS, DTMB5415, etc.)

---

### **Phase 2: Test Conditions Import (1.5 hours)**

#### **Step 2.1: Create Schema** (15 min)

**Migration:** Add `benchmark_test_conditions` table

```sql
CREATE TABLE catalog_real.benchmark_test_conditions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    test_type TEXT NOT NULL,
    hull_name TEXT NOT NULL,
    speed_knots DECIMAL(6,2) NOT NULL,
    froude_number DECIMAL(6,4) NOT NULL,
    reynolds_number DECIMAL(12,2) NOT NULL,
    wave_height_m DECIMAL(6,2) DEFAULT 0,
    wave_period_s DECIMAL(6,2) DEFAULT 0,
    heading_deg DECIMAL(5,1) DEFAULT 0,
    description TEXT,
    standard TEXT NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW() NOT NULL
);

CREATE INDEX idx_test_conditions_hull ON catalog_real.benchmark_test_conditions(hull_name);
CREATE INDEX idx_test_conditions_type ON catalog_real.benchmark_test_conditions(test_type);
```

#### **Step 2.2: Create Model** (10 min)

**File:** `backend/Shared/Models/BenchmarkTestCondition.cs`

```csharp
namespace Shared.Models;

[Table("benchmark_test_conditions", Schema = "catalog_real")]
public class BenchmarkTestCondition
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string TestType { get; set; } = string.Empty;
    
    [Required]
    public string HullName { get; set; } = string.Empty;
    
    public decimal SpeedKnots { get; set; }
    public decimal FroudeNumber { get; set; }
    public decimal ReynoldsNumber { get; set; }
    public decimal WaveHeightM { get; set; }
    public decimal WavePeriodS { get; set; }
    public decimal HeadingDeg { get; set; }
    public string? Description { get; set; }
    
    [Required]
    public string Standard { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
}
```

#### **Step 2.3: Create Importer** (30 min)

**File:** `backend/DataService/Services/Catalog/BenchmarkTestImporter.cs`

```csharp
public class BenchmarkTestImporter
{
    private readonly DataDbContext _context;
    private readonly ILogger<BenchmarkTestImporter> _logger;
    private readonly string _dataPath;

    public async Task ImportAsync(CancellationToken cancellationToken = default)
    {
        var csvPath = Path.Combine(_dataPath, "templates/MLData/benchmark_test_conditions.txt");
        
        if (!File.Exists(csvPath))
        {
            _logger.LogWarning("Benchmark test conditions file not found: {Path}", csvPath);
            return;
        }

        _logger.LogInformation("[BENCHMARK] Importing test conditions from {Path}", csvPath);

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        var records = csv.GetRecords<BenchmarkTestCsvRow>().ToList();
        var imported = 0;

        foreach (var record in records)
        {
            var test = new BenchmarkTestCondition
            {
                Id = Guid.NewGuid(),
                TestType = record.Test_Type,
                HullName = record.Hull,
                SpeedKnots = record.Speed_knots,
                FroudeNumber = record.Froude_Number,
                ReynoldsNumber = record.Reynolds_Number,
                WaveHeightM = record.Wave_Height_m,
                WavePeriodS = record.Wave_Period_s,
                HeadingDeg = record.Heading_deg,
                Description = record.Description,
                Standard = record.Standard,
                CreatedAt = DateTime.UtcNow
            };

            await _context.BenchmarkTestConditions.AddAsync(test, cancellationToken);
            imported++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[BENCHMARK] ✅ Imported {Count} test conditions", imported);
    }
}

public class BenchmarkTestCsvRow
{
    public string Test_Type { get; set; } = string.Empty;
    public string Hull { get; set; } = string.Empty;
    public decimal Speed_knots { get; set; }
    public decimal Froude_Number { get; set; }
    public decimal Reynolds_Number { get; set; }
    public decimal Wave_Height_m { get; set; }
    public decimal Wave_Period_s { get; set; }
    public decimal Heading_deg { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Standard { get; set; } = string.Empty;
}
```

#### **Step 2.4: Test** (10 min)

```sql
SELECT test_type, hull_name, speed_knots, froude_number, standard 
FROM catalog_real.benchmark_test_conditions;
```

**Expected:** 19 rows

---

### **Phase 3: Wageningen B-Series (1.5 hours)**

#### **Step 3.1: Create Service** (45 min)

**File:** `backend/DataService/Services/Catalog/WageningenBSeriesService.cs`

```csharp
public class WageningenBSeriesService
{
    private List<WageningenCoefficient>? _coefficients;
    private readonly ILogger<WageningenBSeriesService> _logger;
    private readonly string _dataPath;

    public async Task LoadCoefficientsAsync(CancellationToken cancellationToken = default)
    {
        var csvPath = Path.Combine(_dataPath, "templates/MLData/wageningen_coefficients.txt");
        
        if (!File.Exists(csvPath))
        {
            _logger.LogWarning("Wageningen coefficients file not found: {Path}", csvPath);
            return;
        }

        _logger.LogInformation("[WAGENINGEN] Loading B-series coefficients from {Path}", csvPath);

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        _coefficients = csv.GetRecords<WageningenCoefficient>().ToList();

        _logger.LogInformation("[WAGENINGEN] ✅ Loaded {Count} coefficient terms", _coefficients.Count);
    }

    public PropellerPerformance CalculatePerformance(
        double J, 
        int Z, 
        double AeA0, 
        double PD)
    {
        if (_coefficients == null)
            throw new InvalidOperationException("Coefficients not loaded");

        double KT = 0, KQ = 0;

        foreach (var coeff in _coefficients)
        {
            var term = coeff.C_KT * Math.Pow(J, coeff.s) * Math.Pow(AeA0, coeff.t)
                     * Math.Pow(PD, coeff.u) * Math.Pow(Z, coeff.v);
            KT += term;

            term = coeff.C_KQ * Math.Pow(J, coeff.s) * Math.Pow(AeA0, coeff.t)
                 * Math.Pow(PD, coeff.u) * Math.Pow(Z, coeff.v);
            KQ += term;
        }

        var efficiency = (J / (2 * Math.PI)) * (KT / KQ);

        return new PropellerPerformance
        {
            AdvanceCoefficient = J,
            ThrustCoefficient = KT,
            TorqueCoefficient = KQ,
            Efficiency = Math.Clamp(efficiency, 0, 1)
        };
    }
}

public class WageningenCoefficient
{
    public int Term { get; set; }
    public int s { get; set; }
    public int t { get; set; }
    public int u { get; set; }
    public int v { get; set; }
    public double C_KT { get; set; }
    public double C_KQ { get; set; }
}

public class PropellerPerformance
{
    public double AdvanceCoefficient { get; set; }
    public double ThrustCoefficient { get; set; }
    public double TorqueCoefficient { get; set; }
    public double Efficiency { get; set; }
}
```

#### **Step 3.2: Create API Endpoint** (30 min)

**File:** `backend/DataService/Controllers/PropellerController.cs`

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/propellers")]
public class PropellerController : ControllerBase
{
    private readonly WageningenBSeriesService _wageningen;

    [HttpPost("wageningen/calculate")]
    public ActionResult<PropellerPerformance> CalculateWageningen(
        [FromBody] WageningenRequest request)
    {
        try
        {
            var result = _wageningen.CalculatePerformance(
                request.J, 
                request.Z, 
                request.AeA0, 
                request.PD
            );

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record WageningenRequest(
    double J,      // Advance coefficient (0-1.5)
    int Z,         // Number of blades (2-7)
    double AeA0,   // Blade area ratio (0.3-1.05)
    double PD      // Pitch/diameter ratio (0.5-1.4)
);
```

#### **Step 3.3: Test** (15 min)

```bash
# Test calculation
curl -X POST http://localhost:5003/api/v1/propellers/wageningen/calculate \
  -H "Content-Type: application/json" \
  -d '{"J":0.7, "Z":4, "AeA0":0.55, "PD":1.0}'

# Expected response:
# {
#   "advanceCoefficient": 0.7,
#   "thrustCoefficient": 0.2813,
#   "torqueCoefficient": 0.0426,
#   "efficiency": 0.657
# }
```

---

## ✅ **CHECKLIST**

### **Before Starting:**
- [ ] Verify CSV files exist in `.plan/app-docs/templates/MLData/`
- [ ] Backup database (optional)
- [ ] Create feature branch: `git checkout -b feature/benchmark-catalog-data`

### **Implementation:**
- [ ] Phase 1: Benchmark hulls importer (2h)
- [ ] Phase 2: Test conditions importer (1.5h)
- [ ] Phase 3: Wageningen B-series service (1.5h)
- [ ] Update seeding to call all importers
- [ ] Run migrations
- [ ] Test all data loaded
- [ ] Verify counts: 9 hulls, 19 tests, 33 coefficients

### **After Completion:**
- [ ] Run pre-commit checks
- [ ] Update catalog feature docs
- [ ] Update README with new data
- [ ] Commit changes
- [ ] Update `.plan/features/05-CATALOG-FEATURES.md` status

---

## 📊 **IMPACT**

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| **Catalog Vessels** | 6 | 15 (6+9) | +150% |
| **With Geometry** | 1 (Wigley) | 1 (IGES later) | Same for now |
| **Test Scenarios** | 0 | 19 | ✅ New! |
| **Propeller Data** | Demo (4 pts) | Production (33 terms) | ✅ Complete! |
| **Catalog Completion** | 53% | ~75% | +22% |

---

## 🎯 **SUCCESS CRITERIA**

### **Complete When:**
- ✅ 9 benchmark hulls in catalog (KVLCC2, KCS, DTMB5415, etc.)
- ✅ 19 test conditions stored
- ✅ Wageningen API endpoint working
- ✅ Can calculate propeller performance
- ✅ All data queryable via UI
- ✅ Seeding runs without errors
- ✅ Tests pass

---

## 🚀 **NEXT STEPS**

**TODAY (Quick Win - 4-6 hours):**
1. Implement benchmark data import
2. Test and verify
3. Update catalog browser to show benchmark hulls
4. Mark as "Reference Data" badge

**THIS WEEK (When Ready):**
5. Create propeller selector UI
6. Add "Compare with Test Data" feature in resistance module
7. Display test conditions in hull detail page

**LATER (Requires Library):**
8. Implement IGES import with OpenCascade.js
9. Add 3D viewer for hulls with geometry
10. Extract stations/waterlines from IGES

---

## 💬 **SUMMARY**

**Ready to import RIGHT NOW:**
- ✅ 9 benchmark hulls (CSV)
- ✅ 19 test conditions (CSV)
- ✅ 33 Wageningen coefficients (CSV)

**Future (needs library):**
- ⏭️ IGES geometry parsing (OpenCascade.js or three-iges-loader)
- ⏭️ 3D visualization (Three.js)
- ⏭️ Automated geometry extraction

**This unblocks 3 major TODOs in one go!** 🎉

**Time to implement:** 4-6 hours  
**Value delivered:** Production-ready propeller calculations + benchmark validation framework

**Let's get this data into the system!** 🚀
