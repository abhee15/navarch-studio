#!/bin/bash
set -e

echo "🔄 Resetting NavArch Studio Dev Environment"
echo "============================================"
echo ""

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Confirm
echo -e "${RED}⚠️  WARNING: This will delete all data and build artifacts${NC}"
echo ""
read -p "Are you sure you want to reset? (yes/no): " -r
echo ""
if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]
then
    echo "Reset cancelled"
    exit 0
fi

# Stop all containers
echo -e "${BLUE}🛑 Stopping all containers...${NC}"
docker-compose down -v

# Clean backend builds
echo -e "${BLUE}🧹 Cleaning backend builds...${NC}"
cd backend
dotnet clean > /dev/null 2>&1
find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true
cd ..

# Clean frontend builds
echo -e "${BLUE}🧹 Cleaning frontend builds...${NC}"
cd frontend
rm -rf node_modules dist .vite 2>/dev/null || true
cd ..

# Remove Docker volumes
echo -e "${BLUE}🗑️ Removing Docker volumes...${NC}"
docker volume rm navarch-studio_postgres_data 2>/dev/null || echo "   (postgres volume already removed)"
docker volume rm navarch-studio_redis_data 2>/dev/null || echo "   (redis volume already removed)"

# Clean logs
echo -e "${BLUE}🧹 Cleaning logs...${NC}"
find . -name "*.log" -type f -delete 2>/dev/null || true

echo ""
echo -e "${GREEN}✅ Environment reset complete${NC}"
echo ""
echo -e "${BLUE}📝 Next steps:${NC}"
echo "   1. Run './scripts/dev-setup.sh' to reinitialize"
echo "   2. Or manually start with 'docker-compose up -d'"
echo ""
