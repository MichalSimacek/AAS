# 🚀 Produkční nasazení AAS - Kompletní návod

## 📋 Rychlý přehled

Tato aplikace je **ASP.NET Core 8.0 MVC** s PostgreSQL databází.

**Co je potřeba:**
- Server s Ubuntu 20.04+ (min 2GB RAM, 20GB disk)
- Docker & Docker Compose
- Doména směřující na server
- SMTP email účet (Gmail, SendGrid, atd.)

---

## 🎯 RYCHLÝ START (10 minut)

### 1. Příprava serveru

```bash
# Instalace Dockeru
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Instalace Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Firewall
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

### 2. Nahrání projektu

```bash
# Vytvoření adresáře
sudo mkdir -p /opt/aas
cd /opt/aas

# Klonování z GitHubu
git clone https://github.com/MichalSimacek/AAS.git .

# NEBO nahrajte soubory přes SCP
```

### 3. Konfigurace

```bash
# Zkopírujte vzorový config
cp .env.production.example .env.production

# UPRAVTE (nano nebo vi):
nano .env.production
```

**POVINNÉ nastavení:**

```bash
# ⚠️ ZMĚŇTE HESLA!
DB_PASSWORD=SuperSilneHeslo123!@#
ADMIN_EMAIL=admin@vasedomena.cz
ADMIN_PASSWORD=AdminHeslo456!@#

# SMTP (příklad pro Gmail)
EMAIL_SMTP_HOST=smtp.gmail.com
EMAIL_SMTP_PORT=587
EMAIL_USERNAME=vas-email@gmail.com
EMAIL_PASSWORD=vase-app-heslo  # https://myaccount.google.com/apppasswords
EMAIL_FROM=noreply@vasedomena.cz

# Doména
DOMAIN=vasedomena.cz
SSL_EMAIL=admin@vasedomena.cz
```

### 4. Spuštění

```bash
# Načtení proměnných
export $(cat .env.production | grep -v '^#' | xargs)

# Build a start
docker-compose -f docker-compose.prod.yml up -d --build

# Počkejte 30 sekund
sleep 30

# Kontrola
docker-compose -f docker-compose.prod.yml ps
docker-compose -f docker-compose.prod.yml logs web
```

### 5. SSL certifikát

```bash
# Načtěte proměnné
export $(cat .env.production | grep -v '^#' | xargs)

# Spusťte SSL setup
./setup-ssl.sh

