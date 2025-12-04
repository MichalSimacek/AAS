# Aristocratic Artwork Sale - Ubuntu Server Deployment Guide

## 🚀 Přehled

Tato aplikace je ASP.NET Core 8.0 web aplikace s PostgreSQL databází. Existují **3 způsoby nasazení**:

1. **Docker Compose** (Doporučeno - Nejjednodušší)
2. **Systemd Service** (Přímé spuštění bez Dockeru)
3. **Nginx Reverse Proxy** (Pro produkční hosting)

---

## Metoda 1: Docker Compose (Doporučeno)

### Prerekvizity

```bash
# Aktualizace systému
sudo apt update && sudo apt upgrade -y

# Instalace Dockeru
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER
newgrp docker

# Instalace Docker Compose
sudo apt install docker-compose -y

# Ověření instalace
docker --version
docker-compose --version
```

### Příprava projektu

```bash
# 1. Naklonujte nebo zkopírujte projekt
cd /var/www/
sudo git clone <your-repo-url> aristocratic-artwork-sale
cd aristocratic-artwork-sale

# 2. Nastavte oprávnění
sudo chown -R $USER:$USER /var/www/aristocratic-artwork-sale
chmod +x docker-entrypoint.sh backup-setup.sh setup-remote-sync.sh
```

### Konfigurace Environment Variables

```bash
# Vytvořte .env soubor
cat > .env << 'EOF'
# Database Configuration
POSTGRES_USER=aas_user
POSTGRES_PASSWORD=VaseSilneHeslo123!
POSTGRES_DB=aas_production

# ASP.NET Configuration
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000

# DeepL API (volitelné - pro automatický překlad blogů)
DEEPL_API_KEY=your-deepl-api-key-here

# Email Configuration (volitelné - pro email confirmace)
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=your-email@gmail.com
SMTP_PASSWORD=your-app-password
EOF

# Zabezpečte .env soubor
chmod 600 .env
```

### Spuštění aplikace

```bash
# 1. Build Docker images
docker-compose -f docker-compose.prod.yml build --no-cache

# 2. Spuštění služeb
docker-compose -f docker-compose.prod.yml up -d

# 3. Kontrola stavu
docker-compose -f docker-compose.prod.yml ps
docker logs -f aas-web-prod

# 4. Ověření běhu
# Aplikace běží na http://localhost:5000
curl http://localhost:5000
```

### Správa služby

```bash
# Restart
docker-compose -f docker-compose.prod.yml restart

# Stop
docker-compose -f docker-compose.prod.yml down

# Zobrazit logy
docker logs -f aas-web-prod
docker logs -f aas-db-prod

# Aktualizace aplikace
git pull
docker-compose -f docker-compose.prod.yml build --no-cache web
docker-compose -f docker-compose.prod.yml up -d
```

---

## Metoda 2: Systemd Service (Bez Dockeru)

### Prerekvizity

```bash
# 1. Instalace .NET 8 SDK
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# Přidat do PATH
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
source ~/.bashrc

# Ověření
dotnet --version

# 2. Instalace PostgreSQL
sudo apt install postgresql postgresql-contrib -y
sudo systemctl start postgresql
sudo systemctl enable postgresql

# 3. Vytvoření databáze
sudo -u postgres psql << EOF
CREATE USER aas_user WITH PASSWORD 'VaseSilneHeslo123!';
CREATE DATABASE aas_production OWNER aas_user;
GRANT ALL PRIVILEGES ON DATABASE aas_production TO aas_user;
\q
EOF
```

### Build & Deploy

```bash
# 1. Zkopírujte projekt
cd /var/www/aristocratic-artwork-sale

# 2. Build aplikace
cd src/AAS.Web
dotnet restore
dotnet publish -c Release -o /var/www/aas-app

# 3. Nastavte connection string
cat > /var/www/aas-app/appsettings.Production.json << 'EOF'
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=aas_production;Username=aas_user;Password=VaseSilneHeslo123!"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*"
}
EOF

# 4. Spusťte migrace
cd /var/www/aas-app
dotnet AAS.Web.dll --migrate
```

### Vytvoření Systemd Service

```bash
# Vytvořte service file
sudo nano /etc/systemd/system/aas-web.service
```

Vložte:

```ini
[Unit]
Description=Aristocratic Artwork Sale Web Application
After=network.target postgresql.service

[Service]
Type=notify
User=www-data
WorkingDirectory=/var/www/aas-app
ExecStart=/home/YOUR_USER/.dotnet/dotnet /var/www/aas-app/AAS.Web.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=aas-web
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

```bash
# Reload a start
sudo systemctl daemon-reload
sudo systemctl enable aas-web
sudo systemctl start aas-web

