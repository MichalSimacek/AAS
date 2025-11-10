#!/bin/bash

# Fix 500 Error and Navbar Centering

set -e

echo "=========================================="
echo "  Fixing 500 Error & Navbar"
echo "=========================================="
echo ""

if [ ! -f .env.production ]; then
    echo "❌ .env.production not found!"
    exit 1
fi

set -a
source .env.production
set +a

# Detect docker-compose
if docker compose version &> /dev/null; then
    DOCKER_COMPOSE="docker compose"
elif command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker-compose"
else
    echo "❌ Docker Compose not found"
    exit 1
fi

echo "🔧 Fix 1: Running database migration..."
echo ""

docker exec -i aas-db-prod psql -U aas -d aas << 'EOSQL'
-- Add Status column (0 = Available, 1 = Sold)
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'Collections' AND column_name = 'Status') THEN
        ALTER TABLE "Collections" ADD COLUMN "Status" integer NOT NULL DEFAULT 0;
        RAISE NOTICE 'Column Status added';
    ELSE
        RAISE NOTICE 'Column Status already exists';
    END IF;
END $$;

-- Add Price column
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'Collections' AND column_name = 'Price') THEN
        ALTER TABLE "Collections" ADD COLUMN "Price" numeric(18,2);
        RAISE NOTICE 'Column Price added';
    ELSE
        RAISE NOTICE 'Column Price already exists';
    END IF;
END $$;

-- Add Currency column (0 = EUR, 1 = USD)
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'Collections' AND column_name = 'Currency') THEN
        ALTER TABLE "Collections" ADD COLUMN "Currency" integer NOT NULL DEFAULT 0;
        RAISE NOTICE 'Column Currency added';
    ELSE
        RAISE NOTICE 'Column Currency already exists';
    END IF;
END $$;

-- Verify
SELECT column_name, data_type, column_default 
FROM information_schema.columns 
WHERE table_name = 'Collections' 
  AND column_name IN ('Status', 'Price', 'Currency')
ORDER BY column_name;
EOSQL

echo ""
echo "✅ Database migration complete!"
echo ""

echo "🔧 Fix 2: Rebuilding web container with navbar fix..."
$DOCKER_COMPOSE -f docker-compose.prod.yml build web

echo ""
echo "🔄 Restarting web container..."
docker restart aas-web-prod

echo ""
echo "⏳ Waiting for application to start..."
sleep 5

echo ""
echo "📊 Checking application status..."
docker logs aas-web-prod --tail 20

echo ""
echo "=========================================="
echo "✅ Fixes Applied!"
echo "=========================================="
echo ""
echo "🌐 Test the application:"
echo "   https://aristocraticartworksale.com"
echo ""
echo "Expected results:"
echo "   ✅ Collections page works (no 500 error)"
echo "   ✅ Navbar items centered"
echo "   ✅ Price and Status fields available in admin"
echo ""
