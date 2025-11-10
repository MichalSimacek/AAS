#!/bin/bash

# Debug script pro statické soubory
echo "=========================================="
echo "🔍 Debug Static Files - HTTP 400"
echo "=========================================="
echo ""

cd /AAS

# Barvy
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}1. Checking Docker containers...${NC}"
docker ps --format "table {{.Names}}\t{{.Status}}"
echo ""

echo -e "${BLUE}2. Checking Nginx container static files...${NC}"
NGINX_CONTAINER=$(docker ps --filter "name=nginx" --format "{{.Names}}" | head -1)
if [ -z "$NGINX_CONTAINER" ]; then
    echo -e "${RED}❌ Nginx container not found!${NC}"
    exit 1
fi
echo "Nginx container: $NGINX_CONTAINER"
echo ""

echo -e "${YELLOW}Checking /app/wwwroot/ in Nginx:${NC}"
if docker exec $NGINX_CONTAINER ls /app/wwwroot/ > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Directory exists${NC}"
    docker exec $NGINX_CONTAINER ls -la /app/wwwroot/
else
    echo -e "${RED}❌ Directory NOT found!${NC}"
fi
echo ""

echo -e "${YELLOW}Checking specific files:${NC}"
FILES=("css/site-new.css" "js/site.js" "images/logo.png" "images/logo-hero.png")
for file in "${FILES[@]}"; do
    if docker exec $NGINX_CONTAINER test -f "/app/wwwroot/$file"; then
        echo -e "${GREEN}✅ $file${NC}"
    else
        echo -e "${RED}❌ $file (MISSING!)${NC}"
    fi
done
echo ""

echo -e "${BLUE}3. Checking Web container static files...${NC}"
WEB_CONTAINER=$(docker ps --filter "name=web" --format "{{.Names}}" | head -1)
if [ -z "$WEB_CONTAINER" ]; then
    echo -e "${RED}❌ Web container not found!${NC}"
else
    echo "Web container: $WEB_CONTAINER"
    echo -e "${YELLOW}Checking /app/wwwroot/ in Web:${NC}"
    if docker exec $WEB_CONTAINER ls /app/wwwroot/ > /dev/null 2>&1; then
        echo -e "${GREEN}✅ Directory exists${NC}"
        docker exec $WEB_CONTAINER ls -la /app/wwwroot/
    else
        echo -e "${RED}❌ Directory NOT found!${NC}"
    fi
fi
echo ""

echo -e "${BLUE}4. Checking shared volume...${NC}"
docker volume ls | grep static
echo ""

echo -e "${BLUE}5. Checking Web container logs (entrypoint)...${NC}"
docker logs $WEB_CONTAINER 2>&1 | grep -i "static\|copying\|wwwroot" | tail -20
echo ""

echo -e "${BLUE}6. Checking Nginx configuration...${NC}"
echo -e "${YELLOW}Nginx config test:${NC}"
docker exec $NGINX_CONTAINER nginx -t 2>&1
echo ""

echo -e "${YELLOW}Static files location block:${NC}"
docker exec $NGINX_CONTAINER cat /etc/nginx/nginx.conf 2>/dev/null | grep -A 10 "location ~\*" | head -20
echo ""

echo -e "${BLUE}7. Checking Nginx error log...${NC}"
docker exec $NGINX_CONTAINER tail -30 /var/log/nginx/error.log 2>/dev/null || echo "No error log available"
echo ""

echo -e "${BLUE}8. Testing HTTP requests...${NC}"
echo -e "${YELLOW}Testing /css/site-new.css:${NC}"
curl -I http://localhost/css/site-new.css 2>&1 | head -10
echo ""

echo -e "${YELLOW}Testing from inside Nginx container:${NC}"
docker exec $NGINX_CONTAINER ls -la /app/wwwroot/css/site-new.css 2>&1 || echo "File not found"
echo ""

echo "=========================================="
echo "🎯 Debug Complete"
echo "=========================================="
echo ""
echo "Next steps based on findings:"
echo "  - If /app/wwwroot/ is empty in Nginx → Entrypoint didn't copy files"
echo "  - If files exist but still 400 → Nginx config issue"
echo "  - Check error log for details"
echo ""
