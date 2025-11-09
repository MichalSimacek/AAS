# 🚀 Aristocratic Artwork Sale - Produkční Nasazení

## Kompletní návod krok po kroku

Tento návod vás provede celým procesem nasazení aplikace do produkčního prostředí.

---

## 📋 Obsah

1. [Požadavky](#požadavky)
2. [Příprava serveru](#příprava-serveru)
3. [Konfigurace](#konfigurace)
4. [SSL Certifikáty](#ssl-certifikáty)
5. [Nasazení](#nasazení)
6. [Ověření](#ověření)
7. [Údržba](#údržba)
8. [Záloha a obnova](#záloha-a-obnova)
9. [Monitoring](#monitoring)
10. [Troubleshooting](#troubleshooting)

---

## 1. Požadavky

### Hardware

**Minimální:**
- CPU: 2 cores
- RAM: 4 GB
- Disk: 50 GB SSD
- Bandwidth: 100 Mbps

**Doporučené:**
- CPU: 4 cores
- RAM: 8 GB
- Disk: 100 GB SSD
- Bandwidth: 1 Gbps

### Software

- **OS**: Ubuntu 22.04 LTS / Debian 12 (doporučeno) nebo CentOS/RHEL 9
- **Docker**: 24.0+ (bude nainstalován)
- **Docker Compose**: 2.20+ (bude nainstalován)
- **Git**: Pro deployment ze zdrojového kódu

### Network

- Veřejná IP adresa
- Doména zaměřená na server (DNS A záznam)
- Otevřené porty:
  - 80 (HTTP)
  - 443 (HTTPS)
  - 22 (SSH)

---

## 2. Příprava serveru

### Krok 1: Připojení k serveru

```bash
ssh root@your-server-ip
```

### Krok 2: Update systému

```bash
apt update && apt upgrade -y
```

### Krok 3: Instalace Docker

```bash
# Přidání Docker repository
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Start Docker služby
systemctl start docker
systemctl enable docker

# Ověření instalace
docker --version
```

### Krok 4: Instalace Docker Compose

```bash
# Stažení Docker Compose
curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose

# Nastavení oprávnění
chmod +x /usr/local/bin/docker-compose

# Ověření instalace
docker-compose --version
```

### Krok 5: Instalace dalších nástrojů

```bash
apt install -y git curl wget nano certbot python3-certbot-nginx
```

### Krok 6: Vytvoření uživatele pro aplikaci (doporučeno)

```bash
# Vytvoření uživatele
adduser aas
usermod -aG docker aas
usermod -aG sudo aas

# Přepnutí na uživatele
su - aas
```

### Krok 7: Konfigurace firewall

```bash
# UFW (Ubuntu/Debian)
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
sudo ufw status
```

---

## 3. Konfigurace

### Krok 1: Clone repository

```bash
cd ~
git clone https://github.com/your-username/AAS.git
cd AAS
```

**NEBO** pokud nasazujete lokální kód:

```bash
# Na vašem lokálním počítači
scp -r C:\AAS aas@your-server-ip:~/
```

### Krok 2: Vytvoření .env.production souboru

```bash
cp .env.production.template .env.production
nano .env.production
```

Vyplňte VŠECHNY povinné hodnoty:

```bash
# =============================================================================
# DATABASE
# =============================================================================
DB_NAME=aas_prod
DB_USER=aas
DB_PASSWORD=        # Vygenerujte: openssl rand -base64 32

# =============================================================================
# EMAIL (DŮLEŽITÉ!)
# =============================================================================
EMAIL_SMTP_HOST=smtp.gmail.com        # Nebo váš SMTP server
EMAIL_SMTP_PORT=587
EMAIL_USE_STARTTLS=true
EMAIL_USERNAME=your-email@gmail.com
EMAIL_PASSWORD=your-app-specific-password    # Pro Gmail: https://myaccount.google.com/apppasswords
EMAIL_FROM=noreply@aristocraticartworksale.com
EMAIL_TO=aristocratic-artwork-sell@proton.me

# =============================================================================
# ADMIN ÚČET
# =============================================================================
ADMIN_EMAIL=admin@aristocraticartworksale.com
ADMIN_PASSWORD=     # Min 12 znaků, velká/malá písmena, čísla, speciální znaky

# =============================================================================
# SSL/TLS
# =============================================================================
DOMAIN_NAME=aristocraticartworksale.com
LETSENCRYPT_EMAIL=admin@aristocraticartworksale.com

# =============================================================================
# TRANSLATION (Volitelné)
# =============================================================================
TRANSLATION_ENABLED=false
TRANSLATION_ENDPOINT=
TRANSLATION_API_KEY=
```

### Krok 3: Zabezpečení .env souboru

```bash
chmod 600 .env.production
```

---

## 4. SSL Certifikáty

### Možnost A: Let's Encrypt (DOPORUČENO - ZDARMA)

#### Krok 1: Zastavte všechny běžící služby na portu 80/443

```bash
sudo systemctl stop nginx apache2 2>/dev/null || true
```

#### Krok 2: Vygenerujte certifikáty

```bash
sudo certbot certonly --standalone \
  -d aristocraticartworksale.com \
  -d www.aristocraticartworksale.com \
  --email admin@aristocraticartworksale.com \
  --agree-tos \
  --no-eff-email
```

#### Krok 3: Zkopírujte certifikáty do projektu

```bash
sudo mkdir -p nginx/ssl
sudo cp /etc/letsencrypt/live/aristocraticartworksale.com/fullchain.pem nginx/ssl/
sudo cp /etc/letsencrypt/live/aristocraticartworksale.com/privkey.pem nginx/ssl/
sudo chown -R aas:aas nginx/ssl
```

#### Krok 4: Nastavte automatickou obnovu

```bash
sudo crontab -e
```

Přidejte:
```
0 2 * * * certbot renew --quiet --deploy-hook "cd /home/aas/AAS && docker-compose -f docker-compose.production.yml restart nginx"
```

### Možnost B: Vlastní certifikáty

Pokud máte vlastní SSL certifikáty:

```bash
mkdir -p nginx/ssl
# Zkopírujte své certifikáty
cp /path/to/your/fullchain.pem nginx/ssl/
cp /path/to/your/privkey.pem nginx/ssl/
chmod 644 nginx/ssl/fullchain.pem
chmod 600 nginx/ssl/privkey.pem
```

---

## 5. Nasazení

### Krok 1: Ověření konfigurace

```bash
# Zkontrolujte, že máte všechny potřebné soubory
ls -la .env.production
ls -la nginx/ssl/fullchain.pem
ls -la nginx/ssl/privkey.pem
ls -la docker-compose.production.yml
```

### Krok 2: Build a spuštění

```bash
# Načtení environment variables
export $(cat .env.production | grep -v '^#' | xargs)

# Build aplikace
docker-compose -f docker-compose.production.yml build

# Spuštění všech služeb
docker-compose -f docker-compose.production.yml up -d

# Sledování logů
docker-compose -f docker-compose.production.yml logs -f
```

### Krok 3: Počkejte na inicializaci

Aplikace potřebuje cca 30-60 sekund na:
- Spuštění PostgreSQL
- Provedení databázových migrací
- Inicializaci ASP.NET Core aplikace

---

## 6. Ověření

### Krok 1: Zkontrolujte běžící containery

```bash
docker-compose -f docker-compose.production.yml ps
```

Měli byste vidět 3 běžící containery:
- `aas-db-prod` (PostgreSQL)
- `aas-web-prod` (ASP.NET Core)
- `aas-nginx-prod` (Nginx)

### Krok 2: Zkontrolujte health status

```bash
docker-compose -f docker-compose.production.yml ps
# Všechny by měly mít status "Up" a "(healthy)"
```

### Krok 3: Otestujte v prohlížeči

Otevřete prohlížeč a navštivte:

```
https://aristocraticartworksale.com
```

Měli byste vidět domovskou stránku aplikace.

### Krok 4: Přihlaste se jako admin

1. Jděte na: `https://aristocraticartworksale.com/Identity/Account/Login`
2. Použijte údaje z `.env.production`:
   - Email: hodnota `ADMIN_EMAIL`
   - Heslo: hodnota `ADMIN_PASSWORD`

### Krok 5: Zkontrolujte funkčnost

- ✅ Domovská stránka se načítá
- ✅ Kolekce se zobrazují
- ✅ Obrázky se načítají
- ✅ Přepínání jazyků funguje
- ✅ Formulář pro poptávku funguje
- ✅ Admin přihlášení funguje
- ✅ SSL certifikát je validní (zelený zámek v prohlížeči)

---

## 7. Údržba

### Prohlížení logů

```bash
# Všechny služby
docker-compose -f docker-compose.production.yml logs -f

# Pouze web aplikace
docker-compose -f docker-compose.production.yml logs -f web

# Pouze databáze
docker-compose -f docker-compose.production.yml logs -f db

# Pouze nginx
docker-compose -f docker-compose.production.yml logs -f nginx
```

### Restart služeb

```bash
# Restart všech služeb
docker-compose -f docker-compose.production.yml restart

# Restart pouze web aplikace
docker-compose -f docker-compose.production.yml restart web

# Restart databáze
docker-compose -f docker-compose.production.yml restart db
```

### Zastavení aplikace

```bash
docker-compose -f docker-compose.production.yml down
```

### Kompletní restart (s rebuild)

```bash
docker-compose -f docker-compose.production.yml down
docker-compose -f docker-compose.production.yml build --no-cache
docker-compose -f docker-compose.production.yml up -d
```

### Update aplikace

```bash
# 1. Pull změn
git pull origin main

# 2. Rebuild a restart
docker-compose -f docker-compose.production.yml down
docker-compose -f docker-compose.production.yml build --no-cache web
docker-compose -f docker-compose.production.yml up -d

# 3. Zkontrolujte logy
docker-compose -f docker-compose.production.yml logs -f web
```

---

## 8. Záloha a obnova

### Záloha databáze

```bash
# Manuální záloha
docker-compose -f docker-compose.production.yml exec db pg_dump -U aas aas_prod > backup_$(date +%Y%m%d_%H%M%S).sql

# Nebo přes Docker volume
docker run --rm \
  --volumes-from aas-db-prod \
  -v $(pwd)/backups:/backup \
  postgres:16-alpine \
  pg_dump -U aas -d aas_prod -F c -f /backup/backup_$(date +%Y%m%d_%H%M%S).dump
```

### Automatická záloha (cron)

```bash
crontab -e
```

Přidejte:
```
0 2 * * * cd /home/aas/AAS && docker-compose -f docker-compose.production.yml exec -T db pg_dump -U aas aas_prod | gzip > /home/aas/backups/db_$(date +\%Y\%m\%d).sql.gz
```

### Obnova databáze

```bash
# Z SQL souboru
docker-compose -f docker-compose.production.yml exec -T db psql -U aas -d aas_prod < backup_20240101.sql

# Z dump souboru
docker-compose -f docker-compose.production.yml exec -T db pg_restore -U aas -d aas_prod -c /backup/backup_20240101.dump
```

### Záloha uploaded souborů

```bash
# Backup uploads složky
docker run --rm \
  --volumes-from aas-web-prod \
  -v $(pwd)/backups:/backup \
  alpine \
  tar czf /backup/uploads_$(date +%Y%m%d_%H%M%S).tar.gz -C /app/wwwroot uploads
```

---

## 9. Monitoring

### Systémové prostředky

```bash
# CPU a RAM usage
docker stats

# Disk usage
df -h
docker system df
```

### Health checks

```bash
# HTTP health endpoint
curl http://localhost:80/health

# Database health
docker-compose -f docker-compose.production.yml exec db pg_isready -U aas
```

### Doporučené monitoring nástroje

1. **Uptime monitoring**: [UptimeRobot](https://uptimerobot.com/) (zdarma)
2. **Application Performance**: [Azure Application Insights](https://azure.microsoft.com/en-us/services/monitor/)
3. **Log management**: [Seq](https://datalust.co/seq) nebo [ELK Stack](https://www.elastic.co/elk-stack)
4. **Server monitoring**: [Netdata](https://www.netdata.cloud/)

---

## 10. Troubleshooting

### Problém: Container se neustále restartuje

```bash
# Zkontrolujte logy
docker-compose -f docker-compose.production.yml logs web

# Zkontrolujte environment variables
docker-compose -f docker-compose.production.yml exec web env | grep -E "DB_|EMAIL_|ADMIN_"
```

**Řešení**: Zkontrolujte, že všechny povinné environment variables jsou nastaveny v `.env.production`

### Problém: Nelze se připojit k databázi

```bash
# Zkontrolujte, že databáze běží
docker-compose -f docker-compose.production.yml ps db

# Zkontrolujte databázové logy
docker-compose -f docker-compose.production.yml logs db

# Test připojení
docker-compose -f docker-compose.production.yml exec db psql -U aas -d aas_prod -c "SELECT 1;"
```

**Řešení**: Zkontrolujte DB_PASSWORD v `.env.production`

### Problém: SSL certifikát nefunguje

```bash
# Zkontrolujte, že certifikáty existují
ls -la nginx/ssl/

# Zkontrolujte Nginx logy
docker-compose -f docker-compose.production.yml logs nginx
```

**Řešení**: Přegenerujte certifikáty podle sekce [SSL Certifikáty](#ssl-certifikáty)

### Problém: Email se neposílá

```bash
# Zkontrolujte email konfiguraci
docker-compose -f docker-compose.production.yml exec web env | grep EMAIL_

# Zkontrolujte logy
docker-compose -f docker-compose.production.yml logs web | grep -i email
```

**Řešení**:
- Pro Gmail: Vygenerujte App-Specific Password
- Zkontrolujte SMTP port (587 pro STARTTLS, 465 pro SSL)
- Ověřte, že EMAIL_FROM je autorizován na vašem SMTP serveru

### Problém: Vysoké využití CPU/RAM

```bash
# Zkontrolujte Docker stats
docker stats

# Restart aplikace
docker-compose -f docker-compose.production.yml restart web
```

**Řešení**: Zvyšte RAM limity v `docker-compose.production.yml`

### Problém: Disk je plný

```bash
# Zkontrolujte disk usage
df -h
docker system df

# Vyčistěte nepoužívané Docker objekty
docker system prune -a
```

---

## 📞 Podpora

Pokud narazíte na problémy:

1. Zkontrolujte logy: `docker-compose -f docker-compose.production.yml logs -f`
2. Ověřte konfiguraci v `.env.production`
3. Zkontrolujte [appsettings.SECURITY.md](./src/AAS.Web/appsettings.SECURITY.md)
4. Projděte [Security Checklist](#bezpečnostní-checklist)

---

## 🎉 Gratulujeme!

Vaše aplikace běží v produkci!

**Next steps:**
- Nastavte automatické zálohy
- Nakonfigurujte monitoring
- Projděte Security Checklist
- Otestujte disaster recovery plán

---

*Dokument vytvořen: 2025-01-09*
*Verze: 1.0*
