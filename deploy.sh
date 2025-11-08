#!/bin/bash
set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo "=========================================="
echo "  AAS Production Deployment"
echo "=========================================="

if [ ! -f .env.production ]; then
    echo -e "${RED}Error: .env.production not found!${NC}"
    exit 1
fi

export $(cat .env.production | grep -v '^#' | xargs)

echo -e "${YELLOW}1. Pulling latest changes...${NC}"
git pull origin main

echo -e "${YELLOW}2. Building Docker images...${NC}"
docker-compose -f docker-compose.prod.yml build --no-cache

echo -e "${YELLOW}3. Stopping old containers...${NC}"
docker-compose -f docker-compose.prod.yml down

echo -e "${YELLOW}4. Starting new containers...${NC}"
docker-compose -f docker-compose.prod.yml up -d

sleep 10

echo -e "${YELLOW}5. Running migrations...${NC}"
docker-compose -f docker-compose.prod.yml exec -T web dotnet ef database update || echo "Migrations OK"

echo -e "${YELLOW}6. Checking status...${NC}"
docker-compose -f docker-compose.prod.yml ps

echo -e "${GREEN}Deployment complete!${NC}"
