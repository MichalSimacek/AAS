# 🚀 Quick Start - Produkční Nasazení

Rychlý průvodce pro nasazení Aristocratic Artwork Sale do produkce.

> **Pro detailní návod viz**: [PRODUCTION-DEPLOYMENT.md](./PRODUCTION-DEPLOYMENT.md)
> **Pro bezpečnostní kontrolu viz**: [SECURITY-CHECKLIST.md](./SECURITY-CHECKLIST.md)

---

## Minimální Požadavky

- **Server**: Ubuntu 22.04 LTS / Debian 12
- **CPU**: 2 cores (doporučeno 4)
- **RAM**: 4 GB (doporučeno 8 GB)
- **Disk**: 50 GB SSD (doporučeno 100 GB)
- **Doména**: Nastavená DNS A záznam na server
- **Porty**: 22 (SSH), 80 (HTTP), 443 (HTTPS)

---

## Rychlá Instalace (30 minut)

### 1. Příprava Serveru (5 min)

```bash
# Připojení k serveru
ssh root@your-server-ip

# Update systému
apt update && apt upgrade -y

# Instalace Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh
systemctl start docker
systemctl enable docker

# Instalace Docker Compose
curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
chmod +x /usr/local/bin/docker-compose

# Instalace nástrojů
apt install -y git curl certbot
```

### 2. Firewall (2 min)

```bash
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
ufw enable
```

### 3. Clone Projektu (1 min)

```bash
cd ~
git clone https://github.com/your-username/AAS.git
cd AAS
```

### 4. Konfigurace (5 min)

```bash
# Vytvoření .env souboru
cp .env.production.template .env.production
nano .env.production
```

**Vyplňte tyto POVINNÉ hodnoty:**

```bash
# Databázové heslo (vygenerujte: openssl rand -base64 32)
DB_PASSWORD=your-strong-database-password-here

# Email konfigurace (pro Gmail použijte App-Specific Password)
EMAIL_SMTP_HOST=smtp.gmail.com
EMAIL_USERNAME=your-email@gmail.com
EMAIL_PASSWORD=your-app-specific-password
EMAIL_FROM=noreply@aristocraticartworksale.com

# Admin účet (heslo min 12 znaků)
ADMIN_EMAIL=admin@aristocraticartworksale.com
ADMIN_PASSWORD=your-strong-admin-password-here

# Doména
DOMAIN_NAME=aristocraticartworksale.com
LETSENCRYPT_EMAIL=admin@aristocraticartworksale.com
```

Uložte (Ctrl+O, Enter) a zavřete (Ctrl+X).

```bash
# Zabezpečení souboru
chmod 600 .env.production
```

### 5. SSL Certifikáty (5 min)

```bash
# Zastavení služeb na portu 80/443
systemctl stop nginx apache2 2>/dev/null || true

# Vygenerování Let's Encrypt certifikátů
sudo certbot certonly --standalone \
  -d aristocraticartworksale.com \
  -d www.aristocraticartworksale.com \
  --email admin@aristocraticartworksale.com \
  --agree-tos \
  --no-eff-email

# Zkopírování certifikátů do projektu
sudo mkdir -p nginx/ssl
sudo cp /etc/letsencrypt/live/aristocraticartworksale.com/fullchain.pem nginx/ssl/
sudo cp /etc/letsencrypt/live/aristocraticartworksale.com/privkey.pem nginx/ssl/
sudo chown -R $USER:$USER nginx/ssl

# Automatická obnova (nastavení cron)
(crontab -l 2>/dev/null; echo "0 2 * * * certbot renew --quiet --deploy-hook 'cd $HOME/AAS && docker-compose -f docker-compose.production.yml restart nginx'") | crontab -
```

### 6. Nasazení (10 min)

```bash
# Načtení environment variables
export $(cat .env.production | grep -v '^#' | xargs)

# Build aplikace
docker-compose -f docker-compose.production.yml build

# Spuštění
docker-compose -f docker-compose.production.yml up -d

# Sledování logů (počkejte cca 60 sekund na inicializaci)
docker-compose -f docker-compose.production.yml logs -f
```

**Ukončit sledování logů**: Ctrl+C

### 7. Ověření (2 min)

```bash
# Zkontrolujte běžící containery
docker-compose -f docker-compose.production.yml ps
```

Měli byste vidět:
- ✅ `aas-db-prod` - Up (healthy)
- ✅ `aas-web-prod` - Up (healthy)
- ✅ `aas-nginx-prod` - Up (healthy)

**Otestujte v prohlížeči:**
```
https://aristocraticartworksale.com
```

---

## Kontrola Bezpečnosti (5 min)

```bash
# Zkontrolujte SSL
curl -I https://aristocraticartworksale.com

# Zkontrolujte security headers
curl -I https://aristocraticartworksale.com | grep -E "Strict-Transport-Security|X-Frame-Options|X-Content-Type-Options"

# Online testy (v prohlížeči)
# - SSL Labs: https://www.ssllabs.com/ssltest/
# - Security Headers: https://securityheaders.com/
```

**Kompletní kontrolní seznam**: [SECURITY-CHECKLIST.md](./SECURITY-CHECKLIST.md)

---

## Základní Údržba

### Prohlížení Logů

```bash
# Všechny služby
docker-compose -f docker-compose.production.yml logs -f

# Pouze web aplikace
docker-compose -f docker-compose.production.yml logs -f web
```

