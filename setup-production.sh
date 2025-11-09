#!/bin/bash

# Production Setup Script for Aristocratic Artwork Sale
# This script helps set up the production environment step by step

set -e

echo "=========================================="
echo "Aristocratic Artwork Sale - Production Setup"
echo "=========================================="
echo ""

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo -e "${RED}❌ Docker is not installed${NC}"
    echo "Please install Docker first: https://docs.docker.com/get-docker/"
    exit 1
fi

if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
    echo -e "${RED}❌ Docker Compose is not installed${NC}"
    echo "Please install Docker Compose: https://docs.docker.com/compose/install/"
    exit 1
fi

echo -e "${GREEN}✅ Docker is installed${NC}"
echo ""

# Check if .env.production exists
if [ ! -f .env.production ]; then
    echo -e "${YELLOW}⚠️  .env.production not found${NC}"
    echo "Creating from template..."
    cp .env.production.example .env.production
    chmod 600 .env.production
    echo -e "${GREEN}✅ Created .env.production${NC}"
    echo ""
    echo -e "${YELLOW}⚠️  IMPORTANT: Edit .env.production and fill in all values!${NC}"
    echo "Run: nano .env.production"
    echo ""
    read -p "Press Enter when you've configured .env.production..."
fi

# Source .env.production
export $(cat .env.production | grep -v '^#' | xargs)

# Check required variables
REQUIRED_VARS=("DB_PASSWORD" "EMAIL_SMTP_HOST" "EMAIL_USERNAME" "EMAIL_PASSWORD" "ADMIN_EMAIL" "ADMIN_PASSWORD")
MISSING_VARS=()

for var in "${REQUIRED_VARS[@]}"; do
    if [ -z "${!var}" ] || [[ "${!var}" == *"CHANGE_ME"* ]]; then
        MISSING_VARS+=("$var")
    fi
done

if [ ${#MISSING_VARS[@]} -ne 0 ]; then
    echo -e "${RED}❌ Missing or unchanged required variables:${NC}"
    printf '   - %s\n' "${MISSING_VARS[@]}"
    echo ""
    echo "Please edit .env.production and set all required values"
    exit 1
fi

echo -e "${GREEN}✅ All required environment variables are set${NC}"
echo ""

# Check if domain points to this server
echo "Checking DNS configuration..."
DOMAIN="aristocraticartworksale.com"
SERVER_IP=$(curl -s ifconfig.me)
DOMAIN_IP=$(dig +short $DOMAIN | tail -n1)

if [ "$SERVER_IP" != "$DOMAIN_IP" ]; then
    echo -e "${YELLOW}⚠️  Warning: Domain DNS may not be configured correctly${NC}"
    echo "   Domain $DOMAIN resolves to: $DOMAIN_IP"
    echo "   Server IP: $SERVER_IP"
    echo ""
    read -p "Continue anyway? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
else
    echo -e "${GREEN}✅ DNS configured correctly${NC}"
fi
echo ""

# Check if ports 80 and 443 are available
if ss -tulpn | grep -q :80; then
    echo -e "${RED}❌ Port 80 is already in use${NC}"
    echo "Please stop the service using port 80:"
    ss -tulpn | grep :80
    exit 1
fi

if ss -tulpn | grep -q :443; then
    echo -e "${RED}❌ Port 443 is already in use${NC}"
    echo "Please stop the service using port 443:"
    ss -tulpn | grep :443
    exit 1
fi

echo -e "${GREEN}✅ Ports 80 and 443 are available${NC}"
echo ""

# Create directories
echo "Creating required directories..."
mkdir -p certbot/conf certbot/www uploads backups
echo -e "${GREEN}✅ Directories created${NC}"
echo ""

# Ask about SSL setup
echo "=========================================="
echo "SSL Certificate Setup"
echo "=========================================="
echo ""
echo "Do you already have SSL certificates?"
echo "1) No, obtain new certificates from Let's Encrypt (recommended)"
echo "2) Yes, I have existing certificates"
echo ""
read -p "Enter choice (1 or 2): " ssl_choice

if [ "$ssl_choice" = "1" ]; then
    echo ""
    echo "Starting initial setup for certificate generation..."
    
    # Create init nginx config
    cat > nginx/nginx.init.conf << 'EOF'
user nginx;
worker_processes auto;
error_log /var/log/nginx/error.log warn;
pid /var/run/nginx.pid;

events {
    worker_connections 1024;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;
    
    server {
        listen 80;
        listen [::]:80;
        server_name aristocraticartworksale.com www.aristocraticartworksale.com;

        location /.well-known/acme-challenge/ {
            root /var/www/certbot;
        }

        location / {
            return 200 "Server is ready for SSL setup\n";
            add_header Content-Type text/plain;
        }
    }
}
EOF
    
    # Temporarily modify docker-compose
    sed -i.bak 's|nginx.prod.conf|nginx.init.conf|g' docker-compose.production.yml
    
    # Start nginx for ACME challenge
    echo "Starting Nginx for ACME challenge..."
    docker-compose -f docker-compose.production.yml up -d nginx
    
    sleep 3
    
    # Request certificates
    echo ""
    read -p "Enter your email for Let's Encrypt notifications: " le_email
    
    echo "Requesting SSL certificates..."
    docker-compose -f docker-compose.production.yml run --rm certbot certonly \
      --webroot \
      --webroot-path=/var/www/certbot \
      --email "$le_email" \
      --agree-tos \
      --no-eff-email \
      -d aristocraticartworksale.com \
      -d www.aristocraticartworksale.com
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ SSL certificates obtained successfully${NC}"
        
        # Restore production config
        mv docker-compose.production.yml.bak docker-compose.production.yml
        
        # Stop init setup
        docker-compose -f docker-compose.production.yml down
    else
        echo -e "${RED}❌ Failed to obtain SSL certificates${NC}"
        echo "Please check the error messages above"
        exit 1
    fi
elif [ "$ssl_choice" = "2" ]; then
    echo ""
    echo "Please place your certificates in:"
    echo "  certbot/conf/live/aristocraticartworksale.com/fullchain.pem"
    echo "  certbot/conf/live/aristocraticartworksale.com/privkey.pem"
    echo ""
    read -p "Press Enter when certificates are in place..."
    
    if [ ! -f "certbot/conf/live/aristocraticartworksale.com/fullchain.pem" ]; then
        echo -e "${RED}❌ Certificate files not found${NC}"
        exit 1
    fi
fi

echo ""
echo "=========================================="
echo "Starting Production Stack"
echo "=========================================="
echo ""

# Start full stack
docker-compose -f docker-compose.production.yml up -d

echo ""
echo "Waiting for services to start..."
sleep 10

# Check service status
echo ""
echo "Service Status:"
docker-compose -f docker-compose.production.yml ps

echo ""
echo "=========================================="
echo "Deployment Complete! 🎉"
echo "=========================================="
echo ""
echo "Your application should now be running at:"
echo "  https://aristocraticartworksale.com"
echo ""
echo "Admin login:"
echo "  Email: $ADMIN_EMAIL"
echo "  Password: (as configured in .env.production)"
echo ""
echo "Useful commands:"
echo "  View logs: docker-compose -f docker-compose.production.yml logs -f"
echo "  Restart: docker-compose -f docker-compose.production.yml restart"
echo "  Stop: docker-compose -f docker-compose.production.yml down"
echo ""
echo "For more information, see PRODUCTION_DEPLOYMENT.md"
