-- Test: Set price and status for first collection

-- Check current state
SELECT "Id", "Title", "Status", "Price", "Currency" 
FROM "Collections" 
ORDER BY "Id" 
LIMIT 5;

-- Update first collection to Available with price
UPDATE "Collections" 
SET 
    "Status" = 0,  -- 0 = Available
    "Price" = 5000.00,
    "Currency" = 0  -- 0 = EUR
WHERE "Id" = (SELECT "Id" FROM "Collections" ORDER BY "Id" LIMIT 1);

-- Verify update
SELECT "Id", "Title", "Status", "Price", "Currency" 
FROM "Collections" 
WHERE "Id" = (SELECT "Id" FROM "Collections" ORDER BY "Id" LIMIT 1);

-- Show URL slug for testing
SELECT "Slug", "Title", "Status", "Price", "Currency" 
FROM "Collections" 
WHERE "Id" = (SELECT "Id" FROM "Collections" ORDER BY "Id" LIMIT 1);
