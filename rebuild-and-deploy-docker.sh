#!/bin/bash

echo "================================================"
echo "🔧 Rebuild Docker Image + Deploy Fix"
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
echo -e "${YELLOW}Step 1: Navigating to project directory...${NC}"
cd /AAS || { echo -e "${RED}❌ Directory /AAS not found${NC}"; exit 1; }
echo -e "${GREEN}✅ In directory: $(pwd)${NC}"

echo ""
echo -e "${YELLOW}Step 2: Pulling latest changes from GitHub...${NC}"
git pull origin main 2>&1 | tail -10
echo -e "${GREEN}✅ Code updated${NC}"

echo ""
echo -e "${YELLOW}Step 3: Finding docker-compose file...${NC}"
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
echo -e "${YELLOW}Step 4: Stopping current containers...${NC}"
docker compose -f "$COMPOSE_FILE" down web 2>/dev/null || docker-compose -f "$COMPOSE_FILE" down web 2>/dev/null
echo -e "${GREEN}✅ Containers stopped${NC}"

echo ""
echo -e "${YELLOW}Step 5: Rebuilding Docker image with new code...${NC}"
echo "This may take 2-5 minutes..."
if docker compose -f "$COMPOSE_FILE" build --no-cache web 2>/dev/null; then
    echo -e "${GREEN}✅ Built with 'docker compose' (v2)${NC}"
elif docker-compose -f "$COMPOSE_FILE" build --no-cache web 2>/dev/null; then
    echo -e "${GREEN}✅ Built with 'docker-compose' (v1)${NC}"
else
    echo -e "${RED}❌ Build failed${NC}"
    exit 1
fi

echo ""
echo -e "${YELLOW}Step 6: Starting containers with new image...${NC}"
if docker compose -f "$COMPOSE_FILE" up -d web 2>/dev/null; then
    echo -e "${GREEN}✅ Started with 'docker compose' (v2)${NC}"
elif docker-compose -f "$COMPOSE_FILE" up -d web 2>/dev/null; then
    echo -e "${GREEN}✅ Started with 'docker-compose' (v1)${NC}"
else
    echo -e "${RED}❌ Start failed${NC}"
    exit 1
fi

echo ""
echo -e "${YELLOW}Step 7: Waiting for application to start (20 seconds)...${NC}"
sleep 20

echo ""
echo -e "${YELLOW}Step 8: Verifying container is running...${NC}"
docker ps | grep aas-web

echo ""
echo -e "${YELLOW}Step 9: Checking application logs...${NC}"
echo "Last 25 lines:"
docker logs --tail 25 aas-web-prod 2>/dev/null || docker logs --tail 25 $(docker ps --filter "name=web" --format "{{.Names}}" | head -1)

echo ""
echo -e "${YELLOW}Step 10: Verifying fix is in container...${NC}"
echo "Checking Status dropdown code:"
docker exec aas-web-prod cat /app/src/AAS.Web/Areas/Admin/Views/Collections/Edit.cshtml 2>/dev/null | grep -A 2 "Model.Status" || echo "Could not verify - check manually"

echo ""
echo "================================================"
echo -e "${GREEN}✅ Rebuild and Deployment Complete!${NC}"
echo "================================================"
echo ""
echo "📋 Next Steps - Testing:"
echo "1. Open Admin panel: https://aristocraticartworksale.com/Admin/Collections"
echo "2. Click 'Edit' on any collection"
echo "3. Verify that Status dropdown shows CORRECT current value (not always 'Available')"
echo "4. Verify that Currency dropdown shows CORRECT current value (not always 'EUR')"
echo "5. Change ONLY the title, don't touch Status/Currency/Price"
echo "6. Click Save"
echo "7. Open Edit again - verify Status/Currency/Price are UNCHANGED"
echo ""
echo "🔍 Monitor logs in real-time:"
echo "   docker logs -f aas-web-prod"
echo ""
echo "🔍 Watch for debug messages when editing (NEW in this fix):"
echo "   docker logs -f aas-web-prod | grep 'EDIT POST DEBUG'"
echo ""
echo "❓ If you see debug messages like:"
echo "   [EDIT POST DEBUG] Updated Status to: Available (0)"
echo "   [EDIT POST DEBUG] Updated Currency to: EUR (0)"
echo "   Then the fix is working and saving correctly!"
echo ""
