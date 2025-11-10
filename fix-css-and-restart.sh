#!/bin/bash

# Fix CSS Loading and Restart Script
# This script fixes Nginx configuration and restarts all services

set -e

echo "=========================================="
echo "  Fixing CSS Loading Issue"
echo "=========================================="
echo ""

# Check if .env.production exists
if [ ! -f .env.production ]; then
    echo "❌ ERROR: .env.production not found!"
    exit 1
fi

echo "✅ Found .env.production"
echo ""

# Load environment variables
set -a
source .env.production
set +a

# Detect docker-compose command
if docker compose version &> /dev/null; then
    DOCKER_COMPOSE="docker compose"
elif command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker-compose"
else
    echo "❌ Docker Compose not found"
    exit 1
fi

echo "🔧 Updated Nginx configuration to properly serve static files"
echo ""

echo "🛑 Stopping services..."
$DOCKER_COMPOSE -f docker-compose.prod.yml --env-file .env.production down

echo ""
echo "🚀 Starting services..."
$DOCKER_COMPOSE -f docker-compose.prod.yml --env-file .env.production up -d

echo ""
echo "⏳ Waiting for services to be ready..."
sleep 10

echo ""
echo "🔍 Checking Nginx configuration..."
docker exec aas-nginx-prod nginx -t

echo ""
echo "📋 Checking static files in shared volume..."
docker exec aas-nginx-prod ls -la /app/wwwroot/ || echo "⚠️  Could not list files"

echo ""
echo "🌐 Testing static file access..."
echo "   Checking CSS..."
docker exec aas-nginx-prod ls -la /app/wwwroot/css/ || echo "⚠️  CSS directory not found"

echo "   Checking JS..."
docker exec aas-nginx-prod ls -la /app/wwwroot/js/ || echo "⚠️  JS directory not found"

echo "   Checking images..."
docker exec aas-nginx-prod ls -la /app/wwwroot/images/ || echo "⚠️  Images directory not found"

echo ""
echo "📋 Service Status:"
docker ps --filter "name=aas-" --format "table {{.Names}}\t{{.Status}}"

echo ""
echo "=========================================="
echo "✅ Fix Applied and Services Restarted!"
echo "=========================================="
echo ""
echo "🔍 Test CSS loading:"
echo "   1. Open browser: https://aristocraticartworksale.com"
echo "   2. Open DevTools (F12) -> Network tab"
echo "   3. Refresh page (Ctrl+F5)"
echo "   4. Check if CSS files load with status 200"
echo ""
echo "If CSS still doesn't load, check logs:"
echo "  docker logs aas-nginx-prod --tail 50"
echo "  docker logs aas-web-prod --tail 50"
echo ""
