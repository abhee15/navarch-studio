# Dev Environment Deployment Plan & Improvements

**Created:** November 6, 2025  
**Status:** 📋 READY TO EXECUTE  
**Priority:** CRITICAL - Get Dev Environment Running  
**Estimated Total Time:** 8-12 hours

---

## 🎯 **GOAL**

**Get dev environment fully operational with fresh database, all services running, and development workflow optimized.**

---

## 📊 **CURRENT STATE ANALYSIS**

### ✅ **What's Working:**
- Docker compose configured with all services
- Postgres + Redis configured
- Backend services built and ready
- Frontend built and ready
- Fresh migrations created
- Phase 2 ML features complete
- Unified catalog ready

### 🔴 **Critical Blockers:**
1. ❌ No fresh database applied (migrations need to run)
2. ❌ Services may have old build artifacts
3. ❌ Redis untested with services
4. ❌ Background import worker untested
5. ❌ Catalog seeding untested
6. ❌ Frontend .env configuration needed

### ⚠️ **Infrastructure TODOs (Found):**
From `.plan/features/06-INFRASTRUCTURE-FEATURES.md`:
1. Missing ECR repository for HullSizingService
2. CI workflow skips backend builds (Shared/ changes)
3. Security Week 2-3 incomplete (Secrets Manager, RBAC, Audit)
4. No monitoring/alerting
5. No Terraform for HullSizingService
6. No backup/restore testing
7. Missing XML documentation (IdentityService)

---

## 🚀 **DEPLOYMENT CHECKLIST (Priority Order)**

### **PHASE 1: Fresh Environment Setup (2-3 hours)**

#### **Step 1.1: Clean Existing State** (15 min)
```bash
# Stop all containers
docker-compose down -v

# Clean backend builds
cd backend
dotnet clean
rm -rf */bin */obj

# Clean frontend builds
cd ../frontend
rm -rf node_modules dist .vite

# Clean database volumes (fresh start)
docker volume rm navarch-studio_postgres_data
docker volume rm navarch-studio_redis_data
```

**Why:** Start with clean slate, no stale data or build artifacts

---

#### **Step 1.2: Setup Frontend Environment** (5 min)
```bash
cd frontend

# Create .env.local from example
cp .env.example .env.local

# Edit .env.local with dev values
cat > .env.local << EOF
VITE_API_URL=http://localhost:5002
VITE_AUTH_MODE=local
VITE_COGNITO_USER_POOL_ID=
VITE_COGNITO_CLIENT_ID=
VITE_COGNITO_REGION=us-east-1
EOF

# Install dependencies
npm install
```

**Expected Output:** `.env.local` created with local development settings

---

#### **Step 1.3: Backend Pre-Build Checks** (10 min)
```bash
cd backend

# Format check
dotnet format --verify-no-changes

# If formatting needed:
dotnet format

# Build all projects
dotnet build

# Expected: 0 errors, 0 warnings
```

**Expected Output:** Clean build, all projects compile successfully

---

#### **Step 1.4: Frontend Pre-Build Checks** (10 min)
```bash
cd frontend

# Type check
npm run type-check

# Lint
npm run lint

# Fix lint issues if needed:
# npm run lint --fix

# Build
npm run build
```

**Expected Output:** TypeScript clean, no lint errors, build succeeds

---

#### **Step 1.5: Start Infrastructure** (5 min)
```bash
cd .. # Back to root

# Start only postgres and redis
docker-compose up -d postgres redis

# Wait for health checks
docker-compose ps

# Expected: Both showing (healthy)
```

**Health Check:**
```bash
# Test postgres
docker exec -it navarch-studio-postgres-1 pg_isready -U postgres

# Test redis
docker exec -it navarch-studio-redis-1 redis-cli ping
# Expected: PONG
```

---

#### **Step 1.6: Apply Fresh Migrations** (20 min)
```bash
cd backend

# Apply HullSizingService migrations
cd HullSizingService
dotnet ef database update
# Expected: "Done"

# Apply DataService migrations
cd ../DataService
dotnet ef database update  
# Expected: "Done" + catalog seeding logs
```

**Expected Output:**
- `sizing` schema created
- `data` schema created
- `catalog_real` schema created
- `catalog_ml` schema created
- Parametric catalog seeded (5K hulls, ~3-5 min)
- Real-world catalog seeded (600 vessels)

