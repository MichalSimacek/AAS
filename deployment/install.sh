#!/bin/bash
# Aristocratic Artwork Sale - Installation Script for Ubuntu Server
# This script installs all dependencies and sets up the application

set -e

echo "======================================"
echo "AAS - Installation Script"
echo "======================================"

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo "Please run as root (use sudo)"
    exit 1
fi

# Update system
echo "Updating system packages..."
apt update && apt upgrade -y

# Install PostgreSQL
echo "Installing PostgreSQL..."
apt install -y postgresql postgresql-contrib

# Install .NET 9.0
echo "Installing .NET 9.0..."
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0
rm dotnet-install.sh

# Add .NET to PATH for all users
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> /etc/profile.d/dotnet.sh
echo 'export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools' >> /etc/profile.d/dotnet.sh
source /etc/profile.d/dotnet.sh

# Install Nginx
echo "Installing Nginx..."
apt install -y nginx

# Configure PostgreSQL
echo "Configuring PostgreSQL..."
sudo -u postgres psql -c "CREATE USER aas WITH PASSWORD 'ChangeMeStrong!';" || true
sudo -u postgres psql -c "CREATE DATABASE aas OWNER aas;" || true
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE aas TO aas;" || true

# Enable and start PostgreSQL
systemctl enable postgresql
systemctl start postgresql

echo "======================================"
echo "Installation completed successfully!"
echo "======================================"
echo ""
echo "Next steps:"
echo "1. Copy your application files to /var/www/aas"
echo "2. Configure appsettings.json with your SMTP settings"
echo "3. Run deploy.sh to build and deploy the application"
echo ""
