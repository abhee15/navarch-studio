# 🚀 Deploy Dev Environment - Quick Start Guide

**Last Updated:** November 6, 2025  
**Estimated Time:** 5 minutes  
**Status:** ✅ READY TO EXECUTE

---

## ⚡ **QUICK START (3 Commands)**

```bash
# 1. Setup environment (automated)
./scripts/dev-setup.sh

# 2. Start services
docker-compose up -d

# 3. Start frontend
cd frontend && npm run dev

# DONE! Open http://localhost:5173
```

**Time:** 5 minutes total  
**Result:** Fully working dev environment

---

## 📦 **WHAT YOU'RE DEPLOYING**

### **Catalogs (Total: 5,609+ items):**
- ✅ **Real-World:** 600 curated vessels
- ✅ **Benchmark:** 9 reference hulls (KVLCC2, KCS, DTMB5415, etc.)
- ✅ **ML/Parametric:** 5,000 synthetic hulls (MIT ShipD)
- ✅ **Test Scenarios:** 19 validation conditions
- ✅ **Propeller Data:** Wageningen B-series (33 coefficients)

### **Features:**
- ✅ **3 Solver Modes:** First-Principles, Real-World, ML/Parametric
- ✅ **Unified Catalog:** Real/ML toggle with permissions
- ✅ **Hydrostatics:** Full workspace
- ✅ **Resistance:** Holtrop-Mennen
- ✅ **Propeller Calculator:** Wageningen B-series
- ✅ **Redis Caching:** <5ms repeat queries

### **Infrastructure:**
- ✅ PostgreSQL (5 schemas, 15+ tables, 50+ indexes)
- ✅ Redis (distributed cache)
- ✅ Docker compose (health checks)
- ✅ Fresh migrations (no legacy conflicts)

---

## 🗄️ **DATABASE SCHEMAS (Auto-Created)**

| Schema | Tables | Purpose |
|--------|--------|---------|
| `data` | 10+ | Hydrostatics, vessels, loadcases, resistance |
| `sizing` | 5+ | Mission cases, sizing runs, candidates |
| `catalog_user` | 2 | Real-world vessels (600+9) |
| `catalog_real` | 2 | Benchmark test conditions, water properties |
| `catalog_ml` | 1 | Parametric hulls (5K-82K) |

---

## 🔍 **VERIFICATION STEPS**

### **Step 1: Check Environment Status**
```bash
./scripts/dev-status.sh
```

**Expected Output:**
```
🐳 Docker Services:
   ✅ PostgreSQL: Running
   ✅ Redis: Running

🗄️ Database Status:
   ✅ PostgreSQL: Running
      Database: sri_template_dev exists
      ML Catalog: 5000 hulls
      Real Catalog: 609 vessels (600 curated + 9 benchmark)
   ✅ Redis: Running

🏥 Service Health:
   ✅ IdentityService   (port 5001): Healthy
   ✅ ApiGateway        (port 5002): Healthy
   ✅ DataService       (port 5003): Healthy
   ✅ HullSizingService (port 5004): Healthy

🎨 Frontend:
   ✅ Running on http://localhost:5173
```

---

### **Step 2: Test in Browser**

**2.1 Login:**
- Navigate to http://localhost:5173
- Register new user: test@example.com / Test1234!
- Login

**2.2 Catalog Toggle:**
- Navigate to /catalog
- See Real-World mode (green) active
- Stats show 609 vessels
- Toggle to ML/Parametric (purple)
- Stats show 5,000 hulls
- Test filters and pagination

**2.3 ML Solver:**
- Navigate to /sizing/wizard
- Create mission
- Step 4: Select ML/Parametric mode (purple 🤖 card)
- Generate candidates
- Verify purple provenance panels show:
  - Parametric hull ID (e.g., "CS1_00123")
  - Similarity score
  - MIT ShipD Dataset badge

**2.4 Propeller Calculator:**
```bash
curl -X POST http://localhost:5002/api/v1/propellers/wageningen/calculate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"J":0.7, "Z":4, "AeA0":0.55, "PD":1.0}'

# Expected:
# {"advanceCoefficient":0.7,"thrustCoefficient":0.2813,"torqueCoefficient":0.0426,"efficiency":0.657,...}
```

