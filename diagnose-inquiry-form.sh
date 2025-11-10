#!/bin/bash

echo "🔍 Diagnosing Inquiry Form Issue"
echo "================================"
echo ""

echo "1️⃣ Check recent logs (last 100 lines):"
docker logs --tail 100 aas-web-prod 2>&1 | grep -E "INQUIRY|fail:|error:|POST|Inquiries"

echo ""
echo "2️⃣ Check if route exists:"
docker exec aas-web-prod grep -r "Inquiries" /app/*.dll 2>/dev/null || echo "Cannot search DLL"

echo ""
echo "3️⃣ Test inquiry endpoint from inside container:"
docker exec aas-web-prod curl -X POST http://localhost:5000/Inquiries/Create \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "FirstName=Test&LastName=Test&Email=test@test.com" 2>&1 | head -20

echo ""
echo "4️⃣ Check DataProtection keys:"
ls -la /AAS/dataprotection-keys/ 2>&1 || echo "Directory doesn't exist"

echo ""
echo "================================"
echo "Please also:"
echo "1. Clear browser cookies completely"
echo "2. Try in incognito mode"
echo "3. Check browser Network tab for full request details"