# NEBO manuálně:
sudo certbot certonly --standalone -d vasedomena.cz -d www.vasedomena.cz --email admin@vasedomena.cz --agree-tos
sudo cp /etc/letsencrypt/live/vasedomena.cz/*.pem nginx/ssl/
docker-compose -f docker-compose.prod.yml restart nginx
```

### ✅ HOTOVO!

Vaše aplikace běží na:
- **HTTP**: http://vasedomena.cz  
- **HTTPS**: https://vasedomena.cz

**Admin přihlášení:**
- URL: https://vasedomena.cz/Identity/Account/Login
- Email: admin@vasedomena.cz (co jste nastavili)
- Heslo: AdminHeslo456 (co jste nastavili)

---

## 🔄 AKTUALIZACE (Deploy nové verze)

### Automaticky

```bash
cd /opt/aas
./deploy.sh
```

### Manuálně

```bash
cd /opt/aas

# 1. Stáhnout změny
git pull origin main

# 2. Rebuild
docker-compose -f docker-compose.prod.yml build --no-cache

# 3. Restart
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d

# 4. Migrace (pokud jsou)
docker-compose -f docker-compose.prod.yml exec web dotnet ef database update

# 5. Kontrola
docker-compose -f docker-compose.prod.yml ps
```

---

## 💾 ZÁLOHA

### Ruční záloha

```bash
cd /opt/aas
./backup.sh

# Vytvoří: backups/aas_backup_YYYYMMDD_HHMMSS.tar.gz
```

### Automatická záloha (cron)

```bash
# Otevřete crontab
crontab -e

# Přidejte (denní záloha ve 2:00):
0 2 * * * cd /opt/aas && ./backup.sh >> /opt/aas/logs/backup.log 2>&1
```

### Obnova

```bash
cd /opt/aas

# 1. Zastavte aplikaci
docker-compose -f docker-compose.prod.yml down

# 2. Extrahujte zálohu
tar -xzf backups/aas_backup_YYYYMMDD_HHMMSS.tar.gz

# 3. Obnovte databázi
docker-compose -f docker-compose.prod.yml up -d db
sleep 5
cat aas_backup_*_database.sql | docker-compose -f docker-compose.prod.yml exec -T db psql -U aasuser -d aas_production

# 4. Obnovte uploads
tar -xzf aas_backup_*_uploads.tar.gz

# 5. Spusťte
docker-compose -f docker-compose.prod.yml up -d
```

---

## 📊 MONITORING & ÚDRŽBA

### Zobrazení logů

```bash
# Všechny logy
docker-compose -f docker-compose.prod.yml logs -f

# Pouze web
docker-compose -f docker-compose.prod.yml logs -f web

# Poslední 100 řádků
docker-compose -f docker-compose.prod.yml logs --tail=100 web
```

### Kontrola stavu

```bash
# Status kontejnerů
docker-compose -f docker-compose.prod.yml ps

# Využití zdrojů
docker stats

# Disk space
df -h
du -sh wwwroot/uploads
```

### Restart služeb

```bash
# Restart web aplikace
docker-compose -f docker-compose.prod.yml restart web

# Restart databáze
docker-compose -f docker-compose.prod.yml restart db

# Restart všeho
docker-compose -f docker-compose.prod.yml restart
```

---

## 🔧 ŘEŠENÍ PROBLÉMŮ

### Aplikace se nespustí

```bash
# Zkontrolujte logy
docker-compose -f docker-compose.prod.yml logs web

# Zkontrolujte DB připojení
docker-compose -f docker-compose.prod.yml exec web dotnet --info

# Restartujte
docker-compose -f docker-compose.prod.yml restart
```

### Chyba databáze

```bash
# Zkontrolujte, zda běží
docker-compose -f docker-compose.prod.yml ps db

# Připojte se k DB
docker-compose -f docker-compose.prod.yml exec db psql -U aasuser -d aas_production

# Spusťte migrace
docker-compose -f docker-compose.prod.yml exec web dotnet ef database update
```

### Nginx 502 Bad Gateway

```bash
# Zkontrolujte web kontejner
docker-compose -f docker-compose.prod.yml ps web

# Restartujte nginx
docker-compose -f docker-compose.prod.yml restart nginx

# Zkontrolujte logy
docker-compose -f docker-compose.prod.yml logs nginx
```

### SSL certifikát expiroval

```bash
# Obnovte
sudo certbot renew --force-renewal

# Zkopírujte nové
sudo cp /etc/letsencrypt/live/$DOMAIN/*.pem nginx/ssl/

# Restart nginx
docker-compose -f docker-compose.prod.yml restart nginx
```

---

## 📧 NASTAVENÍ SMTP

### Gmail

1. Povolte 2FA: https://myaccount.google.com/security
2. Vytvořte App Password: https://myaccount.google.com/apppasswords
3. V `.env.production`:

```bash
EMAIL_SMTP_HOST=smtp.gmail.com
EMAIL_SMTP_PORT=587
EMAIL_USERNAME=vas-email@gmail.com
EMAIL_PASSWORD=app-password-zde
```

### SendGrid

```bash
EMAIL_SMTP_HOST=smtp.sendgrid.net
EMAIL_SMTP_PORT=587
EMAIL_USERNAME=apikey
EMAIL_PASSWORD=your-sendgrid-api-key
```

### Mailgun

```bash
EMAIL_SMTP_HOST=smtp.mailgun.org
EMAIL_SMTP_PORT=587
EMAIL_USERNAME=postmaster@mg.yourdomain.com
EMAIL_PASSWORD=your-mailgun-password
```

---

## 🌐 PŘEKLAD

Pro automatický překlad názvů a popisů kolekcí:

```bash
# V .env.production změňte:
TRANSLATION_ENABLED=true
TRANSLATION_PROVIDER=LibreTranslate
TRANSLATION_ENDPOINT=https://libretranslate.com/translate
TRANSLATION_API_KEY=  # ponechte prázdné pro veřejnou službu

# Restart
docker-compose -f docker-compose.prod.yml restart web
```

**Poznámka:** Bez API klíče může být pomalé při vysoké zátěži.

---

## 📝 UŽITEČNÉ PŘÍKAZY

```bash
# Status
docker-compose -f docker-compose.prod.yml ps

# Logy (real-time)
docker-compose -f docker-compose.prod.yml logs -f web

# Restart aplikace
docker-compose -f docker-compose.prod.yml restart web

# Stop všeho
docker-compose -f docker-compose.prod.yml down

# Start všeho
docker-compose -f docker-compose.prod.yml up -d

# Rebuild bez cache
docker-compose -f docker-compose.prod.yml build --no-cache web

# Vstup do kontejneru
docker-compose -f docker-compose.prod.yml exec web bash

# Database backup
./backup.sh

# Deploy
./deploy.sh
```

---

## ⚠️ BEZPEČNOST

### Nikdy necommitujte:

```gitignore
.env.production
nginx/ssl/*.pem
backups/
logs/
wwwroot/uploads/*
```

### Silná hesla

- Minimálně 16 znaků
- Mix písmen, čísel, symbolů
- Použijte password manager

### Pravidelné aktualizace

```bash
# Aktualizujte Docker images
docker-compose -f docker-compose.prod.yml pull

# Aktualizujte systém
sudo apt update && sudo apt upgrade -y
```

---

## ✅ CHECKLIST

### Před spuštěním:
- [ ] `.env.production` vytvořen a nakonfigurován
- [ ] Všechna hesla silná a unikátní  
- [ ] SMTP email nakonfigurován a otestován
- [ ] DNS ukazuje na server
- [ ] Firewall otevřený (80, 443, 22)
- [ ] Docker a Docker Compose nainstalováno

### Po spuštění:
- [ ] Aplikace běží (http://localhost)
- [ ] SSL certifikát nainstalován (https://)
- [ ] Admin login funguje
- [ ] Registrace + email verification funguje
- [ ] Vytvoření kolekce funguje
- [ ] Upload obrázků funguje
- [ ] Překlady fungují
- [ ] Automatické zálohy nastaveny
- [ ] SSL auto-renewal nakonfigurován

---

## 📞 PODPORA

- **GitHub Issues**: https://github.com/MichalSimacek/AAS/issues
- **Email**: aristocratic-artwork-sell@proton.me

---

**Vytvořeno:** 2024  
**Verze:** 1.0  
**Poslední aktualizace:** Po commitu "Nastaveni pro produkci"
