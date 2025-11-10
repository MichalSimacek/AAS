#!/bin/bash

# Automated Deployment Script for Aristocratic Artwork Sale
# Specifically configured for: Host PostgreSQL + ProtonMail Bridge
# Author: AI Assistant
# Date: 2025-11-09

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo ""
echo "=========================================="
echo "  Aristocratic Artwork Sale"
echo "  Automated Production Deployment"
echo "=========================================="
echo ""

# Default compose file
COMPOSE_FILE="docker-compose.production.yml"

# Function to print colored messages
print_success() { echo -e "${GREEN}✅ $1${NC}"; }
print_error() { echo -e "${RED}❌ $1${NC}"; }
print_warning() { echo -e "${YELLOW}⚠️  $1${NC}"; }
print_info() { echo -e "${BLUE}ℹ️  $1${NC}"; }

# Step 1: Check prerequisites
echo "Step 1: Checking prerequisites..."
if ! command -v docker &> /dev/null; then
    print_error "Docker is not installed"
    exit 1
fi
print_success "Docker is installed"

# Detect which docker-compose command to use
if docker compose version &> /dev/null; then
    DOCKER_COMPOSE="docker compose"
    print_success "Docker Compose V2 detected"
elif command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker-compose"
    print_success "Docker Compose V1 detected"
else
    print_error "Docker Compose is not installed"
    exit 1
fi
echo ""

# Step 2: Stop old container if running
echo "Step 2: Stopping old containers..."
if docker ps -a | grep -q "aas-web"; then
    print_info "Found old container, stopping..."
    docker stop aas-web 2>/dev/null || true
    docker rm aas-web 2>/dev/null || true
    print_success "Old container removed"
else
    print_info "No old container found"
fi
echo ""

# Step 3: Verify .env.production exists and is configured
echo "Step 3: Checking environment configuration..."
if [ ! -f .env.production ]; then
    print_error ".env.production not found!"
    print_info "Please create it with correct values first"
    exit 1
fi

# Source environment variables
set -a
source .env.production
set +a

# Validate critical variables
CRITICAL_VARS=("DB_HOST" "DB_PASSWORD" "EMAIL_SMTP_HOST" "EMAIL_PASSWORD" "ADMIN_PASSWORD")
MISSING=()

for var in "${CRITICAL_VARS[@]}"; do
    if [ -z "${!var}" ]; then
        MISSING+=("$var")
    fi
done