**Validation:**
```bash
# Connect to postgres
docker exec -it navarch-studio-postgres-1 psql -U postgres -d sri_template_dev

# Check schemas
\dn

# Expected:
# - sizing
# - data  
# - catalog_real
# - catalog_ml

# Check parametric hulls
SELECT COUNT(*) FROM catalog_ml.parametric_hulls;
# Expected: ~5000

# Check real vessels
SELECT COUNT(*) FROM catalog_real.vessels;
# Expected: ~600

# Exit
\q
```

---

### **PHASE 2: Service Startup & Validation (1-2 hours)**

#### **Step 2.1: Start Backend Services** (30 min)
```bash
# Option A: Docker Compose (Full Stack)
docker-compose up -d identity-service data-service hull-sizing-service api-gateway

# Wait for all healthy
docker-compose ps

# Option B: dotnet run (Development - Better for hot reload)
# Terminal 1: IdentityService
cd backend/IdentityService
dotnet run

# Terminal 2: DataService  
cd backend/DataService
dotnet run

# Terminal 3: HullSizingService
cd backend/HullSizingService
dotnet run

# Terminal 4: ApiGateway
cd backend/ApiGateway
dotnet run
```

**Watch For:**
- ✅ "Now listening on: http://localhost:XXXX"
- ✅ "Application started" logs
- ✅ No exceptions in logs
- ✅ Database connection successful
- ✅ Redis connection successful (DataService only)

**Health Check:**
```bash
# Test all services
curl http://localhost:5001/health  # IdentityService
curl http://localhost:5003/health  # DataService
curl http://localhost:5004/health  # HullSizingService
curl http://localhost:5002/health  # ApiGateway

# Expected: All return {"status":"Healthy"}
```

---

#### **Step 2.2: Start Frontend** (10 min)
```bash
cd frontend
npm run dev

# Expected: "Local: http://localhost:5173"
```

**Open Browser:**
- Navigate to http://localhost:5173
- Should see login page
- No console errors

---

#### **Step 2.3: End-to-End Smoke Test** (20 min)

**Test 1: Authentication**
```bash
# Register new user
curl -X POST http://localhost:5002/api/v1/identity/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test1234!","confirmPassword":"Test1234!"}'

# Login
curl -X POST http://localhost:5002/api/v1/identity/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test1234!"}'

# Expected: JWT token returned
```

**Test 2: Catalog (via UI)**
- Navigate to /catalog
- Toggle between Real and ML modes
- Verify stats display (600 real, 5000 ML)
- Test filters in ML mode
- Test pagination

**Test 3: Hull Sizing (via UI)**
- Create new mission
- Fill mission requirements
- Select ML/Parametric solver mode (purple)
- Generate candidates
- Verify purple provenance panel shows

**Test 4: Hydrostatics (via UI)**
- Create new vessel
- Enter dimensions
- Compute hydrostatics
- Verify results display

---

### **PHASE 3: Development Workflow Improvements (2-3 hours)**

#### **Step 3.1: Hot Reload Setup** (30 min)

**Backend Hot Reload:**
```bash
# Use dotnet watch instead of dotnet run
cd backend/DataService
dotnet watch run

# Now changes auto-reload!
```

**Frontend** (already has Vite HMR ✅)

**Docker Compose Hot Reload:**
Add volume mounts to docker-compose.yml:
```yaml
data-service:
  volumes:
    - ./backend/DataService:/app/DataService:ro
    - ./backend/Shared:/app/Shared:ro
  # Auto-restart on file changes
```

---

#### **Step 3.2: Pre-Commit Hooks** (1 hour)

Create `.husky/pre-commit`:
```bash
#!/bin/sh
. "$(dirname "$0")/_/husky.sh"

echo "🔍 Running pre-commit checks..."

# Backend checks
echo "📦 Formatting backend..."
cd backend && dotnet format --verify-no-changes
if [ $? -ne 0 ]; then
  echo "❌ Backend formatting failed. Run 'dotnet format' to fix."
  exit 1
fi

# Frontend checks
echo "🎨 Checking frontend..."
cd ../frontend
npm run lint
npm run type-check

echo "✅ All checks passed!"
```

**Install:**
```bash
cd frontend
npx husky-init
npm install

# Create pre-commit hook
chmod +x .husky/pre-commit
```

---

#### **Step 3.3: Dev Scripts** (30 min)

