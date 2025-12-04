#!/bin/bash
# Fix pro docker-compose Python 3.12 distutils problém

echo "🔧 Fixing docker-compose compatibility..."
echo ""

# Zjistit verzi Pythonu
PYTHON_VERSION=$(python3 --version 2>&1 | awk '{print $2}')
echo "Python verze: $PYTHON_VERSION"

# Zjistit docker-compose verzi
if command -v docker-compose &> /dev/null; then
    COMPOSE_VERSION=$(docker-compose --version 2>&1 | awk '{print $3}' | tr -d ',')
    echo "docker-compose verze: $COMPOSE_VERSION"
else
    echo "docker-compose není nainstalován"
fi

echo ""
echo "Vyberte řešení:"
echo "1) Nainstalovat python3-distutils (rychlé, ale deprecated)"
echo "2) Upgrade na Docker Compose v2 (doporučeno)"
echo "3) Obojí (nejbezpečnější)"
echo ""
read -p "Zadejte číslo (1-3): " choice

case $choice in
    1)
        echo ""
        echo "📦 Instaluji python3-distutils..."
        sudo apt update
        sudo apt install python3-distutils python3-setuptools -y
        echo ""
        echo "✅ Hotovo! Zkuste znovu:"
        echo "   docker-compose --version"
        ;;
    2)
        echo ""
        echo "⬆️  Upgraduji na Docker Compose v2..."
        
        # Odinstalace staré verze
        sudo apt remove docker-compose -y
        
        # Instalace compose v2 plugin
        sudo apt update
        sudo apt install docker-compose-plugin -y
        
        echo ""
        echo "✅ Hotovo! Nový příkaz je:"
        echo "   docker compose version"
        echo ""
        echo "Místo 'docker-compose' používejte 'docker compose' (bez pomlčky)"
        ;;
    3)
        echo ""
        echo "📦 Instaluji obojí..."
        
        # Distutils
        sudo apt update
        sudo apt install python3-distutils python3-setuptools -y
        
        # Compose v2
        sudo apt install docker-compose-plugin -y
        
        echo ""
        echo "✅ Hotovo! Můžete používat:"
        echo "   docker-compose (starý způsob)"
        echo "   docker compose (nový způsob)"
        ;;
    *)
        echo "Neplatná volba!"
        exit 1
        ;;
esac

echo ""
echo "🧪 Test:"
if docker compose version &> /dev/null; then
    echo "✅ 'docker compose' funguje!"
    docker compose version
fi

if docker-compose --version &> /dev/null; then
    echo "✅ 'docker-compose' funguje!"
    docker-compose --version
fi

echo ""
echo "🎉 Oprava dokončena!"
