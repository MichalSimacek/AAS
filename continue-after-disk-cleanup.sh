#!/bin/bash

# Continue build after disk space cleanup

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo ""
echo "=========================================="
echo "  Continuing Build After Disk Cleanup"
echo "=========================================="
echo ""

# Make sure TMPDIR is set for this session
export TMPDIR=/mnt/data/tmp
mkdir -p $TMPDIR

echo -e "${GREEN}✅ TMPDIR set to: $TMPDIR${NC}"

# Check disk space
echo ""
echo "📊 Current disk usage:"
df -h | grep -E "(Filesystem|/dev/mapper/vg1-root|/dev/mapper/vg1-data)"

echo ""
echo "🐳 Docker root directory:"
docker info | grep "Docker Root Dir"

echo ""
echo "📁 Project location:"
ls -la /AAS | head -5

if [ ! -f /AAS/.env.production ]; then
    echo -e "${YELLOW}⚠️  .env.production not found in /AAS${NC}"
    exit 1
fi

cd /AAS

# Detect docker-compose
if docker compose version &> /dev/null; then
    DOCKER_COMPOSE="docker compose"
elif command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker-compose"
else
    echo "❌ Docker Compose not found"
    exit 1
fi

echo ""
echo "🔨 Starting build (this may take several minutes)..."
echo ""

# Load environment
set -a
source .env.production
set +a

# Build with no cache to ensure fresh build
$DOCKER_COMPOSE -f docker-compose.prod.yml build --no-cache web

echo ""
echo "🚀 Starting services..."
$DOCKER_COMPOSE -f docker-compose.prod.yml --env-file .env.production up -d

echo ""
echo "⏳ Waiting for services to start..."
sleep 15

echo ""
echo "📋 Service status:"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

echo ""
echo "🧪 Testing application..."
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://aristocraticartworksale.com/Collections 2>&1 || echo "000")
if [ "$HTTP_STATUS" = "200" ]; then
    echo -e "${GREEN}✅ Collections page: $HTTP_STATUS OK${NC}"
else
    echo -e "${YELLOW}⚠️  Collections page: $HTTP_STATUS${NC}"
    echo "Check logs: docker logs aas-web-prod --tail 50"
fi

echo ""
echo "=========================================="
echo "✅ Build Complete!"
echo "=========================================="
echo ""
echo "💡 Important notes:"
echo ""
echo "1. TMPDIR is set for this session only"
echo "   To make it permanent, add to ~/.bashrc:"
echo "   echo 'export TMPDIR=/mnt/data/tmp' >> ~/.bashrc"
echo ""
echo "2. Your project is now at: /mnt/data/work/AAS"
echo "   Accessed via symlink: /AAS"
echo ""
echo "3. Docker data is now at: /mnt/data/docker"
echo "   (172GB available)"
echo ""
echo "🌐 Test your application:"
echo "   https://aristocraticartworksale.com"
echo ""