if [ ${#MISSING[@]} -ne 0 ]; then
    print_error "Missing required variables in .env.production:"
    printf '   - %s\n' "${MISSING[@]}"
    exit 1
fi

# Auto-fix host connectivity configuration for network_mode: host
FIXED=false

if [ "$DB_HOST" = "host.docker.internal" ]; then
    print_info "Auto-fixing DB_HOST: host.docker.internal → localhost (for network_mode: host)"
    sed -i 's/DB_HOST=host.docker.internal/DB_HOST=localhost/g' .env.production
    DB_HOST="localhost"
    FIXED=true
fi

if [ "$EMAIL_SMTP_HOST" = "host.docker.internal" ]; then
    print_info "Auto-fixing EMAIL_SMTP_HOST: host.docker.internal → 127.0.0.1 (for network_mode: host)"
    sed -i 's/EMAIL_SMTP_HOST=host.docker.internal/EMAIL_SMTP_HOST=127.0.0.1/g' .env.production
    EMAIL_SMTP_HOST="127.0.0.1"
    FIXED=true
fi

if [ "$FIXED" = true ]; then
    print_success "Updated .env.production for host network mode"
    # Reload environment variables
    set -a
    source .env.production
    set +a
fi

# Verify configuration
if [ "$DB_HOST" != "localhost" ]; then
    print_warning "DB_HOST is '$DB_HOST' - expected 'localhost' for host PostgreSQL with network_mode: host"
fi

if [ "$EMAIL_SMTP_HOST" != "127.0.0.1" ] && [ "$EMAIL_SMTP_HOST" != "localhost" ]; then
    print_warning "EMAIL_SMTP_HOST is '$EMAIL_SMTP_HOST' - expected '127.0.0.1' or 'localhost'"
fi

print_success "Environment configuration validated"
echo ""

# Step 4: Use host-optimized docker-compose
echo "Step 4: Configuring for host services..."

# Use docker-compose.host.yml which connects to host PostgreSQL and ProtonMail Bridge
COMPOSE_FILE="docker-compose.host.yml"

if [ ! -f "$COMPOSE_FILE" ]; then
    print_error "docker-compose.host.yml not found!"
    print_info "This file should be in the repository"
    exit 1
fi

print_success "Using $COMPOSE_FILE (configured for host PostgreSQL + ProtonMail Bridge)"
echo ""

# Step 5: Check DNS
echo "Step 5: Verifying DNS configuration..."
DOMAIN="aristocraticartworksale.com"
print_info "Checking if $DOMAIN points to this server..."

SERVER_IP=$(curl -s ifconfig.me 2>/dev/null || echo "unknown")
DOMAIN_IP=$(dig +short $DOMAIN 2>/dev/null | tail -n1 || echo "unknown")

if [ "$SERVER_IP" != "$DOMAIN_IP" ]; then
    print_warning "DNS might not be configured"
    echo "   Domain resolves to: $DOMAIN_IP"
    echo "   Server IP: $SERVER_IP"
    print_info "SSL certificates will fail if DNS is not correct"
    read -p "Continue? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
else
    print_success "DNS configured correctly ($DOMAIN → $SERVER_IP)"
fi
echo ""

# Step 6: Check ports
echo "Step 6: Checking port availability..."
PORT_80_IN_USE=false
PORT_443_IN_USE=false

if ss -tulpn 2>/dev/null | grep -q ":80 "; then
    PORT_80_IN_USE=true
    print_warning "Port 80 is in use"
    ss -tulpn | grep ":80 "
fi

if ss -tulpn 2>/dev/null | grep -q ":443 "; then
    PORT_443_IN_USE=true
    print_warning "Port 443 is in use"
    ss -tulpn | grep ":443 "
fi

if [ "$PORT_80_IN_USE" = true ] || [ "$PORT_443_IN_USE" = true ]; then
    print_error "Ports 80/443 are in use. Stop conflicting services first."
    echo "Hint: sudo systemctl stop nginx apache2"
    exit 1
fi

print_success "Ports 80 and 443 are available"
echo ""

# Step 7: Create directories
echo "Step 7: Creating required directories..."
mkdir -p certbot/conf certbot/www uploads backups
chmod 755 certbot certbot/conf certbot/www uploads backups 2>/dev/null || true
print_success "Directories created"
echo ""

# Step 8: SSL Certificate Setup
echo "Step 8: SSL Certificate Setup..."
echo ""

if [ -f "certbot/conf/live/aristocraticartworksale.com/fullchain.pem" ]; then
    print_success "SSL certificates already exist"
    SKIP_SSL_SETUP=false
else
    print_info "SSL certificates not found. Need to obtain from Let's Encrypt."
    echo ""
    echo "Choose SSL setup method:"
    echo "  1) Automatic - Obtain certificates from Let's Encrypt (recommended)"
    echo "  2) Manual - I'll provide certificates later"
    echo "  3) Skip - Deploy without HTTPS (NOT RECOMMENDED for production)"
    echo ""
    read -p "Enter choice (1/2/3): " ssl_choice
    
    if [ "$ssl_choice" = "1" ]; then
        echo ""
        print_info "Starting automatic SSL setup..."
        
        # Create temporary Nginx config for ACME challenge
        cat > nginx/nginx.init.conf << 'NGINX_EOF'
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
            return 200 "Server ready for SSL setup\n";
            add_header Content-Type text/plain;
        }
    }
}
NGINX_EOF
        
        # Backup and modify docker-compose
        cp $COMPOSE_FILE $COMPOSE_FILE.ssl_backup
        sed -i 's|nginx.prod.conf|nginx.init.conf|g' $COMPOSE_FILE
        
        print_info "Starting Nginx for ACME challenge..."
        $DOCKER_COMPOSE -f $COMPOSE_FILE up -d nginx
        
        sleep 3
        
        # Get email for Let's Encrypt
        read -p "Enter email for Let's Encrypt notifications: " LE_EMAIL
        
        print_info "Requesting SSL certificates..."
        echo ""
        echo "Request certificate for:"
        echo "  1) aristocraticartworksale.com only"
        echo "  2) aristocraticartworksale.com + www.aristocraticartworksale.com (requires www DNS record)"
        read -p "Enter choice (1 or 2): " cert_choice
        
        if [ "$cert_choice" = "2" ]; then
            CERT_DOMAINS="-d aristocraticartworksale.com -d www.aristocraticartworksale.com"
        else
            CERT_DOMAINS="-d aristocraticartworksale.com"
        fi
        
        if $DOCKER_COMPOSE -f $COMPOSE_FILE run --rm --entrypoint certbot certbot certonly \
          --webroot \
          --webroot-path=/var/www/certbot \
          --email "$LE_EMAIL" \
          --agree-tos \
          --no-eff-email \
          $CERT_DOMAINS; then
            
            print_success "SSL certificates obtained!"
            
            # Restore production config
            mv $COMPOSE_FILE.ssl_backup $COMPOSE_FILE
            
            # Stop temporary setup
            $DOCKER_COMPOSE -f $COMPOSE_FILE down
            
            SKIP_SSL_SETUP=false
        else
            print_error "Failed to obtain SSL certificates"
            print_info "Restoring configuration..."
            mv $COMPOSE_FILE.ssl_backup $COMPOSE_FILE
            $DOCKER_COMPOSE -f $COMPOSE_FILE down
            exit 1
        fi
        
    elif [ "$ssl_choice" = "2" ]; then
        print_info "Manual SSL setup selected"
        print_info "Place your certificates at:"
        echo "  - certbot/conf/live/aristocraticartworksale.com/fullchain.pem"
        echo "  - certbot/conf/live/aristocraticartworksale.com/privkey.pem"
        read -p "Press Enter when certificates are in place..."
        
        if [ ! -f "certbot/conf/live/aristocraticartworksale.com/fullchain.pem" ]; then
            print_error "Certificates not found"
            exit 1
        fi
        SKIP_SSL_SETUP=false
        
    elif [ "$ssl_choice" = "3" ]; then
        print_warning "Deploying without HTTPS - NOT RECOMMENDED"
        SKIP_SSL_SETUP=true
    fi
