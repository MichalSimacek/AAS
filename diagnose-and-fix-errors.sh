#!/bin/bash

# Diagnose and fix 500 errors

set -e

echo "=========================================="
echo "  Diagnosing Application Errors"
echo "=========================================="
echo ""

echo "1. Checking web application logs..."
echo "======================================="
docker logs aas-web-prod --tail 100 | grep -A5 -B5 -i "error\|exception\|fail" || echo "No obvious errors in recent logs"

echo ""
echo "2. Checking database structure..."
echo "======================================="
docker exec aas-db-prod psql -U aas -d aas -c "\d \"Collections\"" 2>&1 | head -40

echo ""
echo "3. Checking if Status, Price, Currency columns exist..."
echo "======================================="
COLUMNS=$(docker exec aas-db-prod psql -U aas -d aas -t -c "SELECT column_name FROM information_schema.columns WHERE table_name = 'Collections' AND column_name IN ('Status', 'Price', 'Currency') ORDER BY column_name;" | xargs)

if [ -z "$COLUMNS" ]; then
    echo "❌ Columns missing! Adding them now..."
    
    docker exec -i aas-db-prod psql -U aas -d aas << 'EOSQL'
-- Add Status column
ALTER TABLE "Collections" ADD COLUMN IF NOT EXISTS "Status" integer NOT NULL DEFAULT 0;

-- Add Price column
ALTER TABLE "Collections" ADD COLUMN IF NOT EXISTS "Price" numeric(18,2);

-- Add Currency column
ALTER TABLE "Collections" ADD COLUMN IF NOT EXISTS "Currency" integer NOT NULL DEFAULT 0;

-- Verify
SELECT column_name, data_type, column_default 
FROM information_schema.columns 
WHERE table_name = 'Collections' 
  AND column_name IN ('Status', 'Price', 'Currency')
ORDER BY column_name;
EOSQL
    
    echo "✅ Columns added!"
else
    echo "✅ Columns exist: $COLUMNS"
fi

echo ""
echo "4. Checking collection count..."
echo "======================================="
docker exec aas-db-prod psql -U aas -d aas -c "SELECT COUNT(*) as total_collections FROM \"Collections\";"

echo ""
echo "5. Sample collection data..."
echo "======================================="
docker exec aas-db-prod psql -U aas -d aas -c "SELECT \"Id\", LEFT(\"Title\", 30) as \"Title\", \"Status\", \"Price\", \"Currency\" FROM \"Collections\" LIMIT 3;"

echo ""
echo "6. Restarting web container..."
echo "======================================="
docker restart aas-web-prod

echo ""
echo "⏳ Waiting for restart..."
sleep 10

echo ""
echo "7. Checking if application started..."
echo "======================================="
docker logs aas-web-prod --tail 30

echo ""
echo "8. Testing Collections page..."
echo "======================================="
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://aristocraticartworksale.com/Collections 2>&1 || echo "000")
echo "Status: $HTTP_STATUS"

if [ "$HTTP_STATUS" = "200" ]; then
    echo "✅ Collections page OK!"
else
    echo "❌ Still getting error $HTTP_STATUS"
    echo ""
    echo "Recent application errors:"
    docker logs aas-web-prod --tail 50 | grep -i "error\|exception" | tail -20
fi

echo ""
echo "=========================================="
echo "  Diagnosis Complete"
echo "=========================================="
