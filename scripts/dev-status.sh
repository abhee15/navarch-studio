#!/bin/bash

echo "📊 NavArch Studio Dev Environment Status"
echo "=========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check Docker services
echo -e "${BLUE}🐳 Docker Services:${NC}"
docker-compose ps 2>/dev/null || echo -e "${YELLOW}   ⚠️  Docker compose not running${NC}"
echo ""

# Check databases
echo -e "${BLUE}🗄️ Database Status:${NC}"

# Check if postgres is running
if docker ps | grep -q "navarch-studio-postgres"; then
  # Check connection
  if docker exec navarch-studio-postgres-1 pg_isready -U postgres > /dev/null 2>&1; then
    echo -e "${GREEN}   ✅ PostgreSQL: Running${NC}"

    # Check database exists
    DB_EXISTS=$(docker exec navarch-studio-postgres-1 psql -U postgres -lqt | cut -d \| -f 1 | grep -w sri_template_dev | wc -l)
    if [ "$DB_EXISTS" -eq "1" ]; then
      echo -e "${GREEN}      Database: sri_template_dev exists${NC}"

      # Count records
      PARAM_COUNT=$(docker exec navarch-studio-postgres-1 psql -U postgres -d sri_template_dev -t -c "SELECT COUNT(*) FROM catalog_ml.parametric_hulls;" 2>/dev/null | tr -d ' ')
      REAL_COUNT=$(docker exec navarch-studio-postgres-1 psql -U postgres -d sri_template_dev -t -c "SELECT COUNT(*) FROM catalog_real.vessels;" 2>/dev/null | tr -d ' ')

      if [ ! -z "$PARAM_COUNT" ]; then
        echo -e "${GREEN}      ML Catalog: $PARAM_COUNT hulls${NC}"
      fi

      if [ ! -z "$REAL_COUNT" ]; then
        echo -e "${GREEN}      Real Catalog: $REAL_COUNT vessels${NC}"
      fi
    else
      echo -e "${YELLOW}      Database: Not created yet${NC}"
    fi
  else
    echo -e "${RED}   ❌ PostgreSQL: Not responding${NC}"
  fi
else
  echo -e "${RED}   ❌ PostgreSQL: Not running${NC}"
fi

# Check Redis
if docker ps | grep -q "navarch-studio-redis"; then
  if docker exec navarch-studio-redis-1 redis-cli ping > /dev/null 2>&1; then
    echo -e "${GREEN}   ✅ Redis: Running${NC}"
  else
    echo -e "${RED}   ❌ Redis: Not responding${NC}"
  fi
else
  echo -e "${RED}   ❌ Redis: Not running${NC}"
fi

echo ""

# Check service health
echo -e "${BLUE}🏥 Service Health:${NC}"

check_service() {
  local name=$1
  local port=$2

  if curl -s -f "http://localhost:${port}/health" > /dev/null 2>&1; then
    echo -e "${GREEN}   ✅ $name (port $port): Healthy${NC}"
  else
    if lsof -i ":${port}" > /dev/null 2>&1; then
      echo -e "${YELLOW}   ⚠️  $name (port $port): Running but not healthy${NC}"
    else
      echo -e "${RED}   ❌ $name (port $port): Not running${NC}"
    fi
  fi
}

check_service "IdentityService  " 5001
check_service "ApiGateway       " 5002
check_service "DataService      " 5003
check_service "HullSizingService" 5004

echo ""

# Check frontend
echo -e "${BLUE}🎨 Frontend:${NC}"
if lsof -i ":5173" > /dev/null 2>&1; then
  echo -e "${GREEN}   ✅ Running on http://localhost:5173${NC}"
else
  echo -e "${RED}   ❌ Not running${NC}"
fi

echo ""

# Check build status
echo -e "${BLUE}🔨 Build Status:${NC}"

# Backend
if [ -d "backend/DataService/bin" ]; then
  echo -e "${GREEN}   ✅ Backend: Built${NC}"
else
  echo -e "${YELLOW}   ⚠️  Backend: Not built${NC}"
fi

# Frontend
if [ -d "frontend/node_modules" ]; then
  echo -e "${GREEN}   ✅ Frontend: Dependencies installed${NC}"
else
  echo -e "${YELLOW}   ⚠️  Frontend: Dependencies not installed${NC}"
fi

echo ""

# Summary
echo -e "${BLUE}📝 Summary:${NC}"
echo "   Run './scripts/dev-setup.sh' if services are not initialized"
echo "   Run 'docker-compose up -d' to start backend services"
echo "   Run 'cd frontend && npm run dev' to start frontend"
echo ""
