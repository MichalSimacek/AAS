#!/bin/bash
# Aristocratic Artwork Sale - Server Update Script
# Aplikuje všechny změny z development na produkční server

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo "=========================================="
echo "  AAS - Server Update Script"
echo "=========================================="
echo ""

# Kontrola, zda jsme ve správném adresáři
if [ ! -f "docker-compose.prod.yml" ] && [ ! -f "src/AAS.Web/AAS.Web.csproj" ]; then
    echo -e "${RED}❌ Chyba: Spusťte tento script z root adresáře projektu!${NC}"
    exit 1
fi

# Detekce deployment typu
if [ -f "docker-compose.prod.yml" ]; then
    DEPLOYMENT_TYPE="docker"
    echo -e "${GREEN}🐳 Detekován Docker deployment${NC}"
elif systemctl is-active --quiet aas-web; then
    DEPLOYMENT_TYPE="systemd"
    echo -e "${GREEN}⚙️  Detekován Systemd deployment${NC}"
else
    echo -e "${RED}❌ Nelze detekovat typ deploymentu!${NC}"
    exit 1
fi

echo ""
read -p "Pokračovat s update? (y/n): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Update zrušen"
    exit 0
fi

# Detect Docker Compose command
if [ "$DEPLOYMENT_TYPE" == "docker" ]; then
    if docker compose version &> /dev/null 2>&1; then
        DOCKER_COMPOSE="docker compose"
        echo -e "${GREEN}✅ Použiji: docker compose (v2)${NC}"
    elif command -v docker-compose &> /dev/null; then
        DOCKER_COMPOSE="docker-compose"
        echo -e "${GREEN}✅ Použiji: docker-compose (v1)${NC}"
    else
        echo -e "${RED}❌ Docker Compose není nainstalován!${NC}"
        exit 1
    fi
fi

# Backup
echo ""
echo -e "${YELLOW}📦 Vytváření zálohy...${NC}"
BACKUP_FILE="../aas-backup-$(date +%Y%m%d-%H%M%S).tar.gz"

if [ "$DEPLOYMENT_TYPE" == "docker" ]; then
    sudo $DOCKER_COMPOSE -f docker-compose.prod.yml down
    sudo tar -czf "$BACKUP_FILE" .
    echo -e "${GREEN}✅ Záloha vytvořena: $BACKUP_FILE${NC}"
else
    sudo systemctl stop aas-web
    sudo tar -czf "$BACKUP_FILE" /var/www/aas-app
    echo -e "${GREEN}✅ Záloha vytvořena: $BACKUP_FILE${NC}"
fi

# Git pull
echo ""
echo -e "${YELLOW}⬇️  Stahování změn z git...${NC}"
if git pull origin main 2>/dev/null || git pull origin master 2>/dev/null; then
    echo -e "${GREEN}✅ Git pull úspěšný${NC}"
else
    echo -e "${YELLOW}⚠️  Git pull selhal, pokračuji bez pull${NC}"
fi

# TinyMCE download
echo ""
if [ ! -d "src/AAS.Web/wwwroot/lib/tinymce" ]; then
    echo -e "${YELLOW}📥 Stahuji TinyMCE...${NC}"
    cd /tmp
    wget -q https://download.tiny.cloud/tinymce/community/tinymce_7.5.1.zip
    unzip -q tinymce_7.5.1.zip
    
    # Vytvoř adresář pokud neexistuje
    mkdir -p "$OLDPWD/src/AAS.Web/wwwroot/lib"
    
    # Kopíruj TinyMCE
    cp -r tinymce/js/tinymce "$OLDPWD/src/AAS.Web/wwwroot/lib/"
    
    # Cleanup
    rm -rf tinymce tinymce_7.5.1.zip
    cd "$OLDPWD"
    
    echo -e "${GREEN}✅ TinyMCE nainstalován${NC}"
else
    echo -e "${GREEN}✅ TinyMCE již existuje${NC}"
fi

