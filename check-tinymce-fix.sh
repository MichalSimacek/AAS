#!/bin/bash
# Check if TinyMCE fix is applied on server

echo "🔍 Checking if TinyMCE fix is applied..."
echo ""

# Check Create.cshtml
echo "📄 Checking Create.cshtml..."
if grep -q "id=\"tinymce-content\"" /var/www/aristocratic-artwork-sale/src/AAS.Web/Areas/Admin/Views/Blog/Create.cshtml; then
    echo "✅ Create.cshtml: NEW version (has tinymce-content div)"
else
    echo "❌ Create.cshtml: OLD version (missing tinymce-content div)"
fi

if grep -q "textarea.*tinymce-editor" /var/www/aristocratic-artwork-sale/src/AAS.Web/Areas/Admin/Views/Blog/Create.cshtml; then
    echo "⚠️  Create.cshtml: Still contains textarea (OLD)"
else
    echo "✅ Create.cshtml: No textarea found (NEW)"
fi

echo ""

# Check Edit.cshtml
echo "📄 Checking Edit.cshtml..."
if grep -q "id=\"tinymce-content\"" /var/www/aristocratic-artwork-sale/src/AAS.Web/Areas/Admin/Views/Blog/Edit.cshtml; then
    echo "✅ Edit.cshtml: NEW version (has tinymce-content div)"
else
    echo "❌ Edit.cshtml: OLD version (missing tinymce-content div)"
fi

if grep -q "textarea.*tinymce-editor" /var/www/aristocratic-artwork-sale/src/AAS.Web/Areas/Admin/Views/Blog/Edit.cshtml; then
    echo "⚠️  Edit.cshtml: Still contains textarea (OLD)"
else
    echo "✅ Edit.cshtml: No textarea found (NEW)"
fi

echo ""

# Check CSS
echo "📄 Checking site.css..."
if grep -q "textarea\[name=\"ContentCs\"\]" /var/www/aristocratic-artwork-sale/src/AAS.Web/wwwroot/css/site.css; then
    echo "✅ site.css: NEW version (has textarea hiding CSS)"
else
    echo "❌ site.css: OLD version (missing textarea CSS)"
fi

echo ""
echo "============================================"
echo ""

# Count how many are updated
NEW_COUNT=0
OLD_COUNT=0

if grep -q "id=\"tinymce-content\"" /var/www/aristocratic-artwork-sale/src/AAS.Web/Areas/Admin/Views/Blog/Create.cshtml; then
    ((NEW_COUNT++))
else
    ((OLD_COUNT++))
fi

if grep -q "id=\"tinymce-content\"" /var/www/aristocratic-artwork-sale/src/AAS.Web/Areas/Admin/Views/Blog/Edit.cshtml; then
    ((NEW_COUNT++))
else
    ((OLD_COUNT++))
fi

if grep -q "textarea\[name=\"ContentCs\"\]" /var/www/aristocratic-artwork-sale/src/AAS.Web/wwwroot/css/site.css; then
    ((NEW_COUNT++))
else
    ((OLD_COUNT++))
fi

if [ $OLD_COUNT -eq 0 ]; then
    echo "✅ ALL FILES UPDATED! ($NEW_COUNT/3)"
    echo ""
    echo "🚀 Now run:"
    echo "   sudo docker compose -f docker-compose.prod.yml restart web"
    echo ""
    echo "   Then in browser:"
    echo "   F12 → Application → Clear Storage → Clear site data"
    echo "   Ctrl + Shift + R (hard refresh)"
else
    echo "❌ SOME FILES ARE OLD! ($NEW_COUNT/3 updated, $OLD_COUNT/3 old)"
    echo ""
    echo "🔧 Run this to update:"
    echo "   cd /var/www/aristocratic-artwork-sale"
    echo "   git pull origin main"
    echo "   sudo docker compose -f docker-compose.prod.yml restart web"
fi
