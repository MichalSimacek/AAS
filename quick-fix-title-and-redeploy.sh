#!/bin/bash

echo "🔧 Quick Fix: Adding name='Title' attribute and redeploying"

cd /app

# Commit změny
git add src/AAS.Web/Areas/Admin/Views/Collections/Edit.cshtml
git commit -m "Fix: Add explicit name='Title' attribute to Edit form"

echo ""
echo "✅ Změny commitnuty. Nyní je potřeba:"
echo ""
echo "1. Push do GitHubu:"
echo "   git push origin main"
echo ""
echo "2. Na serveru pull a rebuild:"
echo "   cd /AAS"
echo "   sudo git pull origin main"
echo "   sudo docker compose -f docker-compose.prod.yml build --no-cache web"
echo "   sudo docker compose -f docker-compose.prod.yml up -d --force-recreate web"
echo ""
