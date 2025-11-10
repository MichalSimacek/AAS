#!/bin/bash

# ========================================
# Aristocratic Artwork Sale
# Production Deployment Script
# ========================================

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo ""
echo "=========================================="
echo "  Aristocratic Artwork Sale"
echo "  Production Deployment"
echo "=========================================="
echo ""

# Check prerequisites
if ! command -v docker &> /dev/null; then
    echo -e "${RED}❌ Docker is not installed${NC}"
    exit 1
fi

# Detect docker-compose
if docker compose version &> /dev/null; then
    DOCKER_COMPOSE="docker compose"
elif command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker-compose"
else
    echo -e "${RED}❌ Docker Compose is not installed${NC}"
    exit 1
fi

# Check .env.production
if [ ! -f .env.production ]; then
    echo -e "${RED}❌ .env.production not found!${NC}"
    echo ""
    echo "Create .env.production with required variables:"
    echo "  DB_HOST=db"
    echo "  DB_PASSWORD=<your-password>"
    echo "  ADMIN_EMAIL=<your-email>"
    echo "  ADMIN_PASSWORD=<your-password>"
    exit 1
fi

# Load environment
set -a
source .env.production
set +a

echo -e "${GREEN}✅ Prerequisites OK${NC}"
echo ""

# Options
REBUILD=false
CLEAN_VOLUMES=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --rebuild)
            REBUILD=true
            shift
            ;;
        --clean)
            CLEAN_VOLUMES=true
            shift
            ;;
        --help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --rebuild      Rebuild containers from scratch"
            echo "  --clean        Remove volumes (fresh database)"
            echo "  --help         Show this help"
            echo ""
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Stop containers
echo -e "${BLUE}🛑 Stopping containers...${NC}"
$DOCKER_COMPOSE -f docker-compose.prod.yml --env-file .env.production down

if [ "$CLEAN_VOLUMES" = true ]; then
    echo -e "${YELLOW}⚠️  Removing volumes (database will be reset)...${NC}"
    docker volume rm aas_postgres-data 2>/dev/null || true
    docker volume rm aas_static-files 2>/dev/null || true
fi

# Build if requested
if [ "$REBUILD" = true ]; then
    echo -e "${BLUE}🔨 Rebuilding containers...${NC}"
    $DOCKER_COMPOSE -f docker-compose.prod.yml build --no-cache
fi

# Start services
echo -e "${BLUE}🚀 Starting services...${NC}"
$DOCKER_COMPOSE -f docker-compose.prod.yml --env-file .env.production up -d

# Wait for database
echo -e "${BLUE}⏳ Waiting for database...${NC}"
timeout=60
counter=0
while [ $counter -lt $timeout ]; do
    if docker inspect --format='{{.State.Health.Status}}' aas-db-prod 2>/dev/null | grep -q "healthy"; then
        echo -e "${GREEN}✅ Database is healthy!${NC}"
        break
    fi
    sleep 2
    counter=$((counter + 2))
done

if [ $counter -ge $timeout ]; then
    echo -e "${RED}❌ Database health check timeout!${NC}"
    exit 1
fi

# Wait a bit for web to start
echo -e "${BLUE}⏳ Starting web application...${NC}"
sleep 5

# Check services
echo ""
echo -e "${BLUE}📋 Service Status:${NC}"
docker ps --filter "name=aas-" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

echo ""
echo -e "${GREEN}=========================================="
echo "✅ Deployment Complete!"
echo "==========================================${NC}"
echo ""
echo "Application: https://aristocraticartworksale.com"
echo ""
echo "To check logs:"
echo "  docker logs -f aas-web-prod"
echo "  docker logs -f aas-db-prod"
echo "  docker logs -f aas-nginx-prod"
echo ""
