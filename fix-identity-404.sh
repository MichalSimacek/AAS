#!/bin/bash

# Fix Identity 404 - Restart Nginx with updated config

set -e

echo "=========================================="
echo "  Fixing Identity Pages 404"
echo "=========================================="
echo ""

echo "✅ Updated nginx.prod.conf:"
echo "   /Identity/ now proxies to backend (not static files)"
echo ""

echo "🔄 Restarting Nginx..."
docker restart aas-nginx-prod

echo ""
echo "⏳ Waiting for Nginx to start..."
sleep 5

echo ""
echo "🧪 Testing Identity routes..."
echo ""

echo "1. Testing /Identity/Account/Login:"
STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://aristocraticartworksale.com/Identity/Account/Login)
if [ "$STATUS" = "200" ]; then
    echo "   ✅ Login page: $STATUS OK"
else
    echo "   ❌ Login page: $STATUS (expected 200)"
fi

echo ""
echo "2. Testing /Identity/Account/Register:"
STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://aristocraticartworksale.com/Identity/Account/Register)
if [ "$STATUS" = "200" ]; then
    echo "   ✅ Register page: $STATUS OK"
else
    echo "   ❌ Register page: $STATUS (expected 200)"
fi

echo ""
echo "=========================================="
echo "✅ Identity Pages Fixed!"
echo "=========================================="
echo ""
echo "🌐 Test in browser:"
echo "   Login:    https://aristocraticartworksale.com/Identity/Account/Login"
echo "   Register: https://aristocraticartworksale.com/Identity/Account/Register"
echo ""
