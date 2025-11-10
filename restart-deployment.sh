#!/bin/bash

# Quick Restart Script with Environment File
# This script ensures .env.production is loaded and restarts the services

set -e

echo "=========================================="
echo "  Restarting AAS Application"
echo "=========================================="
echo ""

# Check if .env.production exists
if [ ! -f .env.production ]; then
    echo "❌ ERROR: .env.production not found!"
    echo "Please create .env.production file first"
    exit 1
fi

echo "✅ Found .env.production"
echo ""

# Load environment variables
set -a
source .env.production
set +a

echo "📋 Environment variables loaded:"
echo "   DB_HOST=$DB_HOST"
echo "   DB_NAME=$DB_NAME"
echo "   DB_USER=$DB_USER"
echo ""

# Detect docker-compose command
if docker compose version &> /dev/null; then
    DOCKER_COMPOSE="docker compose"
elif command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker-compose"
else
    echo "❌ Docker Compose not found"
    exit 1
fi

echo "🛑 Stopping existing containers..."
$DOCKER_COMPOSE -f docker-compose.prod.yml --env-file .env.production down

echo ""
echo "🗑️  Removing old volumes (to ensure clean start)..."
docker volume rm aas_postgres-data 2>/dev/null || echo "   No old postgres volume found"

echo ""
echo "🚀 Starting services with health checks..."
$DOCKER_COMPOSE -f docker-compose.prod.yml --env-file .env.production up -d --build

echo ""
echo "⏳ Waiting for database to be healthy..."
timeout=60
counter=0
while [ $counter -lt $timeout ]; do
    if docker inspect --format='{{.State.Health.Status}}' aas-db-prod 2>/dev/null | grep -q "healthy"; then
        echo "✅ Database is healthy!"
        break
    fi
    echo "   Waiting... ($counter/$timeout)"
    sleep 2
    counter=$((counter + 2))
done

if [ $counter -ge $timeout ]; then
    echo "❌ Database health check timeout!"
    echo "Checking logs:"
    docker logs aas-db-prod --tail 20
    exit 1
fi

echo ""
echo "📋 Checking web application logs..."
sleep 5
docker logs aas-web-prod --tail 30

echo ""
echo "=========================================="
echo "✅ Deployment complete!"
echo "=========================================="
echo ""
echo "To check logs:"
echo "  docker logs -f aas-web-prod"
echo "  docker logs -f aas-db-prod"
echo "  docker logs -f aas-nginx-prod"
echo ""
