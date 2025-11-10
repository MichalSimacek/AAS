#!/bin/bash

# Fix Badges Display and Complete Email Configuration

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo ""
echo "=========================================="
echo "  Fix Badges & Complete Email Setup"
echo "=========================================="
echo ""

if [ ! -f .env.production ]; then
    echo -e "${RED}❌ .env.production not found!${NC}"
    exit 1
fi

# Detect docker-compose
if docker compose version &> /dev/null; then
    DOCKER_COMPOSE="docker compose"
elif command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker-compose"
else
    echo -e "${RED}❌ Docker Compose not found${NC}"
    exit 1
fi

echo "📋 Changes included:"
echo ""
echo "1. ✅ Collections Index Controller - Fixed to load Status, Price, Currency"
echo "2. ✅ Status badge visible on collection thumbnails"
echo "3. ✅ Contact page - Email updated to info@aristocraticartworksale.com"
echo "4. ✅ EmailService - Fixed ENV variable names (EMAIL_SMTP_HOST, etc.)"
echo "5. ✅ EmailSenderAdapter - Registration emails from noreply@aristocraticartworksale.com"
echo "6. ✅ Inquiry emails - Sent to inquiry@aristocraticartworksale.com"
echo ""

echo "🔍 Checking email configuration..."
set -a
source .env.production
set +a

if [ -z "$EMAIL_SMTP_HOST" ]; then
    echo -e "${YELLOW}⚠️  EMAIL_SMTP_HOST not set in .env.production${NC}"
    echo "   Email functionality will log to console only (development mode)"
else
    echo -e "${GREEN}✅ EMAIL_SMTP_HOST: $EMAIL_SMTP_HOST${NC}"
fi

if [ -z "$EMAIL_FROM" ]; then
    echo -e "${YELLOW}⚠️  EMAIL_FROM not set, will use default: noreply@aristocraticartworksale.com${NC}"
else
    echo -e "${GREEN}✅ EMAIL_FROM: $EMAIL_FROM${NC}"
fi

if [ -z "$EMAIL_TO" ]; then
    echo -e "${YELLOW}⚠️  EMAIL_TO not set for inquiries${NC}"
else
    echo -e "${GREEN}✅ EMAIL_TO (inquiries): $EMAIL_TO${NC}"
fi

echo ""
echo "🛑 Stopping containers..."
$DOCKER_COMPOSE -f docker-compose.prod.yml down

echo ""
echo "🔨 Rebuilding web container (--no-cache)..."
$DOCKER_COMPOSE -f docker-compose.prod.yml build --no-cache web

echo ""
echo "🚀 Starting services..."
$DOCKER_COMPOSE -f docker-compose.prod.yml --env-file .env.production up -d

echo ""
echo "⏳ Waiting for services..."
sleep 10

echo ""
echo "🧪 Testing application..."
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://aristocraticartworksale.com/Collections 2>&1 || echo "000")
if [ "$HTTP_STATUS" = "200" ]; then
    echo -e "${GREEN}✅ Collections page: $HTTP_STATUS OK${NC}"
else
    echo -e "${YELLOW}⚠️  Collections page: $HTTP_STATUS${NC}"
fi

echo ""
echo "=========================================="
echo "✅ Deployment Complete!"
echo "=========================================="
echo ""
echo "📧 Email Configuration:"
echo "   • Registration emails: FROM noreply@aristocraticartworksale.com"
echo "   • Inquiry emails: TO inquiry@aristocraticartworksale.com"
echo "   • Contact page: info@aristocraticartworksale.com"
echo ""
echo "🟢 Status Badges:"
echo "   • Now visible on Collections index page"
echo "   • Green 'AVAILABLE' / Red 'SOLD'"
echo "   • Price displayed for available items"
echo ""
echo "🧪 Testing Email:"
echo ""
echo "1. Registration email:"
echo "   - Register new user"
echo "   - Check email for confirmation link"
echo "   - Check logs: docker logs aas-web-prod | grep -i email"
echo ""
echo "2. Inquiry email:"
echo "   - Go to collection detail"
echo "   - Click 'I'm Interested'"
echo "   - Fill form and submit"
echo "   - Check inquiry@aristocraticartworksale.com"
echo ""
echo "💡 If emails don't arrive:"
echo "   - Check ProtonMail Bridge is running"
echo "   - Verify EMAIL_SMTP_HOST=host.docker.internal"
echo "   - Check logs: docker logs aas-web-prod --tail 100"
echo ""
