#!/bin/bash
# Aristocratic Artwork Sale - Deployment Script
# This script builds and deploys the application

set -e

APP_NAME="aas"
APP_DIR="/var/www/aas"
SERVICE_NAME="aas.service"
PUBLISH_DIR="./publish"

echo "======================================"
echo "AAS - Deployment Script"
echo "======================================"

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo "Please run as root (use sudo)"
    exit 1
fi

# Stop service if running
echo "Stopping service..."
systemctl stop $SERVICE_NAME || true

# Build application
echo "Building application..."
cd /var/www/aas/src/AAS.Web
~/.dotnet/dotnet publish -c Release -o $PUBLISH_DIR

# Create app directory if not exists
mkdir -p $APP_DIR/app
mkdir -p $APP_DIR/app/wwwroot/uploads/images
mkdir -p $APP_DIR/app/wwwroot/uploads/audio

# Copy published files
echo "Copying files..."
cp -r $PUBLISH_DIR/* $APP_DIR/app/
chown -R www-data:www-data $APP_DIR

# Create systemd service
echo "Creating systemd service..."
cat > /etc/systemd/system/$SERVICE_NAME << 'EOF'
[Unit]
Description=Aristocratic Artwork Sale Web Application
After=network.target postgresql.service

[Service]
Type=notify
WorkingDirectory=/var/www/aas/app
ExecStart=/root/.dotnet/dotnet /var/www/aas/app/AAS.Web.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=aas
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000
Environment=DOTNET_ROOT=/root/.dotnet

[Install]
WantedBy=multi-user.target
EOF

# Reload systemd and start service
echo "Starting service..."
systemctl daemon-reload
systemctl enable $SERVICE_NAME
systemctl start $SERVICE_NAME

# Configure Nginx
echo "Configuring Nginx..."
cat > /etc/nginx/sites-available/aas << 'EOF'
server {
    listen 80;
    server_name aristocraticartworksale.com www.aristocraticartworksale.com;

    # Redirect HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name aristocraticartworksale.com www.aristocraticartworksale.com;

    # SSL certificates (configure with Certbot)
    # ssl_certificate /etc/letsencrypt/live/aristocraticartworksale.com/fullchain.pem;
    # ssl_certificate_key /etc/letsencrypt/live/aristocraticartworksale.com/privkey.pem;

    # SSL configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_prefer_server_ciphers on;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;

    client_max_body_size 100M;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }

    # Static files caching
    location ~* \.(jpg|jpeg|png|gif|ico|css|js|svg|woff|woff2|ttf|eot)$ {
        proxy_pass http://localhost:5000;
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
EOF

# Enable site
ln -sf /etc/nginx/sites-available/aas /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default

# Test and reload Nginx
nginx -t
systemctl reload nginx

echo "======================================"
echo "Deployment completed successfully!"
echo "======================================"
echo ""
echo "Service status:"
systemctl status $SERVICE_NAME --no-pager
echo ""
echo "Next steps:"
echo "1. Configure SSL with: sudo certbot --nginx -d aristocraticartworksale.com -d www.aristocraticartworksale.com"
echo "2. Update appsettings.json with your SMTP and translation settings"
echo "3. Restart service: sudo systemctl restart aas"
echo ""
echo "Admin credentials:"
echo "Email: admin@aristocraticartworksale.com"
echo "Password: ChangeMe_Aristo#2025"
echo ""
