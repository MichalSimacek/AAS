#!/bin/bash

echo "================================================"
echo "🔍 Diagnosing Server Structure and Deployment"
echo "================================================"

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo ""
echo -e "${YELLOW}Step 1: Checking current directory...${NC}"
pwd
ls -la

echo ""
echo -e "${YELLOW}Step 2: Looking for ASP.NET application directories...${NC}"
echo "Checking /AAS:"
if [ -d "/AAS" ]; then
    echo -e "${GREEN}✅ /AAS exists${NC}"
    ls -la /AAS/ | head -20
else
    echo -e "${RED}❌ /AAS not found${NC}"
fi

echo ""
echo "Checking /var/www/aas:"
if [ -d "/var/www/aas" ]; then
    echo -e "${GREEN}✅ /var/www/aas exists${NC}"
    ls -la /var/www/aas/ | head -20
else
    echo -e "${RED}❌ /var/www/aas not found${NC}"
fi

echo ""
echo "Checking /app:"
if [ -d "/app" ]; then
    echo -e "${GREEN}✅ /app exists${NC}"
    ls -la /app/ | head -20
else
    echo -e "${RED}❌ /app not found${NC}"
fi

echo ""
echo -e "${YELLOW}Step 3: Finding .csproj files (ASP.NET projects)...${NC}"
find /AAS /var/www /app -name "*.csproj" 2>/dev/null | head -10

echo ""
echo -e "${YELLOW}Step 4: Finding published/compiled applications...${NC}"
find /AAS /var/www /app -name "AAS.Web.dll" 2>/dev/null | head -10

echo ""
echo -e "${YELLOW}Step 5: Checking for running dotnet processes...${NC}"
ps aux | grep -i dotnet | grep -v grep

echo ""
echo -e "${YELLOW}Step 6: Checking systemd services...${NC}"
systemctl list-units --type=service --state=running | grep -i aas

echo ""
echo -e "${YELLOW}Step 7: Checking for deployment scripts...${NC}"
find /AAS /var/www /app -name "deploy*.sh" 2>/dev/null | head -10

echo ""
echo -e "${YELLOW}Step 8: Checking Docker status...${NC}"
if command -v docker &> /dev/null; then
    echo "Docker is installed"
    docker ps 2>/dev/null || echo "Docker not running or no permission"
else
    echo "Docker not installed"
fi

echo ""
echo -e "${YELLOW}Step 9: Checking nginx configuration...${NC}"
if [ -d "/etc/nginx" ]; then
    echo "Nginx config directory exists"
    ls -la /etc/nginx/sites-enabled/ 2>/dev/null
    echo ""
    echo "Checking for AAS-related configs:"
    grep -r "aas\|aristocratic" /etc/nginx/sites-enabled/ 2>/dev/null | head -5
fi

echo ""
echo -e "${YELLOW}Step 10: Looking for Edit.cshtml in all locations...${NC}"
find /AAS /var/www /app -path "*/Admin/Views/Collections/Edit.cshtml" 2>/dev/null

echo ""
echo "================================================"
echo -e "${BLUE}📋 Summary${NC}"
echo "================================================"
echo "Please review the output above to determine:"
echo "1. Where is the source code located?"
echo "2. Where is the compiled/published application?"
echo "3. How is the application running (systemd/docker/other)?"
echo "4. What deployment script should be used?"
echo ""
