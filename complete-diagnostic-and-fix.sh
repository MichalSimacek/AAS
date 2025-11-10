#!/bin/bash

# Complete Diagnostic and Fix Script

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo ""
echo "=========================================="
echo "  Complete Diagnostic & Fix"
echo "=========================================="
echo ""

# Check docker-compose
if docker compose version &> /dev/null; then
    DOCKER_COMPOSE="docker compose"
elif command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker-compose"
else
    echo -e "${RED}❌ Docker Compose not found${NC}"
    exit 1
fi

echo "STEP 1: Checking database structure..."
echo "======================================="

DB_CHECK=$(docker exec aas-db-prod psql -U aas -d aas -t -c "SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'Collections' AND column_name IN ('Status', 'Price', 'Currency');" 2>/dev/null || echo "0")

if [ "$DB_CHECK" -lt 3 ]; then
    echo -e "${YELLOW}⚠️  Database columns missing! Adding them now...${NC}"
    
    docker exec -i aas-db-prod psql -U aas -d aas << 'EOSQL'
-- Add Status column
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Collections' AND column_name = 'Status') THEN
        ALTER TABLE "Collections" ADD COLUMN "Status" integer NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added Status column';
    END IF;
END $$;

-- Add Price column
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Collections' AND column_name = 'Price') THEN
        ALTER TABLE "Collections" ADD COLUMN "Price" numeric(18,2);
        RAISE NOTICE 'Added Price column';
    END IF;
END $$;

-- Add Currency column
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Collections' AND column_name = 'Currency') THEN
        ALTER TABLE "Collections" ADD COLUMN "Currency" integer NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added Currency column';
    END IF;
END $$;
EOSQL
    echo -e "${GREEN}✅ Database columns added${NC}"
else
    echo -e "${GREEN}✅ Database columns exist${NC}"
fi

echo ""
echo "STEP 2: Checking current collection data..."
echo "============================================"

docker exec aas-db-prod psql -U aas -d aas -c "SELECT \"Id\", LEFT(\"Title\", 30) as \"Title\", \"Status\", \"Price\", \"Currency\" FROM \"Collections\" LIMIT 5;"

echo ""
echo "STEP 3: Stopping containers..."
echo "==============================="
$DOCKER_COMPOSE -f docker-compose.prod.yml down

echo ""
echo "STEP 4: Rebuilding web container (NO CACHE)..."
echo "==============================================="
$DOCKER_COMPOSE -f docker-compose.prod.yml build --no-cache web

echo ""
echo "STEP 5: Starting services..."
echo "============================="
if [ -f .env.production ]; then
    set -a
    source .env.production
    set +a
    $DOCKER_COMPOSE -f docker-compose.prod.yml --env-file .env.production up -d
else
    $DOCKER_COMPOSE -f docker-compose.prod.yml up -d
fi

echo ""
echo "STEP 6: Waiting for application to start..."
echo "============================================="
sleep 15

echo ""
echo "STEP 7: Checking application status..."
echo "======================================="

WEB_STATUS=$(docker inspect --format='{{.State.Status}}' aas-web-prod 2>/dev/null || echo "not found")
echo "Web container: $WEB_STATUS"

if [ "$WEB_STATUS" = "running" ]; then
    echo -e "${GREEN}✅ Web container running${NC}"
else
    echo -e "${RED}❌ Web container not running!${NC}"
    echo "Recent logs:"
    docker logs aas-web-prod --tail 30
    exit 1
fi

echo ""
echo "STEP 8: Testing Collections page..."
echo "===================================="

HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://aristocraticartworksale.com/Collections 2>&1 || echo "000")
if [ "$HTTP_STATUS" = "200" ]; then
    echo -e "${GREEN}✅ Collections page: $HTTP_STATUS OK${NC}"
else
    echo -e "${YELLOW}⚠️  Collections page: $HTTP_STATUS${NC}"
fi

echo ""
echo "=========================================="
echo "  Diagnostic Complete"
echo "=========================================="
echo ""
echo "📝 Next steps:"
echo ""
echo "1. Go to admin panel:"
echo "   https://aristocraticartworksale.com/Admin/Collections"
echo ""
echo "2. Edit any collection:"
echo "   - Set Status: Available"
echo "   - Set Price: 5000"
echo "   - Set Currency: EUR"
echo "   - Click Save"
echo ""
echo "3. View the collection detail page"
echo ""
echo "4. You should see:"
echo "   ✅ Green 'AVAILABLE' badge under photos"
echo "   ✅ Price '5 000 €' displayed"
echo "   ✅ 'I'm Interested' button"
echo ""
echo "If you still don't see changes:"
echo "  - Hard refresh browser: Ctrl+Shift+R"
echo "  - Try incognito mode"
echo "  - Check browser console (F12) for errors"
echo ""
