#!/bin/bash

# Update Application with Price and Status Features

set -e

echo "=========================================="
echo "  Updating AAS Application"
echo "  Adding Price & Status Features"
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

echo "📋 Changes included:"
echo "   ✅ Model updated: Status, Price, Currency fields"
echo "   ✅ Database migration created"
echo "   ✅ Header: 'Home' link added, menu centered"
echo "   ✅ Collection Detail: Price & Status display"
echo "   ✅ Collection Detail: 'I'm Interested' button moved under photos"
echo "   ✅ Admin Create/Edit: Price & Status fields added"
echo "   ✅ Translations added for all 10 languages"
echo ""

echo "🔨 Rebuilding web container..."
$DOCKER_COMPOSE -f docker-compose.prod.yml build web

echo ""
echo "🛑 Stopping containers..."
$DOCKER_COMPOSE -f docker-compose.prod.yml --env-file .env.production down

echo ""
echo "🚀 Starting services..."
$DOCKER_COMPOSE -f docker-compose.prod.yml --env-file .env.production up -d

echo ""
echo "⏳ Waiting for database..."
sleep 10

echo ""
echo "📊 Checking services..."
docker ps --filter "name=aas-" --format "table {{.Names}}\t{{.Status}}"

echo ""
echo "=========================================="
echo "✅ Update Complete!"
echo "=========================================="
echo ""
echo "📝 New Features:"
echo "   • Collections now have Status (Available/Sold)"
echo "   • Price field with EUR/USD currency"
echo "   • SOLD items show red badge"
echo "   • Available items show price"
echo "   • 'I'm Interested' button under photos"
echo "   • Header menu centered with Home link"
echo ""
echo "🌐 Test the application:"
echo "   https://aristocraticartworksale.com"
echo ""
echo "👤 Admin panel:"
echo "   https://aristocraticartworksale.com/Admin/Collections"
echo "   Create/Edit collections with new Price & Status fields"
echo ""
