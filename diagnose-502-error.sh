#!/bin/bash

echo "🔍 Diagnosing 502 Bad Gateway Error"
echo "===================================="
echo ""

echo "1️⃣ Checking container status:"
docker ps -a | grep aas

echo ""
echo "2️⃣ Checking aas-web-prod logs (last 50 lines):"
docker logs --tail 50 aas-web-prod 2>&1

echo ""
echo "3️⃣ Checking if web container is running:"
if docker ps | grep -q aas-web-prod; then
    echo "✅ Container is running"
else
    echo "❌ Container is NOT running"
fi

echo ""
echo "4️⃣ Checking database connection:"
docker exec aas-db-prod pg_isready -U postgres 2>&1 || echo "❌ Database issue"

echo ""
echo "5️⃣ Checking nginx logs:"
docker logs --tail 20 aas-nginx-prod 2>&1

echo ""
echo "===================================="
echo "To restart the application, run:"
echo "cd /AAS"
echo "sudo docker compose -f docker-compose.prod.yml restart web"
echo ""
