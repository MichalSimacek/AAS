#!/bin/bash

# Fix Price Display and Layout Issues

set -e

echo "=========================================="
echo "  Fixing Price Display & Layout"
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

echo "📋 Fixes applied:"
echo ""
echo "1. ✅ Edit Controller: Now saves Status, Price, Currency"
echo "2. ✅ Detail View: Price & Status moved INSIDE photo card"
echo "3. ✅ Detail View: Button placed directly under photos"
echo "4. ✅ Detail View: Removed duplicate price/status from right column"
echo "5. ✅ Better layout - no footer overlap"
echo ""

echo "🔨 Rebuilding web container..."
$DOCKER_COMPOSE -f docker-compose.prod.yml build web

echo ""
echo "🔄 Restarting services..."
docker restart aas-web-prod
docker restart aas-nginx-prod

echo ""
echo "⏳ Waiting for application..."
sleep 8

echo ""
echo "🧪 Testing application..."
STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://aristocraticartworksale.com/Collections 2>&1 || echo "000")
if [ "$STATUS" = "200" ]; then
    echo "✅ Collections page: $STATUS OK"
else
    echo "⚠️  Collections page: $STATUS"
fi

echo ""
echo "=========================================="
echo "✅ Fixes Applied!"
echo "=========================================="
echo ""
echo "🌐 Test the changes:"
echo "   1. Go to Admin panel"
echo "   2. Edit a collection"
echo "   3. Set Status = Available, Price = 5000, Currency = EUR"
echo "   4. Save"
echo "   5. View the collection detail page"
echo ""
echo "Expected result:"
echo "   ✅ Status badge (green AVAILABLE) visible under photos"
echo "   ✅ Price displayed: '5 000 €' under photos"
echo "   ✅ 'I'm Interested' button under price"
echo "   ✅ No footer overlap"
echo "   ✅ Clean layout in single card"
echo ""
