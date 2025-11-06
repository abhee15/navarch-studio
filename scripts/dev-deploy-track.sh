#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 NavArch Studio Dev Deployment Tracker${NC}"
echo "==========================================="
echo ""

# Step-by-step deployment checklist
echo -e "${BLUE}📋 Deployment Progress Checklist:${NC}"
echo ""

# Step 1: Infrastructure
echo -e "${BLUE}[1/7] Infrastructure${NC}"
if docker ps | grep -q "navarch-studio-postgres.*healthy"; then
  echo -e "${GREEN}   ✅ PostgreSQL: Running & Healthy${NC}"
else
  echo -e "${RED}   ❌ PostgreSQL: Not healthy${NC}"
  echo -e "${YELLOW}      → Run: docker-compose up -d postgres${NC}"
fi

if docker ps | grep -q "navarch-studio-redis.*healthy"; then
  echo -e "${GREEN}   ✅ Redis: Running & Healthy${NC}"
else
  echo -e "${RED}   ❌ Redis: Not healthy${NC}"
  echo -e "${YELLOW}      → Run: docker-compose up -d redis${NC}"
fi
echo ""

# Step 2: Migrations
echo -e "${BLUE}[2/7] Database Migrations${NC}"
if docker exec navarch-studio-postgres-1 psql -U postgres -d sri_template_dev -c "SELECT 1" > /dev/null 2>&1; then
  echo -e "${GREEN}   ✅ Database: sri_template_dev exists${NC}"
  
  # Check schemas
  SCHEMAS=$(docker exec navarch-studio-postgres-1 psql -U postgres -d sri_template_dev -t -c "SELECT nspname FROM pg_namespace WHERE nspname IN ('data', 'sizing', 'catalog_user', 'catalog_real', 'catalog_ml');" 2>/dev/null | wc -l)
  if [ "$SCHEMAS" -eq "5" ]; then
    echo -e "${GREEN}   ✅ Schemas: All 5 created (data, sizing, catalog_user, catalog_real, catalog_ml)${NC}"
  else
    echo -e "${YELLOW}   ⚠️  Schemas: Only $SCHEMAS/5 found${NC}"
    echo -e "${YELLOW}      → Run migrations: cd backend/DataService && dotnet ef database update${NC}"
  fi
else
  echo -e "${RED}   ❌ Database: Not created${NC}"
  echo -e "${YELLOW}      → Run migrations: cd backend/HullSizingService && dotnet ef database update${NC}"
  echo -e "${YELLOW}      → Then: cd backend/DataService && dotnet ef database update${NC}"
fi
echo ""

# Step 3: Catalog Data
echo -e "${BLUE}[3/7] Catalog Data Seeding${NC}"

# ML Catalog
PARAM_COUNT=$(docker exec navarch-studio-postgres-1 psql -U postgres -d sri_template_dev -t -c "SELECT COUNT(*) FROM catalog_ml.parametric_hulls;" 2>/dev/null | tr -d ' ')
if [ ! -z "$PARAM_COUNT" ] && [ "$PARAM_COUNT" -gt "0" ]; then
  echo -e "${GREEN}   ✅ ML Catalog: $PARAM_COUNT parametric hulls${NC}"
else
  echo -e "${YELLOW}   ⚠️  ML Catalog: No data (seeds on DataService startup)${NC}"
fi

# Real Catalog
REAL_COUNT=$(docker exec navarch-studio-postgres-1 psql -U postgres -d sri_template_dev -t -c "SELECT COUNT(*) FROM catalog_user.vessels_real;" 2>/dev/null | tr -d ' ')
if [ ! -z "$REAL_COUNT" ] && [ "$REAL_COUNT" -gt "0" ]; then
  echo -e "${GREEN}   ✅ Real Catalog: $REAL_COUNT vessels${NC}"
  
  # Check benchmark
  BENCHMARK_COUNT=$(docker exec navarch-studio-postgres-1 psql -U postgres -d sri_template_dev -t -c "SELECT COUNT(*) FROM catalog_user.vessels_real WHERE data_quality = 'Reference';" 2>/dev/null | tr -d ' ')
  if [ ! -z "$BENCHMARK_COUNT" ] && [ "$BENCHMARK_COUNT" -eq "9" ]; then
    echo -e "${GREEN}      → Benchmark: $BENCHMARK_COUNT reference hulls (KVLCC2, KCS, etc.)${NC}"
  else
    echo -e "${YELLOW}      → Benchmark: $BENCHMARK_COUNT/9 reference hulls (seeded via migration)${NC}"
  fi