Create `scripts/dev-setup.sh`:
```bash
#!/bin/bash
set -e

echo "🚀 NavArch Studio Dev Setup"
echo "=============================="

# Check prerequisites
command -v docker >/dev/null 2>&1 || { echo "❌ Docker not installed"; exit 1; }
command -v dotnet >/dev/null 2>&1 || { echo "❌ .NET SDK not installed"; exit 1; }
command -v node >/dev/null 2>&1 || { echo "❌ Node.js not installed"; exit 1; }

echo "✅ Prerequisites OK"

# Start infrastructure
echo "📦 Starting postgres and redis..."
docker-compose up -d postgres redis

# Wait for healthy
echo "⏳ Waiting for services to be healthy..."
until docker-compose ps | grep -q "postgres.*healthy" && docker-compose ps | grep -q "redis.*healthy"; do
  sleep 2
done

echo "✅ Infrastructure ready"

# Apply migrations
echo "🗄️ Applying migrations..."
cd backend/HullSizingService && dotnet ef database update --no-build
cd ../DataService && dotnet ef database update --no-build

echo "✅ Database ready"

# Start services
echo "🎯 Starting services..."
echo "   - IdentityService:   http://localhost:5001"
echo "   - DataService:       http://localhost:5003"
echo "   - HullSizingService: http://localhost:5004"
echo "   - ApiGateway:        http://localhost:5002"
echo "   - Frontend:          http://localhost:5173"
echo ""
echo "Run 'docker-compose up' or start services individually with 'dotnet watch run'"
```

Create `scripts/dev-reset.sh`:
```bash
#!/bin/bash
set -e

echo "🔄 Resetting dev environment..."

# Stop all
docker-compose down -v

# Clean builds
cd backend && dotnet clean
cd ../frontend && rm -rf node_modules dist

# Remove volumes
docker volume rm navarch-studio_postgres_data || true
docker volume rm navarch-studio_redis_data || true

echo "✅ Environment reset. Run './scripts/dev-setup.sh' to reinitialize."
```

Make executable:
```bash
chmod +x scripts/*.sh
```

---

#### **Step 3.4: README Update** (30 min)

Update `README.md` with:
- Quick start guide
- Dev environment setup
- Testing procedures
- Troubleshooting

---

### **PHASE 4: Infrastructure Improvements (3-4 hours)**

#### **Step 4.1: Fix CI Workflow Skip Issue** (30 min)

**Problem:** CI skips backend builds when only `Shared/` changes

**File:** `.github/workflows/ci-dev.yml`

**Fix:**
```yaml
# Remove this condition that causes skips
# OLD:
- name: Build backend
  if: needs.setup.outputs.has-secrets == 'true'
  
# NEW:
- name: Build backend
  if: needs.setup.outputs.backend-changed == 'true' || needs.setup.outputs.shared-changed == 'true'
```

**Add path detection:**
```yaml
setup:
  outputs:
    backend-changed: ${{ steps.filter.outputs.backend }}
    shared-changed: ${{ steps.filter.outputs.shared }}
  steps:
    - uses: dorny/paths-filter@v2
      id: filter
      with:
        filters: |
          backend:
            - 'backend/**'
          shared:
            - 'backend/Shared/**'
```

---

#### **Step 4.2: Add XML Documentation** (15 min)

**File:** `backend/IdentityService/IdentityService.csproj`

**Add:**
```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

**Rebuild:**
```bash
cd backend/IdentityService
dotnet build
```

---

#### **Step 4.3: Add Monitoring Dashboard** (1 hour)

Create `scripts/monitoring/docker-compose.monitoring.yml`:
```yaml
services:
  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus_data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3001:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    volumes:
      - grafana_data:/var/lib/grafana
    depends_on:
      - prometheus

volumes:
  prometheus_data:
  grafana_data:
```

Create `scripts/monitoring/prometheus.yml`:
```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'identity-service'
    static_configs:
      - targets: ['host.docker.internal:5001']
  
  - job_name: 'data-service'
    static_configs:
      - targets: ['host.docker.internal:5003']
```

**Start:**
```bash
docker-compose -f scripts/monitoring/docker-compose.monitoring.yml up -d