fi
echo ""

# Step 9: Build and deploy
echo "Step 9: Building and deploying application..."
print_info "This may take a few minutes..."

# Export env variables for docker-compose
export $(cat .env.production | grep -v '^#' | xargs)

# Build the application
print_info "Building Docker image..."
$DOCKER_COMPOSE -f $COMPOSE_FILE build web

# Start services (exclude db since we use host PostgreSQL)
print_info "Starting services..."
if [ "$SKIP_SSL_SETUP" = true ]; then
    print_warning "Starting WITHOUT SSL (HTTP only)"
    $DOCKER_COMPOSE -f $COMPOSE_FILE up -d web
else
    $DOCKER_COMPOSE -f $COMPOSE_FILE up -d web nginx certbot
fi

print_success "Services started"
echo ""

# Step 10: Wait and verify
echo "Step 10: Verifying deployment..."
print_info "Waiting for services to stabilize..."
sleep 10

# Check service status
echo ""
print_info "Service Status:"
$DOCKER_COMPOSE -f $COMPOSE_FILE ps

# Test connection
echo ""
print_info "Testing application..."
if [ "$SKIP_SSL_SETUP" = true ]; then
    if curl -f -s -o /dev/null http://localhost:8080 2>/dev/null; then
        print_success "Application is responding on port 8080"
    else
        print_warning "Application might not be responding yet"
    fi
else
    if curl -f -s -I https://aristocraticartworksale.com 2>/dev/null | grep -q "200 OK"; then
        print_success "Application is responding on HTTPS"
    elif curl -f -s -I http://aristocraticartworksale.com 2>/dev/null | grep -q "301\|302"; then
        print_success "HTTP to HTTPS redirect is working"
    else
        print_warning "Application might not be responding yet (give it a minute)"
    fi
fi

echo ""
echo "=========================================="
print_success "Deployment Complete! 🎉"
echo "=========================================="
echo ""

if [ "$SKIP_SSL_SETUP" = true ]; then
    echo "⚠️  Application deployed WITHOUT HTTPS"
    echo ""
    echo "Access your application at:"
    echo "  http://aristocraticartworksale.com"
else
    echo "🔒 Application deployed with HTTPS"
    echo ""
    echo "Access your application at:"
    echo "  https://aristocraticartworksale.com"
fi

echo ""
echo "👤 Admin Login:"
echo "  Email: $ADMIN_EMAIL"
echo "  Password: (as configured in .env.production)"
echo ""
echo "📋 Useful Commands:"
echo "  View logs:    $DOCKER_COMPOSE -f $COMPOSE_FILE logs -f"
echo "  Restart:      $DOCKER_COMPOSE -f $COMPOSE_FILE restart"
echo "  Stop:         $DOCKER_COMPOSE -f $COMPOSE_FILE down"
echo "  Update app:   git pull && ./deploy.sh"
echo ""
echo "📖 Documentation: See PRODUCTION_DEPLOYMENT.md for details"
echo ""
print_success "Happy selling! 🎨"
echo ""
