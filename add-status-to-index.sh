#!/bin/bash

# Add Status Badge to Collections Index

set -e

echo "=========================================="
echo "  Adding Status to Collections Index"
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

echo "📋 Changes:"
echo "   ✅ Status badge overlay on collection thumbnails"
echo "   ✅ AVAILABLE (green) / SOLD (red)"
echo "   ✅ Price displayed under image (if available)"
echo "   ✅ Position: Top right corner of image"
echo ""

echo "🔨 Rebuilding web container..."
$DOCKER_COMPOSE -f docker-compose.prod.yml build web

echo ""
echo "🔄 Restarting web container..."
docker restart aas-web-prod

echo ""
echo "⏳ Waiting for application..."
sleep 5

echo ""
echo "🧪 Testing Collections page..."
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://aristocraticartworksale.com/Collections 2>&1 || echo "000")
if [ "$HTTP_STATUS" = "200" ]; then
    echo "✅ Collections page: $HTTP_STATUS OK"
else
    echo "⚠️  Collections page: $HTTP_STATUS"
fi

echo ""
echo "=========================================="
echo "✅ Status Badges Added to Index!"
echo "=========================================="
echo ""
echo "🌐 Test it:"
echo "   https://aristocraticartworksale.com/Collections"
echo ""
echo "You should see:"
echo "   🟢 Green 'AVAILABLE' badge on available items"
echo "   🔴 Red 'SOLD' badge on sold items"
echo "   💰 Price displayed under image (for available items with price)"
echo "   📍 Badge position: Top right corner of thumbnail"
echo ""
