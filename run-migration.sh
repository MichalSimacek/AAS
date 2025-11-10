#!/bin/bash

# Run database migration for Price and Status fields

set -e

echo "=========================================="
echo "  Running Database Migration"
echo "=========================================="
echo ""

echo "📊 Adding Status, Price, and Currency columns to Collections table..."

docker exec -i aas-db-prod psql -U aas -d aas << 'EOSQL'
-- Add Status column (0 = Available, 1 = Sold)
ALTER TABLE "Collections" 
ADD COLUMN IF NOT EXISTS "Status" integer NOT NULL DEFAULT 0;

-- Add Price column
ALTER TABLE "Collections" 
ADD COLUMN IF NOT EXISTS "Price" numeric(18,2);

-- Add Currency column (0 = EUR, 1 = USD)
ALTER TABLE "Collections" 
ADD COLUMN IF NOT EXISTS "Currency" integer NOT NULL DEFAULT 0;

-- Verify columns
\d "Collections"
EOSQL

echo ""
echo "✅ Migration complete!"
echo ""
echo "📋 Columns added:"
echo "   - Status (integer, default 0 = Available)"
echo "   - Price (decimal, optional)"
echo "   - Currency (integer, default 0 = EUR)"
echo ""