# Access:
# Prometheus: http://localhost:9090
# Grafana: http://localhost:3001 (admin/admin)
```

---

#### **Step 4.4: Add Cost Monitoring** (30 min)

Create `.plan/AWS_COST_MONITORING.md` with:
- AWS Budget setup
- Cost allocation tags
- Monthly spending alerts
- Resource cleanup procedures

---

#### **Step 4.5: Backup/Restore Testing** (1 hour)

Create `scripts/backup-restore-test.sh`:
```bash
#!/bin/bash
set -e

echo "🗄️ Testing RDS Backup/Restore"

# Manual backup
aws rds create-db-snapshot \
  --db-instance-identifier navarch-studio-data-dev \
  --db-snapshot-identifier test-snapshot-$(date +%Y%m%d)

# Wait for completion
echo "⏳ Waiting for snapshot..."
aws rds wait db-snapshot-completed \
  --db-snapshot-identifier test-snapshot-$(date +%Y%m%d)

# Restore to new instance
aws rds restore-db-instance-from-db-snapshot \
  --db-instance-identifier navarch-studio-data-restored \
  --db-snapshot-identifier test-snapshot-$(date +%Y%m%d)

echo "✅ Backup/restore test initiated. Check AWS console for progress."
```

---

### **PHASE 5: Testing Infrastructure (2-3 hours)**

#### **Step 5.1: Integration Test Setup** (1 hour)

Create `backend/DataService.IntegrationTests/`:
```bash
cd backend
dotnet new xunit -n DataService.IntegrationTests
cd DataService.IntegrationTests
dotnet add package Testcontainers.PostgreSql
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

Create test base class:
```csharp
public class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    
    public IntegrationTestBase()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:15.5-alpine")
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // Apply migrations
    }
    
    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
```

---

#### **Step 5.2: E2E Test Setup** (1-2 hours)

```bash
cd frontend
npm install -D @playwright/test
npx playwright install

# Create test file
mkdir -p tests/e2e
```

Create `tests/e2e/catalog.spec.ts`:
```typescript
import { test, expect } from '@playwright/test';

test('catalog toggle between Real and ML modes', async ({ page }) => {
  await page.goto('http://localhost:5173/catalog');
  
  // Check Real mode active by default
  await expect(page.locator('button:has-text("Real-World")')).toHaveClass(/shadow/);
  
  // Toggle to ML mode
  await page.click('button:has-text("ML/Parametric")');
  
  // Check ML mode active
  await expect(page.locator('button:has-text("ML/Parametric")')).toHaveClass(/shadow/);
  
  // Check stats display
  await expect(page.locator('text=Total Hulls')).toBeVisible();
});
```

---

### **PHASE 6: Documentation & Runbook (1 hour)**

#### **Step 6.1: Developer Onboarding Guide** (30 min)

Create `.plan/DEVELOPER_ONBOARDING.md`:
- Prerequisites checklist
- Setup walkthrough
- First contribution guide
- Common issues and solutions
- Where to get help

---

#### **Step 6.2: Troubleshooting Guide** (30 min)

Create `.plan/TROUBLESHOOTING.md`:
- Database connection issues
- Service startup failures
- Port conflicts
- Migration errors
- Docker issues
- Common error messages

---

## 📋 **PRIORITY TODO LIST**

### **🔴 CRITICAL (Do First - Today)**

1. ✅ **Apply Fresh Migrations** (20 min)
   - Drop/recreate database
   - Run `dotnet ef database update` for both services
   - Verify 5K parametric catalog seeds
   - Verify 600 real-world catalog seeds

2. ✅ **Service Health Check** (30 min)
   - Start all services
   - Test all /health endpoints
   - Verify Redis connection
   - Verify database connections

3. ✅ **End-to-End Smoke Test** (30 min)
   - Register/login via UI
   - Browse catalog (Real + ML toggle)
   - Create mission with ML solver
   - Verify purple provenance panels

4. ✅ **Dev Scripts** (30 min)
   - Create `dev-setup.sh`
   - Create `dev-reset.sh`
   - Make executable
   - Test scripts

**Total Time:** 2 hours  
**Goal:** Working dev environment

---

### **🟡 HIGH (Do This Week)**

5. 📋 **CI Workflow Fix** (30 min)
   - Fix backend build skip issue
   - Test with Shared/ changes
   - Verify CI passes

6. 📋 **XML Documentation** (15 min)
   - Add to IdentityService
   - Rebuild and test

