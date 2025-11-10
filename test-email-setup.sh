#!/bin/bash

echo "🔧 Testing Email Configuration"
echo "=============================="
echo ""

echo "1️⃣ Checking ProtonMail Bridge status:"
ps aux | grep protonmail-bridge | grep -v grep
echo ""

echo "2️⃣ Checking if port 1025 is listening:"
netstat -tulpn | grep 1025 || ss -tulpn | grep 1025
echo ""

echo "3️⃣ Testing connection from host:"
nc -zv localhost 1025 2>&1 || echo "nc not available, trying telnet..."
echo ""

echo "4️⃣ Checking Docker network configuration:"
docker exec aas-web-prod cat /etc/hosts | grep host.docker.internal || echo "host.docker.internal not found in /etc/hosts"
echo ""

echo "5️⃣ Testing connection from Docker container to host:"
docker exec aas-web-prod ping -c 2 host.docker.internal 2>&1 || echo "Cannot ping host.docker.internal"
echo ""

echo "6️⃣ Checking environment variables in container:"
docker exec aas-web-prod env | grep EMAIL
echo ""

echo "=============================="
echo "✅ If ProtonMail Bridge is running and port 1025 is open,"
echo "   rebuild the Docker image with the email fix:"
echo ""
echo "   cd /AAS"
echo "   sudo docker compose -f docker-compose.prod.yml build --no-cache web"
echo "   sudo docker compose -f docker-compose.prod.yml up -d --force-recreate web"
echo ""
