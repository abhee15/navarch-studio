#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}📜 NavArch Studio Service Logs${NC}"
echo "=============================="
echo ""

# Show menu
echo "Select service to view logs:"
echo "  1) All services"
echo "  2) Postgres"
echo "  3) Redis"
echo "  4) IdentityService"
echo "  5) DataService"
echo "  6) HullSizingService"
echo "  7) ApiGateway"
echo "  8) PgAdmin"
echo "  9) Follow all logs (live)"
echo ""
read -p "Enter choice [1-9]: " choice

case $choice in
  1)
    docker-compose logs --tail=100
    ;;
  2)
    docker-compose logs --tail=100 postgres
    ;;
  3)
    docker-compose logs --tail=100 redis
    ;;
  4)
    docker-compose logs --tail=100 identity-service
    ;;
  5)
    docker-compose logs --tail=100 data-service
    ;;
  6)
    docker-compose logs --tail=100 hull-sizing-service
    ;;
  7)
    docker-compose logs --tail=100 api-gateway
    ;;
  8)
    docker-compose logs --tail=100 pgadmin
    ;;
  9)
    echo -e "${YELLOW}Following logs (Ctrl+C to exit)...${NC}"
    docker-compose logs -f
    ;;
  *)
    echo -e "${RED}Invalid choice${NC}"
    exit 1
    ;;
esac

