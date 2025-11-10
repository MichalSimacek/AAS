#!/bin/bash

echo "================================================"
echo "🔧 Fix Docker Compose + Deploy Edit Form Fix"
echo "================================================"

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

if [ "$EUID" -ne 0 ]; then 
    echo -e "${RED}❌ Please run as root (use sudo)${NC}"
    exit 1
fi

echo ""
echo -e "${YELLOW}Step 1: Fixing docker-compose distutils error...${NC}"
apt-get update -qq
apt-get install -y python3-distutils 2>&1 | grep -v "^Reading\|^Building\|^Get:"

echo ""
echo -e "${YELLOW}Step 2: Verifying docker-compose works...${NC}"
if docker-compose --version; then
    echo -e "${GREEN}✅ docker-compose is working${NC}"
else
    echo -e "${YELLOW}⚠ Old docker-compose has issues, using 'docker compose' (v2) instead${NC}"
fi

echo ""
echo -e "${YELLOW}Step 3: Navigating to project directory...${NC}"
cd /AAS || { echo -e "${RED}❌ Directory /AAS not found${NC}"; exit 1; }
echo -e "${GREEN}✅ In directory: $(pwd)${NC}"

echo ""
echo -e "${YELLOW}Step 4: Pulling latest changes from git...${NC}"
git pull origin main 2>&1 | tail -5

echo ""
echo -e "${YELLOW}Step 5: Checking docker-compose files...${NC}"
if [ -f "docker-compose.prod.yml" ]; then
    COMPOSE_FILE="docker-compose.prod.yml"
    echo -e "${GREEN}✅ Using: docker-compose.prod.yml${NC}"
elif [ -f "docker-compose.production.yml" ]; then
    COMPOSE_FILE="docker-compose.production.yml"
    echo -e "${GREEN}✅ Using: docker-compose.production.yml${NC}"
elif [ -f "docker-compose.yml" ]; then
    COMPOSE_FILE="docker-compose.yml"
    echo -e "${GREEN}✅ Using: docker-compose.yml${NC}"
else
    echo -e "${RED}❌ No docker-compose file found${NC}"
    exit 1
fi

echo ""
echo -e "${YELLOW}Step 6: Restarting web service...${NC}"

# Try docker compose v2 first (modern)
if docker compose -f "$COMPOSE_FILE" restart web 2>/dev/null; then
    echo -e "${GREEN}✅ Restarted with 'docker compose' (v2)${NC}"
# Try docker-compose v1 as fallback
elif docker-compose -f "$COMPOSE_FILE" restart web 2>/dev/null; then
    echo -e "${GREEN}✅ Restarted with 'docker-compose' (v1)${NC}"
# Manual restart as last resort
else
    echo -e "${YELLOW}⚠ Docker compose command failed, trying manual restart...${NC}"
    CONTAINER_NAME=$(docker ps --filter "name=web" --format "{{.Names}}" | head -1)
    if [ -z "$CONTAINER_NAME" ]; then
        CONTAINER_NAME=$(docker ps --filter "name=aas" --format "{{.Names}}" | head -1)
    fi
    
    if [ -n "$CONTAINER_NAME" ]; then
        docker restart "$CONTAINER_NAME"
        echo -e "${GREEN}✅ Restarted container: $CONTAINER_NAME${NC}"
    else
        echo -e "${RED}❌ Could not find container to restart${NC}"
        docker ps
        exit 1
    fi
fi

echo ""
echo -e "${YELLOW}Step 7: Waiting for application to start (15 seconds)...${NC}"
sleep 15

echo ""
echo -e "${YELLOW}Step 8: Checking container status...${NC}"
docker ps | grep -E "web|aas" | head -3

echo ""
echo -e "${YELLOW}Step 9: Checking recent application logs...${NC}"
CONTAINER_NAME=$(docker ps --filter "name=web" --format "{{.Names}}" | head -1)
if [ -z "$CONTAINER_NAME" ]; then
    CONTAINER_NAME=$(docker ps --filter "name=aas" --format "{{.Names}}" | head -1)
fi

if [ -n "$CONTAINER_NAME" ]; then
    echo "Last 15 lines from $CONTAINER_NAME:"
    docker logs --tail 15 "$CONTAINER_NAME"
fi

echo ""
echo "================================================"
echo -e "${GREEN}✅ Deployment Complete!${NC}"
echo "================================================"
echo ""
echo "📋 Testing Instructions:"
echo "1. Open Admin panel: https://your-domain.com/Admin/Collections"
echo "2. Click 'Edit' on any collection"
echo "3. Verify Status and Currency dropdowns show CURRENT values (not always first option)"
echo "4. Change ONLY the title, don't touch Status/Currency/Price"
echo "5. Click Save"
echo "6. Open Edit again - Status/Currency/Price should be UNCHANGED"
echo ""
echo "🔍 Monitor logs in real-time:"
echo "   docker logs -f $CONTAINER_NAME"
echo ""
echo "🔍 Watch for debug messages when editing:"
echo "   docker logs -f $CONTAINER_NAME | grep 'EDIT POST DEBUG'"
echo ""
echo "❓ If the issue persists, run:"
echo "   docker exec $CONTAINER_NAME cat /app/src/AAS.Web/Areas/Admin/Views/Collections/Edit.cshtml | grep -A 2 'Status'"
echo "   This will verify the fix is actually in the container"
echo ""
