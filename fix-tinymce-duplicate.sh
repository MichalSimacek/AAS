#!/bin/bash
# Quick fix pro TinyMCE duplicate fields

echo "🔧 Fixing TinyMCE duplicate textarea issue..."

# Restart Docker container s force rebuild
cd /var/www/aristocratic-artwork-sale

echo "📦 Pulling latest changes..."
git pull origin main

echo "🔨 Force rebuilding web container..."
sudo docker compose -f docker-compose.prod.yml build --no-cache web

echo "🚀 Restarting services..."
sudo docker compose -f docker-compose.prod.yml up -d --force-recreate web

echo "⏳ Waiting for startup (20s)..."
sleep 20

echo ""
echo "✅ Restart complete!"
echo ""
echo "📝 Nyní v prohlížeči:"
echo "   1. Otevřete Developer Tools (F12)"
echo "   2. Application tab → Clear Storage → Clear site data"
echo "   3. Nebo stiskněte: Ctrl + Shift + Delete → Clear all"
echo "   4. Hard refresh: Ctrl + Shift + R"
echo ""
echo "   Pak jděte na: Admin → Blog → Create"
echo ""
echo "🔍 V konzoli byste měli vidět:"
echo "   'TinyMCE initialized: tinymce-content'"
echo "   'Content synced: ...'"
echo ""
echo "✅ Měli byste vidět JEN JEDEN editor!"