else
  echo -e "${YELLOW}   ⚠️  Real Catalog: No data (seeds on DataService startup)${NC}"
fi

# Test Conditions
TEST_COUNT=$(docker exec navarch-studio-postgres-1 psql -U postgres -d sri_template_dev -t -c "SELECT COUNT(*) FROM catalog_real.benchmark_test_conditions;" 2>/dev/null | tr -d ' ')
if [ ! -z "$TEST_COUNT" ] && [ "$TEST_COUNT" -eq "19" ]; then
  echo -e "${GREEN}   ✅ Test Conditions: $TEST_COUNT validation scenarios${NC}"
elif [ ! -z "$TEST_COUNT" ] && [ "$TEST_COUNT" -gt "0" ]; then
  echo -e "${YELLOW}   ⚠️  Test Conditions: $TEST_COUNT/19 scenarios${NC}"
else
  echo -e "${YELLOW}   ⚠️  Test Conditions: No data (seeded via migration)${NC}"
fi
echo ""

# Step 4: Backend Services
echo -e "${BLUE}[4/7] Backend Services${NC}"

check_service() {
  local name=$1
  local port=$2
  local service=$3
  
  if curl -s -f "http://localhost:${port}/health" > /dev/null 2>&1; then
    echo -e "${GREEN}   ✅ $name: Healthy${NC}"
  else
    if docker ps | grep -q "$service"; then
      echo -e "${YELLOW}   ⚠️  $name: Running but not healthy yet${NC}"
    else
      echo -e "${RED}   ❌ $name: Not running${NC}"
      echo -e "${YELLOW}      → Run: docker-compose up -d $service${NC}"
    fi
  fi
}

check_service "IdentityService  " 5001 "identity-service"
check_service "DataService      " 5003 "data-service"
check_service "HullSizingService" 5004 "hull-sizing-service"
check_service "ApiGateway       " 5002 "api-gateway"
echo ""

# Step 5: Frontend
echo -e "${BLUE}[5/7] Frontend${NC}"
if lsof -i ":5173" > /dev/null 2>&1 || netstat -an | grep -q "5173.*LISTEN"; then
  if curl -s -f "http://localhost:5173" > /dev/null 2>&1; then
    echo -e "${GREEN}   ✅ Running and responding at http://localhost:5173${NC}"
  else
    echo -e "${YELLOW}   ⚠️  Running but not responding${NC}"
  fi
else
  echo -e "${RED}   ❌ Not running${NC}"
  echo -e "${YELLOW}      → Run: cd frontend && npm run dev${NC}"
fi
echo ""

# Step 6: End-to-End Tests
echo -e "${BLUE}[6/7] Manual Verification${NC}"
echo "   Test in browser:"
echo "   1. Open http://localhost:5173"
echo "   2. Register/login"
echo "   3. Navigate to /catalog"
echo "   4. Toggle Real ↔ ML (should show 609 vs 5000)"
echo "   5. Create mission → Select ML solver (purple)"
echo "   6. Verify purple provenance panels"
echo ""

# Step 7: Summary
echo -e "${BLUE}[7/7] Deployment Status${NC}"

ALL_GOOD=true

# Check critical services
if ! docker ps | grep -q "navarch-studio-postgres.*healthy"; then ALL_GOOD=false; fi
if ! curl -s -f "http://localhost:5002/health" > /dev/null 2>&1; then ALL_GOOD=false; fi

if [ "$ALL_GOOD" = true ]; then
  echo -e "${GREEN}   ✅ DEV ENVIRONMENT READY!${NC}"
  echo ""
  echo -e "${BLUE}   🌐 Access Points:${NC}"
  echo "      Frontend: http://localhost:5173"
  echo "      API:      http://localhost:5002"
  echo "      PgAdmin:  http://localhost:5050"
  echo ""
else
  echo -e "${YELLOW}   ⚠️  ENVIRONMENT PARTIALLY READY${NC}"
  echo "      Review issues above and follow suggested fixes"
  echo ""
fi

# Next steps
echo -e "${BLUE}📝 Next Steps:${NC}"
echo "   • If services not running: docker-compose up -d"
echo "   • If frontend not running: cd frontend && npm run dev"
echo "   • View logs: ./scripts/dev-logs.sh"
echo "   • Check status: ./scripts/dev-status.sh"
echo ""