# Kontrola stavu
sudo systemctl status aas-web
sudo journalctl -u aas-web -f
```

---

## Metoda 3: Nginx Reverse Proxy (Pro veřejný přístup)

### Instalace Nginx

```bash
sudo apt install nginx -y
sudo systemctl start nginx
sudo systemctl enable nginx
```

### Konfigurace Nginx

```bash
# Vytvořte config
sudo nano /etc/nginx/sites-available/aristocratic-artwork-sale
```

Vložte:

```nginx
server {
    listen 80;
    server_name aristocraticartworksale.com www.aristocraticartworksale.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # Static files caching
    location ~* \.(jpg|jpeg|png|gif|ico|css|js|woff|woff2|ttf)$ {
        proxy_pass http://localhost:5000;
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    client_max_body_size 20M;
}
```

```bash
# Aktivace konfigurace
sudo ln -s /etc/nginx/sites-available/aristocratic-artwork-sale /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### SSL s Let's Encrypt (HTTPS)

```bash
# Instalace Certbot
sudo apt install certbot python3-certbot-nginx -y

# Získání SSL certifikátu
sudo certbot --nginx -d aristocraticartworksale.com -d www.aristocraticartworksale.com

# Auto-renewal
sudo systemctl enable certbot.timer
sudo systemctl start certbot.timer
```

---

## 🔒 Zabezpečení

### Firewall (UFW)

```bash
sudo ufw allow 22/tcp      # SSH
sudo ufw allow 80/tcp      # HTTP
sudo ufw allow 443/tcp     # HTTPS
sudo ufw enable
sudo ufw status
```

### PostgreSQL Security

```bash
# Editace pg_hba.conf
sudo nano /etc/postgresql/*/main/pg_hba.conf

# Změňte:
# local   all   all   peer
# Na:
# local   all   all   md5

sudo systemctl restart postgresql
```

### Fail2Ban (Ochrana proti útokům)

```bash
sudo apt install fail2ban -y
sudo systemctl enable fail2ban
sudo systemctl start fail2ban
```

---

## 📊 Monitoring & Logs

### Kontrola logů

```bash
# Docker logs
docker logs -f aas-web-prod

# Systemd logs
sudo journalctl -u aas-web -f

# Nginx logs
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```

### Disk Space Monitoring

```bash
# Kontrola místa
df -h

# Vyčištění Docker
docker system prune -a

# Vyčištění logů
sudo journalctl --vacuum-time=7d
```

---

## 🔄 Zálohování

### Automatické zálohy databáze

```bash
# Vytvořte backup script
sudo nano /usr/local/bin/backup-aas-db.sh
```

Vložte:

```bash
#!/bin/bash
BACKUP_DIR="/var/backups/aas"
DATE=$(date +%Y%m%d_%H%M%S)
mkdir -p $BACKUP_DIR

# PostgreSQL backup
PGPASSWORD='VaseSilneHeslo123!' pg_dump -h localhost -U aas_user aas_production > $BACKUP_DIR/aas_db_$DATE.sql

# Komprese
gzip $BACKUP_DIR/aas_db_$DATE.sql

# Smazání starších než 7 dní
find $BACKUP_DIR -name "aas_db_*.sql.gz" -mtime +7 -delete

echo "Backup completed: aas_db_$DATE.sql.gz"
```

```bash
# Oprávnění
sudo chmod +x /usr/local/bin/backup-aas-db.sh

# Cron (každý den ve 2:00)
sudo crontab -e
# Přidat:
0 2 * * * /usr/local/bin/backup-aas-db.sh >> /var/log/aas-backup.log 2>&1
```

---

## 🚨 Troubleshooting

### Aplikace nenaběhne

```bash
# Kontrola portů
sudo netstat -tulpn | grep :5000
sudo lsof -i :5000

# Kontrola oprávnění
ls -la /var/www/aas-app

# Kontrola connection stringu
cat /var/www/aas-app/appsettings.Production.json
```

### Databáze není dostupná

```bash
# Kontrola PostgreSQL
sudo systemctl status postgresql
sudo -u postgres psql -c "\l"

# Test připojení
psql -h localhost -U aas_user -d aas_production
```

### 502 Bad Gateway (Nginx)

```bash
# Kontrola backend služby
sudo systemctl status aas-web
curl http://localhost:5000

# Kontrola Nginx logů
sudo tail -f /var/log/nginx/error.log
```

---

## 📝 Post-Deployment Checklist

- [ ] Aplikace běží a je dostupná
- [ ] SSL certifikát je aktivní (HTTPS)
- [ ] Databáze je zabezpečená
- [ ] Firewall je nakonfigurován
- [ ] Automatické zálohy fungují
- [ ] Admin účet je vytvořen
- [ ] Email konfigurace funguje
- [ ] Monitoring je nastaven
- [ ] DNS záznamy jsou správné
- [ ] Fail2Ban je aktivní

---

## 🔧 Údržba

### Aktualizace aplikace

```bash
# Docker metoda
cd /var/www/aristocratic-artwork-sale
git pull
docker-compose -f docker-compose.prod.yml build --no-cache web
docker-compose -f docker-compose.prod.yml up -d

# Systemd metoda
cd /var/www/aristocratic-artwork-sale
git pull
cd src/AAS.Web
dotnet publish -c Release -o /var/www/aas-app
sudo systemctl restart aas-web
```

### Aktualizace systému

```bash
sudo apt update && sudo apt upgrade -y
sudo systemctl restart aas-web
sudo systemctl restart nginx
```

---

## 📞 Kontakt & Podpora

**Dokumentace:**
- Project Guide: `/app/PROJECT-GUIDE.md`
- Docker Deployment: `/app/DEPLOYMENT.md`

**Důležité soubory:**
- Connection String: `appsettings.Production.json`
- Environment Variables: `.env`
- Nginx Config: `/etc/nginx/sites-available/aristocratic-artwork-sale`
- Systemd Service: `/etc/systemd/system/aas-web.service`

---

**Vytvořeno:** December 2024  
**Verze:** ASP.NET Core 8.0 + PostgreSQL