---

## 🐛 **TROUBLESHOOTING**

### **Issue: Services Not Starting**

```bash
# Check logs
./scripts/dev-logs.sh

# Or specific service:
docker-compose logs data-service
```

---

### **Issue: Database Empty**

```bash
# Reapply migrations
cd backend/DataService
dotnet ef database update

# Check logs for seeding
# Look for:
# [SEED] ✅ Parametric catalog seeded successfully! Imported: 5000 hulls
# [SEED] ✅ Benchmark hulls: 9 found (seeded via migration)
```

---

### **Issue: Need Fresh Start**

```bash
# Nuclear reset
./scripts/dev-reset.sh

# Then setup again
./scripts/dev-setup.sh
```

---

## 📊 **DATA VERIFICATION QUERIES**

```bash
# Connect to database
docker exec -it navarch-studio-postgres-1 psql -U postgres -d sri_template_dev
```

```sql
-- Check all schemas
\dn

-- Count ML catalog
SELECT COUNT(*) FROM catalog_ml.parametric_hulls;
-- Expected: 5000

-- Count real catalog
SELECT COUNT(*) FROM catalog_user.vessels_real;
-- Expected: 609 (600 curated + 9 benchmark)

-- List benchmark hulls
SELECT vessel_id, vessel_type, lpp_m, cb, source 
FROM catalog_user.vessels_real 
WHERE data_quality = 'Reference';
-- Expected: 9 rows (KVLCC2, KCS, etc.)

-- List test conditions
SELECT test_type, hull_name, froude_number, standard 
FROM catalog_real.benchmark_test_conditions;
-- Expected: 19 rows

-- Exit
\q
```

---

## 🎯 **SUCCESS CRITERIA**

### **Environment is Ready When:**
- [x] All 4 services show (healthy)
- [x] Database has all 5 schemas
- [x] ML catalog has 5,000 hulls
- [x] Real catalog has 609 vessels
- [x] Benchmark test conditions: 19 rows
- [x] Redis ping responds PONG
- [x] Frontend loads without errors
- [x] Can register and login
- [x] Catalog toggle works
- [x] ML solver generates candidates
- [x] Propeller API returns results

**ALL CRITERIA MET = READY FOR DEVELOPMENT! ✅**

---

## 🎓 **WHAT'S INCLUDED**

### **Seeded Automatically:**
1. ✅ 600 curated vessels (VesselCatalogSeeder)
2. ✅ 9 benchmark hulls (Migration 20251106211500)
3. ✅ 19 test conditions (Migration 20251106211500)
4. ✅ 5,000 parametric hulls (ParametricCatalogSeeder)
5. ✅ 33 Wageningen coefficients (Hardcoded constants)
6. ✅ Water properties (ITTC data)

### **Available Features:**
- Hydrostatics workspace
- Hull sizing (3 solver modes)
- Resistance & powering
- Catalog browsing (Real/ML)
- Propeller calculations
- Comparison tools

---

## 💡 **DEVELOPMENT TIPS**

### **Hot Reload (Backend):**
```bash
cd backend/DataService
dotnet watch run
# Changes auto-reload!
```

### **View Logs:**
```bash
./scripts/dev-logs.sh
# Interactive menu for all services
```

### **Check Status Anytime:**
```bash
./scripts/dev-status.sh
# Shows complete environment health
```

### **Reset Everything:**
```bash
./scripts/dev-reset.sh  # Warning: deletes all data!
./scripts/dev-setup.sh  # Fresh start
```

---

## 🎊 **READY TO DEPLOY!**

**Your dev environment setup is:**
- ✅ Fully automated
- ✅ Self-contained (no file deps)
- ✅ Production-ready
- ✅ Thoroughly documented
- ✅ Easy to troubleshoot
- ✅ Quick to deploy (5 min)

**Next Command:**
```bash
./scripts/dev-setup.sh
```

**Let's ship it! 🚀**

