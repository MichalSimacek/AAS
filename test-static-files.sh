#!/bin/bash

# Test script pro ověření statických souborů
echo "==================================="
echo "Testing Static Files Configuration"
echo "==================================="

# Barvy pro výstup
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Kontrola Docker kontejnerů
echo -e "\n${YELLOW}1. Checking Docker containers...${NC}"
if docker ps | grep -q "aas-nginx-prod"; then
    echo -e "${GREEN}✅ Nginx container is running${NC}"
else
    echo -e "${RED}❌ Nginx container is NOT running${NC}"
    exit 1
fi

if docker ps | grep -q "aas-web-prod"; then
    echo -e "${GREEN}✅ Web container is running${NC}"
else
    echo -e "${RED}❌ Web container is NOT running${NC}"
    exit 1
fi

# Kontrola statických souborů v Nginx kontejneru
echo -e "\n${YELLOW}2. Checking static files in Nginx container...${NC}"
if docker exec aas-nginx-prod ls /app/wwwroot/ > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Static files directory exists in Nginx${NC}"
    echo "   Contents:"
    docker exec aas-nginx-prod ls -la /app/wwwroot/
else
    echo -e "${RED}❌ Static files directory NOT found in Nginx${NC}"
fi

# Kontrola specifických souborů
echo -e "\n${YELLOW}3. Checking specific static files...${NC}"
FILES=("/app/wwwroot/css/site.css" "/app/wwwroot/js/site.js")
for file in "${FILES[@]}"; do
    if docker exec aas-nginx-prod test -f "$file"; then
        echo -e "${GREEN}✅ Found: $file${NC}"
    else
        echo -e "${RED}❌ Missing: $file${NC}"
    fi
done

# Test HTTP requests (pokud je známa doména)
echo -e "\n${YELLOW}4. Testing HTTP requests...${NC}"
if [ ! -z "$1" ]; then
    DOMAIN=$1
    echo "Testing domain: $DOMAIN"
    
    # Test CSS
    STATUS=$(curl -s -o /dev/null -w "%{http_code}" "https://$DOMAIN/css/site.css")
    if [ "$STATUS" = "200" ]; then
        echo -e "${GREEN}✅ CSS file: HTTP $STATUS${NC}"
    else
        echo -e "${RED}❌ CSS file: HTTP $STATUS${NC}"
    fi
    
    # Test JS
    STATUS=$(curl -s -o /dev/null -w "%{http_code}" "https://$DOMAIN/js/site.js")
    if [ "$STATUS" = "200" ]; then
        echo -e "${GREEN}✅ JS file: HTTP $STATUS${NC}"
    else
        echo -e "${RED}❌ JS file: HTTP $STATUS${NC}"
    fi
else
    echo "Skipping HTTP tests (no domain provided)"
    echo "Usage: $0 <domain> (e.g., $0 aristocraticartworksale.com)"
fi

# Kontrola Nginx logů
echo -e "\n${YELLOW}5. Recent Nginx errors (last 10 lines)...${NC}"
docker exec aas-nginx-prod tail -10 /var/log/nginx/error.log 2>/dev/null || echo "No recent errors"

echo -e "\n${GREEN}==================================="
echo "Test Complete!"
echo "===================================${NC}"
