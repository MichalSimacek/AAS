#!/bin/bash

# Helper script to stop running containers and redeploy
cd /AAS

echo "=========================================="
echo "🛑 Stopping running containers..."
echo "=========================================="

# Try Docker Compose V2 first
if docker compose version &> /dev/null; then
    echo "Using Docker Compose V2..."
    docker compose -f docker-compose.prod.yml down
else
    echo "Using Docker Compose V1..."
    docker-compose -f docker-compose.prod.yml down 2>/dev/null || docker stop aas-nginx-prod aas-web-prod aas-certbot aas-db-prod 2>/dev/null
fi

echo "✅ Containers stopped"
echo ""
echo "=========================================="
echo "🚀 Running deployment..."
echo "=========================================="
echo ""

# Run deployment
./deploy.sh
