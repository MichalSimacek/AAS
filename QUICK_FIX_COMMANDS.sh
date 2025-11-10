#!/bin/bash

# Quick fix commands pro HTTP 400 problém se statickými soubory
# Použití: ./QUICK_FIX_COMMANDS.sh

set -e

echo "========================================"
echo "🔧 Quick Fix - HTTP 400 Static Files"
echo "========================================"

# Barvy
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Funkce pro logování
log_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

log_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

log_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

log_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Kontrola, zda jsme v správném adresáři
if [ ! -f "docker-compose.prod.yml" ]; then
    log_error "docker-compose.prod.yml not found!"
    log_info "Please run this script from the project root directory"
    exit 1
fi

log_success "Found docker-compose.prod.yml"

# Krok 1: Zastavení běžících kontejnerů
echo ""
log_info "Step 1: Stopping running containers..."
docker-compose -f docker-compose.prod.yml down
log_success "Containers stopped"

# Krok 2: Odstranění orphaned volumes (volitelné)
echo ""
read -p "Do you want to remove orphaned volumes? (y/N) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    log_info "Removing orphaned volumes..."
    docker volume prune -f
    log_success "Volumes cleaned"
fi

# Krok 3: Rebuild s --no-cache
echo ""
log_info "Step 2: Rebuilding containers (this may take a few minutes)..."
docker-compose -f docker-compose.prod.yml build --no-cache

if [ $? -eq 0 ]; then
    log_success "Build completed successfully"
else
    log_error "Build failed!"
    exit 1
fi

# Krok 4: Spuštění kontejnerů
echo ""
log_info "Step 3: Starting containers..."
docker-compose -f docker-compose.prod.yml up -d

if [ $? -eq 0 ]; then
    log_success "Containers started"
else
    log_error "Failed to start containers!"
    exit 1
fi

# Krok 5: Čekání na inicializaci
echo ""
log_info "Waiting for containers to initialize (30 seconds)..."
for i in {1..30}; do
    echo -n "."
    sleep 1
done
echo ""

# Krok 6: Kontrola logů
echo ""
log_info "Step 4: Checking logs..."
echo ""
log_info "Web container logs:"
docker-compose -f docker-compose.prod.yml logs --tail=20 web | grep -E "(Static|Error|Started)" || true

echo ""
log_info "Nginx container logs:"
docker-compose -f docker-compose.prod.yml logs --tail=10 nginx || true

# Krok 7: Kontrola status
echo ""
log_info "Step 5: Checking container status..."
docker-compose -f docker-compose.prod.yml ps

# Krok 8: Test statických souborů
echo ""
log_info "Step 6: Testing static files in Nginx container..."
if docker exec aas-nginx-prod ls -la /app/wwwroot/ > /dev/null 2>&1; then
    log_success "Static files directory found in Nginx!"
    docker exec aas-nginx-prod ls /app/wwwroot/
else
    log_error "Static files directory NOT found in Nginx!"
    log_warning "Check if entrypoint script executed correctly"
fi

# Krok 9: Návod na další testování
echo ""
echo "========================================"
log_success "Deployment Complete!"
echo "========================================"
echo ""
log_info "Next steps:"
echo "  1. Test HTTP requests:"
echo "     curl -I https://yourdomain.com/css/site.css"
echo ""
echo "  2. Run comprehensive test:"
echo "     ./test-static-files.sh yourdomain.com"
echo ""
echo "  3. Monitor logs:"
echo "     docker-compose -f docker-compose.prod.yml logs -f"
echo ""
echo "  4. Check for errors:"
echo "     docker exec aas-nginx-prod tail -f /var/log/nginx/error.log"
echo ""

# Nabídka spuštění live logs
read -p "Do you want to see live logs now? (y/N) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    log_info "Showing live logs (Ctrl+C to exit)..."
    docker-compose -f docker-compose.prod.yml logs -f
fi
