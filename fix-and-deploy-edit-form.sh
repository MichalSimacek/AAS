#!/bin/bash

echo "================================================"
echo "🔧 Deploying Edit Form Fix"
echo "================================================"

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo -e "${RED}❌ Please run as root (use sudo)${NC}"
    exit 1
fi

echo ""
echo -e "${YELLOW}Step 1: Checking Docker status...${NC}"
if ! docker ps > /dev/null 2>&1; then
    echo -e "${RED}❌ Docker is not running or not installed${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Docker is running${NC}"

echo ""
echo -e "${YELLOW}Step 2: Finding running containers...${NC}"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

echo ""
echo -e "${YELLOW}Step 3: Checking for ASP.NET container...${NC}"
CONTAINER_NAME=$(docker ps --filter "ancestor=aas-web" --format "{{.Names}}" | head -1)

if [ -z "$CONTAINER_NAME" ]; then
    # Try alternative names
    CONTAINER_NAME=$(docker ps --filter "name=web" --format "{{.Names}}" | head -1)
fi

if [ -z "$CONTAINER_NAME" ]; then
    CONTAINER_NAME=$(docker ps --filter "name=aas" --format "{{.Names}}" | head -1)
fi

if [ -z "$CONTAINER_NAME" ]; then
    echo -e "${RED}❌ Could not find ASP.NET container${NC}"
    echo "Available containers:"
    docker ps --format "{{.Names}}"
    echo ""
    echo "Please restart manually with:"
    echo "  docker restart <container-name>"
    exit 1
fi

echo -e "${GREEN}✅ Found container: ${CONTAINER_NAME}${NC}"

echo ""
echo -e "${YELLOW}Step 4: Restarting ASP.NET application...${NC}"
docker restart "$CONTAINER_NAME"

echo ""
echo -e "${YELLOW}Step 5: Waiting for container to start (10 seconds)...${NC}"
sleep 10

echo ""
echo -e "${YELLOW}Step 6: Checking container status...${NC}"
if docker ps | grep -q "$CONTAINER_NAME"; then
    echo -e "${GREEN}✅ Container is running${NC}"
else
    echo -e "${RED}❌ Container failed to start${NC}"
    echo "Checking logs:"
    docker logs --tail 50 "$CONTAINER_NAME"
    exit 1
fi

echo ""
echo -e "${YELLOW}Step 7: Checking recent logs...${NC}"
echo "Last 20 lines:"
docker logs --tail 20 "$CONTAINER_NAME"

echo ""
echo "================================================"
echo -e "${GREEN}✅ Deployment Complete!${NC}"
echo "================================================"
echo ""
echo "📋 Next Steps:"
echo "1. Open your admin panel in browser"
echo "2. Go to Collections → Edit any collection"
echo "3. Check that Status and Currency dropdowns show correct values"
echo "4. Test saving without changing Status/Price - they should NOT reset"
echo ""
echo "🔍 To check logs for debug messages:"
echo "   docker logs -f $CONTAINER_NAME | grep 'EDIT POST DEBUG'"
echo ""
echo "If the fix still doesn't work, check that files were updated:"
echo "   docker exec $CONTAINER_NAME cat /app/src/AAS.Web/Areas/Admin/Views/Collections/Edit.cshtml | grep 'Model.Status'"
echo ""
