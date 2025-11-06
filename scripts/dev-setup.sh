#!/bin/bash
set -e

echo "🚀 NavArch Studio Dev Environment Setup"
echo "=========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check prerequisites
echo -e "${BLUE}📋 Checking prerequisites...${NC}"

command -v docker >/dev/null 2>&1 || { echo -e "${RED}❌ Docker not installed${NC}"; exit 1; }
command -v dotnet >/dev/null 2>&1 || { echo -e "${RED}❌ .NET SDK not installed${NC}"; exit 1; }
command -v node >/dev/null 2>&1 || { echo -e "${RED}❌ Node.js not installed${NC}"; exit 1; }

echo -e "${GREEN}✅ Prerequisites OK${NC}"
echo ""

# Start infrastructure
echo -e "${BLUE}📦 Starting infrastructure (Postgres + Redis)...${NC}"
docker-compose up -d postgres redis

# Wait for healthy
echo -e "${YELLOW}⏳ Waiting for services to be healthy...${NC}"
MAX_WAIT=60
COUNTER=0
until docker-compose ps | grep -q "postgres.*healthy" && docker-compose ps | grep -q "redis.*healthy"; do
  sleep 2
  COUNTER=$((COUNTER+2))
  if [ $COUNTER -ge $MAX_WAIT ]; then
    echo -e "${RED}❌ Services did not become healthy in time${NC}"
    docker-compose logs postgres redis
    exit 1
  fi
  echo -n "."
done
echo ""
echo -e "${GREEN}✅ Infrastructure ready${NC}"
echo ""

# Check if databases exist
echo -e "${BLUE}🗄️ Checking database state...${NC}"
DB_EXISTS=$(docker exec navarch-studio-postgres-1 psql -U postgres -lqt | cut -d \| -f 1 | grep -w sri_template_dev | wc -l)

if [ "$DB_EXISTS" -eq "0" ]; then
  echo -e "${YELLOW}⚠️  Database does not exist, will be created during migration${NC}"
fi

# Apply migrations
echo -e "${BLUE}🔄 Applying database migrations...${NC}"
echo ""

echo -e "${BLUE}   → HullSizingService migrations${NC}"
cd backend/HullSizingService
dotnet ef database update --no-build 2>&1 | tail -n 5
cd ../..

echo -e "${BLUE}   → DataService migrations${NC}"
cd backend/DataService
echo -e "${YELLOW}   (This may take 3-5 minutes for catalog seeding...)${NC}"
dotnet ef database update --no-build 2>&1 | tail -n 10
cd ../..

echo ""
echo -e "${GREEN}✅ Database migrations complete${NC}"
echo ""

# Verify data
echo -e "${BLUE}📊 Verifying seeded data...${NC}"

PARAM_COUNT=$(docker exec navarch-studio-postgres-1 psql -U postgres -d sri_template_dev -t -c "SELECT COUNT(*) FROM catalog_ml.parametric_hulls;" 2>/dev/null | tr -d ' ')
REAL_COUNT=$(docker exec navarch-studio-postgres-1 psql -U postgres -d sri_template_dev -t -c "SELECT COUNT(*) FROM catalog_real.vessels;" 2>/dev/null | tr -d ' ')

if [ ! -z "$PARAM_COUNT" ] && [ "$PARAM_COUNT" -gt "0" ]; then
  echo -e "${GREEN}   ✅ ML Catalog: $PARAM_COUNT hulls${NC}"
else
  echo -e "${YELLOW}   ⚠️  ML Catalog: 0 hulls (check logs)${NC}"
fi

if [ ! -z "$REAL_COUNT" ] && [ "$REAL_COUNT" -gt "0" ]; then
  echo -e "${GREEN}   ✅ Real Catalog: $REAL_COUNT vessels${NC}"
else
  echo -e "${YELLOW}   ⚠️  Real Catalog: 0 vessels (check logs)${NC}"
fi

echo ""

# Service URLs
echo -e "${GREEN}✅ Dev environment ready!${NC}"
echo ""
echo -e "${BLUE}🎯 Service URLs:${NC}"
echo "   • IdentityService:   http://localhost:5001"
echo "   • DataService:       http://localhost:5003"
echo "   • HullSizingService: http://localhost:5004"
echo "   • ApiGateway:        http://localhost:5002"
echo "   • Frontend:          http://localhost:5173"
echo "   • PgAdmin:           http://localhost:5050 (admin@example.com / admin)"
echo ""

# Next steps
echo -e "${BLUE}📝 Next steps:${NC}"
echo "   1. Start backend services:"
echo "      docker-compose up -d"
echo "      OR"
echo "      cd backend/DataService && dotnet watch run"
echo ""
echo "   2. Start frontend:"
echo "      cd frontend && npm run dev"
echo ""
echo "   3. Open browser to http://localhost:5173"
echo ""

echo -e "${GREEN}🎊 Setup complete! Happy coding!${NC}"

