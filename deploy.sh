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

# Default compose file (updated to use prod config with static files fix)
COMPOSE_FILE="docker-compose.prod.yml"

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

# Auto-fix database host configuration
FIXED=false

# Fix DB_HOST for Docker Compose setup (must be 'db' not localhost)
if [ "$DB_HOST" = "localhost" ] || [ "$DB_HOST" = "127.0.0.1" ] || [ "$DB_HOST" = "host.docker.internal" ]; then
    print_info "Auto-fixing DB_HOST: $DB_HOST → db (for Docker Compose network)"
    sed -i "s/DB_HOST=.*/DB_HOST=db/g" .env.production
    DB_HOST="db"
    FIXED=true
fi

if [ "$EMAIL_SMTP_HOST" = "host.docker.internal" ]; then
    print_info "Auto-fixing EMAIL_SMTP_HOST: host.docker.internal → 127.0.0.1"
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

# Step 4: Select docker-compose file
echo "Step 4: Configuring deployment..."

# Check available compose files
if [ -f "docker-compose.prod.yml" ]; then
    COMPOSE_FILE="docker-compose.prod.yml"
    print_success "Using docker-compose.prod.yml (with static files optimization)"
elif [ -f "docker-compose.host.yml" ]; then
    COMPOSE_FILE="docker-compose.host.yml"
    print_success "Using docker-compose.host.yml (for host PostgreSQL + ProtonMail Bridge)"
elif [ -f "docker-compose.production.yml" ]; then
    COMPOSE_FILE="docker-compose.production.yml"
    print_success "Using docker-compose.production.yml"
else
    print_error "No production docker-compose file found!"
    print_info "Looking for: docker-compose.prod.yml, docker-compose.host.yml, or docker-compose.production.yml"
    exit 1
fi

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

# Check if ports are used by Docker containers (which is OK)
DOCKER_NGINX_RUNNING=false
if docker ps --format '{{.Names}}' 2>/dev/null | grep -q "nginx"; then
    DOCKER_NGINX_RUNNING=true
    print_info "Docker Nginx container detected (will be replaced)"
fi

PORT_80_IN_USE=false
PORT_443_IN_USE=false
NON_DOCKER_CONFLICT=false

# Check port 80
if ss -tulpn 2>/dev/null | grep -q ":80 "; then
    PORT_80_IN_USE=true
    # Check if it's NOT a Docker process
    if ! ss -tulpn 2>/dev/null | grep ":80 " | grep -q "docker-proxy"; then
        if [ "$DOCKER_NGINX_RUNNING" = false ]; then
            NON_DOCKER_CONFLICT=true
            print_warning "Port 80 is in use by non-Docker process"
            ss -tulpn | grep ":80 "
        fi
    fi
fi

# Check port 443
if ss -tulpn 2>/dev/null | grep -q ":443 "; then
    PORT_443_IN_USE=true
    # Check if it's NOT a Docker process
    if ! ss -tulpn 2>/dev/null | grep ":443 " | grep -q "docker-proxy"; then
        if [ "$DOCKER_NGINX_RUNNING" = false ]; then
            NON_DOCKER_CONFLICT=true
            print_warning "Port 443 is in use by non-Docker process"
            ss -tulpn | grep ":443 "
        fi
    fi
fi

if [ "$NON_DOCKER_CONFLICT" = true ]; then
    print_error "Ports 80/443 are in use by non-Docker services."
    print_info "Stop conflicting services first:"
    echo "  - sudo supervisorctl stop nginx-code-proxy"
    echo "  - sudo systemctl stop nginx apache2"
    exit 1
fi

if [ "$DOCKER_NGINX_RUNNING" = true ]; then
    print_success "Ports used by Docker (will be updated during deployment)"
else
    print_success "Ports 80 and 443 are available"
fi
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

# Build the application with no cache for fresh static files
print_info "Building Docker image (with static files optimization)..."
$DOCKER_COMPOSE -f $COMPOSE_FILE build --no-cache web

# Start services
print_info "Starting services..."

# Detect available services
AVAILABLE_SERVICES=$($DOCKER_COMPOSE -f $COMPOSE_FILE config --services)
SERVICES_TO_START=""

# Always try to start these if they exist
for service in db web nginx certbot; do
    if echo "$AVAILABLE_SERVICES" | grep -q "^${service}$"; then
        SERVICES_TO_START="$SERVICES_TO_START $service"
    fi
done

if [ "$SKIP_SSL_SETUP" = true ]; then
    print_warning "Starting WITHOUT SSL (HTTP only)"
    # Remove nginx and certbot from list
    SERVICES_TO_START=$(echo "$SERVICES_TO_START" | sed 's/nginx//g' | sed 's/certbot//g')
fi

# Start the services
if [ -z "$SERVICES_TO_START" ]; then
    print_error "No services found to start!"
    exit 1
fi

print_info "Starting services:$SERVICES_TO_START"
$DOCKER_COMPOSE -f $COMPOSE_FILE up -d $SERVICES_TO_START

print_success "Services started"

# Wait for static files to be copied
print_info "Waiting for static files initialization..."
sleep 5

# Verify static files are available in Nginx
if docker ps | grep -q "nginx"; then
    NGINX_CONTAINER=$(docker ps --filter "name=nginx" --format "{{.Names}}" | head -1)
    if [ ! -z "$NGINX_CONTAINER" ]; then
        if docker exec $NGINX_CONTAINER ls /app/wwwroot/ > /dev/null 2>&1; then
            print_success "Static files initialized in Nginx"
        else
            print_warning "Static files might not be initialized yet"
        fi
    fi
fi

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