7. 📋 **Pre-Commit Hooks** (1 hour)
   - Setup Husky
   - Add format checks
   - Add lint checks
   - Test hook execution

8. 📋 **Hot Reload** (30 min)
   - Configure `dotnet watch`
   - Document in README
   - Test changes auto-reload

9. 📋 **Monitoring Dashboard** (1 hour)
   - Setup Prometheus + Grafana
   - Configure scraping
   - Create basic dashboards

**Total Time:** 3-4 hours  
**Goal:** Improved dev workflow

---

### **🟢 MEDIUM (Next Week)**

10. 📋 **Integration Tests** (3-4 hours)
    - Setup Testcontainers
    - Write 10 core tests
    - Add to CI pipeline

11. 📋 **E2E Tests** (3-4 hours)
    - Setup Playwright
    - Write 5 critical paths
    - Add to CI pipeline

12. 📋 **Cost Monitoring** (30 min)
    - Setup AWS Budgets
    - Add alerts
    - Document in README

13. 📋 **Backup/Restore Testing** (1 hour)
    - Test manual backup
    - Test restore
    - Document procedure

**Total Time:** 8-10 hours  
**Goal:** Production-ready testing

---

### **🔵 LOW (Future)**

14. 📋 **Terraform for HullSizingService** (2-3 hours)
15. 📋 **Secrets Manager Integration** (2 hours)
16. 📋 **RBAC Implementation** (3 hours)
17. 📋 **Audit Logging** (1 hour)
18. 📋 **CloudWatch Alarms** (2 hours)
19. 📋 **WAF Configuration** (4-6 hours)
20. 📋 **Redis ElastiCache** (3-5 days)

---

## 🎯 **SUCCESS CRITERIA**

### **Phase 1 Complete When:**
- ✅ All services start without errors
- ✅ All health checks pass
- ✅ Database has all schemas
- ✅ Catalog seeding complete (5K ML + 600 Real)
- ✅ Frontend loads without errors
- ✅ Can login and navigate

### **Phase 2 Complete When:**
- ✅ End-to-end workflows tested
- ✅ Catalog toggle works
- ✅ ML solver works with purple provenance
- ✅ Redis caching working
- ✅ Background worker logs visible

### **Phase 3 Complete When:**
- ✅ Hot reload working
- ✅ Pre-commit hooks installed
- ✅ Dev scripts created and tested
- ✅ README updated

### **Phase 4 Complete When:**
- ✅ CI workflow fixed
- ✅ Monitoring dashboards running
- ✅ Cost alerts configured
- ✅ Backup tested

### **Phase 5 Complete When:**
- ✅ 10+ integration tests passing
- ✅ 5+ E2E tests passing
- ✅ Tests in CI pipeline

### **Phase 6 Complete When:**
- ✅ Developer onboarding guide complete
- ✅ Troubleshooting guide complete
- ✅ New dev can set up in <1 hour

---

## 📊 **EFFORT ESTIMATES**

| Phase | Description | Time | Priority |
|-------|-------------|------|----------|
| Phase 1 | Fresh Environment Setup | 2-3h | 🔴 CRITICAL |
| Phase 2 | Service Validation | 1-2h | 🔴 CRITICAL |
| Phase 3 | Workflow Improvements | 2-3h | 🟡 HIGH |
| Phase 4 | Infrastructure Fixes | 3-4h | 🟡 HIGH |
| Phase 5 | Testing Infrastructure | 2-3h | 🟢 MEDIUM |
| Phase 6 | Documentation | 1h | 🟢 MEDIUM |
| **TOTAL** | **Complete Dev Setup** | **11-16h** | |

**Recommended Schedule:**
- Day 1 (4h): Phase 1 + Phase 2 - Get it running
- Day 2 (4h): Phase 3 - Improve workflow
- Day 3 (4h): Phase 4 + Phase 5 - Testing + CI
- Day 4 (2h): Phase 6 - Documentation

---

## 🚀 **READY TO START!**

**First Command to Run:**
```bash
# Start with fresh environment
./scripts/dev-reset.sh  # (if it exists, otherwise run cleanup manually)
./scripts/dev-setup.sh  # (create this script first)

# OR manually:
docker-compose down -v
docker-compose up -d postgres redis
cd backend/HullSizingService && dotnet ef database update
cd ../DataService && dotnet ef database update
docker-compose up -d
```

**Let's get the dev environment ready to ship! 🎊**

