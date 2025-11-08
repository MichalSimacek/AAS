#!/bin/bash
set -e

if [ -z "$DOMAIN" ]; then
    echo "Error: DOMAIN not set!"
    exit 1
fi

if [ -z "$SSL_EMAIL" ]; then
    echo "Error: SSL_EMAIL not set!"
    exit 1
fi

mkdir -p nginx/ssl

echo "Installing certbot..."
if ! command -v certbot &> /dev/null; then
    if [ -f /etc/debian_version ]; then
        sudo apt-get update
        sudo apt-get install -y certbot
    elif [ -f /etc/redhat-release ]; then
        sudo yum install -y certbot
    fi
fi

echo "Obtaining SSL certificate for $DOMAIN..."
sudo certbot certonly --standalone -d $DOMAIN -d www.$DOMAIN --email $SSL_EMAIL --agree-tos --no-eff-email

echo "Copying certificates..."
sudo cp /etc/letsencrypt/live/$DOMAIN/fullchain.pem nginx/ssl/
sudo cp /etc/letsencrypt/live/$DOMAIN/privkey.pem nginx/ssl/
sudo chmod 644 nginx/ssl/*

echo "Setting up auto-renewal..."
echo "0 3 * * * certbot renew --quiet && cp /etc/letsencrypt/live/$DOMAIN/*.pem $PWD/nginx/ssl/ && docker-compose -f docker-compose.prod.yml restart nginx" | sudo crontab -

echo "SSL setup complete!"
