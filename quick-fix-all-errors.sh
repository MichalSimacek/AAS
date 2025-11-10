#!/bin/bash

# Quick fix for all errors

set -e

GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

echo "=========================================="
echo "  Quick Fix for All Errors"
echo "=========================================="
echo ""

cd /AAS

echo "Step 1: Adding missing database columns..."
echo "==========================================="

docker exec -i aas-db-prod psql -U aas -d aas << 'EOSQL'
-- Add Status column if not exists
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Collections' AND column_name = 'Status') THEN
        ALTER TABLE "Collections" ADD COLUMN "Status" integer NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added Status column';
    END IF;
END $$;

-- Add Price column if not exists
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Collections' AND column_name = 'Price') THEN
        ALTER TABLE "Collections" ADD COLUMN "Price" numeric(18,2);
        RAISE NOTICE 'Added Price column';
    END IF;
END $$;

-- Add Currency column if not exists
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Collections' AND column_name = 'Currency') THEN
        ALTER TABLE "Collections" ADD COLUMN "Currency" integer NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added Currency column';
    END IF;
END $$;

-- Verify columns
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'Collections' 
  AND column_name IN ('Status', 'Price', 'Currency')
ORDER BY column_name;
EOSQL

echo ""
echo -e "${GREEN}✅ Database columns verified/added${NC}"

echo ""
echo "Step 2: Restarting web application..."
echo "==========================================="
docker restart aas-web-prod

echo ""
echo "⏳ Waiting 10 seconds for application to start..."
sleep 10

echo ""
echo "Step 3: Testing application..."
echo "==========================================="

# Test homepage
echo -n "Homepage: "
STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://aristocraticartworksale.com/ 2>&1 || echo "000")
if [ "$STATUS" = "200" ]; then
    echo -e "${GREEN}$STATUS ✅${NC}"
else
    echo -e "${RED}$STATUS ❌${NC}"
fi

# Test collections
echo -n "Collections: "
STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://aristocraticartworksale.com/Collections 2>&1 || echo "000")
if [ "$STATUS" = "200" ]; then
    echo -e "${GREEN}$STATUS ✅${NC}"
else
    echo -e "${RED}$STATUS ❌${NC}"
fi

# Test about
echo -n "About: "
STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://aristocraticartworksale.com/About 2>&1 || echo "000")
if [ "$STATUS" = "200" ]; then
    echo -e "${GREEN}$STATUS ✅${NC}"
else
    echo -e "${RED}$STATUS ❌${NC}"
fi

echo ""
echo "Step 4: Checking for errors in logs..."
echo "==========================================="
ERRORS=$(docker logs aas-web-prod --tail 50 | grep -i "error\|exception" | wc -l)
if [ "$ERRORS" -gt 0 ]; then
    echo -e "${RED}Found $ERRORS error lines in logs:${NC}"
    docker logs aas-web-prod --tail 50 | grep -i "error\|exception" | tail -10
else
    echo -e "${GREEN}No errors in recent logs ✅${NC}"
fi

echo ""
echo "=========================================="
echo "  Fix Complete"
echo "=========================================="
echo ""
echo "🌐 Test your application:"
echo "   https://aristocraticartworksale.com"
echo ""
echo "If issues persist, check full logs:"
echo "   docker logs aas-web-prod --tail 100"
echo ""