# Rebuild a restart
echo ""
if [ "$DEPLOYMENT_TYPE" == "docker" ]; then
    echo -e "${YELLOW}🔨 Rebuilding Docker image...${NC}"
    sudo $DOCKER_COMPOSE -f docker-compose.prod.yml build --no-cache web
    
    echo ""
    echo -e "${YELLOW}🚀 Spouštím služby...${NC}"
    sudo $DOCKER_COMPOSE -f docker-compose.prod.yml up -d
    
    echo ""
    echo -e "${YELLOW}⏳ Čekám na start (30s)...${NC}"
    sleep 30
    
    echo ""
    echo -e "${GREEN}📊 Stav služeb:${NC}"
    sudo $DOCKER_COMPOSE -f docker-compose.prod.yml ps
    
    echo ""
    echo -e "${GREEN}📝 Poslední logy:${NC}"
    sudo docker logs --tail 30 aas-web-prod
    
else
    echo -e "${YELLOW}🔨 Building aplikace...${NC}"
    cd src/AAS.Web
    dotnet publish -c Release -o /var/www/aas-app
    cd ../..
    
    echo ""
    echo -e "${YELLOW}🚀 Spouštím službu...${NC}"
    sudo systemctl start aas-web
    
    echo ""
    echo -e "${YELLOW}⏳ Čekám na start (10s)...${NC}"
    sleep 10
    
    echo ""
    echo -e "${GREEN}📊 Stav služby:${NC}"
    sudo systemctl status aas-web --no-pager -l
    
    echo ""
    echo -e "${GREEN}📝 Poslední logy:${NC}"
    sudo journalctl -u aas-web -n 30 --no-pager
fi

# Verifikace
echo ""
echo -e "${YELLOW}🔍 Verifikace deploymentu...${NC}"

# Test HTTP response
if curl -s http://localhost:5000 > /dev/null; then
    echo -e "${GREEN}✅ Aplikace odpovídá na HTTP${NC}"
else
    echo -e "${RED}❌ Aplikace neodpovídá!${NC}"
    echo -e "${YELLOW}Zkontrolujte logy výše${NC}"
fi

# Checklist
echo ""
echo "=========================================="
echo "  ✅ Update dokončen!"
echo "=========================================="
echo ""
echo -e "${GREEN}📋 Co bylo aktualizováno:${NC}"
echo "  ✅ Footer lokalizace"
echo "  ✅ Account Settings redesign (soft sidebar)"
echo "  ✅ Navigační 404 fix"
echo "  ✅ TinyMCE self-hosted (bez read-only)"
echo ""
echo -e "${YELLOW}🧪 Manuální verifikace:${NC}"
echo "  1. Otevřete web a změňte jazyk - zkontrolujte footer"
echo "  2. Přihlaste se a jděte do Account Settings"
echo "  3. Zkontrolujte, že navigace funguje (žádné 404)"
echo "  4. Jako admin vytvořte blog post - TinyMCE by měl fungovat"
echo ""
echo -e "${GREEN}📝 Užitečné příkazy:${NC}"

if [ "$DEPLOYMENT_TYPE" == "docker" ]; then
    # Detect compose command for hints
    if command -v docker &> /dev/null && docker compose version &> /dev/null; then
        COMPOSE_HINT="docker compose"
    else
        COMPOSE_HINT="docker-compose"
    fi
    echo "  - Logy:    sudo docker logs -f aas-web-prod"
    echo "  - Restart: sudo $COMPOSE_HINT -f docker-compose.prod.yml restart"
    echo "  - Stop:    sudo $COMPOSE_HINT -f docker-compose.prod.yml down"
else
    echo "  - Logy:    sudo journalctl -u aas-web -f"
    echo "  - Restart: sudo systemctl restart aas-web"
    echo "  - Stop:    sudo systemctl stop aas-web"
fi

echo ""
echo -e "${YELLOW}🔄 Rollback (pokud něco nefunguje):${NC}"
if [ "$DEPLOYMENT_TYPE" == "docker" ]; then
    if command -v docker &> /dev/null && docker compose version &> /dev/null; then
        COMPOSE_HINT="docker compose"
    else
        COMPOSE_HINT="docker-compose"
    fi
    echo "  sudo $COMPOSE_HINT -f docker-compose.prod.yml down"
    echo "  sudo tar -xzf $BACKUP_FILE"
    echo "  sudo $COMPOSE_HINT -f docker-compose.prod.yml up -d"
else
    echo "  sudo systemctl stop aas-web"
    echo "  sudo tar -xzf $BACKUP_FILE -C /"
    echo "  sudo systemctl start aas-web"
fi

echo ""
echo -e "${GREEN}🎉 Hotovo!${NC}"
