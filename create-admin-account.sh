#!/bin/bash

# Create Admin Account - Troubleshooting Script

set -e

echo "=========================================="
echo "  Admin Account Creation"
echo "=========================================="
echo ""

# Check .env.production
if [ ! -f .env.production ]; then
    echo "❌ .env.production not found!"
    exit 1
fi

source .env.production

echo "📋 Checking configuration..."
echo ""

# Validate admin credentials
if [ -z "$ADMIN_EMAIL" ]; then
    echo "❌ ADMIN_EMAIL is not set in .env.production"
    exit 1
fi

if [ -z "$ADMIN_PASSWORD" ]; then
    echo "❌ ADMIN_PASSWORD is not set in .env.production"
    exit 1
fi

if [ ${#ADMIN_PASSWORD} -lt 12 ]; then
    echo "❌ ADMIN_PASSWORD must be at least 12 characters long"
    echo "   Current length: ${#ADMIN_PASSWORD}"
    exit 1
fi

echo "✅ ADMIN_EMAIL: $ADMIN_EMAIL"
echo "✅ ADMIN_PASSWORD length: ${#ADMIN_PASSWORD} characters"
echo ""

# Check if running
if ! docker ps | grep -q aas-web-prod; then
    echo "❌ Web container is not running"
    echo "   Start it with: ./deploy-production.sh"
    exit 1
fi

echo "📊 Current state:"
echo ""

# Check environment in container
echo "1. Environment variables in container:"
docker exec aas-web-prod env | grep -E "(ADMIN_EMAIL|ADMIN_PASSWORD)" || echo "   ⚠️  Admin variables not found in container!"
echo ""

# Check database
echo "2. Users in database:"
USER_COUNT=$(docker exec aas-db-prod psql -U aas -d aas -t -c "SELECT COUNT(*) FROM \"AspNetUsers\";")
echo "   Total users: $USER_COUNT"

if [ "$USER_COUNT" -gt 0 ]; then
    echo ""
    echo "   Existing users:"
    docker exec aas-db-prod psql -U aas -d aas -c "SELECT \"Email\", \"EmailConfirmed\", \"UserName\" FROM \"AspNetUsers\";"
fi

echo ""
echo "3. Web container logs (last 50 lines):"
docker logs aas-web-prod --tail 50
echo ""

echo "=========================================="
echo "  Solutions"
echo "=========================================="
echo ""

if docker exec aas-web-prod env | grep -q ADMIN_EMAIL; then
    echo "✅ Environment variables ARE being passed to container"
    echo ""
    echo "If admin wasn't created, check logs above for errors."
    echo "Common issues:"
    echo "  - Password doesn't meet requirements (12+ chars, upper, lower, digit, special)"
    echo "  - Database connection issues during startup"
    echo ""
    echo "To force recreation:"
    echo "  ./deploy-production.sh --rebuild --clean"
else
    echo "❌ Environment variables are NOT being passed to container!"
    echo ""
    echo "This is the problem. Fix: restart with proper env loading"
    echo ""
    echo "Run: ./deploy-production.sh --rebuild"
fi

echo ""