### Restart Aplikace

```bash
docker-compose -f docker-compose.production.yml restart
```

### Záloha Databáze

```bash
docker-compose -f docker-compose.production.yml exec db pg_dump -U aas aas_prod > backup_$(date +%Y%m%d).sql
```

### Update Aplikace

```bash
# Pull změn
git pull origin main

# Rebuild a restart
docker-compose -f docker-compose.production.yml down
docker-compose -f docker-compose.production.yml build --no-cache web
docker-compose -f docker-compose.production.yml up -d
```

---

## Časté Problémy

### Container se restartuje

```bash
# Zkontrolujte logy
docker-compose -f docker-compose.production.yml logs web

# Ověřte environment variables
docker-compose -f docker-compose.production.yml exec web env | grep -E "DB_|EMAIL_|ADMIN_"
```

### Email se neposílá

**Pro Gmail:**
1. Povolte 2FA: https://myaccount.google.com/security
2. Vygenerujte App Password: https://myaccount.google.com/apppasswords
3. Použijte tento password v `EMAIL_PASSWORD`

### SSL certifikát nefunguje

```bash
# Zkontrolujte certifikáty
ls -la nginx/ssl/

# Přegenerujte certifikáty
sudo certbot certonly --standalone -d aristocraticartworksale.com -d www.aristocraticartworksale.com --force-renewal
sudo cp /etc/letsencrypt/live/aristocraticartworksale.com/* nginx/ssl/
docker-compose -f docker-compose.production.yml restart nginx
```

---

## Automatické Zálohy

```bash
# Vytvoření backup skriptu
cat > /home/$USER/backup.sh <<'EOF'
#!/bin/bash
BACKUP_DIR="/home/$USER/backups"
DATE=$(date +%Y%m%d_%H%M%S)
cd /home/$USER/AAS

# Database backup
docker-compose -f docker-compose.production.yml exec -T db pg_dump -U aas aas_prod | gzip > $BACKUP_DIR/db_$DATE.sql.gz

# Uploads backup
docker run --rm --volumes-from aas-web-prod -v $BACKUP_DIR:/backup alpine tar czf /backup/uploads_$DATE.tar.gz -C /app/wwwroot uploads

# Cleanup old backups (starší než 30 dní)
find $BACKUP_DIR -name "*.gz" -mtime +30 -delete
EOF

chmod +x /home/$USER/backup.sh
mkdir -p /home/$USER/backups

# Nastavení cron (každý den ve 2:00)
(crontab -l 2>/dev/null; echo "0 2 * * * /home/$USER/backup.sh") | crontab -
```

**Kompletní backup guide**: [BACKUP-AND-MONITORING.md](./BACKUP-AND-MONITORING.md)

---

## Monitoring

### Uptime Monitoring (ZDARMA)

1. Registrujte se na: https://uptimerobot.com/
2. Přidejte monitor:
   - Type: HTTPS
   - URL: https://aristocraticartworksale.com
   - Interval: 5 minut

### Server Monitoring

```bash
# Instalace Netdata (automatický monitoring)
bash <(curl -Ss https://my-netdata.io/kickstart.sh)

# Dashboard dostupný na: http://your-server-ip:19999
# POZOR: Zabezpečte firewallem nebo nginx proxy!
```

**Detailní monitoring setup**: [BACKUP-AND-MONITORING.md](./BACKUP-AND-MONITORING.md)

---

## Další Kroky

Po úspěšném nasazení:

1. ✅ **Nastavte automatické zálohy** (viz výše)
2. ✅ **Nakonfigurujte uptime monitoring** (UptimeRobot)
3. ✅ **Projděte kompletní Security Checklist**: [SECURITY-CHECKLIST.md](./SECURITY-CHECKLIST.md)
4. ✅ **Otestujte disaster recovery** (restore ze zálohy)
5. ✅ **Nastavte alerting** (email notifikace při problémech)

---

## 📚 Kompletní Dokumentace

- **[PRODUCTION-DEPLOYMENT.md](./PRODUCTION-DEPLOYMENT.md)** - Detailní deployment guide (10 sekcí)
- **[SECURITY-CHECKLIST.md](./SECURITY-CHECKLIST.md)** - Bezpečnostní kontrolní seznam
- **[BACKUP-AND-MONITORING.md](./BACKUP-AND-MONITORING.md)** - Zálohy a monitoring
- **[src/AAS.Web/appsettings.SECURITY.md](./src/AAS.Web/appsettings.SECURITY.md)** - Security konfigurace

---

## 📞 Podpora

Problémy? Zkontrolujte:

1. **Logy**: `docker-compose -f docker-compose.production.yml logs -f`
2. **Health status**: `docker-compose -f docker-compose.production.yml ps`
3. **Disk space**: `df -h`
4. **Docker resources**: `docker stats`

---

## 🎉 Hotovo!

Vaše aplikace běží v produkci na:
**https://aristocraticartworksale.com**

**Admin přihlášení:**
- URL: `https://aristocraticartworksale.com/Identity/Account/Login`
- Email: Hodnota z `ADMIN_EMAIL` v `.env.production`
- Heslo: Hodnota z `ADMIN_PASSWORD` v `.env.production`

---

*Vytvořeno: 2025-01-09*
*Verze: 1.0*
