# 💾 Záloha a Monitoring - Produkční Prostředí

## Kompletní návod na zálohy a monitoring

---

## 📋 Obsah

1. [Strategie zálohování](#strategie-zálohování)
2. [Automatické zálohy](#automatické-zálohy)
3. [Manuální zálohy](#manuální-zálohy)
4. [Obnova ze zálohy](#obnova-ze-zálohy)
5. [Off-site backup](#off-site-backup)
6. [Monitoring](#monitoring)
7. [Alerting](#alerting)

---

## 1. Strategie zálohování

### Co zálohovat

1. **PostgreSQL databáze** (KRITICKÉ)
   - Všechna aplikační data
   - Uživatelské účty
   - Kolekce, obrázky metadata, překlady

2. **Uploaded files** (DŮLEŽITÉ)
   - `/app/wwwroot/uploads/images`
   - `/app/wwwroot/uploads/audio`

3. **Konfigurace** (DOPORUČENO)
   - `.env.production`
   - `nginx/nginx.conf`
   - `docker-compose.production.yml`

### Backup Schedule

| Typ | Frekvence | Retention | Umístění |
|-----|-----------|-----------|----------|
| Database | Denně ve 2:00 | 30 dní | Local + S3 |
| Uploads | Týdně v neděli | 90 dní | Local + S3 |
| Config | Po každé změně | 180 dní | Git + S3 |

---

## 2. Automatické zálohy

### Krok 1: Vytvoření backup adresáře

```bash
mkdir -p ~/backups/database
mkdir -p ~/backups/uploads
chmod 700 ~/backups
```

### Krok 2: Backup script pro databázi

Vytvořte soubor `scripts/backup-database.sh`:

```bash
#!/bin/bash
set -e

# Konfigurace
BACKUP_DIR="/home/aas/backups/database"
RETENTION_DAYS=30
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="aas_db_${TIMESTAMP}.sql.gz"

# Vytvoření zálohy
cd /home/aas/AAS
docker-compose -f docker-compose.production.yml exec -T db \
    pg_dump -U aas aas_prod | gzip > "${BACKUP_DIR}/${BACKUP_FILE}"

# Ověření
if [ -f "${BACKUP_DIR}/${BACKUP_FILE}" ]; then
    SIZE=$(du -h "${BACKUP_DIR}/${BACKUP_FILE}" | cut -f1)
    echo "[$(date)] ✅ Backup completed: ${BACKUP_FILE} (${SIZE})"

    # Smazání starých záloh
    find "${BACKUP_DIR}" -name "aas_db_*.sql.gz" -type f -mtime +${RETENTION_DAYS} -delete
    echo "[$(date)] ✅ Old backups cleaned (>${RETENTION_DAYS} days)"
else
    echo "[$(date)] ❌ Backup FAILED!"
    exit 1
fi
```

Nastavte oprávnění:
```bash
chmod +x scripts/backup-database.sh
```

### Krok 3: Backup script pro uploads

Vytvořte soubor `scripts/backup-uploads.sh`:

```bash
#!/bin/bash
set -e

# Konfigurace
BACKUP_DIR="/home/aas/backups/uploads"
RETENTION_DAYS=90
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="aas_uploads_${TIMESTAMP}.tar.gz"

# Vytvoření zálohy
cd /home/aas/AAS
docker run --rm \
    --volumes-from aas-web-prod \
    -v "${BACKUP_DIR}:/backup" \
    alpine \
    tar czf "/backup/${BACKUP_FILE}" -C /app/wwwroot uploads

# Ověření
if [ -f "${BACKUP_DIR}/${BACKUP_FILE}" ]; then
    SIZE=$(du -h "${BACKUP_DIR}/${BACKUP_FILE}" | cut -f1)
    echo "[$(date)] ✅ Uploads backup completed: ${BACKUP_FILE} (${SIZE})"

    # Smazání starých záloh
    find "${BACKUP_DIR}" -name "aas_uploads_*.tar.gz" -type f -mtime +${RETENTION_DAYS} -delete
    echo "[$(date)] ✅ Old backups cleaned (>${RETENTION_DAYS} days)"
else
    echo "[$(date)] ❌ Uploads backup FAILED!"
    exit 1
fi
```

Nastavte oprávnění:
```bash
chmod +x scripts/backup-uploads.sh
```

### Krok 4: Nastavení cron jobs

```bash
crontab -e
```

Přidejte:
```bash
# Database backup - každý den ve 2:00
0 2 * * * /home/aas/AAS/scripts/backup-database.sh >> /home/aas/backups/backup.log 2>&1

# Uploads backup - každou neděli ve 3:00
0 3 * * 0 /home/aas/AAS/scripts/backup-uploads.sh >> /home/aas/backups/backup.log 2>&1

# Disk space check - každý den v poledne
0 12 * * * df -h | grep -E "/$|/home" | awk '{if(+$5 > 80) print "⚠️ Disk usage: "$5" on "$6}' | mail -s "Disk Space Alert" admin@aristocraticartworksale.com
```

---

## 3. Manuální zálohy

### Databáze - SQL formát

```bash
cd ~/AAS

# Plain SQL
docker-compose -f docker-compose.production.yml exec -T db \
    pg_dump -U aas aas_prod > backup_manual_$(date +%Y%m%d).sql

# Komprimovaný
docker-compose -f docker-compose.production.yml exec -T db \
    pg_dump -U aas aas_prod | gzip > backup_manual_$(date +%Y%m%d).sql.gz
```

### Databáze - Custom formát (rychlejší restore)

```bash
docker-compose -f docker-compose.production.yml exec -T db \
    pg_dump -U aas -d aas_prod -F c -f /tmp/backup.dump

docker cp aas-db-prod:/tmp/backup.dump ./backup_$(date +%Y%m%d).dump
```

### Uploads

```bash
docker run --rm \
    --volumes-from aas-web-prod \
    -v $(pwd):/backup \
    alpine \
    tar czf /backup/uploads_$(date +%Y%m%d).tar.gz -C /app/wwwroot uploads
```

### Kompletní backup (vše)

```bash
#!/bin/bash
BACKUP_NAME="aas_full_$(date +%Y%m%d_%H%M%S)"
mkdir -p "$BACKUP_NAME"

# Database
docker-compose -f docker-compose.production.yml exec -T db \
    pg_dump -U aas aas_prod | gzip > "$BACKUP_NAME/database.sql.gz"

# Uploads
docker run --rm \
    --volumes-from aas-web-prod \
    -v $(pwd)/$BACKUP_NAME:/backup \
    alpine \
    tar czf /backup/uploads.tar.gz -C /app/wwwroot uploads

# Config
cp .env.production "$BACKUP_NAME/"
cp docker-compose.production.yml "$BACKUP_NAME/"
cp -r nginx "$BACKUP_NAME/"

# Komprimace
tar czf "$BACKUP_NAME.tar.gz" "$BACKUP_NAME"
rm -rf "$BACKUP_NAME"

echo "✅ Full backup created: $BACKUP_NAME.tar.gz"
```

---

## 4. Obnova ze zálohy

### Restore databáze ze SQL zálohy

```bash
# 1. Zastavit aplikaci
docker-compose -f docker-compose.production.yml stop web

# 2. Restore databáze
gunzip < backup_20250109.sql.gz | \
    docker-compose -f docker-compose.production.yml exec -T db \
    psql -U aas -d aas_prod

# 3. Restart aplikace
docker-compose -f docker-compose.production.yml start web
```

### Restore z custom formátu

```bash
docker-compose -f docker-compose.production.yml stop web

docker cp backup_20250109.dump aas-db-prod:/tmp/restore.dump
docker-compose -f docker-compose.production.yml exec db \
    pg_restore -U aas -d aas_prod -c /tmp/restore.dump

docker-compose -f docker-compose.production.yml start web
```

### Restore uploads

```bash
docker run --rm \
    --volumes-from aas-web-prod \
    -v $(pwd):/backup \
    alpine \
    tar xzf /backup/uploads_20250109.tar.gz -C /app/wwwroot
```

### Disaster Recovery - kompletní obnova

```bash
#!/bin/bash
# POZOR: Tento script přepíše všechna současná data!

read -p "⚠️  Tímto přepíšete VŠECHNAdata! Pokračovat? (yes/NO): " confirm
if [ "$confirm" != "yes" ]; then
    echo "Cancelled."
    exit 1
fi

BACKUP_FILE=$1

if [ -z "$BACKUP_FILE" ]; then
    echo "Usage: $0 <backup-file.tar.gz>"
    exit 1
fi

echo "📦 Extrahování zálohy..."
tar xzf "$BACKUP_FILE"
BACKUP_DIR="${BACKUP_FILE%.tar.gz}"

echo "🛑 Zastavení aplikace..."
docker-compose -f docker-compose.production.yml down

echo "💾 Restore databáze..."
gunzip < "$BACKUP_DIR/database.sql.gz" | \
    docker-compose -f docker-compose.production.yml up -d db && \
    sleep 10 && \
    docker-compose -f docker-compose.production.yml exec -T db \
    psql -U aas -d aas_prod

echo "📁 Restore uploads..."
docker run --rm \
    --volumes-from aas-web-prod \
    -v $(pwd)/$BACKUP_DIR:/backup \
    alpine \
    tar xzf /backup/uploads.tar.gz -C /app/wwwroot

echo "🚀 Restart aplikace..."
docker-compose -f docker-compose.production.yml up -d

echo "✅ Restore dokončen!"
```

---

## 5. Off-site Backup

### S3-Compatible Storage (Doporučeno)

#### Instalace AWS CLI

```bash
sudo apt install awscli -y
```

#### Konfigurace

```bash
aws configure
# AWS Access Key ID: [YOUR_KEY]
# AWS Secret Access Key: [YOUR_SECRET]
# Default region name: eu-central-1
# Default output format: json
```

#### Upload do S3

```bash
#!/bin/bash
# scripts/backup-to-s3.sh

BUCKET_NAME="aas-backups"
LOCAL_BACKUP_DIR="/home/aas/backups"

# Sync database backups
aws s3 sync "${LOCAL_BACKUP_DIR}/database" "s3://${BUCKET_NAME}/database/" \
    --storage-class STANDARD_IA \
    --exclude "*" --include "*.sql.gz"

# Sync uploads backups
aws s3 sync "${LOCAL_BACKUP_DIR}/uploads" "s3://${BUCKET_NAME}/uploads/" \
    --storage-class STANDARD_IA \
    --exclude "*" --include "*.tar.gz"

echo "✅ Backups synced to S3"
```

#### Cron job pro S3 sync

```bash
crontab -e
```

Přidejte:
```bash
# Sync to S3 každý den ve 4:00
0 4 * * * /home/aas/AAS/scripts/backup-to-s3.sh >> /home/aas/backups/s3-sync.log 2>&1
```

---

## 6. Monitoring

### Uptime Monitoring

#### UptimeRobot (Zdarma)

1. Registrujte se na https://uptimerobot.com/
2. Přidejte nový monitor:
   - **Type**: HTTPS
   - **URL**: https://aristocraticartworksale.com
   - **Interval**: 5 minutes
   - **Alert Contacts**: Váš email

3. Přidejte další monitor pro health endpoint:
   - **URL**: https://aristocraticartworksale.com/health
   - **Keyword**: "healthy"

### Server Monitoring

#### Instalace Netdata

```bash
bash <(curl -Ss https://my-netdata.io/kickstart.sh)
```

Otevřete v prohlížeči:
```
http://your-server-ip:19999
```

**Zabezpečení Netdata:**

```bash
# Povolte pouze z localhost
sudo nano /etc/netdata/netdata.conf
```

Změňte:
```ini
[web]
    bind to = 127.0.0.1
```

Restart:
```bash
sudo systemctl restart netdata
```

#### SSH tunnel pro přístup

```bash
ssh -L 19999:localhost:19999 aas@your-server-ip
```

Pak otevřete: http://localhost:19999

### Application Performance Monitoring

#### Serilog + Seq (Doporučeno)

1. **Instalace Seq**:

```bash
docker run --name seq -d \
    --restart unless-stopped \
    -e ACCEPT_EULA=Y \
    -v /home/aas/seq-data:/data \
    -p 5341:80 \
    datalust/seq:latest
```

2. **Přidání Seq do docker-compose.production.yml**:

```yaml
  seq:
    image: datalust/seq:latest
    container_name: aas-seq
    restart: unless-stopped
    environment:
      ACCEPT_EULA: "Y"
    volumes:
      - seq-data:/data
    ports:
      - "5341:80"
    networks:
      - aas-network
```

3. **Update aplikace pro log do Seq**:

V `.env.production` přidejte:
```bash
SEQ_SERVER_URL=http://seq:80
SEQ_API_KEY=your-api-key
```

### Docker Monitoring

```bash
# Real-time stats
docker stats

# Container health
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.State}}"

# Logs monitoring
docker-compose -f docker-compose.production.yml logs -f --tail=100
```

### Disk Space Monitoring

```bash
#!/bin/bash
# scripts/check-disk-space.sh

THRESHOLD=80
USAGE=$(df -h / | awk 'NR==2 {print $5}' | sed 's/%//')

if [ "$USAGE" -gt "$THRESHOLD" ]; then
    echo "⚠️ ALERT: Disk usage is ${USAGE}%"

    # Vyčistit Docker
    docker system prune -af --volumes

    # Vyčistit staré logy
    find /var/log -name "*.log" -type f -mtime +30 -delete

    # Send email alert
    echo "Disk usage: ${USAGE}%" | mail -s "Disk Space Alert" admin@aristocraticartworksale.com
fi
```

---

## 7. Alerting

### Email Alerts

#### Instalace MailUtils

```bash
sudo apt install mailutils -y
```

#### Konfigurace SMTP (Gmail)

```bash
sudo nano /etc/postfix/sasl_passwd
```

Přidejte:
```
[smtp.gmail.com]:587 your-email@gmail.com:your-app-password
```

Zabezpečení:
```bash
sudo postmap /etc/postfix/sasl_passwd
sudo chmod 600 /etc/postfix/sasl_passwd.db
sudo rm /etc/postfix/sasl_passwd
```

#### Test email

```bash
echo "Test email from AAS server" | mail -s "Test" admin@aristocraticartworksale.com
```

### Slack Alerts (Doporučeno)

1. Vytvořte Slack Webhook: https://api.slack.com/messaging/webhooks

2. Script pro Slack notifikace:

```bash
#!/bin/bash
# scripts/slack-notify.sh

WEBHOOK_URL="https://hooks.slack.com/services/YOUR/WEBHOOK/URL"
MESSAGE=$1

curl -X POST "$WEBHOOK_URL" \
    -H 'Content-Type: application/json' \
    -d "{\"text\":\"$MESSAGE\"}"
```

3. Použití:

```bash
./scripts/slack-notify.sh "⚠️ Server disk usage > 80%"
```

### Docker Health Alerts

```bash
#!/bin/bash
# scripts/check-docker-health.sh

UNHEALTHY=$(docker ps --filter "health=unhealthy" -q)

if [ ! -z "$UNHEALTHY" ]; then
    CONTAINERS=$(docker ps --filter "health=unhealthy" --format "{{.Names}}")
    echo "⚠️ Unhealthy containers: $CONTAINERS"

    # Send alert
    ./scripts/slack-notify.sh "🚨 Unhealthy containers detected: $CONTAINERS"

    # Auto-restart
    docker restart $UNHEALTHY
fi
```

Cron job:
```bash
*/5 * * * * /home/aas/AAS/scripts/check-docker-health.sh >> /var/log/docker-health.log 2>&1
```

---

## 📊 Monitoring Dashboard

### Vytvoření jednoduchého monitoring scriptu

```bash
#!/bin/bash
# scripts/status-dashboard.sh

clear
echo "=========================================="
echo "  AAS Production Status Dashboard"
echo "=========================================="
echo ""

# Docker Containers
echo "📦 Docker Containers:"
docker-compose -f docker-compose.production.yml ps
echo ""

# System Resources
echo "💻 System Resources:"
echo "CPU: $(top -bn1 | grep "Cpu(s)" | sed "s/.*, *\([0-9.]*\)%* id.*/\1/" | awk '{print 100 - $1"%"}')"
echo "RAM: $(free -h | awk 'NR==2 {print $3 "/" $2 " (" int($3/$2*100) "%)"}')"
echo "Disk: $(df -h / | awk 'NR==2 {print $3 "/" $2 " (" $5 ")"}')"
echo ""

# Database
echo "🗄️  Database:"
DB_SIZE=$(docker-compose -f docker-compose.production.yml exec -T db \
    psql -U aas -d aas_prod -t -c "SELECT pg_size_pretty(pg_database_size('aas_prod'));" | tr -d ' ')
echo "Size: $DB_SIZE"
echo ""

# Latest Backup
echo "💾 Latest Backups:"
echo "Database: $(ls -t ~/backups/database/*.sql.gz 2>/dev/null | head -1 | xargs basename)"
echo "Uploads: $(ls -t ~/backups/uploads/*.tar.gz 2>/dev/null | head -1 | xargs basename)"
echo ""

# SSL Certificate
echo "🔒 SSL Certificate:"
echo "Expires: $(echo | openssl s_client -connect aristocraticartworksale.com:443 2>/dev/null | openssl x509 -noout -enddate | cut -d= -f2)"
echo ""

echo "=========================================="
```

Použití:
```bash
chmod +x scripts/status-dashboard.sh
./scripts/status-dashboard.sh
```

---

## 📞 Emergency Contacts

V případě problémů:

| Co | Kontakt |
|----|---------|
| Server down | hosting-support@provider.com |
| Database issues | dba@company.com |
| Security incident | security@company.com |
| Admin | admin@aristocraticartworksale.com |

---

*Dokument vytvořen: 2025-01-09*
*Verze: 1.0*
