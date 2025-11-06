#!/bin/bash
# Quick update script for code changes

set -e

echo "Updating AAS application..."

# Stop service
sudo systemctl stop aas

# Pull latest changes (if using git)
# git pull

# Build and publish
cd /var/www/aas/src/AAS.Web
~/.dotnet/dotnet publish -c Release -o ./publish

# Copy files
sudo cp -r ./publish/* /var/www/aas/app/
sudo chown -R www-data:www-data /var/www/aas

# Start service
sudo systemctl start aas

echo "Update completed!"
sudo systemctl status aas --no-pager
