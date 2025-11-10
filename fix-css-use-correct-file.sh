#!/bin/bash

# Fix CSS - Use site.css instead of site-new.css
# site-new.css is incomplete (588 lines) vs site.css (1326 lines)

set -e

echo "=========================================="
echo "  Fixing CSS - Using Complete File"
echo "=========================================="
echo ""

if [ ! -f .env.production ]; then
    echo "❌ ERROR: .env.production not found!"
    exit 1
fi

# Load environment variables
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

echo "✅ Changed _Layout.cshtml to use site.css (complete file)"
echo "   site-new.css: 588 lines (incomplete)"
echo "   site.css:     1326 lines (complete) ✅"
echo ""

echo "🔄 Rebuilding web container to copy new CSS..."
$DOCKER_COMPOSE -f docker-compose.prod.yml build web

echo ""
echo "🛑 Stopping containers..."
$DOCKER_COMPOSE -f docker-compose.prod.yml --env-file .env.production down

echo ""
echo "🚀 Starting services..."
$DOCKER_COMPOSE -f docker-compose.prod.yml --env-file .env.production up -d

echo ""
echo "⏳ Waiting for services..."
sleep 10

echo ""
echo "📋 Verifying CSS file in shared volume..."
docker exec aas-nginx-prod ls -lh /app/wwwroot/css/site.css 2>&1 || echo "⚠️  Could not check"

echo ""
echo "🌐 Testing CSS access..."
curl -I https://aristocraticartworksale.com/css/site.css 2>&1 | grep -E "(HTTP|content-type|content-length)" | head -5

echo ""
echo "=========================================="
echo "✅ CSS Fixed - Using Complete File!"
echo "=========================================="
echo ""
echo "🔍 Now test in browser:"
echo "   1. Open: https://aristocraticartworksale.com"
echo "   2. Hard refresh: Ctrl+Shift+R (or Cmd+Shift+R on Mac)"
echo "   3. F12 → Network tab - verify site.css loads (not site-new.css)"
echo ""
echo "Expected result:"
echo "   ✅ Logo normal size"
echo "   ✅ Proper layout and spacing"
echo "   ✅ Gold/dark color scheme"
echo "   ✅ Styled buttons and forms"
echo ""
