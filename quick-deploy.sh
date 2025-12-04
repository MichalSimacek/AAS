#!/bin/bash
# Aristocratic Artwork Sale - Quick Deployment Script pro Ubuntu
# Tento script automaticky nainstaluje a spustí aplikaci

set -e  # Exit při chybě

echo "=========================================="
echo "  AAS - Automatická Instalace na Ubuntu"
echo "=========================================="
echo ""

# Kontrola roota
if [ "$EUID" -eq 0 ]; then 
   echo "⚠️  Nespouštějte tento script jako root!"
   echo "Spusťte: ./quick-deploy.sh"
   exit 1
fi

# Výběr metody instalace
echo "Vyberte metodu instalace:"
echo "1) Docker Compose (Doporučeno - Nejjednodušší)"
echo "2) Systemd Service (Bez Dockeru)"
echo "3) Pouze instalace závislostí"
echo ""
read -p "Zadejte číslo (1-3): " choice

case $choice in
    1)
        METHOD="docker"
        ;;
    2)
        METHOD="systemd"
        ;;
    3)
        METHOD="dependencies"
        ;;
    *)
        echo "Neplatná volba!"
        exit 1
        ;;
esac

echo ""
echo "📦 Instaluji závislosti..."

# Aktualizace systému
sudo apt update -qq
sudo apt upgrade -y -qq

if [ "$METHOD" == "docker" ]; then
    echo "🐳 Instaluji Docker a Docker Compose..."
    
    # Instalace Dockeru
    if ! command -v docker &> /dev/null; then
        curl -fsSL https://get.docker.com -o /tmp/get-docker.sh
        sudo sh /tmp/get-docker.sh
        sudo usermod -aG docker $USER
        rm /tmp/get-docker.sh
    else
        echo "✅ Docker již je nainstalován"
    fi
    
    # Instalace Docker Compose
    if ! command -v docker-compose &> /dev/null; then
        sudo apt install docker-compose -y -qq
    else
        echo "✅ Docker Compose již je nainstalován"
    fi
    
    echo ""
    echo "🔧 Konfigurace..."
    
    # Vytvoření .env souboru
    if [ ! -f .env ]; then
        echo "Zadejte údaje pro konfiguraci:"
        read -p "PostgreSQL heslo (nebo Enter pro generování): " DB_PASS
        
        if [ -z "$DB_PASS" ]; then
            DB_PASS=$(openssl rand -base64 32)
            echo "📝 Vygenerováno DB heslo: $DB_PASS"
        fi
        
        cat > .env << EOF
POSTGRES_USER=aas_user
POSTGRES_PASSWORD=$DB_PASS
POSTGRES_DB=aas_production
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
EOF
        chmod 600 .env
        echo "✅ .env soubor vytvořen"
    fi
    
    echo ""
    echo "🚀 Spouštím aplikaci..."
    
    # Build a start
    docker-compose -f docker-compose.prod.yml build --no-cache
    docker-compose -f docker-compose.prod.yml up -d
    
    echo ""
    echo "⏳ Čekám na start aplikace (30s)..."
    sleep 30
    
    # Kontrola stavu
    echo ""
    echo "📊 Stav služeb:"
    docker-compose -f docker-compose.prod.yml ps
    
    echo ""
    echo "✅ Instalace dokončena!"
    echo ""
    echo "📝 Užitečné příkazy:"
    echo "  - Zobrazit logy:  docker logs -f aas-web-prod"
    echo "  - Restart:        docker-compose -f docker-compose.prod.yml restart"
    echo "  - Stop:           docker-compose -f docker-compose.prod.yml down"
    echo ""
    echo "🌐 Aplikace běží na: http://localhost:5000"
    
elif [ "$METHOD" == "systemd" ]; then
    echo "⚙️  Instaluji .NET SDK a PostgreSQL..."
    
    # .NET SDK
    if ! command -v dotnet &> /dev/null; then
        wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
        chmod +x /tmp/dotnet-install.sh
        /tmp/dotnet-install.sh --channel 8.0
        
        # Přidat do PATH
        echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
        echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
        export DOTNET_ROOT=$HOME/.dotnet
        export PATH=$PATH:$HOME/.dotnet
    else
        echo "✅ .NET SDK již je nainstalován"
    fi
    
    # PostgreSQL
    if ! command -v psql &> /dev/null; then
        sudo apt install postgresql postgresql-contrib -y -qq
        sudo systemctl start postgresql
        sudo systemctl enable postgresql
    else
        echo "✅ PostgreSQL již je nainstalován"
    fi
    
    echo ""
    echo "🗄️  Konfigurace databáze..."
    
    read -p "PostgreSQL heslo (nebo Enter pro generování): " DB_PASS
    if [ -z "$DB_PASS" ]; then
        DB_PASS=$(openssl rand -base64 32)
        echo "📝 Vygenerováno DB heslo: $DB_PASS"
    fi
    
    # Vytvoření databáze
    sudo -u postgres psql << EOF
DROP DATABASE IF EXISTS aas_production;
DROP USER IF EXISTS aas_user;
CREATE USER aas_user WITH PASSWORD '$DB_PASS';
CREATE DATABASE aas_production OWNER aas_user;
GRANT ALL PRIVILEGES ON DATABASE aas_production TO aas_user;
EOF
    
    echo ""
    echo "🔨 Building aplikace..."
    
    cd src/AAS.Web
    dotnet restore
    dotnet publish -c Release -o /var/www/aas-app
    
    # Connection string
    sudo mkdir -p /var/www/aas-app
    sudo cat > /var/www/aas-app/appsettings.Production.json << EOF
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=aas_production;Username=aas_user;Password=$DB_PASS"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*"
}
EOF
    
    echo ""
    echo "🚀 Vytváření systemd service..."
    
    sudo tee /etc/systemd/system/aas-web.service > /dev/null << EOF
[Unit]
Description=Aristocratic Artwork Sale Web Application
After=network.target postgresql.service

[Service]
Type=notify
User=$USER
WorkingDirectory=/var/www/aas-app
ExecStart=$HOME/.dotnet/dotnet /var/www/aas-app/AAS.Web.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=aas-web
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
EOF
    
    sudo systemctl daemon-reload
    sudo systemctl enable aas-web
    sudo systemctl start aas-web
    
    echo ""
    echo "⏳ Čekám na start aplikace (10s)..."
    sleep 10
    
    echo ""
    echo "📊 Stav služby:"
    sudo systemctl status aas-web --no-pager
    
    echo ""
    echo "✅ Instalace dokončena!"
    echo ""
    echo "📝 Užitečné příkazy:"
    echo "  - Zobrazit logy:  sudo journalctl -u aas-web -f"
    echo "  - Restart:        sudo systemctl restart aas-web"
    echo "  - Stop:           sudo systemctl stop aas-web"
    echo ""
    echo "🌐 Aplikace běží na: http://localhost:5000"
    
else
    echo "✅ Závislosti nainstalovány!"
    echo ""
    echo "📖 Další kroky najdete v UBUNTU_DEPLOYMENT.md"
fi

echo ""
echo "=========================================="
echo "  Instalace dokončena!"
echo "=========================================="
echo ""
echo "⚠️  DŮLEŽITÉ:"
echo "  - Pro Docker: Je třeba se odhlásit a znovu přihlásit pro Docker group"
echo "  - Heslo k databázi si uložte na bezpečné místo"
echo "  - Pro veřejný přístup nastavte Nginx (viz UBUNTU_DEPLOYMENT.md)"
echo ""
echo "📚 Kompletní dokumentace: UBUNTU_DEPLOYMENT.md"
