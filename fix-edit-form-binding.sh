#!/bin/bash

# Fix Edit Form Binding Issues

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo ""
echo "=========================================="
echo "  Fix Edit Form Binding"
echo "=========================================="
echo ""

cd /AAS

if [ ! -f .env.production ]; then
    echo "❌ .env.production not found!"
    exit 1
fi

# Detect docker-compose
if docker compose version &> /dev/null; then
    DOCKER_COMPOSE="docker compose"
elif command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker-compose"
else
    echo "❌ Docker Compose not found"
    exit 1
fi

echo "📋 Changes:"
echo "   ✅ Edit method now reads form values directly"
echo "   ✅ Category will save correctly"
echo "   ✅ Price won't be overwritten if not changed"
echo "   ✅ Status and Currency will save correctly"
echo ""

echo "🔨 Rebuilding web container..."
$DOCKER_COMPOSE -f docker-compose.prod.yml build web

echo ""
echo "🔄 Restarting services..."
docker restart aas-web-prod

echo ""
echo "⏳ Waiting for application..."
sleep 8

echo ""
echo "🧪 Testing application..."
STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://aristocraticartworksale.com/Admin/Collections 2>&1 || echo "000")
if [ "$STATUS" = "200" ]; then
    echo -e "${GREEN}✅ Admin panel: $STATUS OK${NC}"
else
    echo -e "${YELLOW}⚠️  Admin panel: $STATUS${NC}"
fi

echo ""
echo "=========================================="
echo "✅ Fix Applied!"
echo "=========================================="
echo ""
echo "🧪 Test the fix:"
echo ""
echo "1. Go to admin panel:"
echo "   https://aristocraticartworksale.com/Admin/Collections"
echo ""
echo "2. Edit any collection:"
echo "   - Change Category to 'Watches'"
echo "   - Set Status to 'Available'"
echo "   - Set Price to '10000'"
echo "   - Set Currency to 'EUR'"
echo "   - Click Save"
echo ""
echo "3. Verify changes:"
echo "   - Category should be 'Watches' (not Paintings)"
echo "   - Price should be '10000' (not empty)"
echo "   - Status and Currency should match"
echo ""
echo "4. Edit again WITHOUT changing price:"
echo "   - Change only Category or Title"
echo "   - Don't touch Price field"
echo "   - Click Save"
echo "   - Price should remain '10000' (not become empty)"
echo ""
